@IsTest
private class yvEduLoginControllerTest {
    @IsTest
    static void login_returnsFailureWhenFieldsAreBlank() {
        Test.startTest();
        yvEduLoginController.LoginResult result = yvEduLoginController.login('', '', '', '/s/');
        Test.stopTest();

        System.assertEquals(false, result.success, 'Blank credentials must never succeed');
        System.assertEquals(null, result.redirectUrl);
    }

    @IsTest
    static void login_returnsFailureWhenInstitutionIdIsUnknown() {
        Test.startTest();
        yvEduLoginController.LoginResult result = yvEduLoginController.login(
            'does-not-exist',
            'nobody@example.com',
            'whatever',
            '/s/'
        );
        Test.stopTest();

        System.assertEquals(false, result.success, 'An unknown institution ID must never succeed');
    }

    @IsTest
    static void login_returnsFailureWhenEmailDoesNotMatchAnyContactWithAPortalUsername() {
        Account institution = new Account(Name = 'Test School', Institution_ID__c = '123456789');
        insert institution;

        Contact con = new Contact(
            LastName = 'Test Contact',
            AccountId = institution.Id,
            Email = 'contact@example.com'
            // Portal_Username__c intentionally left blank
        );
        insert con;

        Test.startTest();
        yvEduLoginController.LoginResult result = yvEduLoginController.login(
            '123456789',
            'contact@example.com',
            'whatever',
            '/s/'
        );
        Test.stopTest();

        System.assertEquals(
            false,
            result.success,
            'A Contact with no Portal_Username__c set must never succeed'
        );

        // The full success path (Portal_Username__c pointing at a real Customer Community Login
        // user, authenticated via Site.login) can't be exercised in a unit test - Site.login
        // requires a live Experience Cloud site request context. That path needs to be verified
        // manually against the sandbox's Login page.
    }
}