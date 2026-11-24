/**
 * A13-01 (DD Doc 2, chapter 13): builds the Availability Month records of a chosen month.
 * One record per active guide (B25__Staff__c), skipping guides who already have that month.
 * Each day is filled from three sources, first match wins:
 *   1. Special_Day__c of type חסום         -> יום חסום, no hours, note from the Special Day
 *   2. Saturday or a Hebcal no-work holiday -> שבת/חג, no hours, note = holiday name or "שבת"
 *   3. A weekday of the guide's commitment  -> מחוייב, start and duration from the commitment
 *      (GoMeddo Availability on the Staff, not Unavailability; duration in half hours, 3 to 9)
 *   Anything else, Friday included, stays empty for the guide to fill.
 *   A Special Day of type הערה only adds its note.
 * Hebcal is called once per run (Named Credential NCHebcalAPI). If it fails, nothing is created.
 * Runs without sharing: a build must see every guide and commitment, whoever presses Run.
 * Who may run it is controlled by access to this class and to the screen flow.
 */
public without sharing class A13_AvailabilityMonthBuilder {

    public static final String STATUS_BLOCKED = 'יום חסום';
    public static final String STATUS_SHABBAT = 'שבת/חג';
    public static final String STATUS_OBLIGATED = 'מחוייב';
    public static final String NOTE_SHABBAT = 'שבת';
    public static final String SPECIAL_BLOCKED = 'חסום';
    public static final String SPECIAL_NOTE = 'הערה';

    private static final Integer NOTE_LENGTH = 80;
    private static final Decimal MIN_HOURS = 3;
    private static final Decimal MAX_HOURS = 9;
    private static final List<String> DAYS = new List<String>{
        'Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'
    };

    public class Stats {
        @AuraEnabled public Integer activeGuides = 0;
        @AuraEnabled public Integer existing = 0;
        @AuraEnabled public Integer missing = 0;
    }

    public class Result {
        @AuraEnabled public Boolean success = false;
        @AuraEnabled public Integer created = 0;
        @AuraEnabled public Integer failed = 0;
        @AuraEnabled public Integer skipped = 0;
        @AuraEnabled public String message;
        @AuraEnabled public List<String> errors = new List<String>();
    }

    // ---------------------------------------------------------------- screen (LWC)

    @AuraEnabled
    public static Stats getStats(Integer year, Integer month) {
        checkMonth(year, month);
        Stats s = new Stats();
        s.activeGuides = [SELECT COUNT() FROM B25__Staff__c WHERE B25__Is_Active__c = true];
        Set<String> months = monthTexts(month);
        String yearText = String.valueOf(year);
        s.existing = [
            SELECT COUNT() FROM Availability_Month__c
            WHERE Year__c = :yearText AND Month__c IN :months AND Staff__r.B25__Is_Active__c = true
        ];
        s.missing = Math.max(0, s.activeGuides - s.existing);
        return s;
    }

    @AuraEnabled
    public static Result build(Integer year, Integer month) {
        checkMonth(year, month);
        return run(year, month);
    }

    // ---------------------------------------------------------------- the build

    public static Result run(Integer year, Integer month) {
        Result r = new Result();
        Date first = Date.newInstance(year, month, 1);
        Date last = first.addMonths(1).addDays(-1);

        // 1. Hebcal first: a callout must come before any DML
        Map<Date, String> holidays;
        try {
            holidays = fetchHolidays(year, month);
        } catch (Exception e) {
            r.message = 'Hebcal: ' + e.getMessage();
            return r;
        }

        // 2. Guides who still need this month
        String yearText = String.valueOf(year);
        Set<String> months = monthTexts(month);
        Set<Id> alreadyBuilt = new Set<Id>();
        for (Availability_Month__c am : [
            SELECT Staff__c FROM Availability_Month__c WHERE Year__c = :yearText AND Month__c IN :months
        ]) {
            alreadyBuilt.add(am.Staff__c);
        }
        List<B25__Staff__c> guides = new List<B25__Staff__c>();
        for (B25__Staff__c s : [SELECT Id, Name FROM B25__Staff__c WHERE B25__Is_Active__c = true ORDER BY Name]) {
            if (alreadyBuilt.contains(s.Id)) {
                r.skipped++;
            } else {
                guides.add(s);
            }
        }
        if (guides.isEmpty()) {
            r.success = true;
            r.message = 'Nothing to build';
            return r;
        }
        Set<Id> guideIds = new Map<Id, B25__Staff__c>(guides).keySet();

        // 3. Commitments that overlap the month
        Map<Id, List<B25__Availability__c>> commitments = new Map<Id, List<B25__Availability__c>>();
        for (B25__Availability__c av : [
            SELECT B25__Staff__c, B25__Days__c, B25__Start_Date__c, B25__End_Date__c,
                   B25__Start_Hour__c, B25__Start_Minute__c, B25__End_Hour__c, B25__End_Minute__c
            FROM B25__Availability__c
            WHERE B25__Staff__c IN :guideIds
            AND B25__Unavailability__c = false
            AND (B25__Start_Date__c = null OR B25__Start_Date__c <= :last)
            AND (B25__End_Date__c = null OR B25__End_Date__c >= :first)
            ORDER BY CreatedDate
        ]) {
            if (!commitments.containsKey(av.B25__Staff__c)) {
                commitments.put(av.B25__Staff__c, new List<B25__Availability__c>());
            }
            commitments.get(av.B25__Staff__c).add(av);
        }

        // 4. Special Days of the month
        Map<Date, List<Special_Day__c>> specials = new Map<Date, List<Special_Day__c>>();
        for (Special_Day__c sd : [
            SELECT Name, Date__c, Day_Type__c, toLabel(Reason__c), Note_He__c
            FROM Special_Day__c
            WHERE Date__c >= :first AND Date__c <= :last
            ORDER BY Date__c, Name
        ]) {
            if (!specials.containsKey(sd.Date__c)) {
                specials.put(sd.Date__c, new List<Special_Day__c>());
            }
            specials.get(sd.Date__c).add(sd);
        }

        // 5. Build and insert
        List<Availability_Month__c> records = new List<Availability_Month__c>();
        for (B25__Staff__c g : guides) {
            records.add(buildMonth(g.Id, year, month, holidays, specials, commitments.get(g.Id)));
        }
        Database.SaveResult[] saved = Database.insert(records, false);
        for (Integer i = 0; i < saved.size(); i++) {
            if (saved[i].isSuccess()) {
                r.created++;
            } else {
                r.failed++;
                if (r.errors.size() < 20) {
                    r.errors.add(guides[i].Name + ': ' + saved[i].getErrors()[0].getMessage());
                }
            }
        }
        r.success = r.failed == 0;
        r.message = r.created + ' created, ' + r.failed + ' failed, ' + r.skipped + ' already built';
        return r;
    }

    @TestVisible
    private static Availability_Month__c buildMonth(Id staffId, Integer year, Integer month,
            Map<Date, String> holidays, Map<Date, List<Special_Day__c>> specials,
            List<B25__Availability__c> commitments) {
        Availability_Month__c am = new Availability_Month__c(
            Staff__c = staffId,
            Year__c = String.valueOf(year),
            Month__c = String.valueOf(month)
        );
        Date first = Date.newInstance(year, month, 1);
        Integer days = Date.daysInMonth(year, month);
        for (Integer d = 1; d <= days; d++) {
            Date day = first.addDays(d - 1);
            String prefix = 'Day_' + String.valueOf(d).leftPad(2, '0');
            String dayName = dayName(day);
            List<String> notes = new List<String>();
            String status = null;

            Boolean blocked = false;
            if (specials.containsKey(day)) {
                for (Special_Day__c sd : specials.get(day)) {
                    if (sd.Day_Type__c == SPECIAL_BLOCKED) {
                        blocked = true;
                    }
                    notes.add(specialNote(sd));
                }
            }
            if (blocked) {
                status = STATUS_BLOCKED;
            }
            if (holidays.containsKey(day)) {
                addFirst(notes, holidays.get(day));
                if (status == null) {
                    status = STATUS_SHABBAT;
                }
            } else if (dayName == 'Saturday') {
                addFirst(notes, NOTE_SHABBAT);
                if (status == null) {
                    status = STATUS_SHABBAT;
                }
            }
            if (status == null && commitments != null) {
                for (B25__Availability__c c : commitments) {
                    if (covers(c, day, dayName)) {
                        status = STATUS_OBLIGATED;
                        Integer startMin = toMinutes(c.B25__Start_Hour__c, c.B25__Start_Minute__c, 0);
                        Integer endMin = toMinutes(c.B25__End_Hour__c, c.B25__End_Minute__c, 24 * 60);
                        am.put(prefix + '_Start__c', Time.newInstance(startMin / 60, Math.mod(startMin, 60), 0, 0));
                        am.put(prefix + '_Duration__c', durationValue(endMin - startMin));
                        break;
                    }
                }
            }
            if (status != null) {
                am.put(prefix + '__c', status);
            }
            if (!notes.isEmpty()) {
                am.put(prefix + '_Note__c', String.join(notes, ' / ').abbreviate(NOTE_LENGTH));
            }
        }
        return am;
    }

    // ---------------------------------------------------------------- Hebcal

    @TestVisible
    private static Map<Date, String> fetchHolidays(Integer year, Integer month) {
        HttpRequest req = new HttpRequest();
        req.setEndpoint('callout:NCHebcalAPI/hebcal?v=1&cfg=json&i=on&maj=on&min=off&mod=off&nx=off'
            + '&mf=off&ss=off&c=off&s=off&year=' + year + '&month=' + month);
        req.setMethod('GET');
        req.setTimeout(20000);
        HttpResponse res = new Http().send(req);
        if (res.getStatusCode() != 200) {
            throw new CalloutException('HTTP ' + res.getStatusCode());
        }
        Map<String, Object> body = (Map<String, Object>) JSON.deserializeUntyped(res.getBody());
        Map<Date, String> holidays = new Map<Date, String>();
        List<Object> items = (List<Object>) body.get('items');
        if (items == null) {
            return holidays;
        }
        for (Object o : items) {
            Map<String, Object> item = (Map<String, Object>) o;
            if (item.get('category') != 'holiday' || item.get('yomtov') != true) {
                continue;
            }
            String dateText = String.valueOf(item.get('date'));
            Date d = Date.valueOf(dateText.left(10));
            String name = item.get('hebrew') != null ? String.valueOf(item.get('hebrew')) : String.valueOf(item.get('title'));
            holidays.put(d, holidays.containsKey(d) ? holidays.get(d) + ' / ' + name : name);
        }
        return holidays;
    }

    // ---------------------------------------------------------------- helpers

    private static Boolean covers(B25__Availability__c c, Date day, String dayName) {
        if (c.B25__Start_Date__c != null && c.B25__Start_Date__c > day) {
            return false;
        }
        if (c.B25__End_Date__c != null && c.B25__End_Date__c < day) {
            return false;
        }
        return c.B25__Days__c != null && new Set<String>(c.B25__Days__c.split(';')).contains(dayName);
    }

    @TestVisible
    private static String durationValue(Integer minutes) {
        Decimal hours = Decimal.valueOf(minutes) / 60;
        hours = (hours * 2).setScale(0, RoundingMode.HALF_UP) / 2;
        hours = Math.max(MIN_HOURS, Math.min(MAX_HOURS, hours));
        Integer whole = hours.intValue();
        return hours == whole ? String.valueOf(whole) : String.valueOf(whole) + '.5';
    }

    // List.add(0, x) fails on an empty list
    private static void addFirst(List<String> items, String value) {
        if (items.isEmpty()) {
            items.add(value);
        } else {
            items.add(0, value);
        }
    }

    private static String specialNote(Special_Day__c sd) {
        if (String.isNotBlank(sd.Note_He__c)) {
            return sd.Note_He__c;
        }
        if (String.isNotBlank(sd.Reason__c)) {
            return sd.Reason__c;
        }
        return sd.Name;
    }

    private static Integer toMinutes(Decimal hour, Decimal minute, Integer fallback) {
        if (hour == null) {
            return fallback;
        }
        return hour.intValue() * 60 + (minute == null ? 0 : minute.intValue());
    }

    // Day name without the user's locale (1900-01-07 was a Sunday)
    private static String dayName(Date d) {
        return DAYS[Math.mod(Date.newInstance(1900, 1, 7).daysBetween(d), 7)];
    }

    private static Set<String> monthTexts(Integer month) {
        return new Set<String>{ String.valueOf(month), String.valueOf(month).leftPad(2, '0') };
    }

    private static void checkMonth(Integer year, Integer month) {
        if (year == null || month == null || month < 1 || month > 12 || year < 2000 || year > 2100) {
            throw new AuraHandledException('חודש או שנה לא תקינים');
        }
    }
}
