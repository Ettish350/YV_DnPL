/**
 * Apex replacement for the Desk_Fiscal_Year_Rollups subflow (v15).
 * Same inputs and output names, so the main flow can swap the subflow element for this action 1:1.
 * One pass over the records instead of ~10 collection filters/transforms.
 * Runs without sharing to match the system-mode record-triggered flow that calls it.
 */
public without sharing class DeskFiscalYearRollups {

    public class Request {
        @InvocableVariable(label='Desk_id') public String Desk_id;
        @InvocableVariable(label='fiscal_year') public String fiscal_year;
        @InvocableVariable(label='transaction_id') public String transaction_id;
        @InvocableVariable(label='commitment_id') public String commitment_id;
        @InvocableVariable(label='DFY_id (not used, kept for parity)') public String DFY_id;
    }

    public class Result {
        @InvocableVariable public Decimal sum_current_amount_var;
        @InvocableVariable public Decimal sum_total_capital_var;
        @InvocableVariable public Decimal sum_total_development_var;
        @InvocableVariable public Integer count_all_gifts_var;
        @InvocableVariable public Integer count_large_gifts_var;
        @InvocableVariable public Decimal total_large_gifts_var;
        @InvocableVariable public Decimal largest_gift_var;
        @InvocableVariable public Decimal Total_transaction_designations;
        @InvocableVariable public Decimal OUTPUT_Tot_GTD_related_to_new_GC_All;
        @InvocableVariable public Decimal OUTPUT_Tot_GTD_related_to_new_GC_Capital;
        @InvocableVariable public Decimal OUTPUT_Tot_GTD_related_to_new_GC_Develop;
        @InvocableVariable public Decimal output_tot_due_transactions;
        @InvocableVariable public Decimal output_tot_due_transactions_capital;
        @InvocableVariable public Decimal output_tot_due_transactions_development;
    }

    private static final Decimal LARGE_GIFT = 100000;
    private static final String CAPITAL = 'Operational';   // the flow's "capital" filters use Operational
    private static final String DEVELOPMENT = 'Development';

    @InvocableMethod(label='Desk Fiscal Year Rollups (Apex)'
                     description='Replaces the Desk_Fiscal_Year_Rollups subflow.')
    public static List<Result> run(List<Request> requests) {
        // Same desk + year + mode is computed once per batch (bulk loads hit the same desk/year a lot).
        Map<String, Result> cache = new Map<String, Result>();
        List<Result> results = new List<Result>();
        for (Request r : requests) {
            String mode = String.isNotBlank(r.transaction_id) ? 'T'
                        : String.isNotBlank(r.commitment_id) ? 'C' : 'N';
            String key = mode + '|' + r.Desk_id + '|' + r.fiscal_year;
            if (!cache.containsKey(key)) {
                if (mode == 'T') {
                    cache.put(key, transactionRollup(r.Desk_id, r.fiscal_year));
                } else if (mode == 'C') {
                    cache.put(key, commitmentRollup(r.Desk_id, r.fiscal_year));
                } else {
                    cache.put(key, new Result()); // flow's default outcome: nothing assigned
                }
            }
            results.add(cache.get(key));
        }
        return results;
    }

    // ---------- transaction path ----------

    private static Result transactionRollup(String deskId, String fy) {
        List<GiftTransaction> gts = [
            SELECT Id, CurrentAmount, Status
            FROM GiftTransaction
            WHERE Desk__c = :deskId AND Fiscal_Year__c = :fy
              AND CurrentAmount > 0 AND Status IN ('Paid', 'Unpaid')
        ];
        List<GiftTransactionDesignation> gtds = [
            SELECT Amount, Project_Type__c, Paid_Transaction__c,
                   GiftTransaction.Commitment_Fiscal_Year__c
            FROM GiftTransactionDesignation
            WHERE GiftTransaction.Desk__c = :deskId
              AND GiftTransaction.Fiscal_Year__c = :fy
              AND GiftTransaction.CurrentAmount > 0
              AND GiftTransaction.Status IN ('Paid', 'Unpaid')
        ];
        return calcTransactions(gts, gtds, fy);
    }

    @TestVisible
    private static Result calcTransactions(List<GiftTransaction> gts,
                                           List<GiftTransactionDesignation> gtds, String fy) {
        Result res = new Result();

        Decimal allSum = 0, paidSum = 0, largeSum = 0, largest = null;
        Integer paidCount = 0, largeCount = 0;
        for (GiftTransaction gt : gts) {
            Decimal amt = nz(gt.CurrentAmount);
            allSum += amt;
            if (gt.Status == 'Paid') {
                paidCount++;
                paidSum += amt;
                if (largest == null || amt > largest) largest = amt;
                if (amt >= LARGE_GIFT) { largeCount++; largeSum += amt; }
            }
        }

        // The flow assigns its outputs inside the loop over paid gifts.
        // No paid gifts = loop never runs = every output stays null. Kept as-is.
        if (paidCount == 0) return res;

        Decimal gtdAll = 0, dueCap = 0, dueDev = 0, paidCap = 0, paidDev = 0,
                gcAll = 0, gcCap = 0, gcDev = 0;
        for (GiftTransactionDesignation d : gtds) {
            Decimal amt = nz(d.Amount);
            String type = d.Project_Type__c;
            Boolean paid = d.Paid_Transaction__c == true;
            Boolean isCap = type == CAPITAL;
            Boolean isDev = type == DEVELOPMENT;

            gtdAll += amt;
            if (isCap) { dueCap += amt; if (paid) paidCap += amt; }
            if (isDev) { dueDev += amt; if (paid) paidDev += amt; }

            if (d.GiftTransaction != null
                    && sameYear(d.GiftTransaction.Commitment_Fiscal_Year__c, fy)) {
                gcAll += amt;
                if (isCap) gcCap += amt;
                if (isDev) gcDev += amt;
            }
        }

        res.largest_gift_var = largest;
        res.sum_current_amount_var = paidSum;
        res.sum_total_capital_var = paidCap;
        res.sum_total_development_var = paidDev;
        res.count_large_gifts_var = largeCount;
        res.count_all_gifts_var = gts.size();
        res.total_large_gifts_var = largeSum;
        res.Total_transaction_designations = gtdAll;   // all GTDs (paid + unpaid), same as flow
        res.OUTPUT_Tot_GTD_related_to_new_GC_All = gcAll;
        res.OUTPUT_Tot_GTD_related_to_new_GC_Capital = gcCap;
        res.OUTPUT_Tot_GTD_related_to_new_GC_Develop = gcDev;
        res.output_tot_due_transactions = allSum;
        res.output_tot_due_transactions_capital = dueCap;
        res.output_tot_due_transactions_development = dueDev;
        return res;
    }

    // ---------- commitment path ----------

    private static Result commitmentRollup(String deskId, String fy) {
        List<GiftCommitment> gcs = [
            SELECT Id, ExpectedTotalCmtAmount
            FROM GiftCommitment
            WHERE Desk__c = :deskId AND Fiscal_Year__c = :fy
              AND Status IN ('Fully Paid', 'Active')
        ];
        List<GiftDefaultDesignation> gdds = [
            SELECT Amount__c, Project_Type__c
            FROM GiftDefaultDesignation
            WHERE ParentRecordId IN :gcs
        ];
        return calcCommitments(gcs, gdds);
    }

    @TestVisible
    private static Result calcCommitments(List<GiftCommitment> gcs, List<GiftDefaultDesignation> gdds) {
        Result res = new Result();
        Decimal total = 0, cap = 0, dev = 0;
        for (GiftCommitment gc : gcs) total += nz(gc.ExpectedTotalCmtAmount);
        for (GiftDefaultDesignation d : gdds) {
            if (d.Project_Type__c == CAPITAL) cap += nz(d.Amount__c);
            if (d.Project_Type__c == DEVELOPMENT) dev += nz(d.Amount__c);
        }
        res.sum_current_amount_var = total;
        res.sum_total_capital_var = cap;
        res.sum_total_development_var = dev;
        res.count_all_gifts_var = gcs.size();
        return res;
    }

    // ---------- helpers ----------

    private static Decimal nz(Decimal d) { return d == null ? 0 : d; }

    /** True when a fiscal-year field value (text or number) matches the input year. */
    public static Boolean sameYear(Object value, String fy) {
        if (value == null || String.isBlank(fy)) return false;
        if (value instanceof Decimal) {
            return ((Decimal) value).stripTrailingZeros().toPlainString() == fy;
        }
        return String.valueOf(value) == fy;
    }
}