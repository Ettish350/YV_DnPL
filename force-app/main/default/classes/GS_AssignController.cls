/**
 * A09-02 PROTOTYPE: server calls for the guide wizard component (candidates for one group, on demand).
 */
public with sharing class GS_AssignController {

    /** Candidates for one reservation. picks = "reservationId|staffId|name" already chosen for the other groups. */
    @AuraEnabled
    public static List<GS_AssignChoice> getCandidates(Id orderId, Id reservationId, List<String> picks) {
        return GS_GuideAssignService.choices(orderId, reservationId, picks);
    }
}