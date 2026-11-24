@IsTest
private class GS_GuideAssignTest {

    // 09:00 to 10:00 UTC (11:00 to 12:00 in Jerusalem): never crosses local midnight
    private static final Datetime START_DT = Datetime.newInstanceGmt(Date.today().addDays(10), Time.newInstance(9, 0, 0, 0));
    private static final Datetime END_DT = START_DT.addHours(1);

    @TestSetup
    static void setup() {
        B25__Reservation_Type__c resType = new B25__Reservation_Type__c(Name = 'Type 1');
        insert resType;
        Skill__c typeSkill = new Skill__c(Name = 'Type Skill');
        insert typeSkill;
        insert new Reservation_Type_Skill__c(Reservation_Type__c = resType.Id, Skill__c = typeSkill.Id);

        B25__Staff__c full = new B25__Staff__c(Name = 'T Full', B25__Is_Active__c = true, Language__c = 'Hebrew');
        B25__Staff__c noSkill = new B25__Staff__c(Name = 'T No skill', B25__Is_Active__c = true, Language__c = 'Hebrew');
        insert new List<B25__Staff__c>{ full, noSkill };
        insert new Staff_Skill__c(Staff__c = full.Id, Skill__c = typeSkill.Id);

        List<B25__Availability__c> availability = new List<B25__Availability__c>();
        for (B25__Staff__c s : new List<B25__Staff__c>{ full, noSkill }) {
            availability.add(new B25__Availability__c(
                B25__Staff__c = s.Id,
                B25__Days__c = 'Sunday;Monday;Tuesday;Wednesday;Thursday;Friday;Saturday',
                B25__Start_Hour__c = 0, B25__Start_Minute__c = 0,
                B25__End_Hour__c = 24, B25__End_Minute__c = 0,
                B25__Unavailability__c = false));
        }
        insert availability;
    }

    private static Opportunity makeOrder() {
        String stage = Opportunity.StageName.getDescribe().getPicklistValues()[0].getValue();
        Opportunity opp = new Opportunity(Name = 'Test order', StageName = stage, CloseDate = Date.today().addDays(30));
        insert opp;
        return opp;
    }

    private static B25__Reservation__c makeReservation(Id oppId) {
        B25__Reservation__c r = new B25__Reservation__c(
            B25__Opportunity__c = oppId,
            B25__Reservation_Type__c = [SELECT Id FROM B25__Reservation_Type__c WHERE Name = 'Type 1' LIMIT 1].Id,
            B25__Start__c = START_DT, B25__End__c = END_DT);
        insert r;
        return r;
    }

    // A certified, available guide is suggested as a full match and ranked first
    @IsTest
    static void fullMatchIsSuggested() {
        Opportunity opp = makeOrder();
        B25__Reservation__c res = makeReservation(opp.Id);
        List<GS_AssignRow> rows = GS_GuideAssignService.plan(opp.Id);
        System.assertEquals(1, rows.size());
        System.assertEquals('FULL', rows[0].matchType);
        System.assertEquals('T Full', rows[0].suggestedName);

        List<GS_AssignChoice> choices = GS_GuideAssignService.choices(opp.Id, res.Id, new List<String>());
        System.assertEquals('FULL', choices[0].matchType);
        System.assertEquals('PARTIAL', choices[1].matchType);
        System.assert(choices[1].label.contains('חסרה הסמכה'), choices[1].label);
    }

    // With nobody certified the row is a partial match and the reason is given
    @IsTest
    static void partialMatchHasReason() {
        delete [SELECT Id FROM Staff_Skill__c];
        Opportunity opp = makeOrder();
        makeReservation(opp.Id);
        List<GS_AssignRow> rows = GS_GuideAssignService.plan(opp.Id);
        System.assertEquals('PARTIAL', rows[0].matchType);
        System.assert(rows[0].reason.contains('חסרה הסמכה'), rows[0].reason);
        System.assertEquals(null, rows[0].suggestedStaffId);
    }

    // The same guide is not suggested for two reservations at the same time
    @IsTest
    static void sameGuideNotSuggestedTwiceAtOnce() {
        Opportunity opp = makeOrder();
        makeReservation(opp.Id);
        makeReservation(opp.Id);
        Integer full = 0;
        for (GS_AssignRow row : GS_GuideAssignService.plan(opp.Id)) {
            if (row.matchType == 'FULL') {
                full++;
            }
        }
        System.assertEquals(1, full);
    }

    // Save sets the guide, and never overwrites a guide assigned meanwhile
    @IsTest
    static void saveSetsGuideOnce() {
        Opportunity opp = makeOrder();
        B25__Reservation__c res = makeReservation(opp.Id);
        Id staffId = [SELECT Id FROM B25__Staff__c WHERE Name = 'T Full' LIMIT 1].Id;
        List<String> pairs = new List<String>{ res.Id + '|' + staffId };

        GS_GuideAssignService.SaveResult first = GS_GuideAssignService.save(pairs);
        System.assertEquals(1, first.saved);
        System.assertEquals(staffId, [SELECT B25__Staff__c FROM B25__Reservation__c WHERE Id = :res.Id].B25__Staff__c);

        GS_GuideAssignService.SaveResult second = GS_GuideAssignService.save(pairs);
        System.assertEquals(0, second.saved);
        System.assertEquals(1, second.failed);
    }

    // Someone with no availability is BLOCKED: listed last, never suggested, and a row with only such people is NONE
    @IsTest
    static void unavailableIsBlockedAndNeverSuggested() {
        delete [SELECT Id FROM B25__Availability__c];
        Opportunity opp = makeOrder();
        B25__Reservation__c res = makeReservation(opp.Id);

        List<GS_AssignChoice> choices = GS_GuideAssignService.choices(opp.Id, res.Id, new List<String>());
        System.assertEquals(2, choices.size());
        for (GS_AssignChoice c : choices) {
            System.assertEquals('BLOCKED', c.tier);
            System.assert(c.gapKinds.contains('bad'));
        }
        System.assertEquals('NONE', GS_GuideAssignService.plan(opp.Id)[0].matchType);
    }

    // A guide with a soft gap (missing skill) is NEAR and still offered; blocked people sort after them
    @IsTest
    static void nearMatchSortsBeforeBlocked() {
        Id fullId = [SELECT Id FROM B25__Staff__c WHERE Name = 'T Full' LIMIT 1].Id;
        delete [SELECT Id FROM B25__Availability__c WHERE B25__Staff__c = :fullId];
        Opportunity opp = makeOrder();
        B25__Reservation__c res = makeReservation(opp.Id);

        List<GS_AssignChoice> choices = GS_GuideAssignService.choices(opp.Id, res.Id, new List<String>());
        System.assertEquals('NEAR', choices[0].tier);
        System.assertEquals('T No skill', choices[0].staffName);
        System.assertEquals('BLOCKED', choices[1].tier);
    }

    // The wizard passes picks as "reservationId|staffId|name": the extra part is ignored
    @IsTest
    static void picksWithNameAreUnderstood() {
        Opportunity opp = makeOrder();
        B25__Reservation__c first = makeReservation(opp.Id);
        B25__Reservation__c second = makeReservation(opp.Id);
        Id staffId = [SELECT Id FROM B25__Staff__c WHERE Name = 'T Full' LIMIT 1].Id;
        List<GS_AssignChoice> choices = GS_AssignController.getCandidates(
            opp.Id, second.Id, new List<String>{ first.Id + '|' + staffId + '|T Full' });
        for (GS_AssignChoice c : choices) {
            if (c.value == staffId) {
                System.assertEquals('BLOCKED', c.tier);
            }
        }
        GS_GuideAssignService.SaveResult saved = GS_GuideAssignService.save(
            new List<String>{ first.Id + '|' + staffId + '|T Full' }, opp.Id);
        System.assertEquals(1, saved.saved);
        System.assertEquals('SAVED', saved.rows[0].status);
        System.assertEquals('WAITING', saved.rows[1].status);
    }

    // The invocable actions return what the flow needs
    @IsTest
    static void actionsWork() {
        Opportunity opp = makeOrder();
        B25__Reservation__c res = makeReservation(opp.Id);

        GS_AssignPlanAction.Request planReq = new GS_AssignPlanAction.Request();
        planReq.orderId = opp.Id;
        GS_AssignPlanAction.Response plan = GS_AssignPlanAction.run(new List<GS_AssignPlanAction.Request>{ planReq })[0];
        System.assertEquals(1, plan.totalCount);
        System.assertEquals(1, plan.fullCount);

        GS_AssignCandidatesAction.Request candReq = new GS_AssignCandidatesAction.Request();
        candReq.orderId = opp.Id;
        candReq.reservationId = res.Id;
        candReq.picks = new List<String>();
        GS_AssignCandidatesAction.Response cands = GS_AssignCandidatesAction.run(new List<GS_AssignCandidatesAction.Request>{ candReq })[0];
        System.assert(cands.hasChoices);
        System.assertNotEquals(GS_GuideAssignService.SKIP, cands.defaultValue);

        GS_AssignSaveAction.Request saveReq = new GS_AssignSaveAction.Request();
        saveReq.pairs = new List<String>{ res.Id + '|' + cands.defaultValue };
        GS_AssignSaveAction.Response saved = GS_AssignSaveAction.run(new List<GS_AssignSaveAction.Request>{ saveReq })[0];
        System.assertEquals(1, saved.saved);
    }
}