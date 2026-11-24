/**
 * A09-02 PROTOTYPE: flow action. Plan for one order: rows waiting for a guide, each with its best suggestion.
 */
public with sharing class GS_AssignPlanAction {

    public class Request {
        @InvocableVariable(required=true label='Order Id')
        public Id orderId;
    }

    public class Response {
        @InvocableVariable(label='Rows')
        public List<GS_AssignRow> rows = new List<GS_AssignRow>();
        @InvocableVariable(label='Total rows')
        public Integer totalCount = 0;
        @InvocableVariable(label='Full match rows')
        public Integer fullCount = 0;
    }

    @InvocableMethod(label='A09-02 Plan guide assignment' description='Rows of the order waiting for a guide, with the best suggestion for each')
    public static List<Response> run(List<Request> requests) {
        List<Response> responses = new List<Response>();
        for (Request req : requests) {
            Response resp = new Response();
            resp.rows = GS_GuideAssignService.plan(req.orderId);
            resp.totalCount = resp.rows.size();
            for (GS_AssignRow row : resp.rows) {
                if (row.matchType == 'FULL') {
                    resp.fullCount++;
                }
            }
            responses.add(resp);
        }
        return responses;
    }
}