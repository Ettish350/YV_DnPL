/**
 * P11 | A11-07 | A reservation of a splittable room (B25__Resource__c.Is_Splittable__c) books every active sub-room
 * (child resources, B25__Parent__c) for the same range. GoMeddo does not do this itself (tested 18.11.2026).
 * Called by the flow P11_Reservation_Split_Sync after save.
 *
 * The sub-room copies carry Split_Parent__c and are kept in step: same stored start and end as the room's reservation,
 * same status, cancelled with the same reason when it is cancelled, and cancelled when the room changes. When a
 * sub-room is already taken, nothing is saved and the message is returned, so the flow can stop the save.
 * Runs without sharing: a sub-room booked by someone else must still be found and kept in step.
 */
public without sharing class P11_SplitRoomReservations {
    public static final String CANCELLED_STATUS = 'בוטלה';

    public class Request {
        @InvocableVariable(label='Reservation Id' required=true)
        public Id reservationId;
    }

    public class Result {
        @InvocableVariable(label='Error message')
        public String errorMessage;
    }

    @InvocableMethod(
        label='P11 Sync sub-room reservations'
        description='P11 | A11-07 | Creates, updates or cancels the parallel reservations of the sub-rooms of a splittable room.'
        category='P11'
    )
    public static List<Result> sync(List<Request> requests) {
        List<Result> results = new List<Result>();
        for (Request request : requests) {
            Result result = new Result();
            Savepoint savepoint = Database.setSavepoint();
            try {
                syncOne(request.reservationId);
            } catch (DmlException e) {
                Database.rollback(savepoint);
                result.errorMessage = 'לא ניתן לשבץ את כל חדרי המשנה של החדר המפוצל: ' + e.getDmlMessage(0);
            }
            results.add(result);
        }
        return results;
    }

    private static void syncOne(Id reservationId) {
        B25__Reservation__c room = [
            SELECT Id, B25__Resource__c, B25__Resource__r.Is_Splittable__c, B25__Start__c, B25__End__c,
                B25__StartLocal__c, B25__EndLocal__c, B25__Status__c, B25__Status__r.Name, Cancel_Reason__c,
                B25__Opportunity__c, B25__Quantity__c, Booking_Kind__c, Hold_Reason__c, Requested_By__c, Usage_Mode__c, B25__Title__c
            FROM B25__Reservation__c
            WHERE Id = :reservationId
        ];
        Boolean cancelled = room.B25__Status__r != null && room.B25__Status__r.Name == CANCELLED_STATUS;
        Map<Id, B25__Resource__c> subRooms = new Map<Id, B25__Resource__c>();
        if (!cancelled && room.B25__Resource__c != null && room.B25__Resource__r.Is_Splittable__c == true) {
            subRooms = new Map<Id, B25__Resource__c>([
                SELECT Id, Name FROM B25__Resource__c WHERE B25__Parent__c = :room.B25__Resource__c AND B25__Is_Active__c = TRUE
            ]);
        }
        Id cancelledId;
        for (B25__Reservation_Status__c s : [SELECT Id FROM B25__Reservation_Status__c WHERE Name = :CANCELLED_STATUS LIMIT 1]) {
            cancelledId = s.Id;
        }

        List<B25__Reservation__c> changes = new List<B25__Reservation__c>();
        Set<Id> covered = new Set<Id>();
        for (B25__Reservation__c copy : [
            SELECT Id, B25__Resource__c, B25__Start__c, B25__End__c, B25__Status__c
            FROM B25__Reservation__c
            WHERE Split_Parent__c = :room.Id AND (B25__Status__c = NULL OR B25__Status__c != :cancelledId)
        ]) {
            if (subRooms.containsKey(copy.B25__Resource__c)) {
                covered.add(copy.B25__Resource__c);
                if (copy.B25__Start__c != room.B25__Start__c || copy.B25__End__c != room.B25__End__c || copy.B25__Status__c != room.B25__Status__c) {
                    changes.add(new B25__Reservation__c(
                        Id = copy.Id,
                        B25__Start__c = room.B25__Start__c,
                        B25__End__c = room.B25__End__c,
                        B25__StartLocal__c = room.B25__StartLocal__c,
                        B25__EndLocal__c = room.B25__EndLocal__c,
                        B25__Status__c = room.B25__Status__c
                    ));
                }
            } else {
                changes.add(new B25__Reservation__c(
                    Id = copy.Id,
                    B25__Status__c = cancelledId,
                    Cancel_Reason__c = room.Cancel_Reason__c != null ? room.Cancel_Reason__c : 'אחר'
                ));
            }
        }
        update changes;

        List<B25__Reservation__c> creates = new List<B25__Reservation__c>();
        for (B25__Resource__c sub : subRooms.values()) {
            if (covered.contains(sub.Id)) {
                continue;
            }
            creates.add(new B25__Reservation__c(
                Split_Parent__c = room.Id,
                B25__Resource__c = sub.Id,
                B25__Start__c = room.B25__Start__c,
                B25__End__c = room.B25__End__c,
                B25__StartLocal__c = room.B25__StartLocal__c,
                B25__EndLocal__c = room.B25__EndLocal__c,
                B25__Status__c = room.B25__Status__c,
                B25__Opportunity__c = room.B25__Opportunity__c,
                B25__Quantity__c = room.B25__Quantity__c,
                Booking_Kind__c = room.Booking_Kind__c,
                Hold_Reason__c = room.Hold_Reason__c,
                Requested_By__c = room.Requested_By__c,
                Usage_Mode__c = room.Usage_Mode__c,
                B25__Title__c = (sub.Name + ' - ' + (room.B25__Title__c == null ? '' : room.B25__Title__c)).abbreviate(255)
            ));
        }
        insert creates;
    }
}