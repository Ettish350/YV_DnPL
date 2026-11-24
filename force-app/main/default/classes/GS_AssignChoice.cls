/**
 * A09-02 PROTOTYPE: one candidate guide for one reservation, for the assignment flow.
 * label is the plain text line (used by the simple version); the other fields feed the card design.
 */
public class GS_AssignChoice {
    @AuraEnabled public String label;     // name plus a tick or the reasons it is not a full match
    @AuraEnabled public String value;     // B25__Staff__c Id
    @AuraEnabled public String matchType; // FULL or PARTIAL
    @AuraEnabled public String tier;      // FULL, NEAR, FAR or BLOCKED
    @AuraEnabled public String staffName;
    @AuraEnabled public String tag;       // e.g. Guide 25, empty for others
    @AuraEnabled public List<String> gapTexts = new List<String>();
    @AuraEnabled public List<String> gapKinds = new List<String>(); // warn or bad, parallel to gapTexts
}