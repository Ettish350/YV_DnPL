/**
 * Apex replacement for the Donors_Rollups subflow.
 * Same inputs and output names, so the main flow can swap the subflow element for this action 1:1.
 * Runs without sharing to match the system-mode record-triggered flow that calls it.
 */
public without sharing class DonorsRollups {

    public class Request {
        @InvocableVariable(label='desk_id') public String desk_id;
        @InvocableVariable(label='fiscal_year') public String fiscal_year;
    }

    public class Result {
        @InvocableVariable public Integer count_fiscal_donors;
        @InvocableVariable public Integer count_new_donors_this_year;
    }

    @InvocableMethod(label='Donors Rollups (Apex)'
                     description='Replaces the Donors_Rollups subflow.')
    public static List<Result> run(List<Request> requests) {
        Map<String, Result> cache = new Map<String, Result>();
        List<Result> results = new List<Result>();
        for (Request r : requests) {
            String key = r.desk_id + '|' + r.fiscal_year;
            if (!cache.containsKey(key)) cache.put(key, rollup(r.desk_id, r.fiscal_year));
            results.add(cache.get(key));
        }
        return results;
    }

    private static Result rollup(String deskId, String fy) {
        Set<Id> donorIds = new Set<Id>();
        for (GiftTransaction gt : [
                SELECT DonorId FROM GiftTransaction
                WHERE Status = 'Paid' AND CurrentAmount > 0
                  AND Desk__c = :deskId AND Fiscal_Year__c = :fy AND DonorId != null]) {
            donorIds.add(gt.DonorId);
        }
        for (GiftCommitment gc : [
                SELECT DonorId FROM GiftCommitment
                WHERE Desk__c = :deskId AND Fiscal_Year__c = :fy AND DonorId != null]) {
            donorIds.add(gc.DonorId);
        }
        List<Account> donors = [SELECT Id, Donor_Since__c FROM Account WHERE Id IN :donorIds];
        return calc(donors, fy);
    }

    @TestVisible
    private static Result calc(List<Account> donors, String fy) {
        Result res = new Result();
        Integer newDonors = 0;
        for (Account a : donors) {
            Object since = a.get('Donor_Since__c');
            if (since == null || String.isBlank(String.valueOf(since))
                    || DeskFiscalYearRollups.sameYear(since, fy)) {
                newDonors++;
            }
        }
        res.count_fiscal_donors = donors.size();
        res.count_new_donors_this_year = newDonors;
        return res;
    }
}