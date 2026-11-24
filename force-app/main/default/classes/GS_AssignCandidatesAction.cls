/**
 * A09-02 PROTOTYPE: flow action. Candidate guides for one reservation, with the gaps of each,
 * taking into account the guides already picked in this session.
 */
public with sharing class GS_AssignCandidatesAction {

    public class Request {
        @InvocableVariable(required=true label='Order Id')
        public Id orderId;
        @InvocableVariable(required=true label='Reservation Id')
        public Id reservationId;
        @InvocableVariable(label='Picks so far (reservationId|staffId)')
        public List<String> picks;
    }

    public class Response {
        @InvocableVariable(label='Choices')
        public List<GS_AssignChoice> choices = new List<GS_AssignChoice>();
        @InvocableVariable(label='Default value')
        public String defaultValue = GS_GuideAssignService.SKIP;
        @InvocableVariable(label='Header')
        public String header;
        @InvocableVariable(label='Has choices')
        public Boolean hasChoices = false;
    }

    @InvocableMethod(label='A09-02 Candidates for a reservation' description='Candidate guides with the reason for each gap')
    public static List<Response> run(List<Request> requests) {
        List<Response> responses = new List<Response>();
        for (Request req : requests) {
            Response resp = new Response();
            resp.choices = GS_GuideAssignService.choices(req.orderId, req.reservationId, req.picks);
            resp.header = GS_GuideAssignService.labelFor(req.orderId, req.reservationId);
            resp.hasChoices = !resp.choices.isEmpty();
            // Pre-select a guide only when there is a full match; otherwise the coordinator must choose
            if (!resp.choices.isEmpty() && resp.choices[0].matchType == 'FULL') {
                resp.defaultValue = resp.choices[0].value;
            }
            responses.add(resp);
        }
        return responses;
    }
}