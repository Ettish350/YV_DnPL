/**
 * A09-13: staff filter for the GoMeddo reservation form, with availability read from Availability Month.
 * Copy of GS_StaffFilterLogic (Neev, 12.11.2026). Only step 4 changed. GS_StaffFilterLogic stays
 * untouched as the reference and the rollback: switch back by setting the GoMeddo system setting
 * "Reservation Form Customizer Class" to GS_StaffFilterLogic.
 *
 * Shown in the Staff lookup and the Reservation Staff related list. A person is offered only if:
 *  1. active (B25__Is_Active__c);
 *  2. holds ALL required Skills. Required = Skills of the Reservation Type plus Skills tagged with the
 *     order's Audience Category. Nothing required = no restriction;
 *  3. speaks the order's Instruction Language, when the order has one;
 *  4. is available for the reservation (Mikey, Lior and Neev, 18.11.2026): the guide's Availability Month
 *     record (Staff__c; was through the commitment until 18.11.2026) for the reservation's month, on the
 *     reservation's day, has status available (זמין), obligated (מחוייב) or booked (משובץ, step 5 decides),
 *     and that day's Start plus Duration covers the reservation's hours, in the person's time zone.
 *     No record for the month, any other status, or a day with no Start or Duration: not available.
 *     Skipped when the reservation's status allows unavailable bookings, when the reservation spans
 *     more than one local day, or when it has no start or end;
 *  5. is not already booked: no other reservation of the person overlaps, unless that reservation's
 *     status allows double booking (GoMeddo's own rule, B25__AllowDoubleBooking__c).
 * Results: one row per person, Guide 25 first, then by name, at most 50.
 * Not in scope (later slices): partial matches with the reason (A09-02), signed agreement (V13-07),
 * the always-available IDF education unit soldier, dependent availability, resource availability.
 */
global with sharing class GS_StaffFilterLogic_AvMonth implements B25.Form.Customizer {

    private static final Integer CANDIDATE_LIMIT = 500;
    private static final Integer RESULT_LIMIT = 50;
    private static final Integer DAY_MINUTES = 24 * 60;

    // Day statuses that pass step 4 (global value set Availability_Day_Status).
    // משובץ passes: step 5 checks the real GoMeddo reservations.
    private static final Set<String> AVAILABLE_STATUSES = new Set<String>{ 'זמין', 'מחוייב', 'משובץ' };

    global void customize(B25.Form form) {
        form.getLookup(B25__Reservation__c.B25__Staff__c).onSearch(new StaffSearchHandler());
        form.getRelatedList(Reservation_Staff__c.SObjectType).onSearch(new StaffSearchHandler());
    }

    global with sharing class StaffSearchHandler extends B25.SearchHandler {

        global override B25.SearchResultCollection getSearchResults(B25.SearchContext context) {
            B25__Reservation__c reservation = context.getForm().getReservation();
            if (reservation == null) {
                return context.getDefaultResults();
            }

            Id typeId = reservation.B25__Reservation_Type__c;
            Id oppId = readId(reservation, 'B25__Opportunity__c');

            String language = null;
            Set<String> audiences = new Set<String>();
            if (oppId != null) {
                for (Opportunity opp : [
                    SELECT Audience_Category__c, InstructionLanguage__c
                    FROM Opportunity
                    WHERE Id = :oppId
                    LIMIT 1
                ]) {
                    language = opp.InstructionLanguage__c;
                    if (opp.Audience_Category__c != null) {
                        audiences.addAll(opp.Audience_Category__c.split(';'));
                    }
                }
            }

            Set<Id> requiredSkills = new Set<Id>();
            if (typeId != null) {
                for (Reservation_Type_Skill__c rts : [
                    SELECT Skill__c
                    FROM Reservation_Type_Skill__c
                    WHERE Reservation_Type__c = :typeId
                ]) {
                    requiredSkills.add(rts.Skill__c);
                }
            }
            if (!audiences.isEmpty()) {
                for (Skill__c skill : [
                    SELECT Id
                    FROM Skill__c
                    WHERE Audience_Category__c IN :audiences
                ]) {
                    requiredSkills.add(skill.Id);
                }
            }

            // Staff who hold every required Skill (null = no certification restriction)
            Set<Id> certifiedStaff = null;
            if (!requiredSkills.isEmpty()) {
                Map<Id, Set<Id>> skillsByStaff = new Map<Id, Set<Id>>();
                for (Staff_Skill__c ss : [
                    SELECT Staff__c, Skill__c
                    FROM Staff_Skill__c
                    WHERE Skill__c IN :requiredSkills
                ]) {
                    if (!skillsByStaff.containsKey(ss.Staff__c)) {
                        skillsByStaff.put(ss.Staff__c, new Set<Id>());
                    }
                    skillsByStaff.get(ss.Staff__c).add(ss.Skill__c);
                }
                certifiedStaff = new Set<Id>();
                for (Id staffId : skillsByStaff.keySet()) {
                    if (skillsByStaff.get(staffId).size() == requiredSkills.size()) {
                        certifiedStaff.add(staffId);
                    }
                }
            }

            String term = context.getSearchTerm();
            String likeTerm = '%' + (term == null ? '' : term.trim()) + '%';

            String soql = 'SELECT Id, Name, B25__Time_Zone__c FROM B25__Staff__c'
                + ' WHERE B25__Is_Active__c = true AND Name LIKE :likeTerm';
            if (certifiedStaff != null) {
                soql += ' AND Id IN :certifiedStaff';
            }
            if (String.isNotBlank(language)) {
                soql += ' AND Language__c INCLUDES (:language)';
            }
            soql += ' ORDER BY Staff_Status_Tier__c ASC NULLS LAST, Name ASC LIMIT ' + CANDIDATE_LIMIT;
            List<B25__Staff__c> candidates = (List<B25__Staff__c>) Database.query(soql);

            // Availability and overlap, for the reservation's time
            // In the open form Start and End are still empty; GoMeddo keeps the times in StartLocal and
            // EndLocal (debug log, 18.11.2026). Start and End first, the Local fields when they are empty.
            Datetime startDt = readDatetime(reservation, 'B25__Start__c');
            if (startDt == null) {
                startDt = readDatetime(reservation, 'B25__StartLocal__c');
            }
            Datetime endDt = readDatetime(reservation, 'B25__End__c');
            if (endDt == null) {
                endDt = readDatetime(reservation, 'B25__EndLocal__c');
            }
            if (!candidates.isEmpty() && startDt != null && endDt != null && endDt > startDt) {
                if (!allowsUnavailableBookings(reservation)) {
                    candidates = keepAvailable(candidates, startDt, endDt);
                }
                candidates = keepNotBooked(candidates, startDt, endDt, reservation.Id);
            }

            B25.SearchResultCollection results = new B25.SearchResultCollection();
            Integer shown = 0;
            for (B25__Staff__c staff : candidates) {
                if (shown >= RESULT_LIMIT) {
                    break;
                }
                results.addSearchResult(new B25.SearchResult(staff.Id, staff.Name));
                shown++;
            }
            return results;
        }

        // Step 4: availability from the guide's Availability Month record for the reservation's day
        private List<B25__Staff__c> keepAvailable(List<B25__Staff__c> staffList, Datetime startDt, Datetime endDt) {
            // The reservation's local day and hours, per person (time zones can differ)
            Map<Id, LocalSlot> slots = new Map<Id, LocalSlot>();
            Set<String> years = new Set<String>();
            Set<String> months = new Set<String>();
            Set<Integer> dayNumbers = new Set<Integer>();
            for (B25__Staff__c staff : staffList) {
                LocalSlot slot = new LocalSlot(staff, startDt, endDt);
                slots.put(staff.Id, slot);
                if (!slot.spansDays) {
                    years.add(String.valueOf(slot.day.year()));
                    // Month__c is text: accept "7" and "07"
                    months.add(String.valueOf(slot.day.month()));
                    months.add(String.valueOf(slot.day.month()).leftPad(2, '0'));
                    dayNumbers.add(slot.day.day());
                }
            }

            Map<Id, List<Availability_Month__c>> byStaff = new Map<Id, List<Availability_Month__c>>();
            if (!dayNumbers.isEmpty()) {
                Set<Id> ids = new Map<Id, B25__Staff__c>(staffList).keySet();
                List<String> fields = new List<String>{ 'Staff__c', 'Month__c', 'Year__c' };
                for (Integer d : dayNumbers) {
                    String prefix = 'Day_' + String.valueOf(d).leftPad(2, '0');
                    fields.add(prefix + '__c');
                    fields.add(prefix + '_Start__c');
                    fields.add(prefix + '_Duration__c');
                }
                String soql = 'SELECT ' + String.join(fields, ', ')
                    + ' FROM Availability_Month__c'
                    + ' WHERE Staff__c IN :ids'
                    + ' AND Year__c IN :years AND Month__c IN :months';
                for (Availability_Month__c am : (List<Availability_Month__c>) Database.query(soql)) {
                    Id staffId = am.Staff__c;
                    if (!byStaff.containsKey(staffId)) {
                        byStaff.put(staffId, new List<Availability_Month__c>());
                    }
                    byStaff.get(staffId).add(am);
                }
            }

            List<B25__Staff__c> kept = new List<B25__Staff__c>();
            for (B25__Staff__c staff : staffList) {
                LocalSlot slot = slots.get(staff.Id);
                if (slot.spansDays) {
                    kept.add(staff); // spans more than one local day: not checked here
                    continue;
                }
                List<Availability_Month__c> records = byStaff.containsKey(staff.Id)
                    ? byStaff.get(staff.Id)
                    : new List<Availability_Month__c>();
                for (Availability_Month__c am : records) {
                    if (coversSlot(am, slot)) {
                        kept.add(staff);
                        break;
                    }
                }
            }
            return kept;
        }

        private Boolean coversSlot(Availability_Month__c am, LocalSlot slot) {
            if (!sameNumber(am.Year__c, slot.day.year()) || !sameNumber(am.Month__c, slot.day.month())) {
                return false;
            }
            String prefix = 'Day_' + String.valueOf(slot.day.day()).leftPad(2, '0');
            String status = (String) am.get(prefix + '__c');
            Time dayStart = (Time) am.get(prefix + '_Start__c');
            String duration = (String) am.get(prefix + '_Duration__c');
            if (status == null || !AVAILABLE_STATUSES.contains(status)) {
                return false;
            }
            // A status without hours should not happen; treat it as not available (Mikey, 18.11.2026)
            if (dayStart == null || String.isBlank(duration)) {
                return false;
            }
            Integer avStart = dayStart.hour() * 60 + dayStart.minute();
            Integer avEnd;
            try {
                avEnd = avStart + (Decimal.valueOf(duration) * 60).intValue();
            } catch (TypeException e) {
                return false;
            }
            avEnd = Math.min(avEnd, DAY_MINUTES);
            return avStart <= slot.startMinutes && avEnd >= slot.endMinutes;
        }

        private Boolean sameNumber(String text, Integer value) {
            if (String.isBlank(text)) {
                return false;
            }
            try {
                return Integer.valueOf(text.trim()) == value;
            } catch (TypeException e) {
                return false;
            }
        }

        // Overlap: drop staff already booked at that time, by GoMeddo's own status rule
        private List<B25__Staff__c> keepNotBooked(List<B25__Staff__c> staffList, Datetime startDt, Datetime endDt, Id currentId) {
            Set<Id> ids = new Map<Id, B25__Staff__c>(staffList).keySet();
            Set<Id> busy = new Set<Id>();
            for (B25__Reservation__c r : [
                SELECT B25__Staff__c
                FROM B25__Reservation__c
                WHERE B25__Staff__c IN :ids
                AND B25__Start__c < :endDt AND B25__End__c > :startDt
                AND Id != :currentId
                AND (B25__Status__c = null OR B25__Status__r.B25__AllowDoubleBooking__c = false)
            ]) {
                busy.add(r.B25__Staff__c);
            }
            for (Reservation_Staff__c rs : [
                SELECT Staff__c
                FROM Reservation_Staff__c
                WHERE Staff__c IN :ids
                AND Reservation__c != :currentId
                AND Reservation__r.B25__Start__c < :endDt AND Reservation__r.B25__End__c > :startDt
                AND (Reservation__r.B25__Status__c = null OR Reservation__r.B25__Status__r.B25__AllowDoubleBooking__c = false)
            ]) {
                busy.add(rs.Staff__c);
            }
            List<B25__Staff__c> kept = new List<B25__Staff__c>();
            for (B25__Staff__c staff : staffList) {
                if (!busy.contains(staff.Id)) {
                    kept.add(staff);
                }
            }
            return kept;
        }

        private Boolean allowsUnavailableBookings(B25__Reservation__c reservation) {
            Id statusId = readId(reservation, 'B25__Status__c');
            if (statusId == null) {
                return false;
            }
            for (B25__Reservation_Status__c status : [
                SELECT B25__Allow_Unavailable_Bookings__c FROM B25__Reservation_Status__c WHERE Id = :statusId LIMIT 1
            ]) {
                return status.B25__Allow_Unavailable_Bookings__c == true;
            }
            return false;
        }

        // The form's reservation may be unsaved or may not carry the field
        private Id readId(B25__Reservation__c reservation, String fieldName) {
            if (reservation.isSet(fieldName)) {
                return (Id) reservation.get(fieldName);
            }
            if (reservation.Id != null) {
                String soql = 'SELECT ' + fieldName + ' FROM B25__Reservation__c WHERE Id = :reservationId LIMIT 1';
                Id reservationId = reservation.Id;
                for (B25__Reservation__c saved : (List<B25__Reservation__c>) Database.query(soql)) {
                    return (Id) saved.get(fieldName);
                }
            }
            return null;
        }

        private Datetime readDatetime(B25__Reservation__c reservation, String fieldName) {
            if (reservation.isSet(fieldName)) {
                return (Datetime) reservation.get(fieldName);
            }
            if (reservation.Id != null) {
                String soql = 'SELECT ' + fieldName + ' FROM B25__Reservation__c WHERE Id = :reservationId LIMIT 1';
                Id reservationId = reservation.Id;
                for (B25__Reservation__c saved : (List<B25__Reservation__c>) Database.query(soql)) {
                    return (Datetime) saved.get(fieldName);
                }
            }
            return null;
        }
    }

    // The reservation's local day and minutes of day, in the person's time zone
    private class LocalSlot {
        public Date day;
        public Integer startMinutes;
        public Integer endMinutes;
        public Boolean spansDays;

        public LocalSlot(B25__Staff__c staff, Datetime startDt, Datetime endDt) {
            String tz = String.isNotBlank(staff.B25__Time_Zone__c) ? staff.B25__Time_Zone__c : UserInfo.getTimeZone().getID();
            day = Date.valueOf(startDt.format('yyyy-MM-dd', tz));
            Date endDay = Date.valueOf(endDt.format('yyyy-MM-dd', tz));
            spansDays = day != endDay;
            startMinutes = Integer.valueOf(startDt.format('H', tz)) * 60 + Integer.valueOf(startDt.format('m', tz));
            endMinutes = Integer.valueOf(endDt.format('H', tz)) * 60 + Integer.valueOf(endDt.format('m', tz));
        }
    }
}