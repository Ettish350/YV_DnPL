public with sharing class CaseService {

    private static Boolean ran = false;

    
     public static void applyAssignmentRulesAfter(Set<Id> caseIds) {

        if (ran) return;
        ran = true;

        List<Case> casesToUpdate = [
            SELECT Id FROM Case WHERE Id IN :caseIds AND Assign_Case_To_Me__c <> TRUE
        ];

        Database.DMLOptions dml = new Database.DMLOptions();
        dml.AssignmentRuleHeader.useDefaultRule = true;

        Database.update(casesToUpdate, dml);
    }
}