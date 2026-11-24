/**
 * Nightly Donorbox → Nonprofit Cloud sync.
 *
 *  1. Reads ALL plans and the last N days of donations from Donorbox (callouts first, DML after).
 *  2. Donors: Person Account matched by email (newest wins). Existing accounts are never changed.
 *     No match → new Person Account (AccountSource = Donorbox, Shipping address).
 *  3. Plans → GiftCommitment (Plan_ID__c) + GiftCommitmentSchedule. Cancelled → Canceled (schedule ended),
 *     paused and failed → stay Active.
 *  4. Donations → GiftTransaction (Transaction_ID__c). A recurring payment fills the nearest
 *     Unpaid installment on its commitment; if none exists, a transaction is created on it.
 *     Paid gifts get Overhead Amount = 15% of the Donorbox amount (Donorbox is USD only).
 *  5. Refunds → GiftRefund + status Fully Refunded.
 *  6. Problems are emailed to the members of the "Donorbox" public group. Anomalies are reported there
 *     too (records are still saved): a matched donor on a desk other than the Donorbox desk, and a
 *     plan or donation that is not in USD.
 *
 * Re-running is safe: everything is matched on Donorbox ids before anything is created.
 *
 * Run now:         System.enqueueJob(new DonorboxSync());
 * Backfill range:  System.enqueueJob(new DonorboxSync(Date.newInstance(2025,1,1), Date.newInstance(2025,3,31)));
 */
// without sharing: matching must see every account and gift, or it would create duplicates.
public without sharing class DonorboxSync implements Queueable, Database.AllowsCallouts {

    @TestVisible static final String COMMITMENT_SOURCE = 'Internet';
    @TestVisible static final String CLEARANCE_SYSTEM = 'Donorbox';
    @TestVisible static final String ACCOUNT_SOURCE = 'Donorbox';
    @TestVisible static final String PAYMENT_METHOD = 'DonorBox';
    // Fixed values the client asked for on every Donorbox gift / commitment.
    @TestVisible static final String DONATION_SOURCE = 'Donor via Association';
    @TestVisible static final String OPERATIONAL = 'Operational';
    @TestVisible static final String TRANSFERRED_BY = 'YV USA Foundation';
    @TestVisible static final Decimal OVERHEAD_PERCENT = 15;
    @TestVisible static final String DEFAULT_LANGUAGE = 'English';
    @TestVisible static final String ACCOUNT_STATUS = 'Active';
    @TestVisible static final String DESIGNATION_NAME = 'General Donation - Operational Budget';
    /** Your exchange-rate flow only converts gifts due on or after this date; older ones get their real amount. */
    @TestVisible static final Date CONVERSION_START = Date.newInstance(2025, 10, 16);
    /** Gifts handled per batch; the run stops between batches when it nears Salesforce limits. */
    @TestVisible static Integer GIFT_BATCH = 50;

    private final Date fromDate;
    private final Date toDate;
    private Donorbox_Sync_Settings__c settings;
    private Date today;
    private Id personRecordTypeId;

    @TestVisible private List<String> errors = new List<String>();
    /** Upper limit of new commitments per run; the run also stops early if it gets close to Salesforce limits. */
    @TestVisible static Integer MAX_NEW_COMMITMENTS = 50;
    @TestVisible private Boolean morePending = false;
    private Id designationId;
    private Boolean designationLooked = false;
    /** Matched donor accounts whose desk is not the Donorbox desk (Default Desk Id): account Id → desk name. */
    @TestVisible private Map<Id, String> offDesk = new Map<Id, String>();
    @TestVisible private Map<String, Integer> counts = new Map<String, Integer>();

    public DonorboxSync() {
        this(null, null);
    }

    public DonorboxSync(Date fromDate, Date toDate) {
        this.fromDate = fromDate;
        this.toDate = toDate;
    }

    public void execute(QueueableContext ctx) {
        run();
    }

    public void run() {
        init();
        try {
            Date fromD = fromDate != null ? fromDate : today.addDays(-intSetting(settings.Lookback_Days__c, 60));
            Map<String, String> params = new Map<String, String>{ 'date_from' => String.valueOf(fromD) };
            if (toDate != null) {
                params.put('date_to', String.valueOf(toDate));
            }
            List<Map<String, Object>> plans = DonorboxApi.getAll('plans', new Map<String, String>());
            List<Map<String, Object>> donations = DonorboxApi.getAll('donations', params);
            process(plans, donations);
        } catch (Exception e) {
            errors.add('Run stopped: ' + e.getTypeName() + ': ' + e.getMessage() + '\n' + e.getStackTraceString());
        }
        // More new plans than one run can take: queue a follow-up run (only if this one made progress).
        Boolean progressed = counts.get('commitments created') != null || counts.get('transactions created') != null
            || counts.get('transactions updated') != null;
        if (morePending && progressed && !Test.isRunningTest()) {
            System.enqueueJob(new DonorboxSync(fromDate, toDate));
            notifyIfNeeded(true);
        } else {
            notifyIfNeeded(false);
        }
    }

    /** Everything after the callouts (split out so tests can run it without callouts). */
    @TestVisible
    private void process(List<Map<String, Object>> plans, List<Map<String, Object>> donations) {
        init();
        bump('plans read', plans.size());
        bump('donations read', donations.size());
        Map<String, Id> accounts = resolveAccounts(plans, donations);
        Map<String, GiftCommitment> commitments = syncPlans(plans, accounts);
        syncDonations(donations, accounts, commitments);
    }

    private void init() {
        if (settings != null) return;
        settings = Donorbox_Sync_Settings__c.getOrgDefaults();
        today = Date.today();
    }

    // ───────────────────────────── Donors ─────────────────────────────

    private Map<String, Id> resolveAccounts(List<Map<String, Object>> plans, List<Map<String, Object>> donations) {
        // Latest Donorbox details per email (donations come oldest first, so later ones win).
        Map<String, Map<String, Object>> donorByEmail = new Map<String, Map<String, Object>>();
        List<Map<String, Object>> all = new List<Map<String, Object>>();
        all.addAll(plans);
        all.addAll(donations);
        for (Map<String, Object> rec : all) {
            Map<String, Object> donor = DonorboxUtil.obj(rec, 'donor');
            String email = DonorboxUtil.normEmail(DonorboxUtil.str(donor, 'email'));
            if (email == null) continue;
            // Merge: newer non-blank values win, but blanks never erase (donations often carry no address).
            Map<String, Object> merged = donorByEmail.containsKey(email) ? donorByEmail.get(email) : new Map<String, Object>();
            for (String k : donor.keySet()) {
                Object v = donor.get(k);
                if (v != null && !(v instanceof String && String.isBlank((String) v))) merged.put(k, v);
                else if (!merged.containsKey(k)) merged.put(k, v);
            }
            donorByEmail.put(email, merged);
        }
        Map<String, Id> result = new Map<String, Id>();
        if (donorByEmail.isEmpty()) return result;

        Set<String> emails = donorByEmail.keySet();
        Id donorboxDesk = String.isNotBlank(settings.Default_Desk_Id__c) ? Id.valueOf(settings.Default_Desk_Id__c) : null;
        for (Account a : [
            SELECT Id, PersonEmail, Desk__c, Desk__r.Name FROM Account
            WHERE IsPersonAccount = true AND PersonEmail IN :emails
            ORDER BY CreatedDate DESC
        ]) {
            String e = DonorboxUtil.normEmail(a.PersonEmail);
            if (e != null && !result.containsKey(e)) {
                result.put(e, a.Id); // newest account wins
                // Donorbox donors belong to the Donorbox (USA) desk; anything else is an anomaly to report.
                // The account is not changed (existing donors are never changed).
                if (donorboxDesk != null && a.Desk__c != donorboxDesk) {
                    offDesk.put(a.Id, a.Desk__c == null ? 'no desk' : 'desk ' + a.Desk__r.Name);
                }
            }
        }

        List<Account> toCreate = new List<Account>();
        List<String> createEmails = new List<String>();
        for (String email : emails) {
            if (!result.containsKey(email)) {
                toCreate.add(newAccount(donorByEmail.get(email)));
                createEmails.add(email);
            }
        }
        if (!toCreate.isEmpty()) {
            Database.SaveResult[] srs = Database.insert(toCreate, false);
            for (Integer i = 0; i < srs.size(); i++) {
                if (srs[i].isSuccess()) {
                    result.put(createEmails[i], srs[i].getId());
                    bump('accounts created', 1);
                } else {
                    errors.add('Could not create account for ' + createEmails[i] + ': ' + messages(srs[i].getErrors()));
                }
            }
        }
        return result;
    }

    private Account newAccount(Map<String, Object> d) {
        String first = DonorboxUtil.str(d, 'first_name');
        String last = DonorboxUtil.str(d, 'last_name');
        if (last == null) {
            String full = DonorboxUtil.str(d, 'name');
            if (full != null && full.contains(' ')) {
                first = first != null ? first : full.substringBeforeLast(' ');
                last = full.substringAfterLast(' ');
            } else {
                last = full != null ? full : DonorboxUtil.str(d, 'email').substringBefore('@');
            }
        }
        Account a = new Account(
            RecordTypeId = personRecordType(),
            LastName = last.left(80),
            PersonEmail = DonorboxUtil.str(d, 'email'),
            AccountSource = ACCOUNT_SOURCE,
            Status__c = ACCOUNT_STATUS,
            ShippingStreet = DonorboxUtil.street(DonorboxUtil.str(d, 'address'), DonorboxUtil.str(d, 'address_line_2')),
            ShippingCity = DonorboxUtil.str(d, 'city'),
            ShippingState = DonorboxUtil.str(d, 'state'),
            ShippingPostalCode = DonorboxUtil.str(d, 'zip_code'),
            ShippingCountry = DonorboxUtil.countryName(DonorboxUtil.str(d, 'country'))
        );
        if (first != null) a.FirstName = first.left(40);
        String phone = DonorboxUtil.str(d, 'phone');
        if (phone != null) a.Phone = phone.left(40);
        if (String.isNotBlank(settings.Default_Desk_Id__c)) {
            a.put('Desk__c', Id.valueOf(settings.Default_Desk_Id__c));
        }
        return a;
    }

    private Id personRecordType() {
        if (personRecordTypeId != null) return personRecordTypeId;
        String devName = settings.Person_Record_Type__c;
        List<RecordType> rts = String.isNotBlank(devName)
            ? [SELECT Id FROM RecordType WHERE SobjectType = 'Account' AND IsPersonType = true AND DeveloperName = :devName LIMIT 1]
            : [SELECT Id FROM RecordType WHERE SobjectType = 'Account' AND IsPersonType = true AND IsActive = true ORDER BY CreatedDate LIMIT 1];
        if (rts.isEmpty()) {
            throw new DonorboxApi.DonorboxException('No Person Account record type found'
                + (String.isNotBlank(devName) ? ' named ' + devName : ''));
        }
        personRecordTypeId = rts[0].Id;
        return personRecordTypeId;
    }

    // ───────────────────────────── Plans ─────────────────────────────

    private Map<String, GiftCommitment> syncPlans(List<Map<String, Object>> plans, Map<String, Id> accounts) {
        Map<String, Map<String, Object>> planById = new Map<String, Map<String, Object>>();
        for (Map<String, Object> p : plans) {
            String pid = DonorboxUtil.str(p, 'id');
            if (pid != null) planById.put(pid, p);
        }
        Map<String, GiftCommitment> byPlan = new Map<String, GiftCommitment>();
        if (planById.isEmpty()) return byPlan;

        Set<String> planIds = planById.keySet();
        for (GiftCommitment c : queryCommitments(planIds, new Set<Id>())) byPlan.put(c.Plan_ID__c, c);

        // 1. New plans → commitments, created with their final status (this org has no Draft status).
        Set<Id> donorIds = new Set<Id>(accounts.values());
        Map<Id, Account> donorNames = new Map<Id, Account>([SELECT Id, Name FROM Account WHERE Id IN :donorIds]);
        List<GiftCommitment> newCmts = new List<GiftCommitment>();
        List<String> newPids = new List<String>();
        for (String pid : planIds) {
            if (byPlan.containsKey(pid)) continue;
            Map<String, Object> p = planById.get(pid);
            Id donorId = accounts.get(DonorboxUtil.normEmail(DonorboxUtil.str(DonorboxUtil.obj(p, 'donor'), 'email')));
            if (donorId == null) {
                errors.add('Plan ' + pid + ': no donor account (missing email?)');
                continue;
            }
            reportAnomalies('Plan ' + pid, donorId, p);
            String cur = DonorboxUtil.str(p, 'currency');
            Date started = DonorboxUtil.toDate(DonorboxUtil.str(p, 'started_at'));
            String type = DonorboxUtil.str(p, 'type');
            // e.g. "Judy Test - 18$ monthly - 2025-12-03"
            String cname = (donorNames.containsKey(donorId) ? donorNames.get(donorId).Name : 'Donorbox donor')
                + ' - ' + DonorboxUtil.money(DonorboxUtil.dec(p, 'amount'), cur)
                + (type == null ? '' : ' ' + type.toLowerCase())
                + (started == null ? '' : ' - ' + String.valueOf(started));
            GiftCommitment c = new GiftCommitment(
                Name = cname.left(255),
                DonorId = donorId,
                Operational_Development__c = OPERATIONAL,
                Plan_ID__c = pid,
                Status = DonorboxUtil.commitmentStatus(DonorboxUtil.str(p, 'status')),
                ScheduleType = 'Recurring',
                RecurrenceType = 'OpenEnded',
                EffectiveStartDate = started,
                Commitment_Source__c = COMMITMENT_SOURCE,
                Payment_Method__c = PAYMENT_METHOD, // your flow copies it to the schedule
                Amount_in_Original_Currency__c = DonorboxUtil.dec(p, 'amount') // your flow builds the schedule from this
            );
            if (cur != null) c.Currency__c = cur.toUpperCase();
            newCmts.add(c);
            newPids.add(pid);
        }
        Set<Id> justCreated = new Set<Id>();
        // Your flow's "Create Recurring Gift Commitment Schedule" action takes one commitment at a time,
        // so they're saved one by one, a few per run. Any left over are picked up by a follow-up job.
        for (Integer i = 0; i < newCmts.size(); i++) {
            // Stop while there's still room for the rest of the run (each save runs your flows).
            if (i >= MAX_NEW_COMMITMENTS
                    || Limits.getQueries() > Limits.getLimitQueries() / 2
                    || Limits.getDmlStatements() > Limits.getLimitDmlStatements() / 2
                    || Limits.getCpuTime() > Limits.getLimitCpuTime() / 2) {
                morePending = true;
                break;
            }
            Database.SaveResult sr = Database.insert(newCmts[i], false);
            if (sr.isSuccess()) {
                justCreated.add(newCmts[i].Id);
                bump('commitments created', 1);
            } else {
                errors.add('Plan ' + newPids[i] + ': could not create commitment: ' + messages(sr.getErrors()));
            }
        }

        addCommitmentDesignations(justCreated);

        // Re-read new commitments to pick up the schedule your flow just created for them.
        if (!justCreated.isEmpty()) {
            for (GiftCommitment c : queryCommitments(new Set<String>(), justCreated)) byPlan.put(c.Plan_ID__c, c);
        }

        // 2. Work out schedule changes and status changes.
        Map<Id, GiftCommitmentSchedule> schedUpdateMap = new Map<Id, GiftCommitmentSchedule>();
        List<GiftCommitmentSchedule> schedInserts = new List<GiftCommitmentSchedule>();
        List<String> schedInsertPids = new List<String>();
        Map<Id, String> wantStatus = new Map<Id, String>();
        Map<Id, Decimal> amountUpdates = new Map<Id, Decimal>();
        // Salesforce won't end a schedule before its last installment's due date.
        Map<Id, Date> lastDue = new Map<Id, Date>();
        Set<Id> cmtIds = new Set<Id>();
        for (GiftCommitment c : byPlan.values()) cmtIds.add(c.Id);
        for (AggregateResult ar : [SELECT GiftCommitmentId cid, MAX(TransactionDueDate) d FROM GiftTransaction
                                   WHERE GiftCommitmentId IN :cmtIds GROUP BY GiftCommitmentId]) {
            lastDue.put((Id) ar.get('cid'), (Date) ar.get('d'));
        }

        for (String pid : byPlan.keySet()) {
            GiftCommitment c = byPlan.get(pid);
            Map<String, Object> p = planById.get(pid);
            String status = DonorboxUtil.commitmentStatus(DonorboxUtil.str(p, 'status'));
            Date start = DonorboxUtil.toDate(DonorboxUtil.str(p, 'started_at'));
            Decimal amount = DonorboxUtil.dec(p, 'amount');
            wantStatus.put(c.Id, status);

            GiftCommitmentSchedule cur = c.CurrentGiftCmtSchedule;
            Decimal oldAmount = c.Amount_in_Original_Currency__c;
            if (c.CurrentGiftCmtScheduleId == null || cur == null) {
                // Your flow didn't create a schedule (or an earlier run failed): create it here.
                schedInserts.add(buildSchedule(p, c.Id, start, status == 'Canceled' ? endDate(p, start) : null, amount));
                schedInsertPids.add(pid);
                continue;
            }
            if (justCreated.contains(c.Id)) {
                // The flow always builds Monthly; correct the period for quarterly/yearly/weekly plans.
                List<Object> cad = DonorboxUtil.cadence(DonorboxUtil.str(p, 'type'));
                if (cur.TransactionPeriod != (String) cad[0] || cur.TransactionInterval != (Integer) cad[1]) {
                    GiftCommitmentSchedule fix = new GiftCommitmentSchedule(Id = cur.Id,
                        TransactionPeriod = (String) cad[0], TransactionInterval = (Integer) cad[1]);
                    if ((String) cad[0] == 'Weekly') fix.TransactionDay = null;
                    mergeInto(schedUpdateMap, fix);
                }
            }
            if (status == 'Canceled') {
                if (cur.EndDate == null) {
                    Date e = endDate(p, cur.StartDate);
                    Date due = lastDue.get(c.Id);
                    if (due != null && due >= e) e = due.addDays(1);
                    mergeInto(schedUpdateMap, new GiftCommitmentSchedule(Id = c.CurrentGiftCmtScheduleId, EndDate = e));
                }
            } else if (status == 'Active') {
                Boolean ended = cur.EndDate != null && cur.EndDate < today;
                // Compare in the plan's own currency; the schedule holds the flow's USD amount.
                Boolean amountChanged = amount != null && oldAmount != null && oldAmount != amount;
                // Keep the exchange rate the flow used: new USD amount = old USD amount × new / old.
                Decimal schedAmount = (amountChanged && oldAmount != 0 && cur.TransactionAmount != null)
                    ? (cur.TransactionAmount * amount / oldAmount).setScale(2) : cur.TransactionAmount;
                if (amountChanged) amountUpdates.put(c.Id, amount);
                if (ended) {
                    // Plan was resumed: start a fresh schedule today.
                    schedInserts.add(buildSchedule(p, c.Id, today, null, schedAmount));
                    schedInsertPids.add(pid);
                } else if (amountChanged && cur.StartDate < today) {
                    // Amount changed: end the old schedule yesterday, new one from today (keeps history).
                    mergeInto(schedUpdateMap, new GiftCommitmentSchedule(Id = c.CurrentGiftCmtScheduleId, EndDate = today.addDays(-1)));
                    schedInserts.add(buildSchedule(p, c.Id, today, null, schedAmount));
                    schedInsertPids.add(pid);
                } else if (amountChanged) {
                    mergeInto(schedUpdateMap, new GiftCommitmentSchedule(Id = c.CurrentGiftCmtScheduleId, TransactionAmount = schedAmount));
                }
            }
            // (Paused plans map to Active, so they're handled like active ones.)
        }

        // 3. Apply: end/adjust old schedules, add new ones, then commitment status + current schedule.
        List<GiftCommitmentSchedule> schedUpdates = schedUpdateMap.values();
        if (!schedUpdates.isEmpty()) {
            Database.SaveResult[] srs = Database.update(schedUpdates, false);
            for (Integer i = 0; i < srs.size(); i++) {
                if (!srs[i].isSuccess()) {
                    errors.add('Schedule ' + schedUpdates[i].Id + ': ' + messages(srs[i].getErrors()));
                }
            }
        }
        Map<Id, GiftCommitment> cmtUpdates = new Map<Id, GiftCommitment>();
        if (!schedInserts.isEmpty()) {
            Database.SaveResult[] srs = Database.insert(schedInserts, false);
            for (Integer i = 0; i < srs.size(); i++) {
                GiftCommitment c = byPlan.get(schedInsertPids[i]);
                if (srs[i].isSuccess()) {
                    c.CurrentGiftCmtScheduleId = srs[i].getId();
                    cmtUpdates.put(c.Id, new GiftCommitment(Id = c.Id, CurrentGiftCmtScheduleId = srs[i].getId()));
                } else {
                    wantStatus.remove(c.Id); // schedule is retried next run
                    errors.add('Plan ' + schedInsertPids[i] + ': could not create schedule: ' + messages(srs[i].getErrors()));
                }
            }
        }
        for (Id cid : amountUpdates.keySet()) {
            GiftCommitment u = cmtUpdates.containsKey(cid) ? cmtUpdates.get(cid) : new GiftCommitment(Id = cid);
            u.Amount_in_Original_Currency__c = amountUpdates.get(cid);
            cmtUpdates.put(cid, u);
        }
        for (String pid : byPlan.keySet()) {
            GiftCommitment c = byPlan.get(pid);
            String want = wantStatus.get(c.Id);
            if (want == null || want == c.Status) continue;
            GiftCommitment u = cmtUpdates.containsKey(c.Id) ? cmtUpdates.get(c.Id) : new GiftCommitment(Id = c.Id);
            u.Status = want;
            cmtUpdates.put(c.Id, u);
        }
        if (!cmtUpdates.isEmpty()) {
            List<GiftCommitment> ups = cmtUpdates.values();
            Database.SaveResult[] srs = Database.update(ups, false);
            for (Integer i = 0; i < srs.size(); i++) {
                if (srs[i].isSuccess()) {
                    bump('commitments updated', 1);
                } else {
                    errors.add('Commitment ' + ups[i].Id + ': ' + messages(srs[i].getErrors()));
                }
            }
        }
        return byPlan;
    }

    /** Several changes to one schedule in a run go out as one update. */
    private static void mergeInto(Map<Id, GiftCommitmentSchedule> m, GiftCommitmentSchedule s) {
        GiftCommitmentSchedule cur = m.get(s.Id);
        if (cur == null) { m.put(s.Id, s); return; }
        for (String f : s.getPopulatedFieldsAsMap().keySet()) cur.put(f, s.get(f));
    }

    /** The designation every Donorbox gift and commitment gets (looked up by name, so it works in every org). */
    private Id designation() {
        if (!designationLooked) {
            designationLooked = true;
            List<GiftDesignation> ds = [SELECT Id FROM GiftDesignation WHERE Name = :DESIGNATION_NAME LIMIT 1];
            if (ds.isEmpty()) errors.add('Gift Designation "' + DESIGNATION_NAME + '" not found; designations were skipped.');
            else designationId = ds[0].Id;
        }
        return designationId;
    }

    /** One Gift Default Designation (100%) on each new commitment. */
    private void addCommitmentDesignations(Set<Id> cmtIds) {
        if (cmtIds.isEmpty() || designation() == null) return;
        Set<Id> has = new Set<Id>();
        for (GiftDefaultDesignation x : [SELECT ParentRecordId FROM GiftDefaultDesignation WHERE ParentRecordId IN :cmtIds]) {
            has.add(x.ParentRecordId);
        }
        List<GiftDefaultDesignation> rows = new List<GiftDefaultDesignation>();
        for (Id cid : cmtIds) {
            if (has.contains(cid)) continue;
            GiftDefaultDesignation x = new GiftDefaultDesignation(ParentRecordId = cid, GiftDesignationId = designationId, AllocatedPercentage = 100);
            x.put('Gift_Commitment__c', cid);
            rows.add(x);
        }
        saveAll(rows, 'Commitment designation');
    }

    /** One Gift Transaction Designation (100%) on each gift that has none yet. */
    private void addGiftDesignations(Set<Id> gtIds) {
        if (gtIds.isEmpty() || designation() == null) return;
        Set<Id> has = new Set<Id>();
        for (GiftTransactionDesignation x : [SELECT GiftTransactionId FROM GiftTransactionDesignation WHERE GiftTransactionId IN :gtIds]) {
            has.add(x.GiftTransactionId);
        }
        // Salesforce checks Amount against Percent, so both are set: 100% = the gift's (converted) amount.
        List<GiftTransactionDesignation> rows = new List<GiftTransactionDesignation>();
        for (GiftTransaction g : [SELECT Id, OriginalAmount FROM GiftTransaction WHERE Id IN :gtIds]) {
            if (has.contains(g.Id)) continue;
            rows.add(new GiftTransactionDesignation(GiftTransactionId = g.Id, GiftDesignationId = designationId,
                Percent = 100, Amount = g.OriginalAmount));
        }
        saveAll(rows, 'Gift designation');
    }

    private void saveAll(List<SObject> rows, String what) {
        if (rows.isEmpty()) return;
        Database.SaveResult[] srs = Database.insert(rows, false);
        for (Integer i = 0; i < srs.size(); i++) {
            if (!srs[i].isSuccess()) errors.add(what + ': ' + messages(srs[i].getErrors()));
        }
    }

    private List<GiftCommitment> queryCommitments(Set<String> planIds, Set<Id> ids) {
        return [
            SELECT Id, Plan_ID__c, Status, Amount_in_Original_Currency__c, CurrentGiftCmtScheduleId,
                   CurrentGiftCmtSchedule.Id, CurrentGiftCmtSchedule.StartDate, CurrentGiftCmtSchedule.EndDate,
                   CurrentGiftCmtSchedule.TransactionAmount, CurrentGiftCmtSchedule.TransactionPeriod,
                   CurrentGiftCmtSchedule.TransactionInterval
            FROM GiftCommitment WHERE Plan_ID__c IN :planIds OR Id IN :ids
        ];
    }

    private GiftCommitmentSchedule buildSchedule(Map<String, Object> p, Id commitmentId, Date start, Date endD, Decimal amount) {
        List<Object> cad = DonorboxUtil.cadence(DonorboxUtil.str(p, 'type'));
        String period = (String) cad[0];
        GiftCommitmentSchedule s = new GiftCommitmentSchedule(
            GiftCommitmentId = commitmentId,
            StartDate = start,
            EndDate = endD,
            TransactionAmount = amount,
            TransactionPeriod = period,
            TransactionInterval = (Integer) cad[1],
            PaymentMethod = PAYMENT_METHOD,
            Type = 'CreateTransactions'
        );
        if (period != 'Weekly') s.TransactionDay = DonorboxUtil.transactionDay(start);
        String cur = DonorboxUtil.str(p, 'currency');
        if (cur != null) s.Currency__c = cur.toUpperCase();
        return s;
    }

    /** End date for a stopped plan: the day it stopped, never before the schedule start. */
    private Date endDate(Map<String, Object> p, Date start) {
        Date stopped = DonorboxUtil.toDate(DonorboxUtil.str(p, 'stopped_at'));
        Date d = stopped != null ? stopped : today;
        return (start != null && d < start) ? start : d;
    }

    // ───────────────────────────── Donations ─────────────────────────────

    private void syncDonations(List<Map<String, Object>> donations, Map<String, Id> accounts, Map<String, GiftCommitment> cmts) {
        Map<String, Map<String, Object>> byId = new Map<String, Map<String, Object>>();
        List<String> order = new List<String>();
        for (Map<String, Object> d : donations) {
            String did = DonorboxUtil.str(d, 'id');
            if (did != null && !byId.containsKey(did)) {
                byId.put(did, d);
                order.add(did);
            }
        }
        if (byId.isEmpty()) return;

        Set<String> ids = byId.keySet();
        Map<String, GiftTransaction> existing = new Map<String, GiftTransaction>();
        for (GiftTransaction g : [SELECT Id, Transaction_ID__c, Status, Overhead_Amount__c FROM GiftTransaction WHERE Transaction_ID__c IN :ids]) {
            existing.put(g.Transaction_ID__c, g);
        }

        // Open (Unpaid) installments that new recurring payments can fill.
        Set<Id> cmtIds = new Set<Id>();
        Date minD;
        Date maxD;
        Integer window = intSetting(settings.Installment_Match_Days__c, 20);
        for (String did : order) {
            Map<String, Object> d = byId.get(did);
            GiftCommitment c = cmts.get(DonorboxUtil.str(d, 'plan_id'));
            if (existing.containsKey(did) || c == null) continue;
            cmtIds.add(c.Id);
            Date dt = DonorboxUtil.toDate(DonorboxUtil.str(d, 'donation_date'));
            if (dt == null) continue;
            if (minD == null || dt < minD) minD = dt;
            if (maxD == null || dt > maxD) maxD = dt;
        }
        Map<Id, List<GiftTransaction>> openByCmt = new Map<Id, List<GiftTransaction>>();
        if (!cmtIds.isEmpty() && minD != null) {
            Date lo = minD.addDays(-window);
            Date hi = maxD.addDays(window);
            for (GiftTransaction g : [
                SELECT Id, GiftCommitmentId, TransactionDueDate FROM GiftTransaction
                WHERE GiftCommitmentId IN :cmtIds AND Status = 'Unpaid' AND Transaction_ID__c = null
                  AND TransactionDueDate >= :lo AND TransactionDueDate <= :hi
                ORDER BY TransactionDueDate
            ]) {
                if (!openByCmt.containsKey(g.GiftCommitmentId)) openByCmt.put(g.GiftCommitmentId, new List<GiftTransaction>());
                openByCmt.get(g.GiftCommitmentId).add(g);
            }
        }

        Map<String, Id> gtByDid = new Map<String, Id>();
        for (String did : existing.keySet()) gtByDid.put(did, existing.get(did).Id);
        Set<Id> donorIds = new Set<Id>(accounts.values());
        Map<Id, Account> donorNames = new Map<Id, Account>([SELECT Id, Name FROM Account WHERE Id IN :donorIds]);

        // Work lists. A gift that should end up Paid is first saved Unpaid, then updated to Paid with its
        // completion and conversion dates: that is the moment your exchange-rate flow converts the amount.
        List<GiftTransaction> inserts = new List<GiftTransaction>();
        List<String> insertDids = new List<String>();
        List<GiftTransaction> fills = new List<GiftTransaction>();      // existing installments being paid
        List<String> fillDids = new List<String>();
        List<GiftTransaction> statusOnly = new List<GiftTransaction>(); // already synced gifts
        List<String> statusDids = new List<String>();
        Set<Id> claimed = new Set<Id>();

        for (String did : order) {
            Map<String, Object> d = byId.get(did);
            String txStatus = writeStatus(d);
            Date paidOn = DonorboxUtil.toDate(DonorboxUtil.str(d, 'donation_date'));
            GiftTransaction ex = existing.get(did);
            if (ex != null) {
                // Already synced: only move the status along (staff edits to other fields are kept).
                GiftTransaction u;
                if (ex.Status != txStatus && ex.Status != 'Fully Refunded') {
                    u = new GiftTransaction(Id = ex.Id, Status = txStatus);
                    if (txStatus == 'Paid') {
                        markPaid(u, paidOn);
                        setOverhead(u, d);
                    }
                } else if (ex.Status == 'Paid' && ex.Overhead_Amount__c == null && overheadFor(d) != null) {
                    // Paid before overhead was calculated: fill it in now.
                    u = new GiftTransaction(Id = ex.Id);
                    setOverhead(u, d);
                }
                if (u != null) {
                    statusOnly.add(u);
                    statusDids.add(did);
                }
                continue;
            }
            Id donorId = accounts.get(DonorboxUtil.normEmail(DonorboxUtil.str(DonorboxUtil.obj(d, 'donor'), 'email')));
            if (donorId == null) {
                errors.add('Donation ' + did + ': no donor account (missing email?)');
                continue;
            }
            reportAnomalies('Donation ' + did, donorId, d);
            GiftTransaction g = buildTransaction(d, did);
            String planId = DonorboxUtil.str(d, 'plan_id');
            if (planId != null) {
                GiftCommitment c = cmts.get(planId);
                if (c == null) {
                    errors.add('Donation ' + did + ': its plan ' + planId + ' is not in Salesforce yet. Will retry next run.');
                    continue;
                }
                GiftTransaction slot = nearestOpen(openByCmt.get(c.Id), paidOn, claimed, window);
                if (slot != null) {
                    // Fill the installment NPC already generated: one update, Unpaid → final status.
                    g.Id = slot.Id;
                    g.Status = txStatus;
                    if (txStatus == 'Paid') {
                        markPaid(g, paidOn);
                        setOverhead(g, d);
                    }
                    // Your flow only converts installments due on/after CONVERSION_START; older ones get the
                    // real (USD) amount here, in case it differs from what the schedule put on the installment.
                    if (slot.TransactionDueDate < CONVERSION_START) g.OriginalAmount = DonorboxUtil.dec(d, 'amount');
                    claimed.add(slot.Id);
                    fills.add(g);
                    fillDids.add(did);
                    continue;
                }
                g.GiftCommitmentId = c.Id;
                if (c.CurrentGiftCmtScheduleId != null) g.GiftCommitmentScheduleId = c.CurrentGiftCmtScheduleId;
            }
            Boolean converts = paidOn != null && paidOn >= CONVERSION_START;
            g.DonorId = donorId;
            // Same pattern as your gift naming: "Judy Test - 18$ - 2026-10-03" (other currencies: "50 EUR").
            g.Name = ((donorNames.containsKey(donorId) ? donorNames.get(donorId).Name : 'Donorbox donor')
                + ' - ' + DonorboxUtil.money(DonorboxUtil.dec(d, 'amount'), DonorboxUtil.str(d, 'currency'))
                + (paidOn == null ? '' : ' - ' + String.valueOf(paidOn))).left(255);
            g.TransactionDueDate = paidOn;
            // Placeholder 1 is replaced by your exchange-rate flow; it skips gifts due before CONVERSION_START.
            g.OriginalAmount = converts ? 1 : DonorboxUtil.dec(d, 'amount');
            if (txStatus == 'Paid' && !converts) { // nothing to convert: straight to Paid
                markPaid(g, paidOn);
                setOverhead(g, d);
            }
            g.Status = (txStatus == 'Paid' && converts) ? 'Unpaid' : txStatus;
            inserts.add(g);
            insertDids.add(did);
        }

        // New gifts, in batches: insert, then the Paid step, then the designation.
        for (Integer start = 0; start < inserts.size(); start += GIFT_BATCH) {
            if (nearLimits()) { morePending = true; break; }
            List<GiftTransaction> batch = new List<GiftTransaction>();
            List<String> batchDids = new List<String>();
            for (Integer k = start; k < Math.min(start + GIFT_BATCH, inserts.size()); k++) {
                batch.add(inserts[k]);
                batchDids.add(insertDids[k]);
            }
            Set<Id> saved = new Set<Id>();
            List<GiftTransaction> pay = new List<GiftTransaction>();
            List<String> payDids = new List<String>();
            Database.SaveResult[] srs = Database.insert(batch, false);
            for (Integer k = 0; k < srs.size(); k++) {
                if (!srs[k].isSuccess()) {
                    errors.add('Donation ' + batchDids[k] + ': ' + messages(srs[k].getErrors()));
                    continue;
                }
                gtByDid.put(batchDids[k], srs[k].getId());
                saved.add(srs[k].getId());
                bump('transactions created', 1);
                // Second save. Your flow copies the commitment's source (Internet) onto new recurring gifts,
                // so Donation Source is set again here; and gifts that should be Paid become Paid now.
                GiftTransaction u = new GiftTransaction(Id = srs[k].getId(), Donation_Source__c = DONATION_SOURCE);
                if (batch[k].Status == 'Unpaid' && writeStatus(byId.get(batchDids[k])) == 'Paid') {
                    u.Status = 'Paid';
                    markPaid(u, batch[k].TransactionDueDate);
                    setOverhead(u, byId.get(batchDids[k]));
                }
                pay.add(u);
                payDids.add(batchDids[k]);
            }
            saveUpdates(pay, payDids, 'second save failed (Paid step / Donation Source)', null);
            addGiftDesignations(saved);
        }

        // Installments being filled, in batches.
        for (Integer start = 0; start < fills.size(); start += GIFT_BATCH) {
            if (nearLimits()) { morePending = true; break; }
            List<GiftTransaction> batch = new List<GiftTransaction>();
            List<String> batchDids = new List<String>();
            for (Integer k = start; k < Math.min(start + GIFT_BATCH, fills.size()); k++) {
                batch.add(fills[k]);
                batchDids.add(fillDids[k]);
            }
            Set<Id> saved = saveUpdates(batch, batchDids, null, gtByDid);
            addGiftDesignations(saved);
        }

        // Status changes on gifts synced before.
        if (!statusOnly.isEmpty()) {
            if (nearLimits()) morePending = true;
            else saveUpdates(statusOnly, statusDids, null, gtByDid);
        }

        applyRefunds(byId, gtByDid, existing);
    }

    /** The Paid step: completion date + conversion date (your exchange-rate flow converts on this change). */
    private void markPaid(GiftTransaction g, Date paidOn) {
        g.TransactionDate = paidOn;
        g.put('Conversion_Date__c', paidOn);
    }

    /**
     * Overhead the US foundation keeps: Overhead Percent (15) of the Donorbox amount. Donorbox is USD only,
     * so the amount equals Original Amount and no conversion is needed. Null for non-USD (reported instead).
     */
    @TestVisible
    private static Decimal overheadFor(Map<String, Object> d) {
        Decimal amount = DonorboxUtil.dec(d, 'amount');
        if (amount == null || !isUsd(d)) return null;
        return (amount * OVERHEAD_PERCENT / 100).setScale(2, RoundingMode.HALF_UP);
    }

    private static void setOverhead(GiftTransaction g, Map<String, Object> d) {
        Decimal v = overheadFor(d);
        if (v != null) g.put('Overhead_Amount__c', v);
    }

    private static Boolean isUsd(Map<String, Object> d) {
        String cur = DonorboxUtil.str(d, 'currency');
        return cur == null || cur.equalsIgnoreCase('USD');
    }

    /** Anomalies are reported in the problem email; the record is still saved. */
    private void reportAnomalies(String what, Id donorId, Map<String, Object> rec) {
        if (offDesk.containsKey(donorId)) {
            errors.add(what + ': donor ' + DonorboxUtil.str(DonorboxUtil.obj(rec, 'donor'), 'email') + ' is on '
                + offDesk.get(donorId) + ', not the Donorbox desk. Saved on the donor\'s desk; please check the donor.');
        }
        if (!isUsd(rec)) {
            errors.add(what + ': currency is ' + DonorboxUtil.str(rec, 'currency').toUpperCase()
                + ', not USD. Saved; non-USD gifts get no Overhead Amount.');
        }
    }

    private Boolean nearLimits() {
        return Limits.getQueries() > Limits.getLimitQueries() / 2
            || Limits.getDmlStatements() > Limits.getLimitDmlStatements() / 2
            || Limits.getCpuTime() > Limits.getLimitCpuTime() / 2;
    }

    /** Updates gifts; returns the ids saved. failNote = extra words for the error line. */
    private Set<Id> saveUpdates(List<GiftTransaction> rows, List<String> dids, String failNote, Map<String, Id> gtByDid) {
        Set<Id> saved = new Set<Id>();
        if (rows.isEmpty()) return saved;
        Database.SaveResult[] srs = Database.update(rows, false);
        for (Integer k = 0; k < srs.size(); k++) {
            if (srs[k].isSuccess()) {
                saved.add(srs[k].getId());
                if (gtByDid != null) gtByDid.put(dids[k], srs[k].getId());
                bump('transactions updated', 1);
            } else {
                errors.add('Donation ' + dids[k] + ': ' + (failNote == null ? '' : failNote + ': ') + messages(srs[k].getErrors()));
            }
        }
        return saved;
    }

    private GiftTransaction buildTransaction(Map<String, Object> d, String did) {
        // Only non-empty values are set, so filling an existing installment never blanks a field.
        Decimal amount = DonorboxUtil.dec(d, 'amount');
        GiftTransaction g = new GiftTransaction(
            Transaction_ID__c = did,
            PaymentMethod = PAYMENT_METHOD,
            Clearance_System__c = CLEARANCE_SYSTEM,
            Anonymous_Gift__c = DonorboxUtil.bool(d, 'anonymous_donation'),
            Amount_in_GT_Currency__c = amount,
            Quick_Donation__c = true,
            Donation_Source__c = DONATION_SOURCE,
            Operational_Development__c = OPERATIONAL,
            Transferred_By__c = TRANSFERRED_BY
        );
        g.put('Overhead_Percent__c', OVERHEAD_PERCENT);
        String lang = DonorboxUtil.str(DonorboxUtil.obj(DonorboxUtil.obj(d, 'donor'), 'additional_detail'), 'preferred_language');
        g.External_Form_Language__c = (lang == null ? DEFAULT_LANGUAGE : lang).left(30);

        String ref = DonorboxUtil.str(d, 'stripe_charge_id');
        if (ref == null) ref = DonorboxUtil.str(d, 'paypal_transaction_id');
        if (ref != null) g.GatewayReference = ref.left(80);

        Decimal fee = DonorboxUtil.dec(d, 'processing_fee');
        if (fee != null) g.ProcessorTransactionFee = fee;
        Decimal appFee = DonorboxUtil.dec(d, 'app_fee');
        if (appFee != null) g.GatewayTransactionFee = appFee;
        if (DonorboxUtil.bool(d, 'fee_covered')) {
            Decimal cover = DonorboxUtil.dec(d, 'cover_fee_amount');
            if (cover != null) g.DonorCoverAmount = cover;
        }

        String cur = DonorboxUtil.str(d, 'currency');
        if (cur != null) g.Currency__c = cur.toUpperCase();
        String v = DonorboxUtil.str(d, 'utm_source');
        if (v != null) g.UTM_Source__c = v.left(255);
        v = DonorboxUtil.str(d, 'utm_campaign');
        if (v != null) g.UTM_Campaign__c = v.left(255);
        v = DonorboxUtil.str(d, 'utm_content');
        if (v != null) g.UTM_Content__c = v.left(255);
        v = DonorboxUtil.str(d, 'comment');
        if (v != null) g.Donor_comment__c = v.left(3000);
        v = DonorboxUtil.tribute(DonorboxUtil.obj(d, 'honor'));
        if (v != null) g.Commemoration_Request__c = v;
        v = DonorboxUtil.str(d, 'check_number');
        if (v != null && v.isNumeric()) g.Check_Number__c = Decimal.valueOf(v);
        return g;
    }

    private String writeStatus(Map<String, Object> d) {
        String s = DonorboxUtil.str(d, 'status');
        s = s == null ? '' : s.toLowerCase();
        if (s == 'refunded') return 'Paid';
        if (s == 'paid' || s == 'succeeded' || s == 'success' || s == 'completed') return 'Paid';
        if (s == 'failed' || s == 'declined') return 'Failed';
        if (s == 'canceled' || s == 'cancelled' || s == 'voided') return 'Canceled';
        return 'Pending';
    }

    private GiftTransaction nearestOpen(List<GiftTransaction> open, Date d, Set<Id> claimed, Integer window) {
        if (open == null || d == null) return null;
        GiftTransaction best;
        Integer bestGap;
        for (GiftTransaction g : open) {
            if (claimed.contains(g.Id)) continue;
            Integer gap = Math.abs(g.TransactionDueDate.daysBetween(d));
            if (gap <= window && (bestGap == null || gap < bestGap)) {
                best = g;
                bestGap = gap;
            }
        }
        return best;
    }

    // ───────────────────────────── Refunds ─────────────────────────────

    private void applyRefunds(Map<String, Map<String, Object>> byId, Map<String, Id> gtByDid, Map<String, GiftTransaction> existing) {
        Map<String, Decimal> refundedByDid = new Map<String, Decimal>();
        for (String did : byId.keySet()) {
            Decimal r = DonorboxUtil.dec(byId.get(did), 'amount_refunded');
            Boolean flagged = DonorboxUtil.str(byId.get(did), 'status') == 'refunded';
            if ((r != null && r > 0) || flagged) {
                if (gtByDid.containsKey(did)) refundedByDid.put(did, r == null ? 0 : r);
            }
        }
        if (refundedByDid.isEmpty()) return;

        Set<Id> gtIds = new Set<Id>();
        for (String did : refundedByDid.keySet()) gtIds.add(gtByDid.get(did));
        Map<Id, Decimal> already = new Map<Id, Decimal>();
        for (AggregateResult ar : [
            SELECT GiftTransactionId gtId, SUM(Amount) total FROM GiftRefund
            WHERE GiftTransactionId IN :gtIds GROUP BY GiftTransactionId
        ]) {
            already.put((Id) ar.get('gtId'), (Decimal) ar.get('total'));
        }

        List<GiftRefund> refunds = new List<GiftRefund>();
        List<String> refundDids = new List<String>();
        List<GiftTransaction> fullyRefunded = new List<GiftTransaction>();
        for (String did : refundedByDid.keySet()) {
            Id gtId = gtByDid.get(did);
            Map<String, Object> d = byId.get(did);
            Decimal amount = DonorboxUtil.dec(d, 'amount');
            Decimal refunded = refundedByDid.get(did);
            if (amount == null) continue;
            if (refunded == 0 && DonorboxUtil.str(d, 'status') == 'refunded') refunded = amount;
            Decimal done = already.containsKey(gtId) ? already.get(gtId) : 0;
            Decimal diff = (refunded - done).setScale(2);
            if (diff > 0) {
                GiftRefund r = new GiftRefund(GiftTransactionId = gtId, Amount = diff, Status = 'Completed');
                r.put('Date', today);
                refunds.add(r);
                refundDids.add(did);
            }
            GiftTransaction ex = existing.get(did);
            Boolean isFull = amount != null && refunded >= amount;
            if (isFull && (ex == null || ex.Status != 'Fully Refunded')) {
                fullyRefunded.add(new GiftTransaction(Id = gtId, Status = 'Fully Refunded'));
            }
        }
        if (!refunds.isEmpty()) {
            Database.SaveResult[] srs = Database.insert(refunds, false);
            for (Integer i = 0; i < srs.size(); i++) {
                if (srs[i].isSuccess()) bump('refunds recorded', 1);
                else errors.add('Refund for donation ' + refundDids[i] + ': ' + messages(srs[i].getErrors()));
            }
        }
        if (!fullyRefunded.isEmpty()) {
            Database.SaveResult[] srs = Database.update(fullyRefunded, false);
            for (Integer i = 0; i < srs.size(); i++) {
                if (!srs[i].isSuccess()) {
                    errors.add('Marking ' + fullyRefunded[i].Id + ' Fully Refunded: ' + messages(srs[i].getErrors()));
                }
            }
        }
    }

    // ───────────────────────────── Reporting ─────────────────────────────

    /** continuing = a follow-up run was queued; it will report, so this one only logs. */
    private void notifyIfNeeded(Boolean continuing) {
        String summary = 'Donorbox sync ' + Datetime.now().format() + (continuing ? ' (continues in a follow-up run)' : '')
            + '\n' + JSON.serializePretty(counts);
        System.debug(LoggingLevel.INFO, summary);
        if (errors.isEmpty()) return;

        List<String> lines = new List<String>();
        for (Integer i = 0; i < errors.size() && i < 200; i++) lines.add('• ' + errors[i]);
        if (errors.size() > 200) lines.add('… and ' + (errors.size() - 200) + ' more');
        String body = summary + '\n\nProblems (' + errors.size() + '):\n' + String.join(lines, '\n');
        System.debug(LoggingLevel.ERROR, body);
        if (continuing) return;

        String groupName = String.isNotBlank(settings.Notify_Group__c) ? settings.Notify_Group__c : 'Donorbox';
        Set<Id> userIds = new Set<Id>();
        for (GroupMember gm : [SELECT UserOrGroupId FROM GroupMember WHERE Group.DeveloperName = :groupName]) {
            if (gm.UserOrGroupId.getSObjectType() == User.SObjectType) userIds.add(gm.UserOrGroupId);
        }
        List<String> to = new List<String>();
        for (User u : [SELECT Email FROM User WHERE Id IN :userIds AND IsActive = true]) to.add(u.Email);
        if (to.isEmpty()) return;

        Messaging.SingleEmailMessage m = new Messaging.SingleEmailMessage();
        m.setToAddresses(to);
        m.setSubject('Donorbox sync: ' + errors.size() + ' problem' + (errors.size() == 1 ? '' : 's'));
        m.setPlainTextBody(body);
        try {
            Messaging.sendEmail(new List<Messaging.SingleEmailMessage>{ m });
        } catch (Exception e) {
            System.debug(LoggingLevel.ERROR, 'Could not email the Donorbox group: ' + e.getMessage());
        }
    }

    // ───────────────────────────── Small helpers ─────────────────────────────

    private void bump(String key, Integer n) {
        counts.put(key, (counts.containsKey(key) ? counts.get(key) : 0) + n);
    }

    private static Integer intSetting(Decimal v, Integer fallback) {
        return v == null ? fallback : v.intValue();
    }

    private static String messages(List<Database.Error> errs) {
        List<String> out = new List<String>();
        for (Database.Error e : errs) {
            out.add(e.getMessage() + (e.getFields().isEmpty() ? '' : ' ' + e.getFields()));
        }
        return String.join(out, '; ');
    }
}