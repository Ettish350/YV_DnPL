/**
 * PdfSummaryServiceTest
 *
 * Required for production deployment: Salesforce blocks Apex deploys below 75%
 * code coverage. Covers the success path, an endpoint-level failure, a non-JSON
 * response, and a thrown exception.
 *
 * Callouts are mocked — no real request reaches payment.yadvashem.org.
 */
@IsTest
private class PdfSummaryServiceTest {

    // ------------------------------------------------------------------ Mocks
    private class Mock implements HttpCalloutMock {
        private final Integer status;
        private final String body;

        Mock(Integer status, String body) {
            this.status = status;
            this.body = body;
        }

        public HttpResponse respond(HttpRequest req) {
            // Assert the request is shaped the way generate.php expects.
            System.assertEquals('POST', req.getMethod(), 'Endpoint only accepts POST');
            System.assert(
                req.getBody().contains('case_id'),
                'Request body must carry case_id'
            );

            HttpResponse res = new HttpResponse();
            res.setStatusCode(this.status);
            res.setBody(this.body);
            res.setHeader('Content-Type', 'application/json');
            return res;
        }
    }

    private class ThrowingMock implements HttpCalloutMock {
        public HttpResponse respond(HttpRequest req) {
            throw new CalloutException('Simulated timeout');
        }
    }

    // ------------------------------------------------------------------ Helper

    /**
     * Returns the first active value of a picklist field.
     *
     * Case in this org requires MainSubject__c and SecondarySubject__c, both
     * restricted picklists. Reading the values from describe instead of
     * hardcoding them means the test survives someone editing the value sets.
     *
     * Note: SecondarySubject__c is a dependent picklist controlled by
     * MainSubject__c. Salesforce enforces that dependency in the UI only, not on
     * API or Apex DML, so any active value inserts cleanly here.
     */
    private static String firstActivePicklistValue(Schema.SObjectField field) {
        for (Schema.PicklistEntry entry : field.getDescribe().getPicklistValues()) {
            if (entry.isActive()) {
                return entry.getValue();
            }
        }
        return null;
    }

    private static Case makeCase() {
        Case c = new Case(
            Subject = 'PDF summary test',
            Generate_PDF_Summary__c = true,
            MainSubject__c      = firstActivePicklistValue(Case.MainSubject__c),
            SecondarySubject__c = firstActivePicklistValue(Case.SecondarySubject__c)
        );
        insert c;
        return c;
    }

    // ------------------------------------------------------------------ Tests
    @IsTest
    static void successWritesResultFields() {
        Case c = makeCase();

        Test.setMock(HttpCalloutMock.class, new Mock(200,
            '{"ok":true,"request_id":"abc123","content_version_id":"068Wm000001x5EQIAY",'
            + '"case_number":"00626964"}'));

        Test.startTest();
        PdfSummaryService.generate(new List<Id>{ c.Id });
        Test.stopTest();

        Case updated = [
            SELECT PDF_Summary_Status__c, PDF_Summary_ContentVersion_Id__c,
                   PDF_Summary_Generated_On__c, PDF_Summary_Error__c,
                   Generate_PDF_Summary__c
            FROM Case WHERE Id = :c.Id
        ];

        System.assertEquals('Success', updated.PDF_Summary_Status__c);
        System.assertEquals('068Wm000001x5EQIAY', updated.PDF_Summary_ContentVersion_Id__c);
        System.assertNotEquals(null, updated.PDF_Summary_Generated_On__c);
        System.assertEquals(null, updated.PDF_Summary_Error__c);
        System.assertEquals(false, updated.Generate_PDF_Summary__c,
            'Checkbox must reset so the user can retry');
    }

    @IsTest
    static void endpointErrorIsRecorded() {
        Case c = makeCase();

        Test.setMock(HttpCalloutMock.class, new Mock(404,
            '{"ok":false,"request_id":"be494611d57e","error":"case_not_found"}'));

        Test.startTest();
        PdfSummaryService.generate(new List<Id>{ c.Id });
        Test.stopTest();

        Case updated = [
            SELECT PDF_Summary_Status__c, PDF_Summary_Error__c, Generate_PDF_Summary__c
            FROM Case WHERE Id = :c.Id
        ];

        System.assertEquals('Error', updated.PDF_Summary_Status__c);
        System.assert(updated.PDF_Summary_Error__c.contains('case_not_found'));
        System.assert(updated.PDF_Summary_Error__c.contains('be494611d57e'),
            'request_id must be kept — it is how the server log entry is found');
        System.assertEquals(false, updated.Generate_PDF_Summary__c);
    }

    @IsTest
    static void nonJsonResponseIsHandled() {
        Case c = makeCase();

        // e.g. an Apache 500 page rather than the endpoint's own JSON.
        Test.setMock(HttpCalloutMock.class, new Mock(500,
            '<html><body>Internal Server Error</body></html>'));

        Test.startTest();
        PdfSummaryService.generate(new List<Id>{ c.Id });
        Test.stopTest();

        Case updated = [
            SELECT PDF_Summary_Status__c, PDF_Summary_Error__c
            FROM Case WHERE Id = :c.Id
        ];

        System.assertEquals('Error', updated.PDF_Summary_Status__c);
        System.assert(updated.PDF_Summary_Error__c.contains('HTTP 500'));
    }

    @IsTest
    static void calloutExceptionIsHandled() {
        Case c = makeCase();

        Test.setMock(HttpCalloutMock.class, new ThrowingMock());

        Test.startTest();
        PdfSummaryService.generate(new List<Id>{ c.Id });
        Test.stopTest();

        Case updated = [
            SELECT PDF_Summary_Status__c, PDF_Summary_Error__c, Generate_PDF_Summary__c
            FROM Case WHERE Id = :c.Id
        ];

        System.assertEquals('Error', updated.PDF_Summary_Status__c);
        System.assert(updated.PDF_Summary_Error__c.contains('Simulated timeout'));
        System.assertEquals(false, updated.Generate_PDF_Summary__c);
    }

    @IsTest
    static void duplicateIdsEnqueueOnce() {
        Case c = makeCase();

        Test.setMock(HttpCalloutMock.class, new Mock(200,
            '{"ok":true,"request_id":"abc123","content_version_id":"068000000000001",'
            + '"case_number":"00626964"}'));

        Test.startTest();
        // Same Id three times — must not upload three PDFs.
        PdfSummaryService.generate(new List<Id>{ c.Id, c.Id, c.Id });
        System.assertEquals(1, Limits.getQueueableJobs(),
            'Duplicate Ids must collapse to a single job');
        Test.stopTest();
    }

    @IsTest
    static void emptyInputDoesNothing() {
        Test.startTest();
        PdfSummaryService.generate(new List<Id>());
        PdfSummaryService.generate(null);
        System.assertEquals(0, Limits.getQueueableJobs());
        Test.stopTest();
    }
}