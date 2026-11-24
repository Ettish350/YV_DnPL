@IsTest
private class A13_AvailabilityMonthBuilderTest {

    // June 2030: 1st is a Saturday, 2nd a Sunday, 5th a Wednesday, 7th a Friday
    private static final Integer TEST_YEAR = 2030;
    private static final Integer TEST_MONTH = 6;

    private class HebcalMock implements HttpCalloutMock {
        Integer status;
        HebcalMock(Integer status) { this.status = status; }
        public HttpResponse respond(HttpRequest req) {
            System.assert(req.getEndpoint().startsWith('callout:NCHebcalAPI/hebcal?'), req.getEndpoint());
            HttpResponse res = new HttpResponse();
            res.setStatusCode(status);
            res.setBody('{"items":['
                + '{"title":"Shavuot","date":"2030-06-05","category":"holiday","subcat":"major","yomtov":true,"hebrew":"שבועות"},'
                + '{"title":"Erev Shavuot","date":"2030-06-04","category":"holiday","subcat":"major","hebrew":"ערב שבועות"},'
                + '{"title":"Rosh Chodesh Tamuz","date":"2030-06-03","category":"roshchodesh","hebrew":"ראש חודש תמוז"}'
                + ']}');
            return res;
        }
    }

    @TestSetup
    static void setup() {
        B25__Staff__c obligated = new B25__Staff__c(Name = 'T Obligated', B25__Is_Active__c = true);
        B25__Staff__c free = new B25__Staff__c(Name = 'T Free', B25__Is_Active__c = true);
        B25__Staff__c ended = new B25__Staff__c(Name = 'T Ended', B25__Is_Active__c = true);
        B25__Staff__c built = new B25__Staff__c(Name = 'T Built', B25__Is_Active__c = true);
        B25__Staff__c inactive = new B25__Staff__c(Name = 'T Inactive', B25__Is_Active__c = false);
        insert new List<B25__Staff__c>{ obligated, free, ended, built, inactive };

        String week = 'Sunday;Monday;Tuesday;Wednesday;Thursday';
        insert new List<B25__Availability__c>{
            // Sun to Thu 8:00 to 16:00
            new B25__Availability__c(B25__Staff__c = obligated.Id, B25__Days__c = week,
                B25__Start_Hour__c = 8, B25__Start_Minute__c = 0, B25__End_Hour__c = 16, B25__End_Minute__c = 0),
            // Ends on 15 June, 9:00 to 12:30
            new B25__Availability__c(B25__Staff__c = ended.Id, B25__Days__c = week,
                B25__End_Date__c = Date.newInstance(TEST_YEAR, TEST_MONTH, 15),
                B25__Start_Hour__c = 9, B25__Start_Minute__c = 0, B25__End_Hour__c = 12, B25__End_Minute__c = 30),
            // An unavailability is not a commitment
            new B25__Availability__c(B25__Staff__c = free.Id, B25__Days__c = week, B25__Unavailability__c = true,
                B25__Start_Hour__c = 0, B25__Start_Minute__c = 0, B25__End_Hour__c = 24, B25__End_Minute__c = 0)
        };

        insert new List<Special_Day__c>{
            new Special_Day__c(Name = 'Closure', Date__c = Date.newInstance(TEST_YEAR, TEST_MONTH, 10),
                Day_Type__c = 'חסום', Note_He__c = 'סגירת אתר לבדיקה'),
            new Special_Day__c(Name = 'Note day', Date__c = Date.newInstance(TEST_YEAR, TEST_MONTH, 11),
                Day_Type__c = 'הערה', Note_He__c = 'יום הערה')
        };

        insert new Availability_Month__c(Staff__c = built.Id, Year__c = String.valueOf(TEST_YEAR), Month__c = String.valueOf(TEST_MONTH));
    }

    private static Map<String, Availability_Month__c> monthsByName() {
        Map<String, Availability_Month__c> byName = new Map<String, Availability_Month__c>();
        String fields = 'Staff__r.Name, Year__c, Month__c, Month_Key__c';
        for (Integer d = 1; d <= 31; d++) {
            String p = 'Day_' + String.valueOf(d).leftPad(2, '0');
            fields += ', ' + p + '__c, ' + p + '_Start__c, ' + p + '_Duration__c, ' + p + '_Note__c';
        }
        for (Availability_Month__c am : (List<Availability_Month__c>) Database.query('SELECT ' + fields + ' FROM Availability_Month__c')) {
            byName.put(am.Staff__r.Name, am);
        }
        return byName;
    }

    private static A13_AvailabilityMonthBuilder.Result runBuild(Integer status) {
        Test.setMock(HttpCalloutMock.class, new HebcalMock(status));
        Test.startTest();
        A13_AvailabilityMonthBuilder.Result r = A13_AvailabilityMonthBuilder.build(TEST_YEAR, TEST_MONTH);
        Test.stopTest();
        return r;
    }

    // Case 1: obligated weekdays, empty Friday, Saturday as Shabbat
    @IsTest
    static void commitmentShabbatAndFriday() {
        A13_AvailabilityMonthBuilder.Result r = runBuild(200);
        System.assertEquals(3, r.created, r.message);
        Availability_Month__c am = monthsByName().get('T Obligated');
        System.assertEquals('מחוייב', am.Day_02__c, 'Sunday is obligated');
        System.assertEquals(Time.newInstance(8, 0, 0, 0), am.Day_02_Start__c);
        System.assertEquals('8', am.Day_02_Duration__c);
        System.assertEquals(null, am.Day_07__c, 'Friday stays empty');
        System.assertEquals('שבת/חג', am.Day_01__c, 'Saturday');
        System.assertEquals('שבת', am.Day_01_Note__c);
        System.assert(am.Month_Key__c != null && am.Month_Key__c.endsWith('-2030-06'), am.Month_Key__c);
    }

    // Case 2: a blocked Special Day wins over the commitment; a note day only adds its note
    @IsTest
    static void specialDays() {
        runBuild(200);
        Availability_Month__c am = monthsByName().get('T Obligated');
        System.assertEquals('יום חסום', am.Day_10__c);
        System.assertEquals(null, am.Day_10_Start__c, 'Blocked day has no hours');
        System.assertEquals('סגירת אתר לבדיקה', am.Day_10_Note__c);
        System.assertEquals('מחוייב', am.Day_11__c, 'Note day keeps the commitment');
        System.assertEquals('יום הערה', am.Day_11_Note__c);
    }

    // Case 3: a Hebcal no-work holiday; erev and Rosh Chodesh are ignored
    @IsTest
    static void hebcalHoliday() {
        runBuild(200);
        Availability_Month__c am = monthsByName().get('T Obligated');
        System.assertEquals('שבת/חג', am.Day_05__c);
        System.assertEquals('שבועות', am.Day_05_Note__c);
        System.assertEquals('מחוייב', am.Day_04__c, 'Erev is a normal day');
        System.assertEquals(null, am.Day_04_Note__c);
        System.assertEquals('מחוייב', am.Day_03__c, 'Rosh Chodesh is a normal day');
    }

    // Case 4: an existing month is skipped; an inactive guide gets nothing; a guide with no commitment gets an empty month
    @IsTest
    static void skipsAndInactive() {
        A13_AvailabilityMonthBuilder.Result r = runBuild(200);
        System.assertEquals(1, r.skipped);
        Map<String, Availability_Month__c> byName = monthsByName();
        System.assertEquals(4, byName.size(), 'Three new plus the existing one');
        System.assert(!byName.containsKey('T Inactive'));
        System.assertEquals(null, byName.get('T Free').Day_02__c, 'No commitment: empty weekday');
        A13_AvailabilityMonthBuilder.Stats s = A13_AvailabilityMonthBuilder.getStats(TEST_YEAR, TEST_MONTH);
        System.assertEquals(4, s.activeGuides);
        System.assertEquals(4, s.existing);
        System.assertEquals(0, s.missing);
    }

    // Case 5: a commitment ending mid-month; duration rounded to half hours
    @IsTest
    static void commitmentEndsMidMonth() {
        runBuild(200);
        Availability_Month__c am = monthsByName().get('T Ended');
        System.assertEquals('מחוייב', am.Day_12__c);
        System.assertEquals('3.5', am.Day_12_Duration__c);
        System.assertEquals(null, am.Day_16__c, 'After the end date');
    }

    // Case 6: Hebcal fails, nothing is created
    @IsTest
    static void hebcalFails() {
        A13_AvailabilityMonthBuilder.Result r = runBuild(500);
        System.assertEquals(false, r.success);
        System.assertEquals(0, r.created);
        System.assert(r.message.startsWith('Hebcal'), r.message);
        System.assertEquals(1, [SELECT COUNT() FROM Availability_Month__c]);
    }

    @IsTest
    static void durationLimits() {
        System.assertEquals('9', A13_AvailabilityMonthBuilder.durationValue(10 * 60));
        System.assertEquals('3', A13_AvailabilityMonthBuilder.durationValue(60));
        System.assertEquals('4.5', A13_AvailabilityMonthBuilder.durationValue(270));
    }

    @IsTest
    static void badMonth() {
        try {
            A13_AvailabilityMonthBuilder.getStats(TEST_YEAR, 13);
            System.assert(false, 'Expected an error');
        } catch (AuraHandledException e) {
            System.assert(true);
        }
    }

    @IsTest
    static void jobRunsAndSchedules() {
        Test.setMock(HttpCalloutMock.class, new HebcalMock(200));
        Test.startTest();
        System.enqueueJob(new A13_AvailabilityMonthJob(TEST_YEAR, TEST_MONTH));
        String jobId = System.schedule('A13-01 test', '0 0 6 15 * ?', new A13_AvailabilityMonthSchedule());
        Test.stopTest();
        // The schedule may also build next month when it fires at stopTest; count June only
        System.assertEquals(4, [SELECT COUNT() FROM Availability_Month__c WHERE Year__c = '2030' AND Month__c = '6']);
        System.assertNotEquals(null, jobId);
    }
}
