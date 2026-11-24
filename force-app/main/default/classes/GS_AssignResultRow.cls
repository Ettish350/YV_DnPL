/**
 * A09-02 PROTOTYPE: one line of the result table shown after saving the assignments.
 */
public class GS_AssignResultRow {
    @AuraEnabled public String reservationId;
    @AuraEnabled public String reservationName; // e.g. R-002037
    @AuraEnabled public String groupLabel;      // group and time
    @AuraEnabled public String staffName;       // empty when still waiting
    @AuraEnabled public String status;          // SAVED, FAILED or WAITING
    @AuraEnabled public String message;         // reason when FAILED
}