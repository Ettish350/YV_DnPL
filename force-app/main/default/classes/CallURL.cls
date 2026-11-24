public with sharing class CallURL {

    public class Request {
        @InvocableVariable(required=true) public String url;
        @InvocableVariable(required=true) public Id caseId;
    }

    @InvocableMethod(label='Call URL' callout=true)
    public static void callURL(List<Request> requests) {
        Request r = requests[0];
        String error;

        try {
            HttpRequest req = new HttpRequest();
            req.setEndpoint(r.url);
            req.setMethod('GET');
            req.setTimeout(120000);

            HttpResponse res = new Http().send(req);

            if (res.getStatusCode() < 200 || res.getStatusCode() >= 300) {
                error = 'HTTP ' + res.getStatusCode() + ' ' + res.getStatus()
                      + ' | ' + res.getBody();
            }
        } catch (Exception e) {
            error = e.getMessage();
        }

        if (error != null) {
            update new Case(Id = r.caseId, Sapir_Error_Message__c = error);
        }
    }
}