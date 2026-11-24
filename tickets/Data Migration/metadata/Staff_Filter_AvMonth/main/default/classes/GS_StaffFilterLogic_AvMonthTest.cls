@IsTest
private class GS_StaffFilterLogic_AvMonthTest {

    // 11:00 to 12:00 in the running user's time zone, ten days ahead: never crosses local midnight
    private static final Datetime START_DT = Datetime.newInstance(Date.today().addDays(10), Time.newInstance(11, 0, 0, 0));
    private static final Datetime END_DT = START_DT.addHours(1);

    private static final String AVAILABLE = 'זמין';
    private static final String OBLIGATED = 'מחוייב';
    private static final String NOT_AVAILABLE = 'לא זמין';
    private static final String BOOKED = 'משובץ';
    private static final String HOLIDAY = 'שבת/חג';
    private static final String BLOCKED = 'יום חסום';

    @TestSetup
    static void setup() {
        // GoMeddo runs the customizer named in this system setting
        insert new B25__System_Setting__c(
            Name = 'Reservation Form Customizer Class',
            B25__String_Value__c = 'GS_StaffFilterLogic_AvMonth'
        );

        // Staff names start with "T " so the tests can search on a shared term
        List<B25__Staff__c> people = new List<B25__Staff__c>{
            staff('T Available', true),
            staff('T Obligated', true),
            staff('T Booked status', true),
            staff('T Padded month', true),
            staff('T Not available', true),
            staff('T Holiday', true),
            staff('T Blocked day', true),
            staff('T No hours', true),
            staff('T Too short', true),
            staff('T No month', true),
            staff('T Busy', true),
            staff('T Inactive', false)
        };
        insert people;

        // Every person has a commitment (GoMeddo Availability). It no longer decides availability.
        Map<String, B25__Availability__c> commitments = new Map<String, B25__Availability__c>();
        for (B25__Staff__c s : people) {
            commitments.put(s.Name, commitment(s.Id));
        }
        insert commitments.values();

        // Re-read time zones: the reservation's day and hours are local to each person
        Map<String, B25__Staff__c> byName = new Map<String, B25__Staff__c>();
        for (B25__Staff__c s : [SELECT Id, Name, B25__Time_Zone__c FROM B25__Staff__c WHERE Name LIKE 'T %']) {
            byName.put(s.Name, s);
        }

        List<Availability_Month__c> months = new List<Availability_Month__c>{
            month(byName.get('T Available'), commitments, AVAILABLE, -60, '3', false),
            month(byName.get('T Obligated'), commitments, OBLIGATED, -60, '3', false),
            month(byName.get('T Booked status'), commitments, BOOKED, -60, '3', false),
            month(byName.get('T Padded month'), commitments, AVAILABLE, -60, '3', true),
            month(byName.get('T Not available'), commitments, NOT_AVAILABLE, -60, '3', false),
            month(byName.get('T Holiday'), commitments, HOLIDAY, -60, '3', false),
            month(byName.get('T Blocked day'), commitments, BLOCKED, -60, '3', false),
            month(byName.get('T No hours'), commitments, AVAILABLE, null, null, false),
            // Starts 30 minutes after the reservation starts: does not cover it
            month(byName.get('T Too short'), commitments, AVAILABLE, 30, '3', false),
            month(byName.get('T Busy'), commitments, AVAILABLE, -60, '3', false),
            month(byName.get('T Inactive'), commitments, AVAILABLE, -60, '3', false)
            // "T No month" has no Availability Month record
        };
        insert months;

        // "T Busy" is already booked at the same time under a blocking status
        B25__Reservation_Status__c blocking = new B25__Reservation_Status__c(
            Name = 'Test approved', B25__AllowDoubleBooking__c = false, B25__Is_Active__c = true);
        B25__Reservation_Status__c overrideStatus = new B25__Reservation_Status__c(
            Name = 'Test override', B25__AllowDoubleBooking__c = false, B25__Is_Active__c = true,
            B25__Allow_Unavailable_Bookings__c = true);
        insert new List<B25__Reservation_Status__c>{ blocking, overrideStatus };
        insert new B25__Reservation__c(B25__Start__c = START_DT, B25__End__c = END_DT,
            B25__Staff__c = byName.get('T Busy').Id, B25__Status__c = blocking.Id);
    }

    private static B25__Staff__c staff(String name, Boolean active) {
        return new B25__Staff__c(Name = name, B25__Is_Active__c = active);
    }

    private static B25__Availability__c commitment(Id staffId) {
        return new B25__Availability__c(
            B25__Staff__c = staffId,
            B25__Days__c = 'Sunday;Monday;Tuesday;Wednesday;Thursday;Friday;Saturday',
            B25__Start_Hour__c = 0, B25__Start_Minute__c = 0,
            B25__End_Hour__c = 24, B25__End_Minute__c = 0,
            B25__Unavailability__c = false
        );
    }

    // One Availability Month record with the reservation's local day filled in.
    // startOffset: minutes from the reservation's local start (null = no start time).
    private static Availability_Month__c month(B25__Staff__c s, Map<String, B25__Availability__c> commitments,
            String status, Integer startOffset, String duration, Boolean padMonth) {
        String tz = String.isNotBlank(s.B25__Time_Zone__c) ? s.B25__Time_Zone__c : UserInfo.getTimeZone().getID();
        Date day = Date.valueOf(START_DT.format('yyyy-MM-dd', tz));
        Integer localStart = Integer.valueOf(START_DT.format('H', tz)) * 60 + Integer.valueOf(START_DT.format('m', tz));

        String monthText = String.valueOf(day.month());
        Availability_Month__c am = new Availability_Month__c(
            Staff__c = s.Id,
            Year__c = String.valueOf(day.year()),
            Month__c = padMonth ? monthText.leftPad(2, '0') : monthText
        );
        String prefix = 'Day_' + String.valueOf(day.day()).leftPad(2, '0');
        am.put(prefix + '__c', status);
        if (startOffset != null) {
            Integer m = Math.max(0, localStart + startOffset);
            am.put(prefix + '_Start__c', Time.newInstance(m / 60, Math.mod(m, 60), 0, 0));
        }
        if (duration != null) {
            am.put(prefix + '_Duration__c', duration);
        }
        return am;
    }

    private static List<String> search(Id statusId, String term) {
        return search(new B25__Reservation__c(B25__Start__c = START_DT, B25__End__c = END_DT), statusId, term);
    }

    private static List<String> search(B25__Reservation__c reservation, Id statusId, String term) {
        if (statusId != null) {
            reservation.B25__Status__c = statusId;
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

    // Case 1: only available, obligated and booked-status days whose hours cover the reservation pass.
    // Covers: not available, holiday, blocked day, no hours, too short, no month record, busy, inactive.
    @IsTest
    static void onlyCoveringAvailableDaysPass() {
        System.assertEquals(
            new List<String>{ 'T Available', 'T Booked status', 'T Obligated', 'T Padded month' },
            search(null, 'T ')
        );
    }

    // Case 2: a commitment alone (no Availability Month) is not enough
    @IsTest
    static void commitmentWithoutMonthExcluded() {
        System.assert(!search(null, 'T ').contains('T No month'), 'No Availability Month, not offered');
    }

    // Case 3: a status without start or duration counts as not available
    @IsTest
    static void statusWithoutHoursExcluded() {
        System.assert(!search(null, 'T ').contains('T No hours'), 'No hours, not offered');
    }

    // Case 4: step 5 still drops a person already booked at the same time
    @IsTest
    static void bookedReservationStillBlocks() {
        System.assert(!search(null, 'T ').contains('T Busy'), 'Booked at the same time, not offered');
    }

    // Case 6: the open form has only StartLocal and EndLocal (Start and End still empty): the filter still applies
    @IsTest
    static void formWithLocalTimesOnly() {
        B25__Reservation__c inForm = new B25__Reservation__c(B25__StartLocal__c = START_DT, B25__EndLocal__c = END_DT);
        System.assertEquals(
            new List<String>{ 'T Available', 'T Booked status', 'T Obligated', 'T Padded month' },
            search(inForm, null, 'T ')
        );
    }

    // Case 5: a reservation status that allows unavailable bookings skips step 4, step 5 still applies
    @IsTest
    static void overrideStatusSkipsAvailability() {
        Id overrideId = [SELECT Id FROM B25__Reservation_Status__c WHERE Name = 'Test override' LIMIT 1].Id;
        List<String> found = search(overrideId, 'T ');
        System.assert(found.contains('T No month'), 'Override status: availability not checked');
        System.assert(found.contains('T Not available'), 'Override status: availability not checked');
        System.assert(!found.contains('T Busy'), 'Override status: still not double booked');
        System.assert(!found.contains('T Inactive'), 'Inactive staff never offered');
    }
}