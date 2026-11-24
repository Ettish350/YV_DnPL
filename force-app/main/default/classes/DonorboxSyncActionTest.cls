@IsTest
private class DonorboxSyncActionTest {

    private class EmptyMock implements HttpCalloutMock {
        public HttpResponse respond(HttpRequest req) {
            HttpResponse res = new HttpResponse();
            res.setStatusCode(200);
            res.setBody('[]');
            return res;
        }
    }

    @IsTest
    static void startsTheSync() {
        Test.setMock(HttpCalloutMock.class, new EmptyMock());
        Test.startTest();
        DonorboxSyncAction.run();
        Test.stopTest();
        System.assertEquals(1, [SELECT COUNT() FROM AsyncApexJob WHERE ApexClass.Name = 'DonorboxSync']);
    }
}