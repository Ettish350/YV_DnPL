@IsTest
private class DeskFiscalYearRollupsTest {

    // Records are built in memory (no DML), so org validation rules and the
    // record-triggered flows don't run and can't break the test.
    private static final String FY = '2026';
    private static final String FAKE_DESK = '001000000000000';

    private static GiftTransaction gt(Decimal amount, String status) {
        return (GiftTransaction) JSON.deserialize(JSON.serialize(new Map<String, Object>{
            'attributes' => new Map<String, Object>{ 'type' => 'GiftTransaction' },
            'CurrentAmount' => amount, 'Status' => status
        }), GiftTransaction.class);
    }

    private static GiftTransactionDesignation gtd(Decimal amount, String type, Boolean paid, String commitmentFy) {
        return (GiftTransactionDesignation) JSON.deserialize(JSON.serialize(new Map<String, Object>{
            'attributes' => new Map<String, Object>{ 'type' => 'GiftTransactionDesignation' },
            'Amount' => amount, 'Project_Type__c' => type, 'Paid_Transaction__c' => paid,
            'GiftTransaction' => new Map<String, Object>{
                'attributes' => new Map<String, Object>{ 'type' => 'GiftTransaction' },
                'Commitment_Fiscal_Year__c' => commitmentFy
            }
        }), GiftTransactionDesignation.class);
    }

    private static GiftCommitment gc(Decimal expected) {
        return (GiftCommitment) JSON.deserialize(JSON.serialize(new Map<String, Object>{
            'attributes' => new Map<String, Object>{ 'type' => 'GiftCommitment' },
            'ExpectedTotalCmtAmount' => expected
        }), GiftCommitment.class);
    }

    private static GiftDefaultDesignation gdd(Decimal amount, String type) {
        return (GiftDefaultDesignation) JSON.deserialize(JSON.serialize(new Map<String, Object>{
            'attributes' => new Map<String, Object>{ 'type' => 'GiftDefaultDesignation' },
            'Amount__c' => amount, 'Project_Type__c' => type
        }), GiftDefaultDesignation.class);
    }

    @IsTest
    static void transactionTotals() {
        List<GiftTransaction> gts = new List<GiftTransaction>{
            gt(150000, 'Paid'), gt(200, 'Paid'), gt(50, 'Unpaid')
        };
        List<GiftTransactionDesignation> gtds = new List<GiftTransactionDesignation>{
            gtd(100, 'Operational', true, FY),
            gtd(40, 'Development', true, '2025'),
            gtd(30, 'Operational', false, null),
            gtd(20, 'Development', false, FY),
            gtd(10, 'Other', true, null)
        };

        DeskFiscalYearRollups.Result r = DeskFiscalYearRollups.calcTransactions(gts, gtds, FY);

        Assert.areEqual(150200, r.sum_current_amount_var);
        Assert.areEqual(150000, r.largest_gift_var);
        Assert.areEqual(3, r.count_all_gifts_var);
        Assert.areEqual(1, r.count_large_gifts_var);
        Assert.areEqual(150000, r.total_large_gifts_var);
        Assert.areEqual(150250, r.output_tot_due_transactions);
        Assert.areEqual(200, r.Total_transaction_designations);
        Assert.areEqual(130, r.output_tot_due_transactions_capital);
        Assert.areEqual(60, r.output_tot_due_transactions_development);
        Assert.areEqual(100, r.sum_total_capital_var);
        Assert.areEqual(40, r.sum_total_development_var);
        Assert.areEqual(120, r.OUTPUT_Tot_GTD_related_to_new_GC_All);
        Assert.areEqual(100, r.OUTPUT_Tot_GTD_related_to_new_GC_Capital);
        Assert.areEqual(20, r.OUTPUT_Tot_GTD_related_to_new_GC_Develop);
    }

    @IsTest
    static void noPaidGiftsLeavesOutputsNull() {
        DeskFiscalYearRollups.Result r = DeskFiscalYearRollups.calcTransactions(
            new List<GiftTransaction>{ gt(50, 'Unpaid') },
            new List<GiftTransactionDesignation>(), FY);
        Assert.isNull(r.sum_current_amount_var);
        Assert.isNull(r.largest_gift_var);
    }

    @IsTest
    static void commitmentTotals() {
        DeskFiscalYearRollups.Result r = DeskFiscalYearRollups.calcCommitments(
            new List<GiftCommitment>{ gc(1000), gc(500) },
            new List<GiftDefaultDesignation>{
                gdd(600, 'Operational'), gdd(300, 'Development'), gdd(200, 'Operational')
            });
        Assert.areEqual(1500, r.sum_current_amount_var);
        Assert.areEqual(800, r.sum_total_capital_var);
        Assert.areEqual(300, r.sum_total_development_var);
        Assert.areEqual(2, r.count_all_gifts_var);
    }

    @IsTest
    static void invocableRunsEveryPath() {
        DeskFiscalYearRollups.Request t = new DeskFiscalYearRollups.Request();
        t.Desk_id = FAKE_DESK; t.fiscal_year = FY; t.transaction_id = 'x';
        DeskFiscalYearRollups.Request c = new DeskFiscalYearRollups.Request();
        c.Desk_id = FAKE_DESK; c.fiscal_year = FY; c.commitment_id = 'y';
        DeskFiscalYearRollups.Request n = new DeskFiscalYearRollups.Request();
        n.Desk_id = FAKE_DESK; n.fiscal_year = FY;

        Test.startTest();
        List<DeskFiscalYearRollups.Result> res = DeskFiscalYearRollups.run(
            new List<DeskFiscalYearRollups.Request>{ t, t, c, n });
        Test.stopTest();

        Assert.areEqual(4, res.size());
        Assert.areEqual(0, res[2].count_all_gifts_var);
        Assert.isNull(res[3].sum_current_amount_var);
    }

    @IsTest
    static void sameYearHandlesTextAndNumbers() {
        Assert.isTrue(DeskFiscalYearRollups.sameYear('2026', FY));
        Assert.isTrue(DeskFiscalYearRollups.sameYear(Decimal.valueOf('2026.00'), FY));
        Assert.isFalse(DeskFiscalYearRollups.sameYear('2025', FY));
        Assert.isFalse(DeskFiscalYearRollups.sameYear(null, FY));
    }
}