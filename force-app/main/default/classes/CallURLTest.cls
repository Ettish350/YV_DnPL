@isTest
private class CallURLTest {

    private class Mock implements HttpCalloutMock {
        Integer code;
        String body;
        Mock(Integer code, String body) { this.code = code; this.body = body; }
        public HTTPResponse respond(HTTPRequest req) {
            HttpResponse res = new HttpResponse();
            res.setStatusCode(code);
            res.setStatus(code == 200 ? 'OK' : 'Server Error');
            res.setBody(body);
            return res;
        }
    }

    private static CallURL.Request buildRequest(Id caseId) {
        CallURL.Request r = new CallURL.Request();
        r.url = 'https://collections.yadvashem.org/api/salesforce/createCtpInSapir?CtpCaseId=' + caseId;
        r.caseId = caseId;
        return r;
    }

    @isTest
    static void successLeavesFieldEmpty() {
        Case c = new Case(
            Subject = 'Test',
            MainSubject__c = 'Collections (Archives, Name Search/Commemoration, Museum and Library)',
            SecondarySubject__c = 'Donating personal items'
        );
        insert c;

        Test.setMock(HttpCalloutMock.class, new Mock(200, 'OK'));
        Test.startTest();
        CallURL.callURL(new List<CallURL.Request>{ buildRequest(c.Id) });
        Test.stopTest();

        Case updated = [SELECT Sapir_Error_Message__c FROM Case WHERE Id = :c.Id];
        System.assertEquals(null, updated.Sapir_Error_Message__c);
    }

    @isTest
    static void errorWritesMessage() {
        Case c = new Case(
            Subject = 'Test',
            MainSubject__c = 'Collections (Archives, Name Search/Commemoration, Museum and Library)',
            SecondarySubject__c = 'Donating personal items'
        );
        insert c;

        Test.setMock(HttpCalloutMock.class, new Mock(500, 'Something broke'));
        Test.startTest();
        CallURL.callURL(new List<CallURL.Request>{ buildRequest(c.Id) });
        Test.stopTest();

        Case updated = [SELECT Sapir_Error_Message__c FROM Case WHERE Id = :c.Id];
        System.assert(updated.Sapir_Error_Message__c.contains('500'));
        System.assert(updated.Sapir_Error_Message__c.contains('Something broke'));
    }
}