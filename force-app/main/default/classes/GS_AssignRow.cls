/**
 * A09-02 PROTOTYPE: one reservation (schedule row) waiting for a guide, as shown by the assignment flow.
 */
public class GS_AssignRow {
    @AuraEnabled public String reservationId;
    @AuraEnabled public String label;            // group and time, for the screens
    @AuraEnabled public String matchType;        // FULL, PARTIAL or NONE
    @AuraEnabled public String suggestedStaffId; // best full match; empty when there is none
    @AuraEnabled public String suggestedName;
    @AuraEnabled public String reason;           // why the best partial match is not full
}