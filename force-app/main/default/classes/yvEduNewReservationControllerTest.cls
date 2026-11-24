@IsTest
private class yvEduNewReservationControllerTest {
    private static final String AUDIENCE = 'Elementary';
    private static final String OTHER_AUDIENCE = 'High_School';

    private class Fixture {
        Id standardBook = Test.getStandardPricebookId();
        Bundle__c bundle;
        Bundle_Item__c guideItem;
        Bundle_Item__c lectureItem;
    }

    /** One active bundle for AUDIENCE with two items; the guide has a bundle price (108) in the standard price book. */
    private static Fixture createFixture() {
        Fixture fixture = new Fixture();
        Product2 guide = new Product2(Name = 'Guide', IsActive = true, DurationOfHours__c = 3);
        Product2 lecture = new Product2(Name = 'Lecture', IsActive = true, DurationOfHours__c = 0);
        insert new List<Product2>{ guide, lecture };
        insert new List<PricebookEntry>{
            new PricebookEntry(Pricebook2Id = fixture.standardBook, Product2Id = guide.Id, UnitPrice = 120, IsActive = true),
            new PricebookEntry(Pricebook2Id = fixture.standardBook, Product2Id = lecture.Id, UnitPrice = 50, IsActive = true)
        };
        fixture.bundle = new Bundle__c(Name = 'Bundle', Active_Bundle__c = true, Audience_Category__c = AUDIENCE);
        insert fixture.bundle;
        fixture.guideItem = new Bundle_Item__c(Name = 'Guide', Bundle__c = fixture.bundle.Id, Product__c = guide.Id, Quantity__c = 1, Active__c = true, Audience_Category__c = AUDIENCE);
        fixture.lectureItem = new Bundle_Item__c(Name = 'Lecture', Bundle__c = fixture.bundle.Id, Product__c = lecture.Id, Quantity__c = 2, Active__c = true, Audience_Category__c = AUDIENCE);
        insert new List<Bundle_Item__c>{ fixture.guideItem, fixture.lectureItem };
        insert new Bundle_Item_Entry__c(Bundle_Item__c = fixture.guideItem.Id, Price_Book__c = fixture.standardBook, Price__c = 108);
        return fixture;
    }

    private static yvEduNewReservationController.GroupInput group(String name, Integer participants) {
        yvEduNewReservationController.GroupInput groupInput = new yvEduNewReservationController.GroupInput();
        groupInput.name = name;
        groupInput.participants = participants;
        return groupInput;
    }

    private static yvEduNewReservationController.BundleChoice choice(Id bundleId, List<Id> itemIds) {
        yvEduNewReservationController.BundleChoice choice = new yvEduNewReservationController.BundleChoice();
        choice.bundleId = bundleId;
        choice.bundleItemIds = itemIds;
        return choice;
    }

    private static yvEduNewReservationController.OrderRequest request(Fixture fixture, List<Id> itemIds, List<yvEduNewReservationController.GroupInput> groups) {
        yvEduNewReservationController.OrderRequest request = new yvEduNewReservationController.OrderRequest();
        request.audienceCategory = AUDIENCE;
        request.bundles = new List<yvEduNewReservationController.BundleChoice>{ choice(fixture.bundle.Id, itemIds) };
        request.groups = groups;
        request.visitDate = Date.today().addDays(14);
        request.visitTime = '10:00';
        return request;
    }

    @IsTest
    static void getAudienceCategories_returnsActiveValuesWithLabels() {
        Test.startTest();
        List<yvEduNewReservationController.Option> options = yvEduNewReservationController.getAudienceCategories();
        Test.stopTest();

        System.assert(!options.isEmpty(), 'The picklist has active values');
        Set<String> values = new Set<String>();
        for (yvEduNewReservationController.Option option : options) {
            System.assert(String.isNotBlank(option.label));
            values.add(option.value);
        }
        System.assert(values.contains(AUDIENCE));
    }

    @IsTest
    static void getActivities_returnsOnlyActiveBundlesAndItemsOfTheChosenAudience() {
        Fixture fixture = createFixture();
        insert new List<Bundle__c>{
            new Bundle__c(Name = 'Inactive bundle', Active_Bundle__c = false, Audience_Category__c = AUDIENCE),
            new Bundle__c(Name = 'Other audience', Active_Bundle__c = true, Audience_Category__c = OTHER_AUDIENCE)
        };
        insert new List<Bundle_Item__c>{
            new Bundle_Item__c(Name = 'Dropped item', Bundle__c = fixture.bundle.Id, Product__c = fixture.guideItem.Product__c, Quantity__c = 1, Active__c = false, Audience_Category__c = AUDIENCE),
            new Bundle_Item__c(Name = 'Wrong audience item', Bundle__c = fixture.bundle.Id, Product__c = fixture.guideItem.Product__c, Quantity__c = 1, Active__c = true, Audience_Category__c = OTHER_AUDIENCE)
        };

        Test.startTest();
        List<yvEduNewReservationController.ActivityCard> cards = yvEduNewReservationController.getActivities(AUDIENCE);
        Test.stopTest();

        System.assertEquals(1, cards.size(), 'Only the active bundle of this audience should be returned');
        System.assertEquals(fixture.bundle.Id, cards[0].id);
        System.assertEquals(2, cards[0].items.size(), 'Inactive items and items of another audience must not be listed');
        System.assertEquals(3, cards[0].totalHours, 'Only the guide has a duration (3 hours)');
    }

    @IsTest
    static void getActivities_returnsNothingWithoutAnAudience() {
        System.assertEquals(0, yvEduNewReservationController.getActivities(null).size());
    }

    @IsTest
    static void getActivities_showsZeroHoursWhenTheDurationIsZero() {
        Product2 product = new Product2(Name = 'No duration product', IsActive = true, DurationOfHours__c = 0);
        insert product;
        Bundle__c bundle = new Bundle__c(Name = 'Bare bundle', Active_Bundle__c = true, Audience_Category__c = AUDIENCE);
        insert bundle;
        insert new Bundle_Item__c(Name = 'Item', Bundle__c = bundle.Id, Product__c = product.Id, Quantity__c = 1, Active__c = true, Audience_Category__c = AUDIENCE);

        Test.startTest();
        List<yvEduNewReservationController.ActivityCard> cards = yvEduNewReservationController.getActivities(AUDIENCE);
        Test.stopTest();

        System.assertEquals(1, cards.size());
        System.assertEquals(0, cards[0].totalHours, 'DurationOfHours__c is required on Product2 now, so a product without time has 0 hours');
    }

    @IsTest
    static void submitOrder_createsOpportunityPricedLinesAndGroupsInOneGo() {
        Fixture fixture = createFixture();
        yvEduNewReservationController.pricebookOverride = fixture.standardBook;
        yvEduNewReservationController.skipConnectedBundle = true;
        yvEduNewReservationController.OrderRequest request = request(
            fixture,
            new List<Id>{ fixture.guideItem.Id, fixture.lectureItem.Id },
            new List<yvEduNewReservationController.GroupInput>{ group('Class A', 30), group('Class B', 25) }
        );

        Test.startTest();
        yvEduNewReservationController.SaveResult result = yvEduNewReservationController.placeOrder(request);
        Test.stopTest();

        System.assertEquals(null, result.error, 'No error expected: ' + result.error);
        Opportunity order = [
            SELECT Name, StageName, Audience_Category__c, Pricebook2Id, NumberOfGroups__c, NumberOfParticipants__c
            FROM Opportunity
            WHERE Id = :result.opportunityId
        ];
        System.assertEquals('New', order.StageName);
        System.assert(order.Name.startsWith('Online order - '), 'Named after the customer and the date: ' + order.Name);
        System.assert(!order.Name.contains('Bundle'), 'The bundles are not part of the order name');
        System.assertEquals(AUDIENCE, order.Audience_Category__c);
        System.assertEquals(fixture.standardBook, order.Pricebook2Id);
        System.assertEquals(2, order.NumberOfGroups__c);
        System.assertEquals(55, order.NumberOfParticipants__c);
        Map<String, B25__Group__c> groupByName = new Map<String, B25__Group__c>();
        for (B25__Group__c savedGroup : [SELECT Name, Number_Participants__c FROM B25__Group__c WHERE Opportunity__c = :order.Id]) {
            groupByName.put(savedGroup.Name, savedGroup);
        }
        System.assertEquals(2, groupByName.size());
        System.assertEquals(30, groupByName.get('Class A').Number_Participants__c, 'The participants of each class are saved on its group');
        System.assertEquals(25, groupByName.get('Class B').Number_Participants__c);
        Map<Id, OpportunityLineItem> lineByItem = new Map<Id, OpportunityLineItem>();
        for (OpportunityLineItem line : [SELECT Bundle_Item__c, UnitPrice, Quantity FROM OpportunityLineItem WHERE OpportunityId = :order.Id]) {
            lineByItem.put(line.Bundle_Item__c, line);
        }
        System.assertEquals(2, lineByItem.size());
        System.assertEquals(108, lineByItem.get(fixture.guideItem.Id).UnitPrice, 'Bundle price wins over the price book entry');
        System.assertEquals(50, lineByItem.get(fixture.lectureItem.Id).UnitPrice, 'No bundle price: the price book entry');
        System.assertEquals(2, lineByItem.get(fixture.lectureItem.Id).Quantity);
        System.assertEquals(0, [SELECT COUNT() FROM Connected_Bundle__c WHERE Opportunity__c = :order.Id], 'The Connected Bundle is not created from the site');
    }

    @IsTest
    static void activityDaySubject_isListedAndSavedOnTheOrderWhenChosen() {
        Fixture fixture = createFixture();
        yvEduNewReservationController.pricebookOverride = fixture.standardBook;
        yvEduNewReservationController.skipConnectedBundle = true;
        Activity_Day_Subject__c subject = new Activity_Day_Subject__c(Name = 'Subject A');
        insert subject;

        List<yvEduNewReservationController.Option> options = yvEduNewReservationController.getActivityDaySubjects();
        System.assertEquals(1, options.size());
        System.assertEquals(subject.Id, options[0].value);
        System.assertEquals('Subject A', options[0].label);

        yvEduNewReservationController.OrderRequest request = request(
            fixture,
            new List<Id>{ fixture.guideItem.Id },
            new List<yvEduNewReservationController.GroupInput>{ group('Class A', 30) }
        );
        request.activityDaySubjectId = subject.Id;
        yvEduNewReservationController.SaveResult result = yvEduNewReservationController.placeOrder(request);

        System.assertEquals(null, result.error, 'No error expected: ' + result.error);
        System.assertEquals(subject.Id, [SELECT Activity_Day_Subject__c FROM Opportunity WHERE Id = :result.opportunityId].Activity_Day_Subject__c);
    }

    @IsTest
    static void submitOrder_savesNothingWhenValidationFails() {
        Fixture fixture = createFixture();
        yvEduNewReservationController.pricebookOverride = fixture.standardBook;
        yvEduNewReservationController.skipConnectedBundle = true;
        List<Id> items = new List<Id>{ fixture.guideItem.Id };

        yvEduNewReservationController.SaveResult tooMany = yvEduNewReservationController.placeOrder(
            request(fixture, items, new List<yvEduNewReservationController.GroupInput>{ group('Class A', 36) })
        );
        yvEduNewReservationController.SaveResult noName = yvEduNewReservationController.placeOrder(
            request(fixture, items, new List<yvEduNewReservationController.GroupInput>{ group(' ', 10) })
        );
        yvEduNewReservationController.SaveResult noItems = yvEduNewReservationController.placeOrder(
            request(fixture, new List<Id>(), new List<yvEduNewReservationController.GroupInput>{ group('Class A', 10) })
        );

        System.assertNotEquals(null, tooMany.error);
        System.assertNotEquals(null, noName.error);
        System.assertNotEquals(null, noItems.error);
        System.assertEquals(0, [SELECT COUNT() FROM Opportunity], 'Nothing is saved when a check fails');
    }

    @IsTest
    static void submitOrder_rejectsItemsOfAnotherBundleAndMissingPriceBook() {
        Fixture fixture = createFixture();
        Bundle__c other = new Bundle__c(Name = 'Other', Active_Bundle__c = true, Audience_Category__c = AUDIENCE);
        insert other;
        Bundle_Item__c foreign = new Bundle_Item__c(Name = 'Foreign', Bundle__c = other.Id, Product__c = fixture.guideItem.Product__c, Quantity__c = 1, Active__c = true, Audience_Category__c = AUDIENCE);
        insert foreign;
        List<yvEduNewReservationController.GroupInput> groups = new List<yvEduNewReservationController.GroupInput>{ group('Class A', 10) };

        yvEduNewReservationController.pricebookOverride = fixture.standardBook;
        yvEduNewReservationController.SaveResult foreignItem = yvEduNewReservationController.placeOrder(
            request(fixture, new List<Id>{ foreign.Id }, groups)
        );
        yvEduNewReservationController.pricebookOverride = null;
        yvEduNewReservationController.SaveResult noPricebook = yvEduNewReservationController.placeOrder(
            request(fixture, new List<Id>{ fixture.guideItem.Id }, groups)
        );

        System.assertNotEquals(null, foreignItem.error, 'An item of another bundle is refused');
        System.assertNotEquals(null, noPricebook.error, 'No price book, no order');
        System.assertEquals(0, [SELECT COUNT() FROM Opportunity]);
    }

    @IsTest
    static void getAvailableDates_marksPastAndBeyondHorizonDaysAsUnavailable() {
        Fixture fixture = createFixture();
        List<Id> items = new List<Id>{ fixture.guideItem.Id };
        Date today = Date.today();

        yvEduNewReservationController.DatesResult thisMonth = yvEduNewReservationController.getAvailableDates(items, today.year(), today.month(), 10);
        Date farAway = today.addMonths(14);
        yvEduNewReservationController.DatesResult beyond = yvEduNewReservationController.getAvailableDates(items, farAway.year(), farAway.month(), 10);

        System.assertEquals(null, thisMonth.error, 'No error expected: ' + thisMonth.error);
        System.assertEquals(Date.daysInMonth(today.year(), today.month()), thisMonth.days.size());
        for (yvEduNewReservationController.DayInfo info : thisMonth.days) {
            if (info.day < today) {
                System.assertEquals(false, info.available, 'Past days are never available');
            } else if (info.day > today) {
                System.assertEquals(true, info.available, 'Items without a GoMeddo resource do not limit the date');
            }
            // today is not asserted: with no museum a day is offered only while a start time is left, so late in the day it closes
        }
        for (yvEduNewReservationController.DayInfo info : beyond.days) {
            System.assertEquals(false, info.available, 'Nothing is offered beyond twelve months');
        }
    }

    @IsTest
    static void submitOrder_rejectsMissingOrPastVisitDate() {
        Fixture fixture = createFixture();
        yvEduNewReservationController.pricebookOverride = fixture.standardBook;
        List<yvEduNewReservationController.GroupInput> groups = new List<yvEduNewReservationController.GroupInput>{ group('Class A', 10) };
        yvEduNewReservationController.OrderRequest noDate = request(fixture, new List<Id>{ fixture.guideItem.Id }, groups);
        noDate.visitDate = null;
        yvEduNewReservationController.OrderRequest pastDate = request(fixture, new List<Id>{ fixture.guideItem.Id }, groups);
        pastDate.visitDate = Date.today().addDays(-1);

        System.assertNotEquals(null, yvEduNewReservationController.placeOrder(noDate).error);
        System.assertNotEquals(null, yvEduNewReservationController.placeOrder(pastDate).error);
        System.assertEquals(0, [SELECT COUNT() FROM Opportunity]);
    }

    @IsTest
    static void getAvailableTimes_offersDefaultStartTimesWhenItemsHaveNoResource() {
        Fixture fixture = createFixture();
        Date day = Date.today().addDays(14);

        yvEduNewReservationController.TimesResult result = yvEduNewReservationController.getAvailableTimes(new List<Id>{ fixture.guideItem.Id }, day, 10);

        System.assertEquals(null, result.error, 'No error expected: ' + result.error);
        System.assertEquals('08:00', result.times[0]);
        System.assertEquals('17:00', result.times[result.times.size() - 1]);
        System.assert(result.times.contains('10:30'));
    }

    @IsTest
    static void getAvailableTimes_rejectsPastDates() {
        Fixture fixture = createFixture();
        yvEduNewReservationController.TimesResult result = yvEduNewReservationController.getAvailableTimes(new List<Id>{ fixture.guideItem.Id }, Date.today().addDays(-1), 10);
        System.assertNotEquals(null, result.error);
        System.assertEquals(0, result.times.size());
    }

    @IsTest
    static void submitOrder_acceptsTheOrderAsJsonText() {
        Fixture fixture = createFixture();
        yvEduNewReservationController.pricebookOverride = fixture.standardBook;
        yvEduNewReservationController.skipConnectedBundle = true;
        yvEduNewReservationController.OrderRequest order = request(
            fixture,
            new List<Id>{ fixture.guideItem.Id },
            new List<yvEduNewReservationController.GroupInput>{ group('Class A', 10) }
        );

        yvEduNewReservationController.SaveResult saved = yvEduNewReservationController.submitOrder(JSON.serialize(order));
        yvEduNewReservationController.SaveResult unreadable = yvEduNewReservationController.submitOrder('not json');

        System.assertEquals(null, saved.error, 'No error expected: ' + saved.error);
        System.assertNotEquals(null, saved.opportunityId);
        System.assertNotEquals(null, unreadable.error);
    }

    @IsTest
    static void submitOrder_savesTheVisitStartAndRejectsATimeThatIsNotOffered() {
        Fixture fixture = createFixture();
        yvEduNewReservationController.pricebookOverride = fixture.standardBook;
        yvEduNewReservationController.skipConnectedBundle = true;
        List<yvEduNewReservationController.GroupInput> groups = new List<yvEduNewReservationController.GroupInput>{ group('Class A', 10) };
        yvEduNewReservationController.OrderRequest good = request(fixture, new List<Id>{ fixture.guideItem.Id }, groups);
        yvEduNewReservationController.OrderRequest bad = request(fixture, new List<Id>{ fixture.guideItem.Id }, groups);
        bad.visitTime = '03:00';

        yvEduNewReservationController.SaveResult refused = yvEduNewReservationController.placeOrder(bad);
        System.assertNotEquals(null, refused.error, 'A time that is not offered is refused');
        System.assertEquals(0, [SELECT COUNT() FROM Opportunity]);

        yvEduNewReservationController.SaveResult saved = yvEduNewReservationController.placeOrder(good);
        System.assertEquals(null, saved.error, 'No error expected: ' + saved.error);
        Opportunity order = [SELECT Visit_Start_Date__c FROM Opportunity WHERE Id = :saved.opportunityId];
        System.assertEquals(good.visitDate, order.Visit_Start_Date__c.date());
    }

    @IsTest
    static void submitOrder_acceptsSeveralDifferentBundlesAndRefusesTheSameBundleTwice() {
        Fixture fixture = createFixture();
        yvEduNewReservationController.pricebookOverride = fixture.standardBook;
        yvEduNewReservationController.skipConnectedBundle = true;
        Product2 extraProduct = new Product2(Name = 'Extra', IsActive = true, DurationOfHours__c = 0);
        insert extraProduct;
        insert new PricebookEntry(Pricebook2Id = fixture.standardBook, Product2Id = extraProduct.Id, UnitPrice = 70, IsActive = true);
        Bundle__c second = new Bundle__c(Name = 'Second bundle', Active_Bundle__c = true, Audience_Category__c = AUDIENCE);
        insert second;
        Bundle_Item__c secondItem = new Bundle_Item__c(Name = 'Extra', Bundle__c = second.Id, Product__c = extraProduct.Id, Quantity__c = 1, Active__c = true, Audience_Category__c = AUDIENCE);
        insert secondItem;
        List<yvEduNewReservationController.GroupInput> groups = new List<yvEduNewReservationController.GroupInput>{ group('Class A', 10) };

        yvEduNewReservationController.OrderRequest two = request(fixture, new List<Id>{ fixture.guideItem.Id }, groups);
        two.bundles.add(choice(second.Id, new List<Id>{ secondItem.Id }));
        yvEduNewReservationController.SaveResult saved = yvEduNewReservationController.placeOrder(two);

        System.assertEquals(null, saved.error, 'No error expected: ' + saved.error);
        System.assertEquals(2, [SELECT COUNT() FROM OpportunityLineItem WHERE OpportunityId = :saved.opportunityId], 'One line per chosen item of both bundles');

        yvEduNewReservationController.OrderRequest twice = request(fixture, new List<Id>{ fixture.guideItem.Id }, groups);
        twice.bundles.add(choice(fixture.bundle.Id, new List<Id>{ fixture.lectureItem.Id }));
        System.assertNotEquals(null, yvEduNewReservationController.placeOrder(twice).error, 'The same bundle twice is refused');

        yvEduNewReservationController.OrderRequest crossed = request(fixture, new List<Id>{ secondItem.Id }, groups);
        System.assertNotEquals(null, yvEduNewReservationController.placeOrder(crossed).error, 'An item of another bundle is refused');
    }

    @IsTest
    static void audiencesOf_readsTheSemicolonSeparatedAudiencesOfAnAccount() {
        System.assertEquals(0, yvEduNewReservationController.audiencesOf(null).size(), 'No account: no restriction');
        System.assertEquals(0, yvEduNewReservationController.audiencesOf(new Account(Name = 'A')).size(), 'No audience set: no restriction');
        Set<String> two = yvEduNewReservationController.audiencesOf(new Account(Name = 'A', Audience_Category__c = 'Elementary;High_School'));
        System.assertEquals(2, two.size());
        System.assert(two.contains('Elementary') && two.contains('High_School'));
    }

    private static yvEduNewReservationController.GroupInput museumGroup(Integer participants, String entryTime) {
        yvEduNewReservationController.GroupInput groupInput = new yvEduNewReservationController.GroupInput();
        groupInput.name = 'Class';
        groupInput.participants = participants;
        groupInput.museumTime = entryTime;
        return groupInput;
    }

    private static List<yvEduNewReservationController.MuseumSlot> museumPieces(List<String> times, Integer free) {
        List<yvEduNewReservationController.MuseumSlot> slots = new List<yvEduNewReservationController.MuseumSlot>();
        for (String slotTime : times) {
            yvEduNewReservationController.MuseumSlot slot = new yvEduNewReservationController.MuseumSlot();
            slot.startTime = slotTime;
            slot.free = free;
            slots.add(slot);
        }
        return slots;
    }

    @IsTest
    static void checkMuseumEntries_countsOnlyTheClassesEnteringInTheSamePiece() {
        // 100 seats in every 10-minute piece from 08:00 to 08:50 (the stay of 120 minutes does not take seats of later pieces)
        List<yvEduNewReservationController.MuseumSlot> slots = museumPieces(
            new List<String>{ '08:00', '08:10', '08:20', '08:30', '08:40', '08:50' },
            100
        );
        yvEduNewReservationController.MuseumTiming timing = new yvEduNewReservationController.MuseumTiming(10, 120);

        // two classes of 60 at different pieces: each piece has room for its own class
        yvEduNewReservationController.checkMuseumEntries(slots, new List<yvEduNewReservationController.GroupInput>{ museumGroup(60, '08:00'), museumGroup(60, '08:10') }, '08:00', '08:50', timing);

        try {
            // 60 + 60 entering in the same piece is more than 100
            yvEduNewReservationController.checkMuseumEntries(slots, new List<yvEduNewReservationController.GroupInput>{ museumGroup(60, '08:00'), museumGroup(60, '08:00') }, '08:00', '08:50', timing);
            System.assert(false, 'Classes entering together that do not fit are refused');
        } catch (Exception e) {
            System.assert(e.getMessage().contains('no room'));
        }
        try {
            // 09:00 is not an open piece
            yvEduNewReservationController.checkMuseumEntries(slots, new List<yvEduNewReservationController.GroupInput>{ museumGroup(10, '09:00') }, '08:00', '09:00', timing);
            System.assert(false, 'An entry outside the open pieces is refused');
        } catch (Exception e) {
            System.assert(e.getMessage().contains('not open'));
        }
        try {
            // 08:40 is open, but the last entry of this visit is 08:30
            yvEduNewReservationController.checkMuseumEntries(slots, new List<yvEduNewReservationController.GroupInput>{ museumGroup(10, '08:40') }, '08:00', '08:30', timing);
            System.assert(false, 'An entry after the last entry time is refused');
        } catch (Exception e) {
            System.assert(e.getMessage().contains('between'));
        }
        try {
            yvEduNewReservationController.checkMuseumEntries(slots, new List<yvEduNewReservationController.GroupInput>{ museumGroup(10, null) }, '08:00', '08:50', timing);
            System.assert(false, 'A class without an entry time is refused');
        } catch (Exception e) {
            System.assert(e.getMessage().contains('museum'));
        }
    }

    private static yvEduNewReservationController.GroupInput arrivingGroup(Integer participants, String arrival) {
        yvEduNewReservationController.GroupInput groupInput = new yvEduNewReservationController.GroupInput();
        groupInput.name = 'Class';
        groupInput.participants = participants;
        groupInput.arrivalTime = arrival;
        return groupInput;
    }

    @IsTest
    static void assignEntries_givesEachClassTheFirstEntryWithRoomAtLeastTheGapAfterItsArrival() {
        List<yvEduNewReservationController.MuseumSlot> slots = museumPieces(
            new List<String>{ '08:00', '08:10', '08:20', '08:30', '08:40', '08:50', '09:00' },
            100
        );
        yvEduNewReservationController.MuseumTiming timing = new yvEduNewReservationController.MuseumTiming(10, 120);

        // arrival 08:00 + the gap of 30 minutes: 08:30; the second class does not fit with the first (60 + 60 > 100), so it enters 10 minutes later
        List<String> entries = yvEduNewReservationController.assignEntries(
            slots,
            new List<yvEduNewReservationController.GroupInput>{ arrivingGroup(60, '08:00'), arrivingGroup(60, '08:00'), arrivingGroup(30, '08:00') },
            0,
            timing
        );
        System.assertEquals(new List<String>{ '08:30', '08:40', '08:30' }, entries, 'The third class still fits in the first piece (60 + 30 <= 100)');

        // a programme before the museum longer than the gap pushes the entry: 45 minutes of programme from 08:00 means no entry before 08:45
        List<String> late = yvEduNewReservationController.assignEntries(
            slots,
            new List<yvEduNewReservationController.GroupInput>{ arrivingGroup(10, '08:00') },
            45,
            timing
        );
        System.assertEquals(new List<String>{ '08:50' }, late);
    }

    @IsTest
    static void assignEntries_refusesAnArrivalWithoutAnEntryAndAMissingArrival() {
        List<yvEduNewReservationController.MuseumSlot> slots = museumPieces(new List<String>{ '08:00', '08:10', '08:20' }, 100);
        yvEduNewReservationController.MuseumTiming timing = new yvEduNewReservationController.MuseumTiming(10, 120);

        try {
            yvEduNewReservationController.assignEntries(slots, new List<yvEduNewReservationController.GroupInput>{ arrivingGroup(10, '08:00') }, 0, timing);
            System.assert(false, 'The first piece is 30 minutes after the arrival: 08:30 does not exist here');
        } catch (Exception e) {
            System.assert(e.getMessage().contains('no entry'), e.getMessage());
        }
        try {
            yvEduNewReservationController.assignEntries(slots, new List<yvEduNewReservationController.GroupInput>{ arrivingGroup(10, null) }, 0, timing);
            System.assert(false, 'A class without an arrival time is refused');
        } catch (Exception e) {
            System.assert(e.getMessage().contains('arrival'), e.getMessage());
        }
        try {
            // a full piece is skipped: 100 taken by the first class, so the second class goes to the next piece, and when there is none it is refused
            List<yvEduNewReservationController.MuseumSlot> oneSlot = museumPieces(new List<String>{ '08:30' }, 100);
            yvEduNewReservationController.assignEntries(
                oneSlot,
                new List<yvEduNewReservationController.GroupInput>{ arrivingGroup(35, '08:00'), arrivingGroup(35, '08:00'), arrivingGroup(35, '08:00') },
                0,
                timing
            );
            System.assert(false, 'Three classes of 35 do not fit one piece of 100');
        } catch (Exception e) {
            System.assert(e.getMessage().contains('no entry'), e.getMessage());
        }
    }

    @IsTest
    static void submitOrder_savesTheVisitEndFromTheProgrammeMinutes() {
        Fixture fixture = createFixture();
        yvEduNewReservationController.pricebookOverride = fixture.standardBook;
        yvEduNewReservationController.skipConnectedBundle = true;
        // the guide takes 3 hours, the lecture has a duration of 0: the programme of a class is 180 minutes; two classes start together
        yvEduNewReservationController.OrderRequest order = request(
            fixture,
            new List<Id>{ fixture.guideItem.Id, fixture.lectureItem.Id },
            new List<yvEduNewReservationController.GroupInput>{ group('Class A', 10), group('Class B', 12) }
        );

        yvEduNewReservationController.SaveResult saved = yvEduNewReservationController.placeOrder(order);

        System.assertEquals(null, saved.error, 'No error expected: ' + saved.error);
        System.assertEquals(0, saved.museumEntries.size(), 'No museum, no entries');
        Opportunity created = [SELECT Visit_Start_Date__c, Visit_End_Date__c FROM Opportunity WHERE Id = :saved.opportunityId];
        Long minutesApart = (created.Visit_End_Date__c.getTime() - created.Visit_Start_Date__c.getTime()) / 60000;
        System.assertEquals(180, minutesApart.intValue(), 'Start and end are 3 hours apart');
    }

    @IsTest
    static void submitOrder_leavesTheVisitEndEmptyWhenAllDurationsAreZero() {
        Fixture fixture = createFixture();
        yvEduNewReservationController.pricebookOverride = fixture.standardBook;
        yvEduNewReservationController.skipConnectedBundle = true;

        yvEduNewReservationController.SaveResult saved = yvEduNewReservationController.placeOrder(
            request(fixture, new List<Id>{ fixture.lectureItem.Id }, new List<yvEduNewReservationController.GroupInput>{ group('Class A', 10) })
        );

        System.assertEquals(null, saved.error, 'No error expected: ' + saved.error);
        System.assertEquals(null, [SELECT Visit_End_Date__c FROM Opportunity WHERE Id = :saved.opportunityId].Visit_End_Date__c);
    }

    @IsTest
    static void submitOrder_savesTheInstructionLanguageAndRefusesAValueThatIsNotInThePicklist() {
        Fixture fixture = createFixture();
        yvEduNewReservationController.pricebookOverride = fixture.standardBook;
        yvEduNewReservationController.skipConnectedBundle = true;
        yvEduNewReservationController.OrderRequest bad = request(
            fixture,
            new List<Id>{ fixture.guideItem.Id },
            new List<yvEduNewReservationController.GroupInput>{ group('Class A', 30) }
        );
        bad.instructionLanguage = 'Klingon';
        yvEduNewReservationController.OrderRequest good = request(
            fixture,
            new List<Id>{ fixture.guideItem.Id },
            new List<yvEduNewReservationController.GroupInput>{ group('Class A', 30) }
        );
        good.instructionLanguage = 'English';

        Test.startTest();
        yvEduNewReservationController.SaveResult refused = yvEduNewReservationController.placeOrder(bad);
        yvEduNewReservationController.SaveResult saved = yvEduNewReservationController.placeOrder(good);
        Test.stopTest();

        System.assertNotEquals(null, refused.error, 'A value that is not in the picklist is refused');
        System.assertEquals(null, saved.error, 'No error expected: ' + saved.error);
        System.assertEquals('English', [SELECT InstructionLanguage__c FROM Opportunity WHERE Id = :saved.opportunityId].InstructionLanguage__c);
    }
}