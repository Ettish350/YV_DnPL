// SeeAllData: the "Account After Create & Update" flow adds every new person account to the real
// "General Newsletter" campaign, which must already be linked to ActiveTrail. Tests use that real campaign.
@IsTest(SeeAllData=true)
private class DonorboxSyncTest {

    // Shapes copied from real Donorbox API responses (names/emails replaced).
    static final String PLAN = '{"id":501,"campaign":{"id":1,"name":"Donation Page"},'
        + '"donor":{"id":11,"first_name":"Judy","last_name":"Test","email":"Judy.Test@example.com","phone":" ",'
        + '"address":"16 Vega Ct","address_line_2":null,"city":"Irvine","state":"CA","zip_code":"92617","country":"US"},'
        + '"type":"monthly","amount":"18.0","payment_method":"Stripe","currency":"USD","started_at":"2025-12-03",'
        + '"last_donation_date":"2026-10-03T16:52:25.084Z","next_donation_date":"2026-11-03","status":"active","stopped_at":null}';

    static final String RECURRING = '{"id":9001,"campaign":{"id":1,"name":"Donation Page"},'
        + '"donor":{"id":11,"first_name":"Judy","last_name":"Test","email":"judy.test@example.com"},'
        + '"amount":"18.0","recurring":true,"amount_refunded":"0.0","stripe_charge_id":"ch_rec","status":"paid",'
        + '"donation_type":"stripe","donation_date":"2026-10-03T16:52:25.084Z","currency":"USD","processing_fee":0.82,'
        + '"app_fee":0.27,"fee_covered":false,"plan_id":501,"interval":"1 M"}';

    static final String ONE_TIME = '{"id":9002,"campaign":{"id":1,"name":"Donation Page"},'
        + '"donor":{"id":12,"first_name":"Ruth","last_name":"Existing","email":"ruth@example.com"},'
        + '"amount":"37.7","recurring":false,"amount_refunded":"0.0","stripe_charge_id":"ch_one","status":"paid",'
        + '"donation_type":"link","donation_date":"2026-10-08T17:38:38.668Z","anonymous_donation":false,"comment":"Thank you",'
        + '"currency":"USD","utm_source":"newsletter","processing_fee":1.96,"fee_covered":true,"cover_fee_amount":1.7,"app_fee":0.57,'
        + '"honor":{"dedication_type":"In memory of","honoree_name":"Vera Test","recipient_name":"Sharon Test",'
        + '"recipient_email":"sharon@example.com","recipient_message":"With love","notify_type":"email"}}';

    static List<Map<String, Object>> parse(String jsonArray) {
        List<Map<String, Object>> out = new List<Map<String, Object>>();
        for (Object o : (List<Object>) JSON.deserializeUntyped(jsonArray)) out.add((Map<String, Object>) o);
        return out;
    }

    /** Fails the test with the sync's own error messages, so the cause is visible in the Tests tab. */
    static void assertNoErrors(DonorboxSync sync) {
        System.assert(sync.errors.isEmpty(), 'Sync errors: ' + String.join(sync.errors, ' | '));
    }

    /** Desk from Donorbox Sync Settings; if blank, borrows any desk this user can see. */
    static Id useDesk() {
        Donorbox_Sync_Settings__c s = Donorbox_Sync_Settings__c.getOrgDefaults();
        if (String.isNotBlank(s.Default_Desk_Id__c)) return (Id) s.Default_Desk_Id__c;
        List<Account> seen = [SELECT Desk__c FROM Account WHERE Desk__c != null AND Desk__r.Name != null LIMIT 1];
        System.assert(!seen.isEmpty(), 'Set Default Desk Id in Donorbox Sync Settings first.');
        s.Default_Desk_Id__c = seen[0].Desk__c;
        upsert s;
        return seen[0].Desk__c;
    }

    static Id personRt() {
        return [SELECT Id FROM RecordType WHERE SobjectType = 'Account' AND IsPersonType = true AND IsActive = true
                ORDER BY CreatedDate LIMIT 1].Id;
    }

    @IsTest
    static void createsDonorsCommitmentsAndGifts() {
        Id rt = personRt();
        Id desk = useDesk();
        Account newer = new Account(RecordTypeId = rt, LastName = 'Existing New', PersonEmail = 'ruth@example.com', Desk__c = desk);
        insert newer;

        Test.startTest();
        DonorboxSync sync = new DonorboxSync();
        sync.process(parse('[' + PLAN + ']'), parse('[' + RECURRING + ',' + ONE_TIME + ']'));
        Test.stopTest();
        assertNoErrors(sync);

        // New donor created, with Donorbox as source and a full country name.
        Account judy = [SELECT Id, AccountSource, ShippingCountry, Phone, Status__c FROM Account WHERE PersonEmail = 'judy.test@example.com'];
        System.assertEquals('Donorbox', judy.AccountSource);
        System.assertEquals('Active', judy.Status__c);
        System.assertEquals('United States', judy.ShippingCountry);
        System.assertEquals(null, judy.Phone, 'blank phone is skipped');

        // Plan → active commitment with a monthly schedule.
        GiftCommitment c = [SELECT Id, Name, Status, DonorId, Commitment_Source__c, Payment_Method__c, Operational_Development__c, CurrentGiftCmtScheduleId FROM GiftCommitment WHERE Plan_ID__c = '501'];
        System.assert(c.Name.endsWith(' - 18$ monthly - 2025-12-03'), 'commitment name: ' + c.Name);
        System.assertEquals('Operational', c.Operational_Development__c);
        System.assertEquals(1, [SELECT COUNT() FROM GiftDefaultDesignation WHERE ParentRecordId = :c.Id]);
        System.assertEquals('Active', c.Status);
        System.assertEquals(judy.Id, c.DonorId);
        System.assertEquals('Internet', c.Commitment_Source__c);
        System.assertEquals('donorbox', c.Payment_Method__c.toLowerCase());
        GiftCommitmentSchedule s = [SELECT TransactionAmount, TransactionPeriod, TransactionDay FROM GiftCommitmentSchedule WHERE Id = :c.CurrentGiftCmtScheduleId];
        System.assertEquals(18, s.TransactionAmount.intValue());
        System.assertEquals('Monthly', s.TransactionPeriod);
        System.assertEquals('3', s.TransactionDay);

        // Recurring payment sits on the commitment.
        GiftTransaction rec = [SELECT Id, GiftCommitmentId, Status, Clearance_System__c, TransactionDate, Amount_in_GT_Currency__c,
                               Quick_Donation__c, Donation_Source__c, Operational_Development__c, Transferred_By__c, External_Form_Language__c
                               FROM GiftTransaction WHERE Transaction_ID__c = '9001'];
        System.assertEquals(c.Id, rec.GiftCommitmentId);
        System.assertEquals('Paid', rec.Status);
        System.assertEquals(Date.newInstance(2026, 10, 3), rec.TransactionDate);
        System.assertEquals(18, rec.Amount_in_GT_Currency__c.intValue());
        System.assert(rec.Quick_Donation__c);
        System.assertEquals('Donor via Association', rec.Donation_Source__c);
        System.assertEquals('Operational', rec.Operational_Development__c);
        System.assertEquals('YV USA Foundation', rec.Transferred_By__c);
        System.assertEquals('English', rec.External_Form_Language__c);
        System.assertEquals('Judy Test - 18$ - 2026-10-03', [SELECT Name FROM GiftTransaction WHERE Id = :rec.Id][0].Name);
        System.assertEquals(15, ((Decimal) [SELECT Overhead_Percent__c FROM GiftTransaction WHERE Id = :rec.Id][0].get('Overhead_Percent__c')).intValue());
        System.assertEquals(2.70, [SELECT Overhead_Amount__c FROM GiftTransaction WHERE Id = :rec.Id][0].Overhead_Amount__c, '15% of 18');
        System.assertEquals(1, [SELECT COUNT() FROM GiftTransactionDesignation WHERE GiftTransactionId = :rec.Id]);
        System.assertEquals('Donorbox', rec.Clearance_System__c);

        // One-time gift goes to the existing matching account, which is left untouched.
        GiftTransaction one = [SELECT DonorId, Commemoration_Request__c, Donor_comment__c, DonorCoverAmount, GatewayReference, PaymentMethod
                               FROM GiftTransaction WHERE Transaction_ID__c = '9002'];
        System.assertEquals(newer.Id, one.DonorId);
        System.assert(one.Commemoration_Request__c.startsWith('In memory of: Vera Test'));
        System.assertEquals('Thank you', one.Donor_comment__c);
        System.assert(one.DonorCoverAmount == 1.7, 'donor cover amount');
        System.assertEquals('ch_one', one.GatewayReference);
        System.assertEquals('DonorBox', one.PaymentMethod);
        System.assertEquals(Date.newInstance(2026, 10, 8), [SELECT TransactionDueDate FROM GiftTransaction WHERE Transaction_ID__c = '9002'].TransactionDueDate);
        System.assertEquals('Existing New', [SELECT LastName FROM Account WHERE Id = :newer.Id].LastName);
        System.assertEquals(5.66, [SELECT Overhead_Amount__c FROM GiftTransaction WHERE Transaction_ID__c = '9002'].Overhead_Amount__c, '15% of 37.7, rounded');
    }

    @IsTest
    static void overheadOnlyOnPaidGiftsAndFilledInLater() {
        useDesk();
        String failed = ONE_TIME.replace('"id":9002', '"id":9003').replace('"status":"paid"', '"status":"failed"')
            .replace('"stripe_charge_id":"ch_one"', '"stripe_charge_id":"ch_fail"');
        DonorboxSync first = new DonorboxSync();
        first.process(parse('[]'), parse('[' + ONE_TIME + ',' + failed + ']'));
        assertNoErrors(first);
        System.assertEquals(null, [SELECT Overhead_Amount__c FROM GiftTransaction WHERE Transaction_ID__c = '9003'].Overhead_Amount__c,
            'no overhead on a failed gift');

        // A Paid gift synced before overhead existed gets it on the next run.
        GiftTransaction paid = [SELECT Id FROM GiftTransaction WHERE Transaction_ID__c = '9002'];
        paid.Overhead_Amount__c = null;
        update paid;
        DonorboxSync second = new DonorboxSync();
        Test.startTest();
        second.process(parse('[]'), parse('[' + ONE_TIME + ']'));
        Test.stopTest();
        assertNoErrors(second);
        System.assertEquals(5.66, [SELECT Overhead_Amount__c FROM GiftTransaction WHERE Id = :paid.Id].Overhead_Amount__c);
    }

    @IsTest
    static void overheadCalculation() {
        System.assertEquals(2.70, DonorboxSync.overheadFor(parse('[' + RECURRING + ']')[0]));
        System.assertEquals(5.66, DonorboxSync.overheadFor(parse('[' + ONE_TIME + ']')[0]));
        System.assertEquals(null, DonorboxSync.overheadFor(parse('[' + ONE_TIME.replace('"currency":"USD"', '"currency":"EUR"') + ']')[0]),
            'non-USD gets no overhead');
    }

    @IsTest
    static void anomaliesAreReported() {
        Id desk = useDesk();
        // Existing donor on the usual desk; the Donorbox desk setting then points elsewhere, so the donor is
        // "off desk". Both donations are from this donor, so no new account needs the (fake) setting desk.
        insert new Account(RecordTypeId = personRt(), LastName = 'Existing', PersonEmail = 'ruth@example.com', Desk__c = desk);
        Donorbox_Sync_Settings__c s = Donorbox_Sync_Settings__c.getOrgDefaults();
        s.Default_Desk_Id__c = '001000000000000';
        upsert s;
        String eur = RECURRING.replace('"id":9001', '"id":9004').replace('"plan_id":501,', '')
            .replace('judy.test@example.com', 'ruth@example.com')
            .replace('"currency":"USD"', '"currency":"EUR"').replace('"recurring":true', '"recurring":false');

        DonorboxSync sync = new DonorboxSync();
        Test.startTest();
        sync.process(parse('[]'), parse('[' + ONE_TIME + ',' + eur + ']'));
        Test.stopTest();

        String all = String.join(sync.errors, ' | ');
        System.assert(all.contains('Donation 9004: currency is EUR, not USD'), 'non-USD reported: ' + all);
        System.assert(all.contains('Donation 9002: donor ruth@example.com is on desk'), 'off-desk donor reported: ' + all);
        System.assertEquals(2, [SELECT COUNT() FROM GiftTransaction WHERE Transaction_ID__c IN ('9002', '9004')],
            'anomalies are still saved');
    }

    @IsTest
    static void rerunCreatesNoDuplicates() {
        useDesk();
        List<Map<String, Object>> plans = parse('[' + PLAN + ']');
        List<Map<String, Object>> dons = parse('[' + RECURRING + ',' + ONE_TIME + ']');
        DonorboxSync first = new DonorboxSync();
        DonorboxSync second = new DonorboxSync();
        Test.startTest();
        first.process(plans, dons);
        second.process(plans, dons);
        Test.stopTest();
        assertNoErrors(first);
        assertNoErrors(second);
        System.assertEquals(2, [SELECT COUNT() FROM Account WHERE PersonEmail IN ('judy.test@example.com', 'ruth@example.com')]);
        System.assertEquals(1, [SELECT COUNT() FROM GiftCommitment WHERE Plan_ID__c = '501']);
        System.assertEquals(2, [SELECT COUNT() FROM GiftTransaction WHERE Transaction_ID__c IN ('9001', '9002')]);
        System.assertEquals(2, [SELECT COUNT() FROM GiftTransactionDesignation WHERE GiftTransaction.Transaction_ID__c IN ('9001', '9002')]);
    }

    @IsTest
    static void cancelledPlanClosesAndRefundIsRecorded() {
        useDesk();
        DonorboxSync first = new DonorboxSync();
        first.process(parse('[' + PLAN + ']'), parse('[' + ONE_TIME + ']'));
        assertNoErrors(first);

        String cancelled = PLAN.replace('"status":"active","stopped_at":null', '"status":"cancelled","stopped_at":"2026-10-10T10:00:00Z"');
        String refunded = ONE_TIME.replace('"status":"paid"', '"status":"refunded"').replace('"amount_refunded":"0.0"', '"amount_refunded":"37.7"');
        DonorboxSync second = new DonorboxSync();
        Test.startTest();
        second.process(parse('[' + cancelled + ']'), parse('[' + refunded + ']'));
        Test.stopTest();
        assertNoErrors(second);

        GiftCommitment c = [SELECT Status, CurrentGiftCmtSchedule.EndDate FROM GiftCommitment WHERE Plan_ID__c = '501'];
        System.assertEquals('Canceled', c.Status);
        System.assertNotEquals(null, c.CurrentGiftCmtSchedule.EndDate);
        GiftTransaction g = [SELECT Id, Status FROM GiftTransaction WHERE Transaction_ID__c = '9002'];
        System.assertEquals('Fully Refunded', g.Status);
        System.assertEquals(1, [SELECT COUNT() FROM GiftRefund WHERE GiftTransactionId = :g.Id]);
    }

    @IsTest
    static void utilMappings() {
        System.assertEquals('Israel', DonorboxUtil.countryName('IL'));
        System.assertEquals('Narnia', DonorboxUtil.countryName('Narnia'));
        System.assertEquals('18$', DonorboxUtil.money(18.00, 'usd'));
        System.assertEquals('37.7 EUR', DonorboxUtil.money(37.70, 'EUR'));
        System.assertEquals('Active', DonorboxUtil.commitmentStatus('failed'));
        System.assertEquals('Canceled', DonorboxUtil.commitmentStatus('cancelled'));
        System.assertEquals('Active', DonorboxUtil.commitmentStatus('paused'));
        System.assertEquals('Yearly', DonorboxUtil.cadence('annually')[0]);
        System.assertEquals('LastDay', DonorboxUtil.transactionDay(Date.newInstance(2026, 1, 31)));
        System.assertEquals('100 Ginko Street apt.102', DonorboxUtil.street('100 Ginko Street apt.102', '102'));
        System.assertEquals('1 Main St\nApt 4', DonorboxUtil.street('1 Main St', 'Apt 4'));
    }

    // ── Callout path ─────────────────────────────────────────

    private class DonorboxMock implements HttpCalloutMock {
        public HttpResponse respond(HttpRequest req) {
            HttpResponse res = new HttpResponse();
            res.setStatusCode(200);
            res.setHeader('Content-Type', 'application/json');
            String url = req.getEndpoint();
            if (!url.contains('page=1&') && !url.endsWith('page=1')) {
                res.setBody('[]');
            } else if (url.contains('/plans')) {
                res.setBody('[' + PLAN + ']');
            } else {
                res.setBody('[' + RECURRING + ']');
            }
            return res;
        }
    }

    @IsTest
    static void queueableReadsDonorboxAndSyncs() {
        useDesk();
        Test.setMock(HttpCalloutMock.class, new DonorboxMock());
        Test.startTest();
        System.enqueueJob(new DonorboxSync());
        Test.stopTest();
        System.assertEquals(1, [SELECT COUNT() FROM GiftTransaction WHERE Transaction_ID__c = '9001']);
    }

    @IsTest
    static void apiErrorIsReportedNotThrown() {
        Test.setMock(HttpCalloutMock.class, new ErrorMock());
        DonorboxSync sync = new DonorboxSync();
        Test.startTest();
        sync.run();
        Test.stopTest();
        System.assertEquals(1, sync.errors.size());
        System.assert(sync.errors[0].contains('HTTP 401'));
    }

    private class ErrorMock implements HttpCalloutMock {
        public HttpResponse respond(HttpRequest req) {
            HttpResponse res = new HttpResponse();
            res.setStatusCode(401);
            res.setBody('{"error":"Authentication failed"}');
            return res;
        }
    }

    @IsTest
    static void schedulesNightly() {
        Test.startTest();
        Id jobId = DonorboxSyncScheduler.scheduleNightly();
        Test.stopTest();
        System.assertNotEquals(null, jobId);
    }
}