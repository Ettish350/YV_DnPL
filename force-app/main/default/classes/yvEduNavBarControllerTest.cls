@IsTest
private class yvEduNavBarControllerTest {
    @IsTest
    static void getInstitutionName_returnsNullWhenUserHasNoContact() {
        // The running test user is an internal user with no ContactId, so this
        // exercises the graceful-null path. Covering the positive (portal user
        // with a linked Contact/Account) path requires a real Experience Cloud
        // user provisioned with an org-specific Customer Community profile.
        Test.startTest();
        String result = yvEduNavBarController.getInstitutionName();
        Test.stopTest();

        System.assertEquals(null, result, 'Expected null when the running user has no associated Contact');
    }

    @IsTest
    static void updateLanguage_updatesRunningUsersLocale() {
        Test.startTest();
        yvEduNavBarController.updateLanguage('en_US');
        Test.stopTest();

        User updatedUser = [SELECT LanguageLocaleKey FROM User WHERE Id = :UserInfo.getUserId()];
        System.assertEquals('en_US', updatedUser.LanguageLocaleKey, 'Expected the user language to be updated');
    }
}