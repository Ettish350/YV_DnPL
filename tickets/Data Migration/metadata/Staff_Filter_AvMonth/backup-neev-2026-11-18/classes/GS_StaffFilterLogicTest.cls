@IsTest
private class GS_StaffFilterLogicTest {

    // Fixed time (09:00 to 10:00 UTC, 11:00 to 12:00 in Jerusalem): never crosses local midnight
    private static final Datetime START_DT = Datetime.newInstanceGmt(Date.today().addDays(10), Time.newInstance(9, 0, 0, 0));
    private static final Datetime END_DT = START_DT.addHours(1);

    @TestSetup
    static void setup() {
        // GoMeddo runs the customizer named in this system setting
        insert new B25__System_Setting__c(
            Name = 'Reservation Form Customizer Class',
            B25__String_Value__c = 'GS_StaffFilterLogic'
        );

        B25__Reservation_Type__c resType = new B25__Reservation_Type__c(Name = 'Type 1');
        insert resType;

        Skill__c typeSkill = new Skill__c(Name = 'Type Skill');
        Skill__c hsSkill = new Skill__c(Name = 'High School', Audience_Category__c = 'High_School');
        insert new List<Skill__c>{ typeSkill, hsSkill };

        insert new Reservation_Type_Skill__c(Reservation_Type__c = resType.Id, Skill__c = typeSkill.Id);

        // Staff names start with "T " so the tests can search on a shared term
        B25__Staff__c both = staff('T Both', true, 'Hebrew;Russian');
        B25__Staff__c hsOnly = staff('T HS only', true, 'Hebrew');
        B25__Staff__c typeOnly = staff('T Type only', true, 'Hebrew;Russian');
        B25__Staff__c inactive = staff('T Inactive', false, 'Hebrew;Russian');
        B25__Staff__c noSkill = staff('T None', true, 'Russian');
        B25__Staff__c noAvail = staff('T No availability', true, 'Hebrew');
        B25__Staff__c away = staff('T Away', true, 'Hebrew');
        B25__Staff__c busy = staff('T Busy', true, 'Hebrew');
        B25__Staff__c softBusy = staff('T Soft busy', true, 'Hebrew');
        insert new List<B25__Staff__c>{ both, hsOnly, typeOnly, inactive, noSkill, noAvail, away, busy, softBusy };

        insert new List<Staff_Skill__c>{
            new Staff_Skill__c(Staff__c = both.Id, Skill__c = typeSkill.Id),
            new Staff_Skill__c(Staff__c = both.Id, Skill__c = hsSkill.Id),
            new Staff_Skill__c(Staff__c = hsOnly.Id, Skill__c = hsSkill.Id),
            new Staff_Skill__c(Staff__c = typeOnly.Id, Skill__c = typeSkill.Id),
            new Staff_Skill__c(Staff__c = inactive.Id, Skill__c = typeSkill.Id),
            new Staff_Skill__c(Staff__c = inactive.Id, Skill__c = hsSkill.Id)
        };

        // Everyone except "T No availability" is available every day, all day
        List<B25__Availability__c> availability = new List<B25__Availability__c>();
        for (B25__Staff__c s : new List<B25__Staff__c>{ both, hsOnly, typeOnly, inactive, noSkill, away, busy, softBusy }) {
            availability.add(availability(s.Id, false));
        }
        // "T Away" is also marked unavailable all day
        availability.add(availability(away.Id, true));
        insert availability;

        // Existing bookings at the same time: "T Busy" under a blocking status, "T Soft busy" under a
        // status that allows double booking
        B25__Reservation_Status__c blocking = new B25__Reservation_Status__c(
            Name = 'Test approved', B25__AllowDoubleBooking__c = false, B25__Is_Active__c = true);
        B25__Reservation_Status__c soft = new B25__Reservation_Status__c(
            Name = 'Test pending', B25__AllowDoubleBooking__c = true, B25__Is_Active__c = true);
        insert new List<B25__Reservation_Status__c>{ blocking, soft };
        insert new List<B25__Reservation__c>{
            new B25__Reservation__c(B25__Start__c = START_DT, B25__End__c = END_DT,
                B25__Staff__c = busy.Id, B25__Status__c = blocking.Id),
            new B25__Reservation__c(B25__Start__c = START_DT, B25__End__c = END_DT,
                B25__Staff__c = softBusy.Id, B25__Status__c = soft.Id)
        };
    }

    private static B25__Staff__c staff(String name, Boolean active, String languages) {
        return new B25__Staff__c(Name = name, B25__Is_Active__c = active, Language__c = languages);
    }

    private static B25__Availability__c availability(Id staffId, Boolean unavailable) {
        return new B25__Availability__c(
            B25__Staff__c = staffId,
            B25__Days__c = 'Sunday;Monday;Tuesday;Wednesday;Thursday;Friday;Saturday',
            B25__Start_Hour__c = 0, B25__Start_Minute__c = 0,
            B25__End_Hour__c = 24, B25__End_Minute__c = 0,
            B25__Unavailability__c = unavailable
        );
    }

    private static Opportunity makeOrder(String audience, String language) {
        String stage = Opportunity.StageName.getDescribe().getPicklistValues()[0].getValue();
        Opportunity opp = new Opportunity(
            Name = 'Test order',
            StageName = stage,
            CloseDate = Date.today().addDays(30),
            Audience_Category__c = audience,
            InstructionLanguage__c = language
        );
        insert opp;
        return opp;
    }

    private static Id typeId() {
        return [SELECT Id FROM B25__Reservation_Type__c WHERE Name = 'Type 1' LIMIT 1].Id;
    }

    private static List<String> search(Id resTypeId, Id oppId, String term) {
        B25__Reservation__c reservation = new B25__Reservation__c(B25__Start__c = START_DT, B25__End__c = END_DT);
        if (resTypeId != null) {
            reservation.B25__Reservation_Type__c = resTypeId;
        }
        if (oppId != null) {
            reservation.B25__Opportunity__c = oppId;
        }
        B25.Test_FormHelper.TestFormContext formContext = new B25.Test_FormHelper.TestFormContext();
        formContext.reservation = reservation;

        B25.Test_FormHelper.TestSearchContext searchContext = new B25.Test_FormHelper.TestSearchContext();
        searchContext.toSearchSObjectType = B25__Staff__c.SObjectType;
        searchContext.parentSObjectType = B25__Reservation__c.SObjectType;
        searchContext.searchTerm = term;
        searchContext.fieldName = 'B25__Staff__c';

        B25.SearchResultCollection results = B25.Test_FormHelper.runLookupOverrideSearch(formContext, searchContext);
        List<String> labels = new List<String>();
        for (B25.SearchResult r : results.getSearchResults()) {
            labels.add(r.getLabel());
        }
        labels.sort();
        return labels;
    }

    // Case 1: order audience High_School, no type skills: only staff holding the High School skill
    @IsTest
    static void audienceSkillFilters() {
        Opportunity opp = makeOrder('High_School', null);
        System.assertEquals(new List<String>{ 'T Both', 'T HS only' }, search(null, opp.Id, 'T '));
    }

    // Case 2: order language Russian, type with its skill: certified staff who speak Russian
    @IsTest
    static void languageFilters() {
        Opportunity opp = makeOrder(null, 'Russian');
        System.assertEquals(new List<String>{ 'T Both', 'T Type only' }, search(typeId(), opp.Id, 'T '));
    }

    // Case 3: type skill plus High School audience: only the staff member holding both
    @IsTest
    static void mustHoldAllRequiredSkills() {
        Opportunity opp = makeOrder('High_School', null);
        System.assertEquals(new List<String>{ 'T Both' }, search(typeId(), opp.Id, 'T '));
    }

    // Case 4: inactive staff are not offered (T Inactive holds both skills)
    @IsTest
    static void inactiveStaffExcluded() {
        Opportunity opp = makeOrder('High_School', null);
        System.assert(!search(typeId(), opp.Id, 'T ').contains('T Inactive'), 'Inactive staff must not appear');
    }

    // Case 5: nothing required and no language: every active, available, free staff member is offered
    @IsTest
    static void noRequirementsShowsAllAvailable() {
        System.assertEquals(
            new List<String>{ 'T Both', 'T HS only', 'T None', 'T Soft busy', 'T Type only' },
            search(null, null, 'T ')
        );
    }

    // Case 6: one row per person (T Both holds two matching skills) and the search term narrows by name
    @IsTest
    static void oneRowPerPersonAndSearchTerm() {
        Opportunity opp = makeOrder('High_School', null);
        System.assertEquals(1, search(typeId(), opp.Id, 'T ').size(), 'One row, not one per skill');
        System.assertEquals(new List<String>{ 'T HS only' }, search(null, null, 'HS'), 'Search term narrows by name');
    }

    // Case 7: no availability record means not offered
    @IsTest
    static void noAvailabilityExcluded() {
        System.assert(!search(null, null, 'T ').contains('T No availability'), 'No availability record, not offered');
    }

    // Case 8: an Unavailability record covering the time blocks an otherwise available person
    @IsTest
    static void unavailabilityBlocks() {
        System.assert(!search(null, null, 'T ').contains('T Away'), 'Marked unavailable, not offered');
    }

    // Case 9: already booked at the same time under a blocking status
    @IsTest
    static void bookedUnderBlockingStatusExcluded() {
        System.assert(!search(null, null, 'T ').contains('T Busy'), 'Booked at the same time, not offered');
    }

    // Case 10: a booking under a status that allows double booking does not block
    @IsTest
    static void bookedUnderDoubleBookingStatusStillOffered() {
        System.assert(search(null, null, 'T ').contains('T Soft busy'), 'Double booking allowed, still offered');
    }
}