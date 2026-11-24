/**
 * Feeds the New Reservation page.
 *  - Step 1: the customer picks an Audience Category (Opportunity.Audience_Category__c values), then a
 *    bundle. Only bundles whose Audience_Category__c includes the chosen value are returned, and only their
 *    active Bundle_Item__c rows that include it as well.
 *  - Every value on a card is derived from existing data - no card-specific fields exist:
 *    duration = sum of Product2.DurationOfHours__c x item quantity. Prices are not part of this step.
 *  - Step "when": getAvailableDates asks GoMeddo (B25.AvailableTimeRanges) which days all the resources of the chosen
 *    items are free, and returns only free/busy per day.
 *  - getAvailableTimes then offers the start times of the chosen day: every resource must be free for the time its
 *    own items take (Product2.DurationOfHours__c x quantity), starting at that time.
 *  - Nothing is saved while the customer goes through the steps. submitOrder creates the whole order at the
 *    end, in one transaction (all or nothing), and re-validates everything the page sent.
 * Runs with sharing and in user mode, so the portal user's own object/field/record access applies.
 */
public with sharing class yvEduNewReservationController {
    private static final String ORDER_STAGE = 'New';
    private static final Integer OPPORTUNITY_NAME_LENGTH = 120;
    private static final Integer CONNECTED_BUNDLE_NAME_LENGTH = 80;
    // Same values as the flow Connected_Bundle_Create_Opportunity_Products
    private static final String WAITING_FOR_APPROVAL_STAGE = 'Waiting for Approval';
    private static final String RESERVATION_TYPE_NAME = 'Reservation';
    private static final String RESERVATION_STATUS_NAME = 'מחכה לאישור';
    /** The museum is checked on the spot (capacity, daily limit, opening hours), so its reservations are approved at once. */
    private static final String MUSEUM_RESERVATION_STATUS_NAME = 'אושרה';
    /** TODO-CONFIG (custom metadata): a programme longer than this many minutes is a long visit: the class chooses when to arrive and the system picks the museum entry (DD A02-04). */
    private static final Integer LONG_VISIT_MINUTES = 180;
    /** TODO-CONFIG (custom metadata): in a long visit the museum entry is at least this many minutes after the arrival (DD A02-04). */
    private static final Integer ARRIVAL_GAP_MINUTES = 30;
    /** GoMeddo resource type of the museum. Its resource is not booked whole: every class holds part of the capacity for one entry slot. */
    private static final String MUSEUM_RESOURCE_TYPE = 'Museum';

    /**
     * One step of the programme of a class: a bundle item with the minutes it takes (duration x quantity) and what it needs: a
     * specific resource (resourceId), any resource of a type (typeId: a classroom ...), or nothing (a gathering). All the museum
     * items of the order are one step, flagged museum: its time is the entry the class chose, the other steps are laid out around it.
     */
    private class ProgrammeStep {
        public Id resourceId;
        public Id typeId;
        public Integer minutes = 0;
        public Boolean museum = false;
    }

    /**
     * How the entries of the museum work, read from the museum resource (no defaults in the code): an entry time is offered
     * every Entry_Step_Minutes__c (08:00, 08:10 ... for 10) and holds its place for Entry_Hold_Minutes__c (an entry at 08:10
     * holds 08:10 to 08:40 for 30). Capacity is counted per piece of one step; an entry holds every piece it touches, so the hold
     * can be shorter or longer than the step and need not divide by it.
     */
    @TestVisible
    private class MuseumTiming {
        public Integer step;
        public Integer hold;
        /** How long the class stays: the duration of the museum products of the order. An entry must fit in the opening hours for this long. */
        public Integer stay = 0;
        /** The minutes of the steps before the museum: the class starts that long before its entry (used to skip entries that would start in the past). */
        public Integer before = 0;
        /** A programme longer than this (minutes) is a long visit: the class chooses when to arrive and the system picks the museum entry. */
        public Integer longVisit = LONG_VISIT_MINUTES;
        /** In a long visit the entry is at least this long (minutes) after the arrival. */
        public Integer gap = ARRIVAL_GAP_MINUTES;

        public MuseumTiming(Integer step, Integer hold) {
            this.step = step;
            this.hold = hold;
        }
    }
    // TODO: look the record type up by developer name once confirmed
    private static final String ORDER_RECORD_TYPE_NAME = 'Order';
    // The most participants in one class is the Custom Label yvEdu_Res_MaxPerGroup (35 per guided group, DD V02-05). Up to 50 in the
    // Tagalit and Overseas activity budget frameworks is not built: the budget framework is not used yet.
    private static Integer maxPerGroup() {
        Integer value = Integer.valueOf(System.Label.yvEdu_Res_MaxPerGroup);
        if (value == null || value < 1) {
            throw new yvEduException('The label yvEdu_Res_MaxPerGroup must be a whole number of 1 or more');
        }
        return value;
    }
    /**
     * One Connected_Bundle__c per bundle is created with the order. The flow Connected_Bundle_Create_Opportunity_Products fires
     * on it and creates Opportunity Products and GoMeddo reservations too, so it must stay inactive. Tests skip the creation.
     */
    @TestVisible
    private static Boolean skipConnectedBundle = false;

    /** Test only: GoMeddo reservation types and statuses of the org are not available in a unit test. */
    @TestVisible
    private static Boolean skipReservations = false;
    // DD 12.2: orders can be placed up to twelve months ahead for every audience
    private static final Integer HORIZON_MONTHS = 12;
    // TODO-CONFIG: gap between the start times offered; the museum entry windows of the DD are every 10 minutes (not built yet)
    private static final Integer SLOT_INTERVAL_MINUTES = 30;
    // TODO-CONFIG: start times offered when the chosen items have no GoMeddo resource (no opening hours to follow)
    private static final Integer DEFAULT_FIRST_START_MINUTE = 8 * 60;
    private static final Integer DEFAULT_LAST_START_MINUTE = 17 * 60;

    /** Test only: stands in for Account.Price_Book__c, which a test running user has no account for. */
    @TestVisible
    private static Id pricebookOverride;

    private class yvEduException extends Exception {
    }

    /**
     * Connected_Bundle__c is a junction (master-detail to Opportunity and to Bundle__c), so its access follows the
     * more restrictive parent, and portal users only read Bundle__c. It is therefore created here, without
     * sharing and without the user's CRUD/FLS. Callers must already have checked that the order belongs to the
     * user and that the bundle fits the order. Only this one record shape can be written.
     */
    private without sharing class SystemAccess {
        /**
         * One museum reservation per class and museum resource: from the entry time of the class for holdMinutes,
         * Quantity = the participants of the class, so only part of the museum capacity is taken. GoMeddo checks the
         * capacity on insert. groups and entryTimes are in the same order.
         */
        private void createMuseumReservations(
            Opportunity order,
            List<B25__Group__c> groups,
            List<String> entryTimes,
            Set<Id> museumResourceIds,
            Date visitDate,
            Integer holdMinutes
        ) {
            List<B25__Reservation_Type__c> types = [SELECT Id FROM B25__Reservation_Type__c WHERE Name = :RESERVATION_TYPE_NAME LIMIT 1];
            List<B25__Reservation_Status__c> statuses = [SELECT Id FROM B25__Reservation_Status__c WHERE Name = :MUSEUM_RESERVATION_STATUS_NAME LIMIT 1];
            if (types.isEmpty() || statuses.isEmpty()) {
                throw new yvEduException('GoMeddo has no reservation type "' + RESERVATION_TYPE_NAME + '" or no status "' + MUSEUM_RESERVATION_STATUS_NAME + '"');
            }
            List<B25__Resource__c> resources = [SELECT Id, Name FROM B25__Resource__c WHERE Id IN :museumResourceIds];
            List<B25__Reservation__c> reservations = new List<B25__Reservation__c>();
            for (B25__Resource__c resource : resources) {
                for (Integer i = 0; i < groups.size(); i++) {
                    List<String> parts = entryTimes[i].split(':');
                    Datetime entry = Datetime.newInstance(visitDate, Time.newInstance(Integer.valueOf(parts[0]), Integer.valueOf(parts[1]), 0, 0));
                    Datetime leave = entry.addMinutes(holdMinutes);
                    reservations.add(
                        new B25__Reservation__c(
                            B25__Resource__c = resource.Id,
                            B25__Reservation_Type__c = types[0].Id,
                            B25__Status__c = statuses[0].Id,
                            B25__Opportunity__c = order.Id,
                            B25__Group__c = groups[i].Id,
                            B25__Start__c = entry,
                            B25__End__c = leave,
                            B25__StartLocal__c = entry,
                            B25__EndLocal__c = leave,
                            B25__Quantity__c = groups[i].Number_Participants__c,
                            B25__Title__c = ('הזמנה של ' + order.Name + ' - ' + resource.Name + ' - ' + groups[i].Name).abbreviate(255)
                        )
                    );
                }
            }
            insert reservations;
        }

        /**
         * One reservation per class on the resource chosen for it (a classroom from assignPool, or the specific resource of the
         * step), from the time of that class for the minutes of the step. Not all or nothing: when GoMeddo refuses one (the resource
         * is taken at that time) the order still goes through and the reservation is left to be made by hand.
         */
        private void createClassReservations(
            Opportunity order,
            List<B25__Group__c> groups,
            List<Id> resourceIds,
            List<Datetime> starts,
            Integer minutes
        ) {
            List<B25__Reservation_Type__c> types = [SELECT Id FROM B25__Reservation_Type__c WHERE Name = :RESERVATION_TYPE_NAME LIMIT 1];
            List<B25__Reservation_Status__c> statuses = [SELECT Id FROM B25__Reservation_Status__c WHERE Name = :RESERVATION_STATUS_NAME LIMIT 1];
            if (types.isEmpty() || statuses.isEmpty()) {
                throw new yvEduException('GoMeddo has no reservation type "' + RESERVATION_TYPE_NAME + '" or no status "' + RESERVATION_STATUS_NAME + '"');
            }
            Set<Id> ids = new Set<Id>(resourceIds);
            List<B25__Resource__c> resources = [SELECT Id, Name FROM B25__Resource__c WHERE Id IN :ids];
            Map<Id, B25__Resource__c> resourceById = new Map<Id, B25__Resource__c>(resources);
            List<B25__Reservation__c> reservations = new List<B25__Reservation__c>();
            for (Integer i = 0; i < groups.size(); i++) {
                if (i >= resourceIds.size() || resourceIds[i] == null) {
                    continue;
                }
                Datetime visitStart = starts[i];
                Datetime visitEnd = visitStart.addMinutes(minutes);
                reservations.add(
                    new B25__Reservation__c(
                        B25__Resource__c = resourceIds[i],
                        B25__Reservation_Type__c = types[0].Id,
                        B25__Status__c = statuses[0].Id,
                        B25__Opportunity__c = order.Id,
                        B25__Group__c = groups[i].Id,
                        B25__Start__c = visitStart,
                        B25__End__c = visitEnd,
                        B25__StartLocal__c = visitStart,
                        B25__EndLocal__c = visitEnd,
                        B25__Quantity__c = groups[i].Number_Participants__c,
                        B25__Title__c = ('הזמנה של ' + order.Name + ' - ' + resourceById.get(resourceIds[i]).Name + ' - ' + groups[i].Name).abbreviate(255)
                    )
                );
            }
            List<Database.SaveResult> saved = Database.insert(reservations, false);
            // a class that does not fit the capacity of its resource is refused: it is booked again without the quantity, still waiting
            // for approval (the number of participants stays on the class), so that every class has a reservation to approve by hand
            List<B25__Reservation__c> again = new List<B25__Reservation__c>();
            for (Integer i = 0; i < saved.size(); i++) {
                if (!saved[i].isSuccess()) {
                    System.debug(LoggingLevel.WARN, 'yvEduNewReservationController: reservation not created: ' + saved[i].getErrors()[0].getMessage());
                    B25__Reservation__c retry = reservations[i].clone(false, true, false, false);
                    retry.B25__Quantity__c = null;
                    again.add(retry);
                }
            }
            if (!again.isEmpty()) {
                List<Database.SaveResult> savedAgain = Database.insert(again, false);
                for (Integer i = 0; i < savedAgain.size(); i++) {
                    if (!savedAgain[i].isSuccess()) {
                        System.debug(LoggingLevel.WARN, 'yvEduNewReservationController: reservation not created without quantity either: ' + savedAgain[i].getErrors()[0].getMessage());
                    }
                }
            }
            // The status is set here, not by the flow: every reservation of the programme is created "מחכה לאישור" (RESERVATION_STATUS_NAME);
            // only the museum is created "אושרה" (MUSEUM_RESERVATION_STATUS_NAME). Since version 5 (10.11.2026) the flow Reservation - Assign Buffers
            // only adds the buffers and no longer looks at B25__Resource__c.Need_Confirmed__c, so that field changes nothing here.
        }

        /** After the reservations exist the order waits for approval, as the flow Connected_Bundle_Create_Opportunity_Products does. */
        private void moveToWaitingForApproval(Id orderId) {
            update new Opportunity(Id = orderId, StageName = WAITING_FOR_APPROVAL_STAGE);
        }

        /** The order lines are built and priced on the server from validated data; they are written without the user's field-level security. */
        private void insertLines(List<OpportunityLineItem> lines) {
            insert lines;
        }

        private void ensureConnectedBundle(Id opportunityId, Id bundleId, String name) {
            if ([SELECT COUNT() FROM Connected_Bundle__c WHERE Opportunity__c = :opportunityId AND Bundle__c = :bundleId] == 0) {
                insert new Connected_Bundle__c(Name = name, Opportunity__c = opportunityId, Bundle__c = bundleId);
            }
        }
    }

    /**
     * Availability is read from GoMeddo without sharing and without the user's rights, because a portal user cannot
     * see other people's reservations and would otherwise get days that look free. Only free/busy per day leaves
     * this class; no reservation data goes back to the page.
     */
    private without sharing class AvailabilityAccess {
        /** The GoMeddo resources of the products of the chosen bundle items (items without a resource do not limit the date). */
        private Set<Id> resourceIds(Set<Id> bundleItemIds) {
            Set<Id> ids = new Set<Id>();
            // The query is kept out of the for statement on purpose: a SOQL for-loop with a bind variable inside this
            // inner class fails at runtime with "Variable does not exist: tmpVar1".
            List<Bundle_Item__c> items = [
                SELECT Product__r.Resource__c
                FROM Bundle_Item__c
                WHERE Id IN :bundleItemIds AND Active__c = TRUE
            ];
            for (Bundle_Item__c item : items) {
                if (item.Product__r != null && item.Product__r.Resource__c != null) {
                    ids.add(item.Product__r.Resource__c);
                }
            }
            return ids;
        }

        /**
         * Asks GoMeddo. The prototype reservation lets it check the capacity against the participants; its status is
         * filled because GoMeddo reads Status__r.AllowDoubleBooking while checking conflicts. If GoMeddo still fails
         * with a null reference on that prototype, the call is repeated without it (no capacity check), so the
         * customer can still see the free days.
         */
        private B25.AvailableTimeRanges.Result timeRanges(Set<Id> resourceIds, Date fromDate, Date toDate, Integer participants) {
            return timeRanges(resourceIds, fromDate, toDate, participants, false);
        }

        /**
         * ignoreOccupancy = true asks only for the opening hours: the prototype reservation then has a status that allows double
         * booking, so the reservations already made do not cut the free ranges (for resources that never stop an order).
         */
        private B25.AvailableTimeRanges.Result timeRanges(Set<Id> resourceIds, Date fromDate, Date toDate, Integer participants, Boolean ignoreOccupancy) {
            B25.AvailableTimeRanges.Context context = new B25.AvailableTimeRanges.Context();
            context.startDate = fromDate;
            context.endDate = toDate;
            context.dimensionName = 'B25__Resource__c';
            context.dimensionFieldName = 'B25__Resource__c';
            context.dimensionIds = resourceIds;

            SObject status = Schema.getGlobalDescribe().get('B25__Reservation_Status__c').newSObject();
            status.put('B25__AllowDoubleBooking__c', ignoreOccupancy);
            B25__Reservation__c prototype = new B25__Reservation__c(B25__Quantity__c = participants);
            prototype.putSObject('B25__Status__r', status);
            context.prototypeReservation = prototype;
            try {
                return B25.AvailableTimeRanges.getTimeRanges(context);
            } catch (NullPointerException npe) {
                System.debug(LoggingLevel.WARN, 'GoMeddo availability failed with the prototype reservation, retrying without it: ' + npe.getMessage());
                context.prototypeReservation = null;
                return B25.AvailableTimeRanges.getTimeRanges(context);
            }
        }

        /** For every GoMeddo resource of the chosen items: the hours its items take (duration x quantity of the products). */
        private Map<Id, Decimal> hoursByResource(Set<Id> bundleItemIds) {
            Map<Id, Decimal> hours = new Map<Id, Decimal>();
            List<Bundle_Item__c> items = [
                SELECT Quantity__c, Product__r.Resource__c, Product__r.DurationOfHours__c
                FROM Bundle_Item__c
                WHERE Id IN :bundleItemIds AND Active__c = TRUE
            ];
            Set<Id> museum = museumIds(resourceIds(bundleItemIds));
            for (Bundle_Item__c item : items) {
                if (item.Product__r == null || item.Product__r.Resource__c == null || museum.contains(item.Product__r.Resource__c)) {
                    continue; // the museum is booked per class, not for the whole visit
                }
                Decimal itemHours = item.Product__r.DurationOfHours__c == null ? 0 : item.Product__r.DurationOfHours__c;
                Decimal quantity = item.Quantity__c == null || item.Quantity__c <= 0 ? 1 : item.Quantity__c;
                Decimal current = hours.containsKey(item.Product__r.Resource__c) ? hours.get(item.Product__r.Resource__c) : 0;
                hours.put(item.Product__r.Resource__c, current + itemHours * quantity);
            }
            return hours;
        }

        /** True when item a comes after item b in the programme: bundles in the order chosen, then Default Order, then name. */
        private Boolean comesAfter(Bundle_Item__c a, Bundle_Item__c b, Map<Id, Integer> bundleRank) {
            if (bundleRank.get(a.Bundle__c) != bundleRank.get(b.Bundle__c)) {
                return bundleRank.get(a.Bundle__c) > bundleRank.get(b.Bundle__c);
            }
            Decimal orderA = a.Default_Order__c == null ? 1000000 : a.Default_Order__c;
            Decimal orderB = b.Default_Order__c == null ? 1000000 : b.Default_Order__c;
            if (orderA != orderB) {
                return orderA > orderB;
            }
            return a.Name > b.Name;
        }

        /**
         * The programme of one class: the chosen items one after the other, the bundles in the order they were chosen and inside a
         * bundle by Default Order. Every step has the minutes of its items (duration x quantity). The museum items of the order
         * are one step. orderedItemIds are the chosen items in the order of the bundles that were chosen.
         */
        private List<ProgrammeStep> programme(List<Id> orderedItemIds, Set<Id> museumResourceIds) {
            List<Bundle_Item__c> items = [
                SELECT Id, Name, Bundle__c, Default_Order__c, Quantity__c, Product__r.Resource__c, Product__r.Resource_Type__c, Product__r.DurationOfHours__c
                FROM Bundle_Item__c
                WHERE Id IN :orderedItemIds AND Active__c = TRUE
            ];
            Map<Id, Bundle_Item__c> itemById = new Map<Id, Bundle_Item__c>(items);
            Map<Id, Integer> bundleRank = new Map<Id, Integer>();
            for (Id itemId : orderedItemIds) {
                Bundle_Item__c item = itemById.get(itemId);
                if (item != null && !bundleRank.containsKey(item.Bundle__c)) {
                    bundleRank.put(item.Bundle__c, bundleRank.size());
                }
            }
            List<Bundle_Item__c> sorted = new List<Bundle_Item__c>(items);
            for (Integer i = 1; i < sorted.size(); i++) {
                Bundle_Item__c current = sorted[i];
                Integer j = i - 1;
                while (j >= 0 && comesAfter(sorted[j], current, bundleRank)) {
                    sorted[j + 1] = sorted[j];
                    j--;
                }
                sorted[j + 1] = current;
            }

            List<ProgrammeStep> steps = new List<ProgrammeStep>();
            ProgrammeStep museumStep = null;
            for (Bundle_Item__c item : sorted) {
                if (item.Product__r == null) {
                    continue;
                }
                Decimal itemHours = item.Product__r.DurationOfHours__c == null ? 0 : item.Product__r.DurationOfHours__c;
                Decimal quantity = item.Quantity__c == null || item.Quantity__c <= 0 ? 1 : item.Quantity__c;
                Integer minutes = (itemHours * quantity * 60).setScale(0, System.RoundingMode.UP).intValue();
                Id resourceId = item.Product__r.Resource__c;
                if (resourceId != null && museumResourceIds.contains(resourceId)) {
                    if (museumStep == null) {
                        museumStep = new ProgrammeStep();
                        museumStep.museum = true;
                        museumStep.resourceId = resourceId;
                        steps.add(museumStep);
                    }
                    museumStep.minutes += minutes;
                    continue;
                }
                ProgrammeStep step = new ProgrammeStep();
                step.resourceId = resourceId;
                step.typeId = resourceId == null ? item.Product__r.Resource_Type__c : null;
                step.minutes = minutes;
                steps.add(step);
            }
            return steps;
        }

        /**
         * One resource of the type for each class (the same resource never twice), for the time of that class: best a free one in
         * GoMeddo for the whole step whose capacity fits the class, the smallest such. When none is free the class still gets a
         * resource: a resource that is missing never stops an order, only the museum does. The result is in the order of the classes.
         */
        private List<Id> assignPool(Id typeId, Integer minutes, List<Integer> participants, List<Datetime> starts) {
            List<B25__Resource__c> resources = [
                SELECT Id, B25__Capacity__c
                FROM B25__Resource__c
                WHERE B25__Resource_Type__c = :typeId AND B25__Is_Active__c = TRUE
                ORDER BY B25__Capacity__c ASC NULLS LAST, Name
            ];
            List<Id> assigned = new List<Id>();
            for (Integer i = 0; i < participants.size(); i++) {
                assigned.add(null);
            }
            if (resources.isEmpty()) {
                return assigned;
            }
            Set<Id> resourceIdSet = new Map<Id, B25__Resource__c>(resources).keySet();
            Date firstDay = starts[0].date();
            Date lastDay = starts[0].date();
            for (Datetime classStart : starts) {
                firstDay = classStart.date() < firstDay ? classStart.date() : firstDay;
                lastDay = classStart.date() > lastDay ? classStart.date() : lastDay;
            }
            B25.AvailableTimeRanges.Result result = timeRanges(resourceIdSet, firstDay, lastDay.addDays(1), 1);
            // per class: the resources GoMeddo has free for its time (the ranges and the entered times are local time)
            List<Set<Id>> freeIds = new List<Set<Id>>();
            for (Integer i = 0; i < participants.size(); i++) {
                Set<Id> freeForClass = new Set<Id>();
                Date day = starts[i].date();
                Integer start = starts[i].hour() * 60 + starts[i].minute();
                for (B25__Resource__c resource : resources) {
                    Map<Date, List<B25.AvailableTimeRanges.TimeRange>> byDay = result == null || result.timeRanges == null
                        ? null
                        : result.timeRanges.get(resource.Id);
                    if (fits(byDay == null ? null : byDay.get(day), start, minutes)) {
                        freeForClass.add(resource.Id);
                    }
                }
                freeIds.add(freeForClass);
            }
            // the biggest classes choose first, so that they get the rooms they fit in
            List<Integer> ranking = new List<Integer>();
            for (Integer i = 0; i < participants.size(); i++) {
                ranking.add(i);
            }
            for (Integer i = 0; i < ranking.size(); i++) {
                for (Integer j = i + 1; j < ranking.size(); j++) {
                    if (participants[ranking[j]] > participants[ranking[i]]) {
                        Integer swap = ranking[i];
                        ranking[i] = ranking[j];
                        ranking[j] = swap;
                    }
                }
            }
            Set<Id> used = new Set<Id>();
            for (Integer index : ranking) {
                Id chosen = null;
                // 1 free and fitting, 2 free, 3 fitting, 4 anything not used yet, 5 anything. A free resource comes before a fitting one:
                // GoMeddo refuses a booking on a resource that is taken at that time for good, but a class that is too big for it is still booked (without quantity).
                for (Integer pass = 1; pass <= 5 && chosen == null; pass++) {
                    for (B25__Resource__c resource : resources) {
                        Boolean fitsClass = resource.B25__Capacity__c != null && resource.B25__Capacity__c >= participants[index];
                        Boolean isFree = freeIds[index].contains(resource.Id);
                        Boolean isNew = !used.contains(resource.Id);
                        Boolean ok = pass == 1
                            ? isFree && fitsClass && isNew
                            : pass == 2 ? isFree && isNew : pass == 3 ? fitsClass && isNew : pass == 4 ? isNew : true;
                        if (ok) {
                            chosen = resource.Id;
                            break;
                        }
                    }
                }
                used.add(chosen);
                assigned[index] = chosen;
            }
            return assigned;
        }

        /**
         * How long after the visit start a class may still enter the museum: the hours of the museum items of the order
         * (duration x quantity of the products); when they have no duration, the longest of the other resources. Null when
         * nothing tells (no limit).
         */
        private Integer museumWindowMinutes(Set<Id> bundleItemIds, Set<Id> museumResourceIds) {
            List<Bundle_Item__c> items = [
                SELECT Quantity__c, Product__r.Resource__c, Product__r.DurationOfHours__c
                FROM Bundle_Item__c
                WHERE Id IN :bundleItemIds AND Active__c = TRUE
            ];
            Decimal hours = 0;
            for (Bundle_Item__c item : items) {
                if (item.Product__r != null && item.Product__r.Resource__c != null && museumResourceIds.contains(item.Product__r.Resource__c)) {
                    Decimal itemHours = item.Product__r.DurationOfHours__c == null ? 0 : item.Product__r.DurationOfHours__c;
                    Decimal quantity = item.Quantity__c == null || item.Quantity__c <= 0 ? 1 : item.Quantity__c;
                    hours += itemHours * quantity;
                }
            }
            if (hours <= 0) {
                for (Decimal otherHours : hoursByResource(bundleItemIds).values()) {
                    hours = Math.max(hours, otherHours);
                }
            }
            return hours > 0 ? (hours * 60).setScale(0, System.RoundingMode.UP).intValue() : null;
        }

        /** The timing of the museum with the stay of the order: hours x quantity of the museum products, in minutes (0 when they have no duration). */
        private MuseumTiming museumTimingFor(Set<Id> bundleItemIds, Set<Id> museumResourceIds) {
            MuseumTiming timing = museumTiming(museumResourceIds);
            List<Bundle_Item__c> items = [
                SELECT Quantity__c, Product__r.Resource__c, Product__r.DurationOfHours__c
                FROM Bundle_Item__c
                WHERE Id IN :bundleItemIds AND Active__c = TRUE
            ];
            Decimal hours = 0;
            for (Bundle_Item__c item : items) {
                if (item.Product__r != null && item.Product__r.Resource__c != null && museumResourceIds.contains(item.Product__r.Resource__c)) {
                    Decimal itemHours = item.Product__r.DurationOfHours__c == null ? 0 : item.Product__r.DurationOfHours__c;
                    Decimal quantity = item.Quantity__c == null || item.Quantity__c <= 0 ? 1 : item.Quantity__c;
                    hours += itemHours * quantity;
                }
            }
            timing.stay = (hours * 60).setScale(0, System.RoundingMode.UP).intValue();
            return timing;
        }

        /** The first and last entry time ("HH:mm") a class may choose: from the visit start up to the window after it. */
        private List<String> museumEntryRange(Set<Id> bundleItemIds, Set<Id> museumResourceIds, String visitTime) {
            Integer window = museumWindowMinutes(bundleItemIds, museumResourceIds);
            Integer first = minuteOfDay(visitTime);
            Integer last = window == null ? 24 * 60 - 1 : Math.min(first + window, 24 * 60 - 1);
            return new List<String>{ visitTime, formatMinute(last) };
        }

        /**
         * The days in [fromDate, toDate) on which a museum resource has no room for the visitors that are coming: the visitors
         * already booked that day (the quantity of the reservations that start that day, one per class; statuses that allow
         * double booking, such as cancelled, do not count) plus the coming ones would pass the Max Day Capacity
         * (B25__Resource__c.Max_Day_Capacity__c) of the resource. A resource without that value has no daily limit.
         */
        private Set<Date> museumFullDays(Set<Id> museumResourceIds, Date fromDate, Date toDate, Integer coming) {
            Set<Date> fullDays = new Set<Date>();
            if (museumResourceIds.isEmpty()) {
                return fullDays;
            }
            List<B25__Resource__c> resources = [SELECT Id, Max_Day_Capacity__c FROM B25__Resource__c WHERE Id IN :museumResourceIds];
            Map<Id, Decimal> limitOf = new Map<Id, Decimal>();
            for (B25__Resource__c resource : resources) {
                if (resource.Max_Day_Capacity__c != null) {
                    limitOf.put(resource.Id, resource.Max_Day_Capacity__c);
                }
            }
            if (limitOf.isEmpty()) {
                return fullDays;
            }
            Set<Id> limitedIds = limitOf.keySet();
            Datetime fromTime = Datetime.newInstance(fromDate, Time.newInstance(0, 0, 0, 0));
            Datetime toTime = Datetime.newInstance(toDate, Time.newInstance(0, 0, 0, 0));
            List<AggregateResult> rows = [
                SELECT B25__Resource__c resourceId, DAY_ONLY(B25__Start__c) visitDay, SUM(B25__Quantity__c) total
                FROM B25__Reservation__c
                WHERE
                    B25__Resource__c IN :limitedIds
                    AND B25__Start__c >= :fromTime
                    AND B25__Start__c < :toTime
                    AND B25__Status__r.B25__AllowDoubleBooking__c = FALSE
                GROUP BY B25__Resource__c, DAY_ONLY(B25__Start__c)
            ];
            Map<Date, Decimal> bookedByDay = new Map<Date, Decimal>();
            for (AggregateResult row : rows) {
                Date day = (Date) row.get('visitDay');
                Decimal booked = (Decimal) row.get('total');
                if (booked != null && booked + coming > limitOf.get((Id) row.get('resourceId'))) {
                    fullDays.add(day);
                }
            }
            // a limit smaller than the visitors coming closes every day, whether it has bookings or not
            for (Decimal dayLimit : limitOf.values()) {
                if (coming > dayLimit) {
                    for (Date day = fromDate; day < toDate; day = day.addDays(1)) {
                        fullDays.add(day);
                    }
                }
            }
            return fullDays;
        }

        /** The resources among the given ones whose resource type is the museum. */
        private Set<Id> museumIds(Set<Id> resourceIds) {
            List<B25__Resource__c> museums = [
                SELECT Id
                FROM B25__Resource__c
                WHERE Id IN :resourceIds AND B25__Resource_Type__r.Name = :MUSEUM_RESOURCE_TYPE
            ];
            return new Map<Id, B25__Resource__c>(museums).keySet();
        }

        /** The step and hold of the museum resources; they must be filled in with whole numbers of 1 or more. */
        private MuseumTiming museumTiming(Set<Id> museumResourceIds) {
            List<B25__Resource__c> resources = [
                SELECT Id, Name, Entry_Step_Minutes__c, Entry_Hold_Minutes__c
                FROM B25__Resource__c
                WHERE Id IN :museumResourceIds
                ORDER BY Name
            ];
            MuseumTiming timing;
            for (B25__Resource__c resource : resources) {
                Decimal step = resource.Entry_Step_Minutes__c;
                Decimal hold = resource.Entry_Hold_Minutes__c;
                if (step == null || hold == null || step < 1 || hold < 1) {
                    throw new yvEduException(
                        'The museum resource ' + resource.Name + ' needs Entry Step Minutes and Entry Hold Minutes (whole numbers, 1 or more)'
                    );
                }
                if (timing != null && (timing.step != step.intValue() || timing.hold != hold.intValue())) {
                    throw new yvEduException('The museum resources of one order must have the same Entry Step Minutes and Entry Hold Minutes');
                }
                timing = new MuseumTiming(step.intValue(), hold.intValue());
            }
            return timing;
        }

        /**
         * The museum of one day cut into pieces of timing.step: the pieces GoMeddo has the museum open, with the seats
         * still free in each (capacity of the resource minus the quantity of the reservations that overlap the piece; statuses
         * that allow double booking, such as cancelled, do not count; several museum resources: the smallest). An entry holds
         * timing.hold, so it needs all the pieces it covers (see checkMuseumEntries). Today only offers pieces that
         * have not started.
         */
        private List<MuseumSlot> museumSlots(Set<Id> museumResourceIds, Date day, MuseumTiming timing) {
            if (museumResourceIds.isEmpty()) {
                return new List<MuseumSlot>();
            }
            B25.AvailableTimeRanges.Result result = timeRanges(museumResourceIds, day, day.addDays(1), 1);
            List<B25__Resource__c> resources = [SELECT Id, Name, B25__Capacity__c FROM B25__Resource__c WHERE Id IN :museumResourceIds];
            Datetime dayStart = Datetime.newInstance(day, Time.newInstance(0, 0, 0, 0));
            Datetime dayEnd = dayStart.addDays(1);
            List<B25__Reservation__c> booked = [
                SELECT B25__Resource__c, B25__Start__c, B25__End__c, B25__Quantity__c
                FROM B25__Reservation__c
                WHERE
                    B25__Resource__c IN :museumResourceIds
                    AND B25__Start__c < :dayEnd
                    AND B25__End__c > :dayStart
                    AND B25__Status__r.B25__AllowDoubleBooking__c = FALSE
            ];
            return museumSlotsOfDay(resources, result, booked, day, timing);
        }

        /**
         * The days in [fromDate, toDate) on which the museum has no entry a class could take: no entry time whose whole hold
         * has a free seat in every piece (open per GoMeddo, capacity minus the overlapping reservations). One GoMeddo call
         * and one query for the whole range.
         */
        private Set<Date> museumNoEntryDays(Set<Id> museumResourceIds, Date fromDate, Date toDate, MuseumTiming timing) {
            Set<Date> closed = new Set<Date>();
            if (museumResourceIds.isEmpty()) {
                return closed;
            }
            B25.AvailableTimeRanges.Result result = timeRanges(museumResourceIds, fromDate, toDate, 1);
            List<B25__Resource__c> resources = [SELECT Id, Name, B25__Capacity__c FROM B25__Resource__c WHERE Id IN :museumResourceIds];
            Datetime fromTime = Datetime.newInstance(fromDate, Time.newInstance(0, 0, 0, 0));
            Datetime toTime = Datetime.newInstance(toDate, Time.newInstance(0, 0, 0, 0));
            List<B25__Reservation__c> booked = [
                SELECT B25__Resource__c, B25__Start__c, B25__End__c, B25__Quantity__c
                FROM B25__Reservation__c
                WHERE
                    B25__Resource__c IN :museumResourceIds
                    AND B25__Start__c < :toTime
                    AND B25__End__c > :fromTime
                    AND B25__Status__r.B25__AllowDoubleBooking__c = FALSE
            ];
            for (Date day = fromDate; day < toDate; day = day.addDays(1)) {
                Datetime dayStart = Datetime.newInstance(day, Time.newInstance(0, 0, 0, 0));
                Datetime dayEnd = dayStart.addDays(1);
                List<B25__Reservation__c> ofDay = new List<B25__Reservation__c>();
                for (B25__Reservation__c reservation : booked) {
                    if (reservation.B25__Start__c < dayEnd && reservation.B25__End__c > dayStart) {
                        ofDay.add(reservation);
                    }
                }
                if (firstFreeEntry(museumSlotsOfDay(resources, result, ofDay, day, timing), timing, 0, 24 * 60) == null) {
                    closed.add(day);
                }
            }
            return closed;
        }

        /** An entry time (minute of day) in [fromMinute, toMinute] whose piece has a free seat, or null. */
        private Integer firstFreeEntry(List<MuseumSlot> slots, MuseumTiming timing, Integer fromMinute, Integer toMinute) {
            Set<Integer> freePieces = new Set<Integer>();
            for (MuseumSlot slot : slots) {
                if (slot.free > 0) {
                    freePieces.add(minuteOfDay(slot.startTime));
                }
            }
            for (Integer entry : new List<Integer>(freePieces)) {
                if (entry < fromMinute || entry > toMinute) {
                    continue;
                }
                return entry;
            }
            return null;
        }

        private List<MuseumSlot> museumSlotsOfDay(
            List<B25__Resource__c> resources,
            B25.AvailableTimeRanges.Result result,
            List<B25__Reservation__c> booked,
            Date day,
            MuseumTiming timing
        ) {
            List<MuseumSlot> slots = new List<MuseumSlot>();
            Datetime dayStart = Datetime.newInstance(day, Time.newInstance(0, 0, 0, 0));
            Integer nowMinute = day == Date.today() ? minuteOfDay(Datetime.now().format('HH:mm')) : -1;
            Integer pieceCount = Math.floor(Decimal.valueOf(24 * 60) / timing.step).intValue();
            Integer stayPieces = Math.max(1, Math.ceil(Decimal.valueOf(timing.stay) / timing.step).intValue());

            // per piece: GoMeddo has it open for every museum resource, the seats still free (the smallest of the resources),
            // and whether the piece only looks closed because it is full (GoMeddo then cuts a gap out of its free ranges)
            List<Boolean> open = new List<Boolean>();
            List<Integer> freeSeats = new List<Integer>();
            List<Boolean> openOrFull = new List<Boolean>();
            for (Integer piece = 0; piece < pieceCount; piece++) {
                Integer start = piece * timing.step;
                Datetime slotStart = dayStart.addMinutes(start);
                Datetime slotEnd = slotStart.addMinutes(timing.step);
                Boolean allOpen = true;
                Boolean allOpenOrFull = true;
                Integer smallestFree = null;
                for (B25__Resource__c resource : resources) {
                    if (resource.B25__Capacity__c == null) {
                        throw new yvEduException('The museum resource ' + resource.Name + ' has no Default Capacity');
                    }
                    Map<Date, List<B25.AvailableTimeRanges.TimeRange>> byDay = result == null || result.timeRanges == null
                        ? null
                        : result.timeRanges.get(resource.Id);
                    Boolean resourceOpen = fits(byDay == null ? null : byDay.get(day), start, timing.step);
                    Decimal used = 0;
                    for (B25__Reservation__c reservation : booked) {
                        // people entering in this piece: the reservations that start in it (their stay does not count later)
                        if (reservation.B25__Resource__c == resource.Id && reservation.B25__Start__c >= slotStart && reservation.B25__Start__c < slotEnd) {
                            used += reservation.B25__Quantity__c == null ? 0 : reservation.B25__Quantity__c;
                        }
                    }
                    Integer free = Math.max(0, (resource.B25__Capacity__c - used).intValue());
                    smallestFree = smallestFree == null ? free : Math.min(smallestFree, free);
                    allOpen = allOpen && resourceOpen;
                    allOpenOrFull = allOpenOrFull && (resourceOpen || free == 0);
                }
                open.add(allOpen);
                freeSeats.add(smallestFree == null ? 0 : smallestFree);
                openOrFull.add(allOpenOrFull);
            }

            // an entry is offered when its own piece is open, and the museum stays open for the whole stay: every piece
            // from the entry until the stay ends is open (or only closed because it is full), and the stay ends before the end of the day
            for (Integer piece = 0; piece < pieceCount; piece++) {
                Integer start = piece * timing.step;
                if (start - timing.before <= nowMinute || !open.get(piece) || piece + stayPieces > pieceCount) {
                    continue;
                }
                Boolean stays = true;
                for (Integer later = piece; later < piece + stayPieces; later++) {
                    if (!openOrFull.get(later)) {
                        stays = false;
                        break;
                    }
                }
                if (stays) {
                    MuseumSlot slot = new MuseumSlot();
                    slot.startTime = formatMinute(start);
                    slot.free = freeSeats.get(piece);
                    slots.add(slot);
                }
            }
            return slots;
        }

        /**
         * The days in [fromDate, toDate) on which at least one start time is left (the same rule as freeStartTimes, from one
         * GoMeddo call for the whole range): a day with no time is not offered. Today only counts the times that have not passed.
         * Used when the order has no museum, because then the customer is asked for a start time.
         */
        private Map<Date, Boolean> daysWithStartTimes(Set<Id> bundleItemIds, Date fromDate, Date toDate) {
            Map<Id, Decimal> hours = hoursByResource(bundleItemIds);
            B25.AvailableTimeRanges.Result result;
            if (!hours.isEmpty()) {
                result = timeRanges(hours.keySet(), fromDate, toDate, 1, true);
            }
            Integer first = hours.isEmpty() ? DEFAULT_FIRST_START_MINUTE : 0;
            Integer last = hours.isEmpty() ? DEFAULT_LAST_START_MINUTE : 24 * 60 - 1;
            Map<Date, Boolean> days = new Map<Date, Boolean>();
            for (Date day = fromDate; day < toDate; day = day.addDays(1)) {
                Integer nowMinute = day == Date.today() ? minuteOfDay(Datetime.now().format('HH:mm')) : -1;
                Boolean anyTime = false;
                for (Integer start = first; start <= last && !anyTime; start += SLOT_INTERVAL_MINUTES) {
                    if (start <= nowMinute) {
                        continue;
                    }
                    Boolean allFree = true;
                    for (Id resourceId : hours.keySet()) {
                        Integer needed = (hours.get(resourceId) * 60).setScale(0, System.RoundingMode.UP).intValue();
                        Map<Date, List<B25.AvailableTimeRanges.TimeRange>> byDay = result == null || result.timeRanges == null
                            ? null
                            : result.timeRanges.get(resourceId);
                        if (!fits(byDay == null ? null : byDay.get(day), start, needed)) {
                            allFree = false;
                            break;
                        }
                    }
                    anyTime = allFree;
                }
                days.put(day, anyTime);
            }
            return days;
        }

        /**
         * Start times ("HH:mm", every SLOT_INTERVAL_MINUTES) of one day at which every resource is free for as long as its
         * items take. Items without a resource do not limit the time. Today only offers times that have not passed.
         */
        private List<String> freeStartTimes(Set<Id> bundleItemIds, Date day, Integer participants) {
            // the other resources limit the start time by their opening hours only (not by what is already booked)
            Map<Id, Decimal> hours = hoursByResource(bundleItemIds);
            Integer nowMinute = day == Date.today() ? minuteOfDay(Datetime.now().format('HH:mm')) : -1;
            Map<Id, Map<Date, List<B25.AvailableTimeRanges.TimeRange>>> rangesByResource;
            if (!hours.isEmpty()) {
                B25.AvailableTimeRanges.Result result = timeRanges(hours.keySet(), day, day.addDays(1), 1, true);
                rangesByResource = result == null ? null : result.timeRanges;
            }

            List<String> times = new List<String>();
            Integer first = hours.isEmpty() ? DEFAULT_FIRST_START_MINUTE : 0;
            Integer last = hours.isEmpty() ? DEFAULT_LAST_START_MINUTE : 24 * 60 - 1;
            for (Integer start = first; start <= last; start += SLOT_INTERVAL_MINUTES) {
                if (start <= nowMinute) {
                    continue;
                }
                Boolean allFree = true;
                for (Id resourceId : hours.keySet()) {
                    Integer needed = (hours.get(resourceId) * 60).setScale(0, System.RoundingMode.UP).intValue();
                    Map<Date, List<B25.AvailableTimeRanges.TimeRange>> byDay = rangesByResource == null ? null : rangesByResource.get(resourceId);
                    if (!fits(byDay == null ? null : byDay.get(day), start, needed)) {
                        allFree = false;
                        break;
                    }
                }
                if (allFree) {
                    times.add(formatMinute(start));
                }
            }
            return times;
        }

        /** True when [start, start + needed) lies inside one of the free ranges (0:00 to 0:00 means free all day). */
        private Boolean fits(List<B25.AvailableTimeRanges.TimeRange> ranges, Integer start, Integer needed) {
            if (ranges == null) {
                return false;
            }
            for (B25.AvailableTimeRanges.TimeRange range : ranges) {
                Integer rangeStart = range.startTime.hour() * 60 + range.startTime.minute();
                Integer rangeEnd = range.endTime.hour() * 60 + range.endTime.minute();
                if (rangeStart == 0 && rangeEnd == 0) {
                    rangeEnd = 24 * 60;
                }
                if (start >= rangeStart && start + needed <= rangeEnd) {
                    return true;
                }
            }
            return false;
        }

        private Integer minuteOfDay(String hhmm) {
            List<String> parts = hhmm.split(':');
            return Integer.valueOf(parts[0]) * 60 + Integer.valueOf(parts[1]);
        }

        private String formatMinute(Integer minute) {
            return String.valueOf(minute / 60).leftPad(2, '0') + ':' + String.valueOf(Math.mod(minute, 60)).leftPad(2, '0');
        }

        /** For each day in [fromDate, toDate): true when every resource has at least one free time range that day. */
        private Map<Date, Boolean> freeDays(Set<Id> resourceIds, Date fromDate, Date toDate, Integer participants, Boolean ignoreOccupancy) {
            Map<Date, Boolean> days = new Map<Date, Boolean>();
            B25.AvailableTimeRanges.Result result;
            if (!resourceIds.isEmpty()) {
                result = timeRanges(resourceIds, fromDate, toDate, participants, ignoreOccupancy);
            }
            for (Date day = fromDate; day < toDate; day = day.addDays(1)) {
                Boolean free = true;
                for (Id resourceId : resourceIds) {
                    // GoMeddo may leave the map empty for a resource it knows nothing about: that day counts as not free
                    Map<Date, List<B25.AvailableTimeRanges.TimeRange>> rangesByDay = result == null || result.timeRanges == null
                        ? null
                        : result.timeRanges.get(resourceId);
                    List<B25.AvailableTimeRanges.TimeRange> ranges = rangesByDay == null ? null : rangesByDay.get(day);
                    if (ranges == null || ranges.isEmpty()) {
                        free = false;
                        break;
                    }
                }
                days.put(day, free);
            }
            return days;
        }
    }

    /**
     * Result of submitOrder. A failure is returned in error (and not thrown) because the Experience Cloud runtime hides the
     * message of a thrown exception: error is our own message (yvEduException) or a generic text, never a technical detail.
     */
    public class SaveResult {
        @AuraEnabled
        public Id opportunityId;
        /** The museum entry of every class ("HH:mm", in the order of the classes); empty without the museum. */
        @AuraEnabled
        public List<String> museumEntries = new List<String>();
        @AuraEnabled
        public String error;
    }

    public class Option {
        @AuraEnabled
        public String value;
        @AuraEnabled
        public String label;
    }

    public class ItemInfo {
        @AuraEnabled
        public Id id;
        @AuraEnabled
        public String name;
        @AuraEnabled
        public Decimal hours;
    }

    public class ActivityCard {
        @AuraEnabled
        public Id id;
        @AuraEnabled
        public String name;
        @AuraEnabled
        public Decimal totalHours;
        @AuraEnabled
        public List<ItemInfo> items = new List<ItemInfo>();
    }

    public class DayInfo {
        @AuraEnabled
        public Date day;
        @AuraEnabled
        public Boolean available;
    }

    /** Days of one month with free/busy. A failure is returned in error (not thrown), for the same reason as SaveResult. */
    public class DatesResult {
        @AuraEnabled
        public List<DayInfo> days = new List<DayInfo>();
        @AuraEnabled
        public String error;
    }

    /** Start times of one day. A failure is returned in error (not thrown), for the same reason as SaveResult. */
    public class TimesResult {
        @AuraEnabled
        public List<String> times = new List<String>();
        @AuraEnabled
        public String error;
    }

    /** One class (group) of the order. museumTime ("HH:mm") is the museum entry of the class, needed when the order includes the museum. */
    public class GroupInput {
        @AuraEnabled
        public String name;
        @AuraEnabled
        public Integer participants;
        @AuraEnabled
        public String museumTime;
        /** Long visit: when the class arrives. The museum entry is then assigned (assignEntries) and written to museumTime. */
        @AuraEnabled
        public String arrivalTime;
    }

    /** A 10-minute piece of the museum day (startTime = its start) and the seats still free in it. */
    public class MuseumSlot {
        @AuraEnabled
        public String startTime;
        @AuraEnabled
        public Integer free;
    }

    /** required = the chosen items include the museum, so every class picks an entry time between firstEntry and lastEntry. */
    public class MuseumResult {
        @AuraEnabled
        public Boolean required = false;
        /** The minutes of the programme before the museum: a class that enters at 08:30 starts at 08:30 minus this. */
        @AuraEnabled
        public Integer beforeMinutes = 0;
        /** True for a long visit (more than LONG_VISIT_MINUTES): the class chooses when to arrive and the entry is assigned. */
        @AuraEnabled
        public Boolean arrivalMode = false;
        @AuraEnabled
        public Integer gapMinutes = 0;
        @AuraEnabled
        public Integer stepMinutes;
        @AuraEnabled
        public Integer holdMinutes;
        @AuraEnabled
        public String firstEntry;
        @AuraEnabled
        public String lastEntry;
        @AuraEnabled
        public List<MuseumSlot> slots = new List<MuseumSlot>();
        @AuraEnabled
        public String error;
    }

    /** One bundle of the order with the items ticked in it. A bundle can be chosen only once per order. */
    public class BundleChoice {
        @AuraEnabled
        public Id bundleId;
        @AuraEnabled
        public List<Id> bundleItemIds;
    }

    /** Everything the customer chose, sent once at the end. */
    public class OrderRequest {
        @AuraEnabled
        public String audienceCategory;
        /** Optional: the Activity Day Subject of the order (Opportunity.Activity_Day_Subject__c). */
        @AuraEnabled
        public Id activityDaySubjectId;
        @AuraEnabled
        public List<BundleChoice> bundles;
        @AuraEnabled
        public List<GroupInput> groups;
        @AuraEnabled
        public Date visitDate;
        @AuraEnabled
        public String visitTime;
        /** Language of the guiding (a value of Opportunity.InstructionLanguage__c); one for the whole order. */
        @AuraEnabled
        public String instructionLanguage;
    }

    /**
     * Active values of Opportunity.Audience_Category__c, labelled in the user's language, limited to the audiences set on
     * the customer's account (Account.Audience_Category__c). An account with no audience set sees all of them.
     */
    @AuraEnabled(cacheable=true)
    public static List<Option> getAudienceCategories() {
        Set<String> allowed = audiencesOf(currentUserAccount());
        List<Option> options = new List<Option>();
        for (Schema.PicklistEntry entry : Opportunity.Audience_Category__c.getDescribe().getPicklistValues()) {
            if (entry.isActive() && (allowed.isEmpty() || allowed.contains(entry.getValue()))) {
                Option option = new Option();
                option.value = entry.getValue();
                option.label = entry.getLabel();
                options.add(option);
            }
        }
        return options;
    }

    /** The values of the picklist Opportunity.InstructionLanguage__c, for the question "in which language should the guiding be". */
    @AuraEnabled(cacheable=true)
    public static List<Option> getInstructionLanguages() {
        List<Option> options = new List<Option>();
        for (Schema.PicklistEntry entry : Opportunity.InstructionLanguage__c.getDescribe().getPicklistValues()) {
            if (entry.isActive()) {
                Option option = new Option();
                option.value = entry.getValue();
                option.label = entry.getLabel();
                options.add(option);
            }
        }
        return options;
    }

    /** The Activity Day Subjects (Opportunity.Activity_Day_Subject__c lookup) the customer can choose from, by name. */
    @AuraEnabled(cacheable=true)
    public static List<Option> getActivityDaySubjects() {
        List<Option> options = new List<Option>();
        for (Activity_Day_Subject__c subject : [SELECT Id, Name FROM Activity_Day_Subject__c WITH USER_MODE ORDER BY Name LIMIT 200]) {
            Option option = new Option();
            option.value = subject.Id;
            option.label = subject.Name;
            options.add(option);
        }
        return options;
    }

    @AuraEnabled(cacheable=true)
    public static List<ActivityCard> getActivities(String audienceCategory) {
        if (String.isBlank(audienceCategory)) {
            return new List<ActivityCard>();
        }
        try {
            List<Bundle__c> bundles = [
                SELECT
                    Id,
                    Name,
                    (
                        SELECT
                            Name,
                            Quantity__c,
                            Product__r.Name,
                            Product__r.NameEn__c,
                            Product__r.DurationOfHours__c
                        FROM Bundle_Items__r
                        WHERE Active__c = TRUE AND Audience_Category__c INCLUDES (:audienceCategory)
                        ORDER BY Default_Order__c ASC NULLS LAST, Name
                    )
                FROM Bundle__c
                WHERE Active_Bundle__c = TRUE AND Audience_Category__c INCLUDES (:audienceCategory)
                WITH USER_MODE
                ORDER BY Name
            ];

            Boolean useEnglishNames = UserInfo.getLanguage().startsWithIgnoreCase('en');

            List<ActivityCard> cards = new List<ActivityCard>();
            for (Bundle__c bundle : bundles) {
                cards.add(buildCard(bundle, useEnglishNames));
            }
            return cards;
        } catch (Exception e) {
            System.debug(LoggingLevel.ERROR, 'yvEduNewReservationController.getActivities failed: ' + e.getMessage() + ' | ' + e.getStackTraceString());
            throw new AuraHandledException('Unable to load activities');
        }
    }

    /**
     * Which days of a month the visit can take place: today up to twelve months ahead, and only days on which every
     * resource of the chosen items is free in GoMeddo (its own rules and buffers apply). Returns free/busy only.
     */
    @AuraEnabled
    public static DatesResult getAvailableDates(List<Id> bundleItemIds, Integer year, Integer month, Integer participants) {
        DatesResult result = new DatesResult();
        try {
            if (bundleItemIds == null || bundleItemIds.isEmpty() || year == null || month == null) {
                throw new yvEduException('Items and a month are required');
            }
            Date monthStart = Date.newInstance(year, month, 1);
            Date monthEnd = monthStart.addMonths(1);
            Date firstDay = monthStart < Date.today() ? Date.today() : monthStart;
            Date lastDay = Date.today().addMonths(HORIZON_MONTHS);
            Date toDate = monthEnd <= lastDay ? monthEnd : lastDay.addDays(1);

            Map<Date, Boolean> free = new Map<Date, Boolean>();
            if (firstDay < toDate) {
                AvailabilityAccess access = new AvailabilityAccess();
                Set<Id> allResources = access.resourceIds(new Set<Id>(bundleItemIds));
                Set<Id> museum = access.museumIds(allResources);
                Set<Id> regular = allResources.clone();
                regular.removeAll(museum);
                // the other resources only need to be open that day (opening hours); how many are already booked does not count
                free = access.freeDays(regular, firstDay, toDate, 1, true);
                if (!museum.isEmpty()) {
                    // the museum is open that day (the places per entry are chosen per class later), so one seat is enough here
                    Map<Date, Boolean> museumOpen = access.freeDays(museum, firstDay, toDate, 1, false);
                    // a day with no room left in the museum is closed, even when it still has free hours
                    Set<Date> fullDays = access.museumFullDays(museum, firstDay, toDate, participants == null ? 0 : participants);
                    // ... and so is a day on which no entry has a free seat all along its hold
                    Set<Date> noEntryDays = access.museumNoEntryDays(museum, firstDay, toDate, access.museumTimingFor(new Set<Id>(bundleItemIds), museum));
                    for (Date day : free.keySet()) {
                        free.put(
                            day,
                            free.get(day) && museumOpen.containsKey(day) && museumOpen.get(day) && !fullDays.contains(day) && !noEntryDays.contains(day)
                        );
                    }
                } else {
                    // no museum: the customer picks a start time, so a day with no start time left (today after the last time, a day
                    // too short for the hours of the items) is not offered
                    Map<Date, Boolean> withTimes = access.daysWithStartTimes(new Set<Id>(bundleItemIds), firstDay, toDate);
                    for (Date day : free.keySet()) {
                        free.put(day, free.get(day) && withTimes.containsKey(day) && withTimes.get(day));
                    }
                }
            }
            for (Date day = monthStart; day < monthEnd; day = day.addDays(1)) {
                DayInfo info = new DayInfo();
                info.day = day;
                info.available = free.containsKey(day) && free.get(day);
                result.days.add(info);
            }
        } catch (Exception e) {
            System.debug(LoggingLevel.ERROR, 'yvEduNewReservationController.getAvailableDates failed: ' + e.getMessage() + ' | ' + e.getStackTraceString());
            result.days = new List<DayInfo>();
            result.error = userMessage(e);
        }
        return result;
    }

    /** The start times ("HH:mm") the visit can begin at on one day; see AvailabilityAccess.freeStartTimes. Free/busy only. */
    @AuraEnabled
    public static TimesResult getAvailableTimes(List<Id> bundleItemIds, Date visitDate, Integer participants) {
        TimesResult result = new TimesResult();
        try {
            if (bundleItemIds == null || bundleItemIds.isEmpty() || visitDate == null) {
                throw new yvEduException('Items and a date are required');
            }
            if (visitDate < Date.today() || visitDate > Date.today().addMonths(HORIZON_MONTHS)) {
                throw new yvEduException('The date is outside the days that can be ordered');
            }
            AvailabilityAccess access = new AvailabilityAccess();
            Set<Id> items = new Set<Id>(bundleItemIds);
            result.times = access.freeStartTimes(items, visitDate, participants);
            Set<Id> museum = access.museumIds(access.resourceIds(items));
            if (!museum.isEmpty()) {
                // a start time is offered only when the classes can still enter the museum from it, up to the window after it
                MuseumTiming timing = access.museumTimingFor(items, museum);
                List<MuseumSlot> slots = access.museumSlots(museum, visitDate, timing);
                List<String> withEntry = new List<String>();
                for (String startTime : result.times) {
                    List<String> range = access.museumEntryRange(items, museum, startTime);
                    if (access.firstFreeEntry(slots, timing, access.minuteOfDay(range[0]), access.minuteOfDay(range[1])) != null) {
                        withEntry.add(startTime);
                    }
                }
                result.times = withEntry;
            }
        } catch (Exception e) {
            System.debug(LoggingLevel.ERROR, 'yvEduNewReservationController.getAvailableTimes failed: ' + e.getMessage() + ' | ' + e.getStackTraceString());
            result.times = new List<String>();
            result.error = userMessage(e);
        }
        return result;
    }

    /**
     * The museum of a day in pieces of one step with the free seats of each, and the minutes of the programme before the museum
     * (a class that enters at 08:30 starts its programme at 08:30 minus them). A class may enter at any piece that is open for its
     * whole stay. required = false when the items do not include the museum. bundleItemIds are in the order the bundles were chosen.
     */
    @AuraEnabled
    public static MuseumResult getMuseumSlots(List<Id> bundleItemIds, Date visitDate) {
        MuseumResult result = new MuseumResult();
        try {
            if (bundleItemIds == null || bundleItemIds.isEmpty() || visitDate == null) {
                throw new yvEduException('Items and a date are required');
            }
            if (visitDate < Date.today() || visitDate > Date.today().addMonths(HORIZON_MONTHS)) {
                throw new yvEduException('The date is outside the days that can be ordered');
            }
            AvailabilityAccess access = new AvailabilityAccess();
            Set<Id> museum = access.museumIds(access.resourceIds(new Set<Id>(bundleItemIds)));
            result.required = !museum.isEmpty();
            if (result.required) {
                result.firstEntry = '00:00';
                result.lastEntry = '23:59';
                MuseumTiming timing = access.museumTimingFor(new Set<Id>(bundleItemIds), museum);
                Boolean museumSeen = false;
                Integer totalMinutes = 0;
                for (ProgrammeStep step : access.programme(bundleItemIds, museum)) {
                    museumSeen = museumSeen || step.museum;
                    if (!museumSeen) {
                        timing.before += step.minutes;
                    }
                    totalMinutes += step.minutes;
                }
                result.beforeMinutes = timing.before;
                result.arrivalMode = totalMinutes > timing.longVisit;
                result.gapMinutes = timing.gap;
                result.stepMinutes = timing.step;
                result.holdMinutes = timing.hold;
                result.slots = access.museumSlots(museum, visitDate, timing);
            }
        } catch (Exception e) {
            System.debug(LoggingLevel.ERROR, 'yvEduNewReservationController.getMuseumSlots failed: ' + e.getMessage() + ' | ' + e.getStackTraceString());
            result.slots = new List<MuseumSlot>();
            result.error = userMessage(e);
        }
        return result;
    }

    /**
     * Creates the whole order at the end of the flow, in one transaction (everything or nothing):
     *  - the Opportunity (stage New, Order record type, the account's price book, audience, number of groups and participants,
     *    close date = the visit date, which is checked again against GoMeddo here)
     *  - one B25__Group__c per class (name and Number_Participants__c)
     *  - one Opportunity Product per chosen Bundle_Item__c of every chosen bundle, linked back through OpportunityLineItem.Bundle_Item__c;
     *    unit price = Bundle_Item_Entry__c.Price__c for the order's price book, else the price book entry; quantity =
     *    Bundle_Item__c.Quantity__c (1 when empty). The price is never taken from the page.
     *  - one GoMeddo reservation (B25__Reservation__c) per resource of the chosen items, then the order moves to Waiting for Approval
     *  - one Connected_Bundle__c per chosen bundle
     * The page's choices are re-validated here. Runs in user mode, except the Connected Bundle (see SystemAccess).
     */
    @AuraEnabled
    public static SaveResult submitOrder(String requestJson) {
        // The order arrives as JSON text: the Experience Cloud (LWR) runtime delivered a custom Apex class parameter empty.
        OrderRequest request;
        try {
            request = (OrderRequest) JSON.deserialize(requestJson, OrderRequest.class);
        } catch (Exception e) {
            SaveResult failed = new SaveResult();
            failed.error = 'The order could not be read: ' + e.getMessage();
            return failed;
        }
        return placeOrder(request);
    }

    @TestVisible
    private static SaveResult placeOrder(OrderRequest request) {
        SaveResult result = new SaveResult();
        Savepoint savepoint = Database.setSavepoint();
        try {
            validate(request);

            Set<Id> requestedBundleIds = new Set<Id>();
            Set<Id> requestedItemIds = new Set<Id>();
            List<Id> orderedItemIds = new List<Id>(); // in the order the bundles were chosen: the programme follows it
            for (BundleChoice choice : request.bundles) {
                requestedBundleIds.add(choice.bundleId);
                requestedItemIds.addAll(choice.bundleItemIds);
                orderedItemIds.addAll(choice.bundleItemIds);
            }
            List<Bundle__c> bundles = [
                SELECT Id, Name
                FROM Bundle__c
                WHERE Id IN :requestedBundleIds AND Active_Bundle__c = TRUE AND Audience_Category__c INCLUDES (:request.audienceCategory)
                WITH USER_MODE
                ORDER BY Name
            ];
            if (bundles.size() != requestedBundleIds.size()) {
                throw new yvEduException('A bundle is not available for this audience');
            }

            List<Bundle_Item__c> items = [
                SELECT Id, Bundle__c, Product__c, Quantity__c
                FROM Bundle_Item__c
                WHERE
                    Id IN :requestedItemIds
                    AND Bundle__c IN :requestedBundleIds
                    AND Active__c = TRUE
                    AND Audience_Category__c INCLUDES (:request.audienceCategory)
                WITH USER_MODE
            ];
            Map<Id, Id> bundleOfItem = new Map<Id, Id>();
            for (Bundle_Item__c item : items) {
                bundleOfItem.put(item.Id, item.Bundle__c);
            }
            for (BundleChoice choice : request.bundles) {
                for (Id itemId : choice.bundleItemIds) {
                    if (bundleOfItem.get(itemId) != choice.bundleId) {
                        throw new yvEduException('An item does not belong to its bundle, is not active or does not fit the audience');
                    }
                }
            }

            Integer totalParticipants = 0;
            for (GroupInput groupInput : request.groups) {
                totalParticipants += groupInput.participants;
            }
            AvailabilityAccess availabilityAccess = new AvailabilityAccess();
            Set<Id> museumIds = availabilityAccess.museumIds(availabilityAccess.resourceIds(requestedItemIds));
            MuseumTiming museumTiming = museumIds.isEmpty() ? null : availabilityAccess.museumTimingFor(requestedItemIds, museumIds);

            // The programme of every class: the steps one after the other. With the museum, the class chose the entry and the steps
            // before it are laid out backwards from it (a gathering of 30 minutes before an entry at 08:30 starts at 08:00); without it
            // every class starts at the visit time.
            List<ProgrammeStep> programme = availabilityAccess.programme(orderedItemIds, museumIds);
            Integer minutesBefore = 0;
            Integer totalMinutes = 0;
            Boolean museumSeen = false;
            for (ProgrammeStep step : programme) {
                museumSeen = museumSeen || step.museum;
                if (!museumSeen) {
                    minutesBefore += step.minutes;
                }
                totalMinutes += step.minutes;
            }
            List<Datetime> classStarts = new List<Datetime>();
            if (museumIds.isEmpty()) {
                if (String.isBlank(request.visitTime)) {
                    throw new yvEduException('Choose a visit time');
                }
                if (!availabilityAccess.freeStartTimes(requestedItemIds, request.visitDate, totalParticipants).contains(request.visitTime)) {
                    throw new yvEduException('The chosen date and time are no longer available');
                }
                List<String> timeParts = request.visitTime.split(':');
                Datetime visitTime = Datetime.newInstance(
                    request.visitDate,
                    Time.newInstance(Integer.valueOf(timeParts[0]), Integer.valueOf(timeParts[1]), 0, 0)
                );
                for (GroupInput groupInput : request.groups) {
                    classStarts.add(visitTime);
                }
            } else {
                museumTiming.before = minutesBefore;
                if (availabilityAccess.museumFullDays(museumIds, request.visitDate, request.visitDate.addDays(1), totalParticipants).contains(request.visitDate)) {
                    throw new yvEduException('The museum has no room for ' + totalParticipants + ' more visitors on this day');
                }
                List<MuseumSlot> museumDay = availabilityAccess.museumSlots(museumIds, request.visitDate, museumTiming);
                if (totalMinutes > museumTiming.longVisit) {
                    // a long visit: the classes chose when to arrive, the entries are the first with room, at least the gap after the arrival
                    List<String> assigned = assignEntries(museumDay, request.groups, minutesBefore, museumTiming);
                    for (Integer i = 0; i < request.groups.size(); i++) {
                        request.groups[i].museumTime = assigned[i];
                    }
                }
                checkMuseumEntries(museumDay, request.groups, '00:00', '23:59', museumTiming);
                for (GroupInput groupInput : request.groups) {
                    List<String> entryParts = groupInput.museumTime.split(':');
                    Datetime entry = Datetime.newInstance(
                        request.visitDate,
                        Time.newInstance(Integer.valueOf(entryParts[0]), Integer.valueOf(entryParts[1]), 0, 0)
                    );
                    classStarts.add(entry.addMinutes(-minutesBefore));
                }
            }
            // the visit starts with the first class and ends with the last
            Datetime visitStart = classStarts[0];
            Datetime visitEnd = classStarts[0].addMinutes(totalMinutes);
            for (Datetime classStart : classStarts) {
                visitStart = classStart < visitStart ? classStart : visitStart;
                visitEnd = classStart.addMinutes(totalMinutes) > visitEnd ? classStart.addMinutes(totalMinutes) : visitEnd;
            }

            Account account = currentUserAccount();
            Set<String> accountAudiences = audiencesOf(account);
            if (!accountAudiences.isEmpty() && !accountAudiences.contains(request.audienceCategory)) {
                throw new yvEduException('The audience is not one of the audiences of this customer');
            }
            Id pricebookId = pricebookOverride != null ? pricebookOverride : (account == null ? null : account.Price_Book__c);
            if (pricebookId == null) {
                throw new yvEduException('The customer has no price book');
            }

            List<OpportunityLineItem> lines = buildLines(items, pricebookId);

            Opportunity order = new Opportunity(
                Name = orderName(account, request.visitDate),
                StageName = ORDER_STAGE,
                CloseDate = request.visitDate,
                AccountId = account == null ? null : account.Id,
                Pricebook2Id = pricebookId,
                Audience_Category__c = request.audienceCategory,
                NumberOfGroups__c = request.groups.size(),
                NumberOfParticipants__c = totalParticipants,
                Visit_Start_Date__c = visitStart,
                Visit_End_Date__c = totalMinutes > 0 ? visitEnd : null
            );
            if (!String.isBlank(request.instructionLanguage)) {
                order.InstructionLanguage__c = request.instructionLanguage;
            }
            if (request.activityDaySubjectId != null) {
                // Read in user mode: a subject the portal user cannot see is rejected, not saved
                if ([SELECT COUNT() FROM Activity_Day_Subject__c WHERE Id = :request.activityDaySubjectId WITH USER_MODE] == 0) {
                    throw new yvEduException('The activity day subject is not available');
                }
                order.Activity_Day_Subject__c = request.activityDaySubjectId;
            }
            Schema.RecordTypeInfo orderType = Schema.SObjectType.Opportunity.getRecordTypeInfosByName().get(ORDER_RECORD_TYPE_NAME);
            if (orderType != null) {
                order.RecordTypeId = orderType.getRecordTypeId();
            }
            Database.insert(order, AccessLevel.USER_MODE);

            for (OpportunityLineItem line : lines) {
                line.OpportunityId = order.Id;
            }
            new SystemAccess().insertLines(lines);

            List<B25__Group__c> groups = new List<B25__Group__c>();
            for (GroupInput groupInput : request.groups) {
                groups.add(
                    new B25__Group__c(Name = groupInput.name.trim(), Opportunity__c = order.Id, Number_Participants__c = groupInput.participants)
                );
            }
            Database.insert(groups, AccessLevel.USER_MODE);

            // GoMeddo reservations: for every class, every step of its programme that has a resource, at the time of that step
            // (the museum has its own reservations, per class and entry). The museum is the only one that stops the order.
            if (!skipReservations) {
                SystemAccess reservationAccess = new SystemAccess();
                List<Integer> sizes = new List<Integer>();
                for (GroupInput groupInput : request.groups) {
                    sizes.add(groupInput.participants);
                }
                Boolean anyReservation = false;
                Integer offset = 0;
                for (ProgrammeStep step : programme) {
                    if (!step.museum && step.minutes > 0 && (step.resourceId != null || step.typeId != null)) {
                        List<Datetime> starts = new List<Datetime>();
                        for (Datetime classStart : classStarts) {
                            starts.add(classStart.addMinutes(offset));
                        }
                        List<Id> resourceIds = new List<Id>();
                        if (step.resourceId != null) {
                            for (Integer i = 0; i < groups.size(); i++) {
                                resourceIds.add(step.resourceId);
                            }
                        } else {
                            resourceIds = availabilityAccess.assignPool(step.typeId, step.minutes, sizes, starts);
                        }
                        reservationAccess.createClassReservations(order, groups, resourceIds, starts, step.minutes);
                        anyReservation = true;
                    }
                    offset += step.minutes;
                }
                if (!museumIds.isEmpty()) {
                    List<String> entryTimes = new List<String>();
                    for (GroupInput groupInput : request.groups) {
                        entryTimes.add(groupInput.museumTime);
                    }
                    reservationAccess.createMuseumReservations(order, groups, entryTimes, museumIds, request.visitDate, museumTiming.hold);
                }
                if (anyReservation || !museumIds.isEmpty()) {
                    reservationAccess.moveToWaitingForApproval(order.Id);
                }
            }

            // Last, so the flow it fires finds the visit start and end, the price book and the order lines already saved
            if (!skipConnectedBundle) {
                SystemAccess access = new SystemAccess();
                for (Bundle__c bundle : bundles) {
                    access.ensureConnectedBundle(order.Id, bundle.Id, (order.Name + ' - ' + bundle.Name).abbreviate(CONNECTED_BUNDLE_NAME_LENGTH));
                }
            }

            if (!museumIds.isEmpty()) {
                for (GroupInput groupInput : request.groups) {
                    result.museumEntries.add(groupInput.museumTime);
                }
            }
            result.opportunityId = order.Id;
            return result;
        } catch (Exception e) {
            Database.rollback(savepoint);
            System.debug(LoggingLevel.ERROR, 'yvEduNewReservationController.submitOrder failed: ' + e.getMessage() + ' | ' + e.getStackTraceString());
            result.opportunityId = null;
            result.error = userMessage(e);
            return result;
        }
    }

    private static Integer toMinutes(String hhmm) {
        List<String> parts = hhmm.split(':');
        return Integer.valueOf(parts[0]) * 60 + Integer.valueOf(parts[1]);
    }

    /**
     * Long visit: the museum entry of every class, in the order of the classes. A class gets the first entry with room for it
     * that is at least the gap after its arrival (and at least the programme before the museum, so that this programme does
     * not start before the arrival); the classes before it have already taken their seats. The page works the same way.
     */
    @TestVisible
    private static List<String> assignEntries(List<MuseumSlot> slots, List<GroupInput> groups, Integer minutesBefore, MuseumTiming timing) {
        List<String> entries = new List<String>();
        Map<String, Integer> taken = new Map<String, Integer>();
        for (GroupInput groupInput : groups) {
            if (String.isBlank(groupInput.arrivalTime) || !Pattern.matches('([01][0-9]|2[0-3]):[0-5][0-9]', groupInput.arrivalTime)) {
                throw new yvEduException('Choose an arrival time for every class');
            }
            Integer earliest = toMinutes(groupInput.arrivalTime) + Math.max(timing.gap, minutesBefore);
            String chosen = null;
            for (MuseumSlot slot : slots) {
                Integer already = taken.containsKey(slot.startTime) ? taken.get(slot.startTime) : 0;
                if (toMinutes(slot.startTime) >= earliest && slot.free - already >= groupInput.participants) {
                    chosen = slot.startTime;
                    taken.put(slot.startTime, already + groupInput.participants);
                    break;
                }
            }
            if (chosen == null) {
                throw new yvEduException('The museum has no entry for the arrival at ' + groupInput.arrivalTime);
            }
            entries.add(chosen);
        }
        return entries;
    }

    /** The pieces ("HH:mm", one step each) an entry at the given time holds: with step 10 and hold 30, 08:10 holds 08:10, 08:20 and 08:30. */
    private static List<String> museumPieces(String entryTime, MuseumTiming timing) {
        List<String> parts = entryTime.split(':');
        Integer minute = Integer.valueOf(parts[0]) * 60 + Integer.valueOf(parts[1]);
        List<String> pieces = new List<String>();
        // the seats are counted in the piece the class enters in (the stay, timing.hold, does not take seats of the later pieces)
        for (Integer offset = 0; offset < timing.step; offset += timing.step) {
            Integer piece = minute + offset;
            pieces.add(String.valueOf(piece / 60).leftPad(2, '0') + ':' + String.valueOf(Math.mod(piece, 60)).leftPad(2, '0'));
        }
        return pieces;
    }

    /**
     * Every class needs an entry time whose pieces are all open, and all the classes together (also those that enter at
     * different times but overlap) must fit in the seats left in every piece.
     */
    @TestVisible
    private static void checkMuseumEntries(
        List<MuseumSlot> slots,
        List<GroupInput> groups,
        String firstEntry,
        String lastEntry,
        MuseumTiming timing
    ) {
        Map<String, Integer> free = new Map<String, Integer>();
        for (MuseumSlot slot : slots) {
            free.put(slot.startTime, slot.free);
        }
        Map<String, Integer> wanted = new Map<String, Integer>();
        for (GroupInput groupInput : groups) {
            if (String.isBlank(groupInput.museumTime) || !Pattern.matches('([01][0-9]|2[0-3]):[0-5][0-9]', groupInput.museumTime)) {
                throw new yvEduException('Choose a museum entry time for every class');
            }
            if (groupInput.museumTime < firstEntry || groupInput.museumTime > lastEntry) {
                throw new yvEduException('The museum entry must be between ' + firstEntry + ' and ' + lastEntry);
            }
            for (String piece : museumPieces(groupInput.museumTime, timing)) {
                if (!free.containsKey(piece)) {
                    throw new yvEduException('The museum is not open for the entry at ' + groupInput.museumTime);
                }
                Integer already = wanted.containsKey(piece) ? wanted.get(piece) : 0;
                wanted.put(piece, already + groupInput.participants);
            }
        }
        for (String piece : wanted.keySet()) {
            if (wanted.get(piece) > free.get(piece)) {
                throw new yvEduException('The museum has no room for these classes at ' + piece + ' any more');
            }
        }
    }

    private static void validate(OrderRequest request) {
        if (request == null || String.isBlank(request.audienceCategory)) {
            throw new yvEduException('The audience is required');
        }
        if (request.bundles == null || request.bundles.isEmpty()) {
            throw new yvEduException('Choose at least one bundle');
        }
        Set<Id> seenBundles = new Set<Id>();
        Set<Id> seenItems = new Set<Id>();
        for (BundleChoice choice : request.bundles) {
            if (choice == null || choice.bundleId == null || !seenBundles.add(choice.bundleId)) {
                throw new yvEduException('Each bundle can be chosen only once');
            }
            if (choice.bundleItemIds == null || choice.bundleItemIds.isEmpty()) {
                throw new yvEduException('Choose at least one item in every bundle');
            }
            for (Id itemId : choice.bundleItemIds) {
                if (!seenItems.add(itemId)) {
                    throw new yvEduException('An item was chosen twice');
                }
            }
        }
        if (request.groups == null || request.groups.isEmpty()) {
            throw new yvEduException('Add at least one group');
        }
        if (request.visitDate == null || request.visitDate < Date.today() || request.visitDate > Date.today().addMonths(HORIZON_MONTHS)) {
            throw new yvEduException('Choose a visit date within the next ' + HORIZON_MONTHS + ' months');
        }
        if (!String.isBlank(request.visitTime) && !Pattern.matches('([01][0-9]|2[0-3]):[0-5][0-9]', request.visitTime)) {
            throw new yvEduException('Choose a visit time');
        }
        if (!String.isBlank(request.instructionLanguage)) {
            Boolean known = false;
            for (Option option : getInstructionLanguages()) {
                known = known || option.value == request.instructionLanguage;
            }
            if (!known) {
                throw new yvEduException('The instruction language is not one of the allowed values');
            }
        }
        for (GroupInput groupInput : request.groups) {
            if (String.isBlank(groupInput.name)) {
                throw new yvEduException('Every group needs a name');
            }
            if (groupInput.participants == null || groupInput.participants < 1 || groupInput.participants > maxPerGroup()) {
                throw new yvEduException('A group must have between 1 and ' + maxPerGroup() + ' participants');
            }
        }
    }

    private static List<OpportunityLineItem> buildLines(List<Bundle_Item__c> items, Id pricebookId) {
        Set<Id> productIds = new Set<Id>();
        Set<Id> itemIds = new Set<Id>();
        for (Bundle_Item__c item : items) {
            productIds.add(item.Product__c);
            itemIds.add(item.Id);
        }
        // The queries are kept out of the for statements: a SOQL for-loop with a bind variable can fail at runtime in this org
        List<PricebookEntry> entries = [
            SELECT Id, Product2Id, UnitPrice
            FROM PricebookEntry
            WHERE Pricebook2Id = :pricebookId AND Product2Id IN :productIds AND IsActive = TRUE
            WITH USER_MODE
        ];
        Map<Id, PricebookEntry> entryByProduct = new Map<Id, PricebookEntry>();
        for (PricebookEntry entry : entries) {
            entryByProduct.put(entry.Product2Id, entry);
        }
        List<Bundle_Item_Entry__c> bundlePrices = [
            SELECT Bundle_Item__c, Price__c
            FROM Bundle_Item_Entry__c
            WHERE Bundle_Item__c IN :itemIds AND Price_Book__c = :pricebookId AND Price__c != NULL
            WITH USER_MODE
        ];
        Map<Id, Decimal> priceByItem = new Map<Id, Decimal>();
        for (Bundle_Item_Entry__c bundlePrice : bundlePrices) {
            priceByItem.put(bundlePrice.Bundle_Item__c, bundlePrice.Price__c);
        }

        List<OpportunityLineItem> lines = new List<OpportunityLineItem>();
        for (Bundle_Item__c item : items) {
            PricebookEntry entry = entryByProduct.get(item.Product__c);
            if (entry == null) {
                throw new yvEduException(missingEntryMessage(item.Product__c, pricebookId));
            }
            lines.add(
                new OpportunityLineItem(
                    PricebookEntryId = entry.Id,
                    Bundle_Item__c = item.Id,
                    Quantity = item.Quantity__c == null || item.Quantity__c <= 0 ? 1 : item.Quantity__c,
                    UnitPrice = priceByItem.containsKey(item.Id) ? priceByItem.get(item.Id) : entry.UnitPrice
                )
            );
        }
        return lines;
    }

    /** Names the product and the price book, so the data gap can be fixed (only used on the error path). */
    private static String missingEntryMessage(Id productId, Id pricebookId) {
        List<Product2> products = [SELECT Name FROM Product2 WHERE Id = :productId];
        List<Pricebook2> pricebooks = [SELECT Name FROM Pricebook2 WHERE Id = :pricebookId];
        return 'The product "' + (products.isEmpty() ? String.valueOf(productId) : products[0].Name) + '" has no active price book entry in the price book "'
            + (pricebooks.isEmpty() ? String.valueOf(pricebookId) : pricebooks[0].Name) + '"';
    }

    /** What the page may show: our own messages (yvEduException) as they are, anything else as a generic text. The details are in the debug log. */
    private static String userMessage(Exception e) {
        return e instanceof yvEduException ? e.getMessage() : 'Something went wrong. Please try again, or contact us if it continues.';
    }

    /** The audiences of an account (a multi-select picklist, values separated by semicolons); empty when none is set. */
    @TestVisible
    private static Set<String> audiencesOf(Account account) {
        Set<String> audiences = new Set<String>();
        if (account != null && String.isNotBlank(account.Audience_Category__c)) {
            for (String value : account.Audience_Category__c.split(';')) {
                audiences.add(value.trim());
            }
        }
        return audiences;
    }

    /** The order is named after the customer and the visit date, without the bundles (they are on the order lines). */
    private static String orderName(Account account, Date visitDate) {
        String customer = account == null || String.isBlank(account.Name) ? 'Online order' : account.Name;
        return (customer + ' - ' + visitDate.format()).abbreviate(OPPORTUNITY_NAME_LENGTH);
    }

    /** The portal user's own account, with the price book that applies to it (Account.Price_Book__c). */
    private static Account currentUserAccount() {
        List<User> users = [SELECT Contact.AccountId FROM User WHERE Id = :UserInfo.getUserId() LIMIT 1];
        if (users.isEmpty() || users[0].Contact == null || users[0].Contact.AccountId == null) {
            return null;
        }
        // Sharing applies (the user only sees their own account) but not field-level security: the price book is
        // only used on the server to price the order and is never returned to the page.
        return [SELECT Id, Name, Price_Book__c, Audience_Category__c FROM Account WHERE Id = :users[0].Contact.AccountId LIMIT 1];
    }

    private static ActivityCard buildCard(Bundle__c bundle, Boolean useEnglishNames) {
        ActivityCard card = new ActivityCard();
        card.id = bundle.Id;
        card.name = bundle.Name;

        for (Bundle_Item__c item : bundle.Bundle_Items__r) {
            ItemInfo info = new ItemInfo();
            info.id = item.Id;
            info.name = itemDisplayName(item, useEnglishNames);

            if (item.Product__r != null && item.Product__r.DurationOfHours__c != null) {
                Decimal quantity = item.Quantity__c == null ? 1 : item.Quantity__c;
                info.hours = item.Product__r.DurationOfHours__c * quantity;
                card.totalHours = (card.totalHours == null ? 0 : card.totalHours) + info.hours;
            }
            card.items.add(info);
        }
        return card;
    }

    private static String itemDisplayName(Bundle_Item__c item, Boolean useEnglishNames) {
        if (item.Product__r == null) {
            return item.Name;
        }
        if (useEnglishNames && String.isNotBlank(item.Product__r.NameEn__c)) {
            return item.Product__r.NameEn__c;
        }
        return item.Product__r.Name;
    }
}