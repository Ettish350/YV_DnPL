/**
 * P11 | A11-15, V11-10 | Close a resource for a private activity, from a reservation.
 * Called by the screen flow P11_Close_Resource_Private (quick action on the reservation).
 *
 * Only users with the custom permission Close_Resource_Private may close (V11-10). Closing adds a child reservation on the
 * same resource and time range, approved, with booking kind "סגירה לפעילות פרטית": one per piece of the range, each with the
 * places still free in that piece, so no other booking fits anywhere in the range even though places were left (A11-15).
 * A resource whose type does not allow shared bookings is already held in full by the reservation: nothing is created.
 * Runs without sharing: the free places are counted over every reservation of the resource, not only the visible ones.
 */
public without sharing class P11_CloseResourcePrivate {
    public static final String PERMISSION = 'Close_Resource_Private';
    public static final String NO_PERMISSION_MESSAGE = 'אין לך הרשאה לסגור משאב לפעילות פרטית';
    public static final String PRIVATE_CLOSE_KIND = 'סגירה לפעילות פרטית';
    public static final String APPROVED_STATUS = 'אושרה';
    public static final String CANCELLED_STATUS = 'בוטלה';

    public class Request {
        @InvocableVariable(label='Reservation Id' required=true)
        public Id reservationId;
    }

    public class Result {
        @InvocableVariable(label='Closed')
        public Boolean closed = false;
        @InvocableVariable(label='Message')
        public String message;
        @InvocableVariable(label='Closing reservation Id')
        public Id closingReservationId;
    }

    @InvocableMethod(
        label='P11 Close resource for private activity'
        description='P11 | A11-15, V11-10 | Blocks the resource of the reservation for the same range, even when places are left. Permission Close_Resource_Private only.'
        category='P11'
    )
    public static List<Result> close(List<Request> requests) {
        Boolean allowed = FeatureManagement.checkPermission(PERMISSION);
        List<Result> results = new List<Result>();
        for (Request request : requests) {
            results.add(closeOne(request.reservationId, allowed));
        }
        return results;
    }

    private static Result closeOne(Id reservationId, Boolean allowed) {
        Result result = new Result();
        if (!allowed) {
            result.message = NO_PERMISSION_MESSAGE;
            return result;
        }
        B25__Reservation__c source = [
            SELECT Id, B25__Resource__c, B25__Resource__r.Name, B25__Resource__r.B25__Capacity__c,
                B25__Resource__r.B25__Resource_Type__r.B25__AllowDoubleBookings__c,
                B25__Start__c, B25__End__c, B25__StartLocal__c, B25__EndLocal__c, B25__Opportunity__c, B25__Status__r.Name
            FROM B25__Reservation__c
            WHERE Id = :reservationId
        ];
        if (source.B25__Resource__c == null) {
            result.message = 'בשיבוץ זה אין משאב לסגירה';
            return result;
        }
        if (source.B25__Status__r != null && source.B25__Status__r.Name == CANCELLED_STATUS) {
            result.message = 'לא ניתן לסגור משאב משיבוץ מבוטל';
            return result;
        }
        if (source.B25__Resource__r.B25__Resource_Type__r == null
            || source.B25__Resource__r.B25__Resource_Type__r.B25__AllowDoubleBookings__c != true) {
            result.closed = true;
            result.message = 'המשאב אינו משותף: השיבוץ כבר תופס אותו במלואו בטווח זה';
            return result;
        }
        Decimal capacity = source.B25__Resource__r.B25__Capacity__c;
        if (capacity == null) {
            result.message = 'לא הוגדרה קיבולת למשאב';
            return result;
        }
        List<Segment> segments = freeSegments(source, capacity);
        if (segments.isEmpty()) {
            result.closed = true;
            result.message = 'המשאב כבר מלא בטווח זה';
            return result;
        }
        List<B25__Reservation_Status__c> statuses = [SELECT Id FROM B25__Reservation_Status__c WHERE Name = :APPROVED_STATUS LIMIT 1];
        List<B25__Reservation__c> closings = new List<B25__Reservation__c>();
        Long localShift = source.B25__StartLocal__c == null ? 0 : source.B25__StartLocal__c.getTime() - source.B25__Start__c.getTime();
        for (Segment segment : segments) {
            closings.add(new B25__Reservation__c(
                B25__Resource__c = source.B25__Resource__c,
                B25__Start__c = segment.startAt,
                B25__End__c = segment.endAt,
                B25__StartLocal__c = Datetime.newInstance(segment.startAt.getTime() + localShift),
                B25__EndLocal__c = Datetime.newInstance(segment.endAt.getTime() + localShift),
                B25__Quantity__c = segment.free,
                B25__Status__c = statuses.isEmpty() ? null : statuses[0].Id,
                B25__Parent_Reservation__c = source.Id,
                B25__Opportunity__c = source.B25__Opportunity__c,
                Booking_Kind__c = PRIVATE_CLOSE_KIND,
                Hold_Reason__c = PRIVATE_CLOSE_KIND,
                Requested_By__c = UserInfo.getName().abbreviate(255),
                B25__Title__c = (PRIVATE_CLOSE_KIND + ' - ' + source.B25__Resource__r.Name).abbreviate(255)
            ));
        }
        Savepoint savepoint = Database.setSavepoint();
        List<Database.SaveResult> saved = Database.insert(closings, false);
        for (Database.SaveResult s : saved) {
            if (!s.isSuccess()) {
                Database.rollback(savepoint);
                result.message = 'הסגירה נכשלה: ' + s.getErrors()[0].getMessage();
                return result;
            }
        }
        Decimal blocked = 0;
        for (Segment segment : segments) {
            blocked = Math.max(blocked, segment.free);
        }
        result.closed = true;
        result.closingReservationId = saved[0].getId();
        result.message = 'המשאב נסגר לפעילות פרטית בטווח השיבוץ (עד ' + blocked.setScale(0) + ' מקומות נחסמו)';
        return result;
    }

    private class Segment {
        Datetime startAt;
        Datetime endAt;
        Decimal free;
        Segment(Datetime startAt, Datetime endAt, Decimal free) {
            this.startAt = startAt;
            this.endAt = endAt;
            this.free = free;
        }
    }

    /**
     * The source range cut at every start and end of the overlapping reservations whose status blocks. Each piece gets the
     * places still free in it; neighbouring pieces with the same number are merged, pieces with nothing free are dropped.
     * One closing reservation per piece fills the resource exactly to its capacity over the whole range.
     */
    private static List<Segment> freeSegments(B25__Reservation__c source, Decimal capacity) {
        List<B25__Reservation__c> overlapping = [
            SELECT B25__Start__c, B25__End__c, B25__Quantity__c
            FROM B25__Reservation__c
            WHERE B25__Resource__c = :source.B25__Resource__c
                AND B25__Start__c < :source.B25__End__c
                AND B25__End__c > :source.B25__Start__c
                AND (B25__Status__c = NULL OR B25__Status__r.B25__AllowDoubleBooking__c = FALSE)
        ];
        Set<Long> cutSet = new Set<Long>{ source.B25__Start__c.getTime(), source.B25__End__c.getTime() };
        for (B25__Reservation__c r : overlapping) {
            if (r.B25__Start__c > source.B25__Start__c) {
                cutSet.add(r.B25__Start__c.getTime());
            }
            if (r.B25__End__c < source.B25__End__c) {
                cutSet.add(r.B25__End__c.getTime());
            }
        }
        List<Long> cuts = new List<Long>(cutSet);
        cuts.sort();
        List<Segment> segments = new List<Segment>();
        for (Integer i = 0; i < cuts.size() - 1; i++) {
            Datetime fromAt = Datetime.newInstance(cuts[i]);
            Datetime toAt = Datetime.newInstance(cuts[i + 1]);
            Decimal taken = 0;
            for (B25__Reservation__c r : overlapping) {
                if (r.B25__Start__c < toAt && r.B25__End__c > fromAt) {
                    taken += r.B25__Quantity__c == null ? 0 : r.B25__Quantity__c;
                }
            }
            Decimal free = capacity - taken;
            if (free <= 0) {
                continue;
            }
            Segment last = segments.isEmpty() ? null : segments[segments.size() - 1];
            if (last != null && last.endAt == fromAt && last.free == free) {
                last.endAt = toAt;
            } else {
                segments.add(new Segment(fromAt, toAt, free));
            }
        }
        return segments;
    }
}