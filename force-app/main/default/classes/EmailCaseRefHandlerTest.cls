@isTest
private class EmailCaseRefHandlerTest {

    @isTest
    static void constructorAndEmptyListDoNothing() {
        // Just instantiate and call with empty/null lists to cover the guard branches
        EmailCaseRefHandler handler = new EmailCaseRefHandler();

        Test.startTest();
        handler.onInitDefaults(null);
        handler.onInitDefaults(new QuickAction.QuickActionDefaults[]{});
        Test.stopTest();

        System.assert(true, 'No exceptions on empty input');
    }
}