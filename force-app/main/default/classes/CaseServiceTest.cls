@IsTest
private class CaseServiceTest {

    // 200 eligible records so every test exercises the bulk path by default.
    private static final Integer ELIGIBLE_COUNT = 200;
    private static final Integer OPTED_IN_COUNT = 3;

    @TestSetup
    static void setupCases() {
        List<Case> cases = new List<Case>();
        for (Integer i = 0; i < ELIGIBLE_COUNT; i++) {
            cases.add(buildCase('Eligible ' + i, false));
        }
        for (Integer i = 0; i < OPTED_IN_COUNT; i++) {
            cases.add(buildCase('Opted in ' + i, true));
        }
        insert cases;
    }

    private static Case buildCase(String subject, Boolean assignToMe) {
        return new Case(
            Subject = subject,
            Status = 'New',
            Origin = 'Web',
            OwnerId = UserInfo.getUserId(),
            Assign_Case_To_Me__c = assignToMe,
            MainSubject__c = 'General Information',
            SecondarySubject__c = 'Other'
        );
    }

    private static Map<Id, Case> loadCases() {
        return new Map<Id, Case>([
            SELECT Id, OwnerId, Assign_Case_To_Me__c
            FROM Case
            ORDER BY Subject
        ]);
    }

    /**
     * Happy path, bulk: 203 cases in, only the 200 not flagged for self-assignment
     * are pushed through the assignment rules.
     */
    @IsTest
    static void doesNotReassignCasesFlaggedForSelfAssignment() {
        Map<Id, Case> before = loadCases();

        Test.startTest();
        CaseService.applyAssignmentRulesAfter(before.keySet());
        Test.stopTest();

        Map<Id, Case> after = loadCases();

        Integer optedInChecked = 0;
        for (Case c : after.values()) {
            if (c.Assign_Case_To_Me__c) {
                optedInChecked++;
                Assert.areEqual(
                    before.get(c.Id).OwnerId,
                    c.OwnerId,
                    'Case flagged Assign_Case_To_Me__c must keep its original owner: ' + c.Id
                );
            } else {
                Assert.isNotNull(c.OwnerId, 'Every processed case should still have an owner');
            }
        }

        Assert.areEqual(OPTED_IN_COUNT, optedInChecked, 'Opted-in cases should still exist and be checked');
        Assert.areEqual(ELIGIBLE_COUNT + OPTED_IN_COUNT, after.size(), 'No cases should be lost by the update');
    }

    /**
     * Passing only opted-in ids means the SOQL returns nothing and
     * Database.update runs against an empty list. Should be a clean no-op.
     */
    @IsTest
    static void handlesSetWithNoEligibleCases() {
        Map<Id, Case> before = loadCases();
        Set<Id> optedInIds = new Set<Id>();
        for (Case c : before.values()) {
            if (c.Assign_Case_To_Me__c) {
                optedInIds.add(c.Id);
            }
        }

        Test.startTest();
        CaseService.applyAssignmentRulesAfter(optedInIds);
        Test.stopTest();

        for (Case c : loadCases().values()) {
            Assert.areEqual(before.get(c.Id).OwnerId, c.OwnerId, 'Nothing should have been reassigned: ' + c.Id);
        }
    }

    @IsTest
    static void handlesEmptySet() {
        Test.startTest();
        CaseService.applyAssignmentRulesAfter(new Set<Id>());
        Test.stopTest();

        Assert.areEqual(
            ELIGIBLE_COUNT + OPTED_IN_COUNT,
            loadCases().size(),
            'An empty input set should not touch any data'
        );
    }

    /**
     * Covers the `if (ran) return;` branch. The second invocation should not
     * issue the SOQL query at all, which is what we assert on.
     */
    @IsTest
    static void runsOnlyOncePerTransaction() {
        Set<Id> caseIds = loadCases().keySet();

        Test.startTest();
        CaseService.applyAssignmentRulesAfter(caseIds);
        Integer queriesAfterFirstCall = Limits.getQueries();

        CaseService.applyAssignmentRulesAfter(caseIds);
        Integer queriesAfterSecondCall = Limits.getQueries();
        Test.stopTest();

        Assert.areEqual(
            queriesAfterFirstCall,
            queriesAfterSecondCall,
            'Second call in the same transaction should short-circuit before querying'
        );
    }
}