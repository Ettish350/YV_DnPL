@IsTest
private class yvLoginControllerTest {
    private static Map<String, String> edu(String institutionId, String email, String password) {
        return new Map<String, String>{ 'institutionId' => institutionId, 'email' => email, 'password' => password };
    }

    private static Map<String, String> agent(String username, String password) {
        return new Map<String, String>{ 'username' => username, 'password' => password };
    }

    @IsTest
    static void blankOrMissingCredentialsFail() {
        System.assertEquals(false, yvLoginController.login('edu', null, '/s/').success);
        System.assertEquals(false, yvLoginController.login('edu', edu('', '', ''), '/s/').success);
        System.assertEquals(false, yvLoginController.login('agent', agent('', ''), '/s/').success);
        System.assertEquals(false, yvLoginController.login('agent', agent('a@b.com', ''), '/s/').success);
    }

    @IsTest
    static void unknownTypeFails() {
        System.assertEquals(false, yvLoginController.login('nope', agent('a@b.com', 'x'), '/s/').success);
        System.assertEquals(false, yvLoginController.login(null, agent('a@b.com', 'x'), '/s/').success);
    }

    @IsTest
    static void eduUnknownInstitutionFails() {
        yvLoginController.LoginResult r = yvLoginController.login('edu', edu('does-not-exist', 'nobody@example.com', 'x'), '/s/');
        System.assertEquals(false, r.success);
        System.assertEquals(null, r.redirectUrl);
    }

    @IsTest
    static void eduContactWithoutPortalUsernameFails() {
        Account institution = new Account(Name = 'Test School', Institution_ID__c = '123456789');
        insert institution;
        insert new Contact(LastName = 'Test Contact', AccountId = institution.Id, Email = 'contact@example.com');

        System.assertEquals(false, yvLoginController.login('edu', edu('123456789', 'contact@example.com', 'x'), '/s/').success);
        System.assertEquals(null, yvLoginController.resolveUsername('edu', edu('123456789', 'contact@example.com', 'x')));
    }

    @IsTest
    static void eduUsernameComesFromTheContact() {
        Account institution = new Account(Name = 'Test School', Institution_ID__c = '123456789');
        insert institution;
        insert new Contact(LastName = 'Test Contact', AccountId = institution.Id, Email = 'contact@example.com', Portal_Username__c = 'school@example.com.yv');

        System.assertEquals('school@example.com.yv', yvLoginController.resolveUsername('edu', edu('123456789', 'contact@example.com', 'x')));
        System.assertEquals(null, yvLoginController.resolveUsername('edu', edu('123456789', 'other@example.com', 'x')));
    }

    @IsTest
    static void agentUsernameIsTrimmed() {
        System.assertEquals('agent@example.com', yvLoginController.resolveUsername('agent', agent('  agent@example.com ', 'x')));
    }

    @IsTest
    static void startUrlStaysInsideTheSite() {
        System.assertEquals('/', yvLoginController.safeStartUrl(null));
        System.assertEquals('/', yvLoginController.safeStartUrl('https://evil.example'));
        System.assertEquals('/', yvLoginController.safeStartUrl('//evil.example'));
        System.assertEquals('/', yvLoginController.safeStartUrl('/\\evil.example'));
        System.assertEquals('/s/', yvLoginController.safeStartUrl('/s/'));
    }

    // The success path (Site.login signing in a real Customer Community Login user) cannot run in a
    // unit test: it needs a live Experience Cloud request. Verify it by hand on the Login page.
}