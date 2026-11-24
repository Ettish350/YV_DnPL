public class CreateCaseWithAssignment {

    public class CaseRequest {
        @InvocableVariable(required=true)
        public Case inputCase;
    }

    @InvocableMethod(label='Create Case with Assignment Rules')
    public static List<Id> createCases(List<CaseRequest> requests) {
        system.debug('!!createCases');
        List<Case> casesToInsert = new List<Case>();

        // Prepare DMLOptions
        Database.DMLOptions dmo = new Database.DMLOptions();
        dmo.assignmentRuleHeader.useDefaultRule = true;

        for (CaseRequest req : requests) {
            req.inputCase.setOptions(dmo);
            casesToInsert.add(req.inputCase);
        }
        system.debug('!!createCases casesToInsert: ' +casesToInsert);
        insert casesToInsert;

        // Return IDs
        List<Id> result = new List<Id>();
        for (Case c : casesToInsert) {
            result.add(c.Id);
        }

        return result;
    }
}