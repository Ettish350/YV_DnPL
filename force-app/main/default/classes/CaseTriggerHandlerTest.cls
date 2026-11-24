@IsTest
private class CaseTriggerHandlerTest {

    private static final Integer CASE_COUNT = 200;

    private static List<Case> buildCases(Integer howMany, Boolean assignToMe) {
        List<Case> cases = new List<Case>();
        for (Integer i = 0; i < howMany; i++) {
            cases.add(new Case(
                Subject = 'Handler test ' + i,
                Status = 'New',
                Origin = 'Web',
                OwnerId = UserInfo.getUserId(),
                Assign_Case_To_Me__c = assignToMe,
                MainSubject__c = 'General Information',
                SecondarySubject__c = 'Other'
            ));
        }
        return cases;
    }

    /**
     * The after-insert branch reads Trigger.newMap, so it only works when reached
     * through real DML. We prove CaseService was invoked indirectly: the service
     * has a one-shot static guard, so a direct call afterwards must short-circuit
     * before issuing its SOQL. Zero extra queries == the trigger already ran it.
     */
    @IsTest
    static void afterInsertDelegatesToCaseService() {
        List<Case> cases = buildCases(CASE_COUNT, false);

        Test.startTest();
        insert cases; // fires CaseTrigger -> CaseTriggerHandler -> CaseService

        Set<Id> caseIds = new Map<Id, Case>(cases).keySet();
        Integer queriesBefore = Limits.getQueries();
        CaseService.applyAssignmentRulesAfter(caseIds);
        Integer queriesAfter = Limits.getQueries();
        Test.stopTest();

        Assert.areEqual(
            queriesBefore,
            queriesAfter,
            'CaseService guard should already be tripped, proving after-insert delegated to it'
        );
        Assert.areEqual(CASE_COUNT, [SELECT COUNT() FROM Case], 'All cases should have survived the trigger');
    }

    /**
     * Mixed payload: cases flagged for self-assignment must come through the
     * trigger with their original owner intact.
     */
    @IsTest
    static void afterInsertRespectsSelfAssignmentFlag() {
        List<Case> cases = buildCases(5, false);
        cases.addAll(buildCases(3, true));

        Test.startTest();
        insert cases;
        Test.stopTest();

        for (Case c : [SELECT Id, OwnerId, Assign_Case_To_Me__c FROM Case WHERE Assign_Case_To_Me__c = TRUE]) {
            Assert.areEqual(
                UserInfo.getUserId(),
                c.OwnerId,
                'Opted-in case should not have been reassigned: ' + c.Id
            );
        }
    }

    /**
     * Every context other than after-insert is a no-op. These are safe to call
     * directly because the guarded branch is never entered.
     */
    @IsTest
    static void nonMatchingContextsDoNothing() {
        List<Case> cases = buildCases(3, false);
        insert cases;
        Map<Id, Case> oldMap = new Map<Id, Case>(cases);

        Test.startTest();
        Integer queriesBefore = Limits.getQueries();
        Integer dmlBefore = Limits.getDmlStatements();

        //           newList, oldMap, isBefore, isAfter, isInsert, isUpdate
        CaseTriggerHandler.execute(cases, null,   true,  false,   true,   false); // before insert
        CaseTriggerHandler.execute(cases, oldMap, true,  false,   false,  true);  // before update
        CaseTriggerHandler.execute(cases, oldMap, false, true,    false,  true);  // after update
        CaseTriggerHandler.execute(cases, oldMap, false, false,   false,  false); // nothing set

        Integer queriesAfter = Limits.getQueries();
        Integer dmlAfter = Limits.getDmlStatements();
        Test.stopTest();

        Assert.areEqual(queriesBefore, queriesAfter, 'Non after-insert contexts should not query');
        Assert.areEqual(dmlBefore, dmlAfter, 'Non after-insert contexts should not perform DML');
    }

    @IsTest
    static void handlesNullCollections() {
        Test.startTest();
        CaseTriggerHandler.execute(null, null, false, false, false, false);
        CaseTriggerHandler.execute(null, null, true, false, true, false);
        Test.stopTest();
        // Reaching here without an exception is the assertion.
        Assert.isTrue(true, 'Handler should tolerate null collections outside the after-insert branch');
    }

    /**
     * Real update DML: trigger fires in after-update context and the handler
     * should leave everything alone.
     */
    @IsTest
    static void updateDmlIsHarmless() {
        List<Case> cases = buildCases(3, false);
        insert cases;

        Test.startTest();
        for (Case c : cases) {
            c.Subject = c.Subject + ' (edited)';
        }
        update cases;
        Test.stopTest();

        Assert.areEqual(3, [SELECT COUNT() FROM Case WHERE Subject LIKE '%(edited)'], 'Update should apply cleanly');
    }
}