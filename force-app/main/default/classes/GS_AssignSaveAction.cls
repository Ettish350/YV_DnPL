/**
 * A09-02 PROTOTYPE: flow action. Saves the chosen guides on the reservations and returns a result table.
 */
public with sharing class GS_AssignSaveAction {

    public class Request {
        @InvocableVariable(required=true label='Pairs (reservationId|staffId)')
        public List<String> pairs;
        @InvocableVariable(label='Order Id (to list the groups still waiting)')
        public Id orderId;
    }

    public class Response {
        @InvocableVariable(label='Saved')
        public Integer saved = 0;
        @InvocableVariable(label='Failed')
        public Integer failed = 0;
        @InvocableVariable(label='Errors')
        public String errors;
        @InvocableVariable(label='Result rows')
        public List<GS_AssignResultRow> results = new List<GS_AssignResultRow>();
    }

    @InvocableMethod(label='A09-02 Save guide assignments' description='Sets the guide on each reservation that still has none')
    public static List<Response> run(List<Request> requests) {
        List<Response> responses = new List<Response>();
        for (Request req : requests) {
            GS_GuideAssignService.SaveResult result = GS_GuideAssignService.save(req.pairs, req.orderId);
            Response resp = new Response();
            resp.saved = result.saved;
            resp.failed = result.failed;
            resp.errors = String.join(result.errors, ', ');
            resp.results = result.rows;
            responses.add(resp);
        }
        return responses;
    }
}