/**
 * A09-02 PROTOTYPE: suggestion engine for assigning guides to the reservations of one order.
 *
 * For every reservation of the order that has no guide yet, every active staff member is checked against:
 *  1. all required certifications (Skills of the Reservation Type plus Skills tagged with the order's
 *     Audience Category), active on the reservation's days (Staff_Skill__c Start and End Date);
 *  2. the order's Instruction Language, when it has one;
 *  3. declared availability for the day and hours (same rule as GS_StaffFilterLogic, A09-13);
 *  4. no overlapping booking, by GoMeddo's own double-booking rule;
 *  5. not already picked, in this session, for another reservation at an overlapping time.
 * A staff member with no gap is a FULL match. One with 1 or 2 gaps is a PARTIAL match and the gaps are
 * returned as text. The result is ordered by number of gaps, then by GS_StaffFilterLogic's order
 * (Staff_Status_Tier__c, so Guide 25 first, then name).
 *
 * Read only, except save(), which sets B25__Staff__c on reservations that still have no guide.
 * Not in scope (prototype): cascade to the group's other schedule rows (A09-05), contract days (A09-03),
 * payment flags (A09-06, A09-15), several guides per row, physical resources, external tours.
 * Known gap: which reservation types need a guide. For now every reservation without a guide and
 * without a cancel reason counts as waiting. See requiresGuide().
 * Technical debt: the availability rule is a copy of GS_StaffFilterLogic's and should move to one shared class.
 */
public with sharing class GS_GuideAssignService {

    public static final String SKIP = 'SKIP';
    private static final Integer STAFF_LIMIT = 1000;
    private static final Integer MAX_GAPS_SHOWN = 2;
    private static final String TZ = 'Asia/Jerusalem';
    private static final List<String> DAYS = new List<String>{
        'Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'
    };

    // ---------- data loaded once per call ----------

    public class Booking {
        public Id reservationId;
        public Id staffId;
        public Datetime startDt;
        public Datetime endDt;
        public String orderName;
    }

    public class Context {
        public Opportunity order;
        public List<B25__Reservation__c> waiting = new List<B25__Reservation__c>();
        public Map<Id, B25__Reservation__c> waitingById = new Map<Id, B25__Reservation__c>();
        public List<B25__Staff__c> staff = new List<B25__Staff__c>();
        public Map<Id, Skill__c> skillsById = new Map<Id, Skill__c>();
        public Map<Id, Set<Id>> typeSkills = new Map<Id, Set<Id>>();
        public Set<Id> audienceSkills = new Set<Id>();
        public Map<Id, List<Staff_Skill__c>> staffSkills = new Map<Id, List<Staff_Skill__c>>();
        public Map<Id, List<B25__Availability__c>> availability = new Map<Id, List<B25__Availability__c>>();
        public List<Booking> bookings = new List<Booking>();
    }

    // One candidate for one reservation, with its gaps. Sorted by gaps, then by the staff order.
    public class Scored implements Comparable {
        public B25__Staff__c staff;
        public Integer order;
        public List<String> gaps = new List<String>();
        public List<String> kinds = new List<String>(); // warn (soft) or bad (hard), parallel to gaps
        public Boolean hard = false;                    // busy, unavailable or already picked: saving would fail

        public void add(String text, String kind) {
            gaps.add(text);
            kinds.add(kind);
            if (kind == 'bad') {
                hard = true;
            }
        }

        public Integer compareTo(Object other) {
            Scored o = (Scored) other;
            if (hard != o.hard) {
                return hard ? 1 : -1;
            }
            if (gaps.size() != o.gaps.size()) {
                return gaps.size() < o.gaps.size() ? -1 : 1;
            }
            if (order == o.order) {
                return 0;
            }
            return order < o.order ? -1 : 1;
        }
    }

    // ---------- public entry points ----------

    /** Plan for the whole order: one row per reservation, with the best suggestion for each. */
    public static List<GS_AssignRow> plan(Id orderId) {
        Context ctx = load(orderId);
        List<GS_AssignRow> rows = new List<GS_AssignRow>();
        List<String> picks = new List<String>();
        for (B25__Reservation__c res : ctx.waiting) {
            List<Scored> scored = evaluate(ctx, res, picks);
            GS_AssignRow row = new GS_AssignRow();
            row.reservationId = res.Id;
            row.label = rowLabel(res);
            row.matchType = 'NONE';
            row.reason = 'אין מדריך מתאים זמין';
            if (!scored.isEmpty()) {
                Scored best = scored[0];
                if (best.gaps.isEmpty()) {
                    row.suggestedName = best.staff.Name;
                    // Full match: suggest it, and hold it so the next row does not get the same guide at the same time
                    row.matchType = 'FULL';
                    row.suggestedStaffId = best.staff.Id;
                    row.reason = '';
                    picks.add(res.Id + '|' + best.staff.Id);
                } else if (!best.hard && best.gaps.size() <= MAX_GAPS_SHOWN) {
                    row.matchType = 'PARTIAL';
                    row.reason = best.staff.Name + ': ' + String.join(best.gaps, '; ');
                }
            }
            rows.add(row);
        }
        return rows;
    }

    /**
     * Choices for one reservation. picks = "reservationId|staffId" already chosen in this session.
     * Returns every active staff member, best first, so the component can search among all of them:
     * FULL (no gap), NEAR (1 or 2 soft gaps), FAR (3 or more soft gaps), BLOCKED (busy, unavailable or
     * already picked: saving would fail). The component lists FULL and NEAR, and shows the rest only on a search.
     */
    public static List<GS_AssignChoice> choices(Id orderId, Id reservationId, List<String> picks) {
        Context ctx = load(orderId);
        List<GS_AssignChoice> out = new List<GS_AssignChoice>();
        B25__Reservation__c res = ctx.waitingById.get(reservationId);
        if (res == null) {
            return out;
        }
        for (Scored s : evaluate(ctx, res, picks == null ? new List<String>() : picks)) {
            GS_AssignChoice c = new GS_AssignChoice();
            c.value = s.staff.Id;
            c.matchType = s.gaps.isEmpty() ? 'FULL' : 'PARTIAL';
            c.tier = s.hard ? 'BLOCKED' : (s.gaps.isEmpty() ? 'FULL' : (s.gaps.size() <= MAX_GAPS_SHOWN ? 'NEAR' : 'FAR'));
            c.label = s.gaps.isEmpty()
                ? s.staff.Name + '  ✔ התאמה מלאה'
                : s.staff.Name + '  ⚠ ' + String.join(s.gaps, '; ');
            c.staffName = s.staff.Name;
            String tier = s.staff.Staff_Status_Tier__c;
            c.tag = tier != null && tier.contains('25') ? tier : null;
            c.gapTexts = s.gaps;
            c.gapKinds = s.kinds;
            out.add(c);
        }
        return out;
    }

    /** Header for a reservation, for the screens. */
    public static String labelFor(Id orderId, Id reservationId) {
        Context ctx = load(orderId);
        B25__Reservation__c res = ctx.waitingById.get(reservationId);
        return res == null ? '' : rowLabel(res);
    }

    public class SaveResult {
        public Integer saved = 0;
        public Integer failed = 0;
        public List<String> errors = new List<String>();
        public List<GS_AssignResultRow> rows = new List<GS_AssignResultRow>();
    }

    public static SaveResult save(List<String> pairs) {
        return save(pairs, null);
    }

    /**
     * Sets the guide on each reservation that still has none. Pairs are "reservationId|staffId".
     * The result has one row per reservation saved or failed, then (when orderId is given) one row per
     * reservation of the order that is still waiting for a guide.
     */
    public static SaveResult save(List<String> pairs, Id orderId) {
        SaveResult result = new SaveResult();
        Map<Id, Id> staffByReservation = new Map<Id, Id>();
        for (String pair : pairs == null ? new List<String>() : pairs) {
            List<String> parts = pair.split('\\|');
            if (parts.size() >= 2 && String.isNotBlank(parts[0]) && String.isNotBlank(parts[1])) {
                staffByReservation.put((Id) parts[0], (Id) parts[1]);
            }
        }
        Set<Id> touched = new Set<Id>();
        if (!staffByReservation.isEmpty()) {
            Map<Id, String> staffNames = new Map<Id, String>();
            for (B25__Staff__c st : [SELECT Id, Name FROM B25__Staff__c WHERE Id IN :staffByReservation.values()]) {
                staffNames.put(st.Id, st.Name);
            }
            List<B25__Reservation__c> updates = new List<B25__Reservation__c>();
            List<GS_AssignResultRow> pending = new List<GS_AssignResultRow>();
            for (B25__Reservation__c r : [
                SELECT Id, Name, B25__Title__c, B25__Group__r.Name, B25__Start__c, B25__End__c, B25__Staff__c
                FROM B25__Reservation__c
                WHERE Id IN :staffByReservation.keySet()
                ORDER BY B25__Start__c ASC NULLS LAST, Name ASC
            ]) {
                touched.add(r.Id);
                GS_AssignResultRow row = resultRow(r, 'FAILED');
                row.staffName = staffNames.get(staffByReservation.get(r.Id));
                if (r.B25__Staff__c != null) {
                    // Someone assigned it while the screens were open: never overwrite
                    result.failed++;
                    row.message = 'כבר שובץ מדריך';
                    result.errors.add(r.Name + ': ' + row.message);
                    result.rows.add(row);
                    continue;
                }
                updates.add(new B25__Reservation__c(Id = r.Id, B25__Staff__c = staffByReservation.get(r.Id)));
                pending.add(row);
            }
            List<Database.SaveResult> saves = Database.update(updates, false);
            for (Integer i = 0; i < saves.size(); i++) {
                GS_AssignResultRow row = pending[i];
                if (saves[i].isSuccess()) {
                    result.saved++;
                    row.status = 'SAVED';
                } else {
                    result.failed++;
                    row.message = saves[i].getErrors()[0].getMessage();
                    result.errors.add(row.reservationName + ': ' + row.message);
                }
                result.rows.add(row);
            }
        }
        if (orderId != null) {
            for (B25__Reservation__c r : [
                SELECT Id, Name, B25__Title__c, B25__Group__r.Name, B25__Start__c, B25__End__c
                FROM B25__Reservation__c
                WHERE B25__Opportunity__c = :orderId AND B25__Staff__c = null AND Cancel_Reason__c = null
                ORDER BY B25__Start__c ASC NULLS LAST, Name ASC
            ]) {
                if (!touched.contains(r.Id)) {
                    result.rows.add(resultRow(r, 'WAITING'));
                }
            }
        }
        return result;
    }

    private static GS_AssignResultRow resultRow(B25__Reservation__c r, String status) {
        GS_AssignResultRow row = new GS_AssignResultRow();
        row.reservationId = r.Id;
        row.reservationName = r.Name;
        row.groupLabel = rowLabel(r);
        row.status = status;
        return row;
    }

    // ---------- loading ----------

    // Prototype: every reservation without a guide counts. The DD ties the need to the product and its
    // assignment rules, so this must be replaced once the data path from product to reservation type is confirmed.
    private static Boolean requiresGuide(B25__Reservation__c res) {
        return res.B25__Staff__c == null && res.Cancel_Reason__c == null;
    }

    public static Context load(Id orderId) {
        Context ctx = new Context();
        for (Opportunity o : [
            SELECT Id, Name, Audience_Category__c, InstructionLanguage__c FROM Opportunity WHERE Id = :orderId LIMIT 1
        ]) {
            ctx.order = o;
        }
        if (ctx.order == null) {
            return ctx;
        }

        for (B25__Reservation__c r : [
            SELECT Id, Name, B25__Title__c, B25__Group__r.Name, B25__Start__c, B25__End__c, B25__Staff__c,
                   Cancel_Reason__c, B25__Reservation_Type__c, B25__Status__c,
                   B25__Status__r.B25__Allow_Unavailable_Bookings__c
            FROM B25__Reservation__c
            WHERE B25__Opportunity__c = :orderId
            ORDER BY B25__Start__c ASC NULLS LAST, Name ASC
        ]) {
            if (requiresGuide(r)) {
                ctx.waiting.add(r);
                ctx.waitingById.put(r.Id, r);
            }
        }
        if (ctx.waiting.isEmpty()) {
            return ctx;
        }

        // Required certifications
        Set<Id> typeIds = new Set<Id>();
        Datetime minStart = null;
        Datetime maxEnd = null;
        for (B25__Reservation__c r : ctx.waiting) {
            if (r.B25__Reservation_Type__c != null) {
                typeIds.add(r.B25__Reservation_Type__c);
            }
            if (r.B25__Start__c != null && (minStart == null || r.B25__Start__c < minStart)) {
                minStart = r.B25__Start__c;
            }
            if (r.B25__End__c != null && (maxEnd == null || r.B25__End__c > maxEnd)) {
                maxEnd = r.B25__End__c;
            }
        }
        Set<Id> allRequired = new Set<Id>();
        for (Reservation_Type_Skill__c rts : [
            SELECT Reservation_Type__c, Skill__c FROM Reservation_Type_Skill__c WHERE Reservation_Type__c IN :typeIds
        ]) {
            if (!ctx.typeSkills.containsKey(rts.Reservation_Type__c)) {
                ctx.typeSkills.put(rts.Reservation_Type__c, new Set<Id>());
            }
            ctx.typeSkills.get(rts.Reservation_Type__c).add(rts.Skill__c);
            allRequired.add(rts.Skill__c);
        }
        Set<String> audiences = new Set<String>();
        if (ctx.order.Audience_Category__c != null) {
            audiences.addAll(ctx.order.Audience_Category__c.split(';'));
        }
        if (!audiences.isEmpty()) {
            for (Skill__c s : [SELECT Id FROM Skill__c WHERE Audience_Category__c IN :audiences]) {
                ctx.audienceSkills.add(s.Id);
                allRequired.add(s.Id);
            }
        }
        if (!allRequired.isEmpty()) {
            ctx.skillsById = new Map<Id, Skill__c>([SELECT Id, Name FROM Skill__c WHERE Id IN :allRequired]);
        }

        // Staff, in the same order as GS_StaffFilterLogic
        ctx.staff = [
            SELECT Id, Name, B25__Time_Zone__c, Language__c, Staff_Status_Tier__c
            FROM B25__Staff__c
            WHERE B25__Is_Active__c = true
            ORDER BY Staff_Status_Tier__c ASC NULLS LAST, Name ASC
            LIMIT :STAFF_LIMIT
        ];
        Set<Id> staffIds = new Map<Id, B25__Staff__c>(ctx.staff).keySet();

        if (!allRequired.isEmpty()) {
            for (Staff_Skill__c ss : [
                SELECT Staff__c, Skill__c, Start_Date__c, End_Date__c
                FROM Staff_Skill__c
                WHERE Staff__c IN :staffIds AND Skill__c IN :allRequired
            ]) {
                if (!ctx.staffSkills.containsKey(ss.Staff__c)) {
                    ctx.staffSkills.put(ss.Staff__c, new List<Staff_Skill__c>());
                }
                ctx.staffSkills.get(ss.Staff__c).add(ss);
            }
        }

        if (minStart != null && maxEnd != null) {
            Date dayBefore = minStart.dateGmt().addDays(-1);
            Date dayAfter = maxEnd.dateGmt().addDays(1);
            for (B25__Availability__c av : [
                SELECT B25__Staff__c, B25__Days__c, B25__Start_Date__c, B25__End_Date__c,
                       B25__Start_Hour__c, B25__Start_Minute__c, B25__End_Hour__c, B25__End_Minute__c,
                       B25__Unavailability__c
                FROM B25__Availability__c
                WHERE B25__Staff__c IN :staffIds
                AND (B25__Start_Date__c = null OR B25__Start_Date__c <= :dayAfter)
                AND (B25__End_Date__c = null OR B25__End_Date__c >= :dayBefore)
            ]) {
                if (!ctx.availability.containsKey(av.B25__Staff__c)) {
                    ctx.availability.put(av.B25__Staff__c, new List<B25__Availability__c>());
                }
                ctx.availability.get(av.B25__Staff__c).add(av);
            }

            // Existing bookings in the window, as main guide or in the Reservation Staff list
            for (B25__Reservation__c r : [
                SELECT Id, B25__Staff__c, B25__Start__c, B25__End__c, B25__Opportunity__r.Name
                FROM B25__Reservation__c
                WHERE B25__Staff__c IN :staffIds
                AND B25__Start__c < :maxEnd AND B25__End__c > :minStart
                AND (B25__Status__c = null OR B25__Status__r.B25__AllowDoubleBooking__c = false)
            ]) {
                Booking b = new Booking();
                b.reservationId = r.Id;
                b.staffId = r.B25__Staff__c;
                b.startDt = r.B25__Start__c;
                b.endDt = r.B25__End__c;
                b.orderName = r.B25__Opportunity__r == null ? null : r.B25__Opportunity__r.Name;
                ctx.bookings.add(b);
            }
            for (Reservation_Staff__c rs : [
                SELECT Staff__c, Reservation__c, Reservation__r.B25__Start__c, Reservation__r.B25__End__c,
                       Reservation__r.B25__Opportunity__r.Name
                FROM Reservation_Staff__c
                WHERE Staff__c IN :staffIds
                AND Reservation__r.B25__Start__c < :maxEnd AND Reservation__r.B25__End__c > :minStart
                AND (Reservation__r.B25__Status__c = null
                     OR Reservation__r.B25__Status__r.B25__AllowDoubleBooking__c = false)
            ]) {
                Booking b = new Booking();
                b.reservationId = rs.Reservation__c;
                b.staffId = rs.Staff__c;
                b.startDt = rs.Reservation__r.B25__Start__c;
                b.endDt = rs.Reservation__r.B25__End__c;
                b.orderName = rs.Reservation__r.B25__Opportunity__r == null
                    ? null : rs.Reservation__r.B25__Opportunity__r.Name;
                ctx.bookings.add(b);
            }
        }
        return ctx;
    }

    // ---------- matching ----------

    /** All staff with their gaps for one reservation, best first. Staff with more than 2 gaps are still returned. */
    public static List<Scored> evaluate(Context ctx, B25__Reservation__c res, List<String> picks) {
        Set<Id> required = new Set<Id>(ctx.audienceSkills);
        if (res.B25__Reservation_Type__c != null && ctx.typeSkills.containsKey(res.B25__Reservation_Type__c)) {
            required.addAll(ctx.typeSkills.get(res.B25__Reservation_Type__c));
        }
        Datetime startDt = res.B25__Start__c;
        Datetime endDt = res.B25__End__c;
        Boolean hasTimes = startDt != null && endDt != null && endDt > startDt;
        Date firstDay = startDt == null ? Date.today() : startDt.dateGmt();
        Date lastDay = endDt == null ? firstDay : endDt.dateGmt();
        Boolean skipAvailability = res.B25__Status__r != null
            && res.B25__Status__r.B25__Allow_Unavailable_Bookings__c == true;

        List<Scored> out = new List<Scored>();
        Integer order = 0;
        for (B25__Staff__c staff : ctx.staff) {
            Scored s = new Scored();
            s.staff = staff;
            s.order = order++;

            // 1. certifications
            if (!required.isEmpty()) {
                Set<Id> held = new Set<Id>();
                for (Staff_Skill__c ss : ctx.staffSkills.containsKey(staff.Id)
                        ? ctx.staffSkills.get(staff.Id) : new List<Staff_Skill__c>()) {
                    if (required.contains(ss.Skill__c)
                            && (ss.Start_Date__c == null || ss.Start_Date__c <= firstDay)
                            && (ss.End_Date__c == null || ss.End_Date__c >= lastDay)) {
                        held.add(ss.Skill__c);
                    }
                }
                List<String> missing = new List<String>();
                for (Id skillId : required) {
                    if (!held.contains(skillId)) {
                        Skill__c sk = ctx.skillsById.get(skillId);
                        missing.add(sk == null ? String.valueOf(skillId) : sk.Name);
                    }
                }
                if (!missing.isEmpty()) {
                    missing.sort();
                    s.add('חסרה הסמכה: ' + String.join(missing, ', '), 'warn');
                }
            }

            // 2. language
            String language = ctx.order.InstructionLanguage__c;
            if (String.isNotBlank(language)
                    && (staff.Language__c == null || !new Set<String>(staff.Language__c.split(';')).contains(language))) {
                s.add('אינו דובר ' + language, 'warn');
            }

            if (hasTimes) {
                // 3. availability
                if (!skipAvailability && !isAvailable(staff, ctx.availability.get(staff.Id), startDt, endDt)) {
                    s.add('לא זמין בשעות אלה', 'bad');
                }
                // 4. existing bookings
                for (Booking b : ctx.bookings) {
                    if (b.staffId == staff.Id && b.reservationId != res.Id
                            && b.startDt < endDt && b.endDt > startDt) {
                        s.add('תפוס בהזמנה ' + (b.orderName == null ? 'אחרת' : b.orderName), 'bad');
                        break;
                    }
                }
                // 5. picked in this session for another reservation at an overlapping time
                for (String pick : picks) {
                    List<String> parts = pick.split('\\|');
                    if (parts.size() < 2 || parts[1] != String.valueOf(staff.Id) || parts[0] == String.valueOf(res.Id)) {
                        continue;
                    }
                    B25__Reservation__c other = ctx.waitingById.get((Id) parts[0]);
                    if (other != null && other.B25__Start__c != null && other.B25__End__c != null
                            && other.B25__Start__c < endDt && other.B25__End__c > startDt) {
                        s.add('נבחר כבר לקבוצה אחרת באותן שעות', 'bad');
                        break;
                    }
                }
            }
            out.add(s);
        }
        out.sort();
        return out;
    }

    // Same rule as GS_StaffFilterLogic.isAvailable (A09-13): a covering availability record and no Unavailability overlap
    private static Boolean isAvailable(B25__Staff__c staff, List<B25__Availability__c> records, Datetime startDt, Datetime endDt) {
        if (records == null) {
            records = new List<B25__Availability__c>();
        }
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
                    return false;
                }
            } else if (avStart <= reqStart && avEnd >= reqEnd) {
                covered = true;
            }
        }
        return covered;
    }

    private static Integer minutesOfDay(Datetime dt, String tz) {
        return Integer.valueOf(dt.format('H', tz)) * 60 + Integer.valueOf(dt.format('m', tz));
    }

    private static Integer toMinutes(Decimal hour, Decimal minute, Integer fallback) {
        if (hour == null) {
            return fallback;
        }
        return hour.intValue() * 60 + (minute == null ? 0 : minute.intValue());
    }

    private static String rowLabel(B25__Reservation__c res) {
        String name = res.B25__Group__r != null && String.isNotBlank(res.B25__Group__r.Name)
            ? res.B25__Group__r.Name
            : (String.isNotBlank(res.B25__Title__c) ? res.B25__Title__c : res.Name);
        if (res.B25__Start__c == null) {
            return name;
        }
        String timeText = res.B25__Start__c.format('dd.MM HH:mm', TZ);
        if (res.B25__End__c != null) {
            timeText += '-' + res.B25__End__c.format('HH:mm', TZ);
        }
        return name + ' | ' + timeText;
    }
}