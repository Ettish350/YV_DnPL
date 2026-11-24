@isTest
private class TestimonyMonthlyEmailSenderTest {

    /**
     * Deliberately unresolvable. OrgWideEmailAddress is not exempt from test
     * data isolation, so in a normal @isTest context the OWA query returns
     * nothing regardless of what is passed here and the class falls back to
     * the running user. Tests assert that fallback rather than a real sender.
     *
     * If a verified OWA exists in the target org, swap this for that address
     * in the SeeAllData=true test below to assert the resolved path instead.
     */
    private static final String UNRESOLVABLE_OWA = 'no-such-owa@example.invalid';

    /**
     * No Account DML anywhere in this class — deliberately.
     *
     * Inserting Accounts fires the "Account After Create & Update" Flow, which
     * fails on a null CampaignId because test data isolation hides the Campaign
     * it looks up. The activity-logging test therefore reuses an existing
     * Person Account rather than creating one.
     *
     * JSON.deserialize builds fully-populated in-memory records, including the
     * read-only formula fields that can't be assigned in Apex.
     */
    private static Testimony_Documentation__c buildRecord(Map<String, Object> fields) {
        Map<String, Object> data = new Map<String, Object>(fields);
        data.put('attributes', new Map<String, Object>{
            'type' => 'Testimony_Documentation__c'
        });
        return (Testimony_Documentation__c) JSON.deserialize(
            JSON.serialize(data), Testimony_Documentation__c.class
        );
    }

    private static List<Testimony_Documentation__c> buildScenario() {
        return new List<Testimony_Documentation__c>{
            // Interviewer A + Photographer X
            buildRecord(new Map<String, Object>{
                'Testimony_Code_from_Case__c'        => 'TD-0001',
                'Documentation_Date__c'              => DateTime.newInstanceGmt(2025, 10, 15, 7, 0, 0),
                'Interviewer_Amount_For_Payment__c'  => 100,
                'Photographer_Amount_For_Payment__c' => 50,
                'interview_email_interviewer__c'     => 'interviewer.a@test.com',
                'interview_name_interviewer__c'      => 'מראיין א',
                'interview_email_photographer__c'    => 'photographer.x@test.com',
                'interview_name_photographer__c'     => 'צלם איקס',
                'interview_first_name_victim__c'     => 'משה',
                'interview_last_name_victim__c'      => 'כהן',
                'Interview_City__c'                  => 'ירושלים'
            }),
            // Same interviewer A, different photographer Y — tests grouping
            buildRecord(new Map<String, Object>{
                'Testimony_Code_from_Case__c'        => 'TD-0002',
                'Documentation_Date__c'              => DateTime.newInstanceGmt(2025, 10, 20, 11, 30, 0),
                'Interviewer_Amount_For_Payment__c'  => 200,
                'Photographer_Amount_For_Payment__c' => 75,
                'interview_email_interviewer__c'     => 'interviewer.a@test.com',
                'interview_name_interviewer__c'      => 'מראיין א',
                'interview_email_photographer__c'    => 'photographer.y@test.com',
                'interview_name_photographer__c'     => 'צלם יוד',
                'interview_first_name_victim__c'     => 'שרה',
                'interview_last_name_victim__c'      => 'לוי',
                'Interview_City__c'                  => 'ירושלים'
            }),
            // Interviewer B, no photographer, null photographer amount
            buildRecord(new Map<String, Object>{
                'Testimony_Code_from_Case__c'       => 'TD-0003',
                'Documentation_Date__c'             => DateTime.newInstanceGmt(2025, 10, 25, 6, 0, 0),
                'Interviewer_Amount_For_Payment__c' => 150,
                'interview_email_interviewer__c'    => 'interviewer.b@test.com',
                'interview_name_interviewer__c'     => 'מראיין ב',
                'interview_first_name_victim__c'    => 'דוד',
                'interview_last_name_victim__c'     => 'פרץ',
                'Interview_City__c'                 => 'ירושלים'
            })
        };
    }

    /**
     * The invocable takes a list of Request wrappers, one per Flow invocation.
     * Records and sender address travel together on the same wrapper.
     */
    private static List<TestimonyMonthlyEmailSender.Request> wrap(
        List<Testimony_Documentation__c> recs
    ) {
        return wrap(recs, UNRESOLVABLE_OWA);
    }

    private static List<TestimonyMonthlyEmailSender.Request> wrap(
        List<Testimony_Documentation__c> recs,
        String fromAddress
    ) {
        return new List<TestimonyMonthlyEmailSender.Request>{
            buildRequest(recs, fromAddress)
        };
    }

    private static TestimonyMonthlyEmailSender.Request buildRequest(
        List<Testimony_Documentation__c> recs,
        String fromAddress
    ) {
        TestimonyMonthlyEmailSender.Request req = new TestimonyMonthlyEmailSender.Request();
        req.records = recs;
        req.fromAddress = fromAddress;
        return req;
    }


    // ---------- send behaviour ----------

    @isTest
    static void testSendEmails() {
        // 2 interviewers + 2 photographers = 4 messages in a single sendEmail call.
        Test.startTest();
        List<String> results = TestimonyMonthlyEmailSender.sendEmails(wrap(buildScenario()));
        Integer invocations = Limits.getEmailInvocations();
        Test.stopTest();

        System.assertEquals(1, invocations,
            'All messages should go out in one sendEmail call, not one per recipient.');
        System.assert(results[0].startsWith('Sent 4 of 4'),
            'Expected two interviewer summaries and two photographer summaries. Actual: '
            + results[0]);
    }


    @isTest
    static void testSendEmails_emptyList() {
        Test.startTest();
        List<String> results = TestimonyMonthlyEmailSender.sendEmails(
            wrap(new List<Testimony_Documentation__c>())
        );
        Integer invocations = Limits.getEmailInvocations();
        Test.stopTest();

        System.assertEquals(0, invocations,
            'An empty collection must not trigger sendEmail.');
        System.assertEquals('No records supplied.', results[0]);
    }


    @isTest
    static void nullRecordCollectionIsHandled() {
        // A Flow can pass a Request whose record collection was never assigned.
        Test.startTest();
        List<String> results = TestimonyMonthlyEmailSender.sendEmails(
            wrap(null, UNRESOLVABLE_OWA)
        );
        Test.stopTest();

        System.assertEquals('No records supplied.', results[0],
            'A null record collection must not throw.');
    }


    @isTest
    static void testNoEmailAddressesSendsNothing() {
        List<Testimony_Documentation__c> recs = new List<Testimony_Documentation__c>{
            buildRecord(new Map<String, Object>{
                'Testimony_Code_from_Case__c'       => 'TD-0004',
                'Interviewer_Amount_For_Payment__c' => 100
            })
        };

        Test.startTest();
        List<String> results = TestimonyMonthlyEmailSender.sendEmails(wrap(recs));
        Integer invocations = Limits.getEmailInvocations();
        Test.stopTest();

        System.assertEquals(0, invocations,
            'Records with no interviewer or photographer email produce no messages.');
        System.assertEquals('No recipients with an email address.', results[0]);
    }


    @isTest
    static void multipleRequestsAreAllProcessed() {
        List<Testimony_Documentation__c> first = new List<Testimony_Documentation__c>{
            buildRecord(new Map<String, Object>{
                'Testimony_Code_from_Case__c'       => 'TD-0005',
                'Interviewer_Amount_For_Payment__c' => 100,
                'interview_email_interviewer__c'    => 'a@test.com'
            })
        };
        List<Testimony_Documentation__c> second = new List<Testimony_Documentation__c>{
            buildRecord(new Map<String, Object>{
                'Testimony_Code_from_Case__c'       => 'TD-0006',
                'Interviewer_Amount_For_Payment__c' => 100,
                'interview_email_interviewer__c'    => 'b@test.com'
            })
        };

        Test.startTest();
        List<String> results = TestimonyMonthlyEmailSender.sendEmails(
            new List<TestimonyMonthlyEmailSender.Request>{
                buildRequest(first, UNRESOLVABLE_OWA),
                buildRequest(second, UNRESOLVABLE_OWA)
            });
        Test.stopTest();

        System.assertEquals(2, results.size(),
            'A summary must be returned for every request, not just the first.');
        System.assert(results[1].startsWith('Sent 1 of 1'),
            'The second request must be processed too. Actual: ' + results[1]);
    }


    // ---------- sender address input ----------

    @isTest
    static void blankFromAddressStillSends() {
        Test.startTest();
        List<String> results = TestimonyMonthlyEmailSender.sendEmails(
            wrap(buildScenario(), null));
        Integer invocations = Limits.getEmailInvocations();
        Test.stopTest();

        System.assertEquals(1, invocations,
            'A Flow that supplies no sender address must still send from the running user.');
        System.assert(results[0].startsWith('Sent 4 of 4'), results[0]);
    }


    @isTest
    static void emptyStringFromAddressStillSends() {
        Test.startTest();
        // A Flow text variable left unset arrives as '' rather than null.
        List<String> results = TestimonyMonthlyEmailSender.sendEmails(
            wrap(buildScenario(), ''));
        Test.stopTest();

        System.assert(results[0].startsWith('Sent 4 of 4'),
            'An empty string must be treated the same as null. Actual: ' + results[0]);
    }


    @isTest
    static void unresolvableFromAddressFallsBackRatherThanFailing() {
        Test.startTest();
        List<String> results = TestimonyMonthlyEmailSender.sendEmails(
            wrap(buildScenario(), 'definitely-not-verified@example.invalid'));
        Integer invocations = Limits.getEmailInvocations();
        Test.stopTest();

        System.assertEquals(1, invocations,
            'An address with no matching OWA must fall back, not abort the send.');
        System.assert(results[0].startsWith('Sent 4 of 4'), results[0]);
    }


    @isTest
    static void eachRequestCarriesItsOwnSenderAddress() {
        // Two Flow invocations bulkified into one Apex call, each with a
        // different sender. Exercises the per-address cache holding two keys.
        List<Testimony_Documentation__c> first = new List<Testimony_Documentation__c>{
            buildRecord(new Map<String, Object>{
                'Testimony_Code_from_Case__c'       => 'TD-0010',
                'Interviewer_Amount_For_Payment__c' => 100,
                'interview_email_interviewer__c'    => 'a@test.com'
            })
        };
        List<Testimony_Documentation__c> second = new List<Testimony_Documentation__c>{
            buildRecord(new Map<String, Object>{
                'Testimony_Code_from_Case__c'       => 'TD-0011',
                'Interviewer_Amount_For_Payment__c' => 100,
                'interview_email_interviewer__c'    => 'b@test.com'
            })
        };

        Test.startTest();
        List<String> results = TestimonyMonthlyEmailSender.sendEmails(
            new List<TestimonyMonthlyEmailSender.Request>{
                buildRequest(first, 'sender.one@example.invalid'),
                buildRequest(second, 'sender.two@example.invalid')
            });
        Test.stopTest();

        System.assertEquals(2, results.size(), 'Both requests must produce a summary.');
        System.assert(results[0].startsWith('Sent 1 of 1'), results[0]);
        System.assert(results[1].startsWith('Sent 1 of 1'), results[1]);
    }


    @isTest
    static void repeatedAddressIsHandled() {
        // Three requests sharing a sender. The class caches the OWA lookup
        // per address; this asserts the observable result rather than the
        // query count, which is a transaction-wide figure that other org
        // automation contributes to.
        List<TestimonyMonthlyEmailSender.Request> requests =
            new List<TestimonyMonthlyEmailSender.Request>();

        for (Integer i = 0; i < 3; i++) {
            requests.add(buildRequest(
                new List<Testimony_Documentation__c>{
                    buildRecord(new Map<String, Object>{
                        'Testimony_Code_from_Case__c'       => 'TD-002' + i,
                        'Interviewer_Amount_For_Payment__c' => 100,
                        'interview_email_interviewer__c'    => 'r' + i + '@test.com'
                    })
                },
                'shared.sender@example.invalid'
            ));
        }

        Test.startTest();
        List<String> results = TestimonyMonthlyEmailSender.sendEmails(requests);
        Test.stopTest();

        System.assertEquals(3, results.size(), 'Every request must return a summary.');
        for (String r : results) {
            System.assert(r.startsWith('Sent 1 of 1'),
                'A repeated sender address must not affect the send. Actual: ' + r);
        }
    }


    // ---------- rendered amounts ----------

    @isTest
    static void amountsRenderWithTwoDecimals() {
        List<Testimony_Documentation__c> recs = buildScenario();
        // Interviewer A appears on TD-0001 (100) and TD-0002 (200).
        String html = TestimonyMonthlyEmailSender.buildEmail(
            new List<Testimony_Documentation__c>{ recs[0], recs[1] }, true, 'מראיין א');

        System.assert(html.contains('100.00'),
            'Amounts must be rendered to two decimal places. Body: ' + html);
        System.assert(html.contains('200.00'), 'Second row amount missing.');
    }


    @isTest
    static void totalMatchesTheSumOfRows() {
        List<Testimony_Documentation__c> recs = buildScenario();
        String html = TestimonyMonthlyEmailSender.buildEmail(
            new List<Testimony_Documentation__c>{ recs[0], recs[1] }, true, 'מראיין א');

        System.assert(html.contains('300.00'),
            'The total row must equal 100 + 200. Body: ' + html);
    }


    @isTest
    static void photographerSeesPhotographerAmounts() {
        List<Testimony_Documentation__c> recs = buildScenario();
        // TD-0001 pays the interviewer 100 and the photographer 50.
        String html = TestimonyMonthlyEmailSender.buildEmail(
            new List<Testimony_Documentation__c>{ recs[0] }, false, 'צלם איקס');

        System.assert(html.contains('50.00'),
            'A photographer summary must show the photographer amount.');
        System.assert(!html.contains('100.00'),
            'A photographer must not be shown the interviewer fee. Body: ' + html);
    }


    @isTest
    static void nullAmountRendersAsZeroRatherThanBlank() {
        Testimony_Documentation__c rec = buildRecord(new Map<String, Object>{
            'Testimony_Code_from_Case__c'    => 'TD-0007',
            'interview_email_interviewer__c' => 'a@test.com',
            'interview_name_interviewer__c'  => 'מראיין א'
        });

        String html = TestimonyMonthlyEmailSender.buildEmail(
            new List<Testimony_Documentation__c>{ rec }, true, 'מראיין א');

        System.assert(html.contains('0.00'),
            'A null amount must render as 0.00 so the rows visibly sum to the total.');
    }


    @isTest
    static void interviewTimeRendersInIsraelTime() {
        // 07:00 UTC on 15 Oct 2025 is 10:00 in Jerusalem (UTC+3).
        List<Testimony_Documentation__c> recs = buildScenario();
        String html = TestimonyMonthlyEmailSender.buildEmail(
            new List<Testimony_Documentation__c>{ recs[0] }, true, 'מראיין א');

        System.assert(html.contains('15/10/2025, 10:00'),
            'Times must be converted to Asia/Jerusalem, not left in GMT. Body: ' + html);
    }


    @isTest
    static void specialCharactersAreEscaped() {
        Testimony_Documentation__c rec = buildRecord(new Map<String, Object>{
            'Testimony_Code_from_Case__c'       => 'TD-0008',
            'Interviewer_Amount_For_Payment__c' => 100,
            'interview_email_interviewer__c'    => 'a@test.com',
            'interview_last_name_victim__c'     => 'צ\'רנומורדיק & <b>'
        });

        String html = TestimonyMonthlyEmailSender.buildEmail(
            new List<Testimony_Documentation__c>{ rec }, true, 'מראיין א');

        System.assert(html.contains('&amp;'), 'Ampersands must be escaped.');
        System.assert(!html.contains('<b>'), 'Raw markup must not survive into the body.');
    }


    // ---------- activity logging ----------

    /**
     * Uses an existing Person Account rather than inserting one, for the
     * reason described at the top of this class.
     *
     * SeeAllData=true means the OWA lookup runs against real org data here,
     * unlike every other test in this class. UNRESOLVABLE_OWA is passed so the
     * assertion holds in any org; substitute a genuinely verified address to
     * assert the resolved sender instead.
     */
    @isTest(SeeAllData=true)
    static void emailsAreLoggedToPersonAccounts() {
        List<Account> existing = [
            SELECT Id, PersonContactId
            FROM Account
            WHERE IsPersonAccount = true AND PersonContactId != null
            LIMIT 1
        ];
        if (existing.isEmpty()) {
            return; // No Person Accounts available in this org.
        }

        Account interviewer = existing[0];
        Id personContactId = interviewer.PersonContactId;

        // Unique so the assertion cannot match pre-existing activity.
        String uniqueAddress = 'log-test-'
            + String.valueOf(Datetime.now().getTime()) + '@example.com';

        Testimony_Documentation__c rec = buildRecord(new Map<String, Object>{
            'Testimony_Code_from_Case__c'       => 'TD-0009',
            'Documentation_Date__c'             => DateTime.newInstanceGmt(2025, 10, 15, 7, 0, 0),
            'Interviewer_Amount_For_Payment__c' => 100,
            'interview_email_interviewer__c'    => uniqueAddress,
            'interview_name_interviewer__c'     => 'מראיין א',
            'Interviewer__c'                    => interviewer.Id
        });

        Test.startTest();
        List<String> results = TestimonyMonthlyEmailSender.sendEmails(
            wrap(new List<Testimony_Documentation__c>{ rec }, UNRESOLVABLE_OWA));
        Test.stopTest();

        List<EmailMessage> logged = [
            SELECT Id, Subject, ToAddress, FromAddress, Status, Incoming, HtmlBody
            FROM EmailMessage
            WHERE ToAddress = :uniqueAddress
        ];

        // Not assertEquals(1): the platform may log the outbound message itself,
        // in which case more than one record carries this address.
        System.assert(!logged.isEmpty(),
            'The sent message should be logged as an EmailMessage. Result: ' + results[0]);
        System.assert(results[0].contains('Emails logged: 1'), results[0]);

        Set<Id> messageIds = new Map<Id, EmailMessage>(logged).keySet();

        List<EmailMessageRelation> relations = [
            SELECT Id, RelationId, RelationType
            FROM EmailMessageRelation
            WHERE EmailMessageId IN :messageIds AND RelationId = :personContactId
        ];

        System.assert(!relations.isEmpty(),
            'The message must be related to the recipient so it shows on their account.');

        Boolean bodyLogged = false;
        Boolean senderLogged = false;
        for (EmailMessage em : logged) {
            if (em.Status == '3' && em.HtmlBody != null && em.HtmlBody.contains('100.00')) {
                bodyLogged = true;
                senderLogged = em.FromAddress == UserInfo.getUserEmail();
            }
        }
        System.assert(bodyLogged,
            'A log entry should carry the statement that was actually sent.');
        System.assert(senderLogged,
            'With no matching OWA the log must record the running user as sender, '
            + 'so the log matches what actually went out.');
    }


    @isTest
    static void recordsWithoutAccountsProduceNoLogs() {
        Test.startTest();
        List<String> results = TestimonyMonthlyEmailSender.sendEmails(wrap(buildScenario()));
        Test.stopTest();

        // Asserts on the class's own behaviour rather than a raw row count,
        // because the platform may log the outbound message independently.
        System.assert(results[0].contains('Emails logged: 0'),
            'Records with no interviewer account must not be logged by this class. Actual: '
            + results[0]);
    }
}