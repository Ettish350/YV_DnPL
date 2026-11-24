public with sharing class CaseTriggerHandler {

    public static void execute(
        List<Case> newList,
        Map<Id, Case> oldMap,
        Boolean isBefore,
        Boolean isAfter,
        Boolean isInsert,
        Boolean isUpdate
    ) {

        
        if (isAfter && isInsert) {
            CaseService.applyAssignmentRulesAfter(Trigger.newMap.keySet());
        }


    }
}