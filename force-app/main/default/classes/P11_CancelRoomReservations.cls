/**
 * P11 | A11-09 | Cancelling a reservation (activity day or session) or an order releases its room reservations at once.
 * Called by the flows P11_Reservation_Cancel_Cascade and P11_Order_Cancel_Cascade.
 *
 * A room reservation here is a reservation with a resource and no staff. Reservations with a guide (B25__Staff__c,
 * or a Reservation_Staff__c line) are never touched: guides are released by hand (Doc 2 process 09).
 * The cancel reason of the cancelled reservation or order is copied (V11-07).
 * Runs without sharing: the user who cancels must release every room reservation of the order, including ones
 * created by the portal that the user cannot see.
 */
public without sharing class P11_CancelRoomReservations {
    public static final String CANCELLED_STATUS = 'בוטלה';

    public class Request {
        @InvocableVariable(label='Cancelled reservation Id')
        public Id reservationId;
        @InvocableVariable(label='Cancelled order Id')
        public Id opportunityId;
        @InvocableVariable(label='Cancel reason')
        public String cancelReason;
    }

    @InvocableMethod(
        label='P11 Cancel room reservations'
        description='P11 | A11-09 | Cancels the room reservations under a cancelled reservation or of a cancelled order, with the same reason. Guides stay.'
        category='P11'
    )
    public static void cancel(List<Request> requests) {
        Map<Id, String> reasonByParent = new Map<Id, String>();
        Map<Id, String> reasonByOrder = new Map<Id, String>();
        for (Request request : requests) {
            if (request.reservationId != null) {
                reasonByParent.put(request.reservationId, request.cancelReason);
            }
            if (request.opportunityId != null) {
                reasonByOrder.put(request.opportunityId, request.cancelReason);
            }
        }
        if (reasonByParent.isEmpty() && reasonByOrder.isEmpty()) {
            return;
        }
        List<B25__Reservation_Status__c> statuses = [SELECT Id FROM B25__Reservation_Status__c WHERE Name = :CANCELLED_STATUS LIMIT 1];
        if (statuses.isEmpty()) {
            throw new IllegalArgumentException('GoMeddo has no reservation status "' + CANCELLED_STATUS + '"');
        }
        Id cancelledId = statuses[0].Id;
        Set<Id> parentIds = reasonByParent.keySet();
        Set<Id> orderIds = reasonByOrder.keySet();

        Map<Id, B25__Reservation__c> rooms = new Map<Id, B25__Reservation__c>([
            SELECT Id, B25__Parent_Reservation__c, B25__Opportunity__c, B25__Parent_Reservation__r.B25__Opportunity__c
            FROM B25__Reservation__c
            WHERE B25__Resource__c != NULL
                AND B25__Staff__c = NULL
                AND (B25__Status__c = NULL OR B25__Status__c != :cancelledId)
                AND (B25__Parent_Reservation__c IN :parentIds
                    OR B25__Opportunity__c IN :orderIds
                    OR B25__Parent_Reservation__r.B25__Opportunity__c IN :orderIds)
        ]);
        if (rooms.isEmpty()) {
            return;
        }
        Set<Id> roomIds = rooms.keySet();
        for (Reservation_Staff__c line : [SELECT Reservation__c FROM Reservation_Staff__c WHERE Reservation__c IN :roomIds]) {
            rooms.remove(line.Reservation__c);
        }

        List<B25__Reservation__c> updates = new List<B25__Reservation__c>();
        for (B25__Reservation__c room : rooms.values()) {
            String reason;
            if (room.B25__Parent_Reservation__c != null && reasonByParent.containsKey(room.B25__Parent_Reservation__c)) {
                reason = reasonByParent.get(room.B25__Parent_Reservation__c);
            } else if (room.B25__Opportunity__c != null && reasonByOrder.containsKey(room.B25__Opportunity__c)) {
                reason = reasonByOrder.get(room.B25__Opportunity__c);
            } else if (room.B25__Parent_Reservation__r != null) {
                reason = reasonByOrder.get(room.B25__Parent_Reservation__r.B25__Opportunity__c);
            }
            updates.add(new B25__Reservation__c(Id = room.Id, B25__Status__c = cancelledId, Cancel_Reason__c = reason));
        }
        update updates;
    }
}