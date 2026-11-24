@IsTest
private class DonorsRollupsTest {

    private static Account donor(String since) {
        return (Account) JSON.deserialize(JSON.serialize(new Map<String, Object>{
            'attributes' => new Map<String, Object>{ 'type' => 'Account' },
            'Donor_Since__c' => since
        }), Account.class);
    }

    @IsTest
    static void countsNewAndExistingDonors() {
        DonorsRollups.Result r = DonorsRollups.calc(
            new List<Account>{ donor(null), donor('2026'), donor('2020') }, '2026');
        Assert.areEqual(3, r.count_fiscal_donors);
        Assert.areEqual(2, r.count_new_donors_this_year);
    }

    @IsTest
    static void invocableRuns() {
        DonorsRollups.Request req = new DonorsRollups.Request();
        req.desk_id = '001000000000000';
        req.fiscal_year = '2026';

        Test.startTest();
        List<DonorsRollups.Result> res = DonorsRollups.run(
            new List<DonorsRollups.Request>{ req, req });
        Test.stopTest();

        Assert.areEqual(2, res.size());
        Assert.areEqual(0, res[0].count_fiscal_donors);
    }
}