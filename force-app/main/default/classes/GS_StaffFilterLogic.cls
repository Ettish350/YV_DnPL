/**
 * A09-13: staff filter by certification (Skill), language, availability and overlap,
 * for the GoMeddo reservation form. Clone of GS_CustomFormLogic (kept untouched as the
 * reference and the rollback).
 *
 * Shown in the Staff lookup and the Reservation Staff related list. A person is offered only if:
 *  1. active (B25__Is_Active__c);
 *  2. holds ALL required Skills. Required = Skills of the Reservation Type plus Skills tagged with the
 *     order's Audience Category. Nothing required = no restriction;
 *  3. speaks the order's Instruction Language, when the order has one;
 *  4. is available for the reservation: an availability record (B25__Availability__c, not marked
 *     Unavailability) covers the day and hours in the person's time zone, and no Unavailability record
 *     overlaps. Skipped when the reservation's status allows unavailable bookings, when the reservation
 *     spans more than one local day, or when it has no start or end;
 *  5. is not already booked: no other reservation of the person overlaps, unless that reservation's
 *     status allows double booking (GoMeddo's own rule, B25__AllowDoubleBooking__c).
 * Results: one row per person, Guide 25 first, then by name, at most 50.
 * Not in scope (later slices): partial matches with the reason (A09-02), signed agreement (V13-07),
 * the always-available IDF education unit soldier, dependent availability, resource availability.
 */
global with sharing class GS_StaffFilterLogic implements B25.Form.Customizer {

    private static final Integer CANDIDATE_LIMIT = 500;
    private static final Integer RESULT_LIMIT = 50;
    private static final List<String> DAYS = new List<String>{
        'Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'
    };

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
            Datetime startDt = readDatetime(reservation, 'B25__Start__c');
            Datetime endDt = readDatetime(reservation, 'B25__End__c');
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

        // Availability: a covering availability record and no overlapping Unavailability record
        private List<B25__Staff__c> keepAvailable(List<B25__Staff__c> staffList, Datetime startDt, Datetime endDt) {
            Set<Id> ids = new Map<Id, B25__Staff__c>(staffList).keySet();
            Date dayBefore = startDt.dateGmt().addDays(-1);
            Date dayAfter = endDt.dateGmt().addDays(1);
            Map<Id, List<B25__Availability__c>> byStaff = new Map<Id, List<B25__Availability__c>>();
            for (B25__Availability__c av : [
                SELECT B25__Staff__c, B25__Days__c, B25__Start_Date__c, B25__End_Date__c,
                       B25__Start_Hour__c, B25__Start_Minute__c, B25__End_Hour__c, B25__End_Minute__c,
                       B25__Unavailability__c
                FROM B25__Availability__c
                WHERE B25__Staff__c IN :ids
                AND (B25__Start_Date__c = null OR B25__Start_Date__c <= :dayAfter)
                AND (B25__End_Date__c = null OR B25__End_Date__c >= :dayBefore)
            ]) {
                if (!byStaff.containsKey(av.B25__Staff__c)) {
                    byStaff.put(av.B25__Staff__c, new List<B25__Availability__c>());
                }
                byStaff.get(av.B25__Staff__c).add(av);
            }
            List<B25__Staff__c> kept = new List<B25__Staff__c>();
            for (B25__Staff__c staff : staffList) {
                List<B25__Availability__c> records = byStaff.containsKey(staff.Id)
                    ? byStaff.get(staff.Id)
                    : new List<B25__Availability__c>();
                if (isAvailable(staff, records, startDt, endDt)) {
                    kept.add(staff);
                }
            }
            return kept;
        }

        private Boolean isAvailable(B25__Staff__c staff, List<B25__Availability__c> records, Datetime startDt, Datetime endDt) {
            String tz = String.isNotBlank(staff.B25__Time_Zone__c) ? staff.B25__Time_Zone__c : UserInfo.getTimeZone().getID();
            Date localDay = Date.valueOf(startDt.format('yyyy-MM-dd', tz));
            Date localEndDay = Date.valueOf(endDt.format('yyyy-MM-dd', tz));
            if (localDay != localEndDay) {
                return true; // spans more than one local day: not checked here
            }
            Integer reqStart = minutesOfDay(startDt, tz);
            Integer reqEnd = minutesOfDay(endDt, tz);
            String dayName = DAYS[Math.mod(Date.newInstance(1900, 1, 7).daysBetween(localDay), 7)];

            Boolean covered = false;
            for (B25__Availability__c av : records) {
                if (av.B25__Start_Date__c != null && av.B25__Start_Date__c > localDay) {
                    continue;
                }
                if (av.B25__End_Date__c != null && av.B25__End_Date__c < localDay) {
                    continue;
                }
                if (av.B25__Days__c == null || !new Set<String>(av.B25__Days__c.split(';')).contains(dayName)) {
                    continue;
                }
                Integer avStart = toMinutes(av.B25__Start_Hour__c, av.B25__Start_Minute__c, 0);
                Integer avEnd = toMinutes(av.B25__End_Hour__c, av.B25__End_Minute__c, 24 * 60);
                if (av.B25__Unavailability__c == true) {
                    if (avStart < reqEnd && avEnd > reqStart) {
                        return false; // blocked
                    }
                } else if (avStart <= reqStart && avEnd >= reqEnd) {
                    covered = true;
                }
            }
            return covered;
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

        private Integer minutesOfDay(Datetime dt, String tz) {
            return Integer.valueOf(dt.format('H', tz)) * 60 + Integer.valueOf(dt.format('m', tz));
        }

        private Integer toMinutes(Decimal hour, Decimal minute, Integer fallback) {
            if (hour == null) {
                return fallback;
            }
            return hour.intValue() * 60 + (minute == null ? 0 : minute.intValue());
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
}