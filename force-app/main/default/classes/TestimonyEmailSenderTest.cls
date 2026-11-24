@isTest
private class TestimonyEmailSenderTest {

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
     * Most interview_* fields on Testimony_Documentation__c are formula fields and
     * cannot be assigned in Apex. JSON.deserialize populates read-only fields on an
     * in-memory sObject, which lets the test feed the class exactly what it reads.
     */
    private static Testimony_Documentation__c buildRecord(Map<String, Object> fieldValues) {
        Map<String, Object> data = new Map<String, Object>(fieldValues);
        data.put('attributes', new Map<String, Object>{
            'type' => 'Testimony_Documentation__c'
        });
        return (Testimony_Documentation__c) JSON.deserialize(
            JSON.serialize(data), Testimony_Documentation__c.class
        );
    }

    private static Testimony_Documentation__c fullRecord(
        String interviewerEmail, String interviewerName,
        String photographerEmail, String photographerName,
        String location
    ) {
        return buildRecord(new Map<String, Object>{
            'Testimony_Code_from_Case__c'     => 'T-6473',
            'InterviewNumber__c'              => 1,
            'Documentation_Date__c'           => DateTime.newInstanceGmt(2026, 8, 30, 7, 30, 0),
            'interview_email_interviewer__c'  => interviewerEmail,
            'interview_name_interviewer__c'   => interviewerName,
            'interview_email_photographer__c' => photographerEmail,
            'interview_name_photographer__c'  => photographerName,
            'interview_phone_interviewer__c'  => '050-1111111',
            'interview_phone_photographer__c' => '050-2222222',
            'interview_last_name_victim__c'   => 'כהן',
            'interview_first_name_victim__c'  => 'שרה',
            'InterviewLocation__c'            => location,
            'Interview_Address__c'            => 'הרצל 10',
            'Interview_City__c'               => 'ירושלים',
            'interview_phone_home__c'         => '02-1234567',
            'interview_phone_mobile__c'       => '050-1234567',
            'interview_country_of_origin__c'  => 'פולין',
            'interview_year_of_birth__c'      => 1938,
            'Description__c'                  => 'הערות לראיון'
        });
    }

    /**
     * The invocable takes a list of Request wrappers, one per Flow invocation.
     * Records and sender address travel together on the same wrapper.
     */
    private static List<TestimonyEmailSender.Request> wrap(
        List<Testimony_Documentation__c> recs
    ) {
        return wrap(recs, UNRESOLVABLE_OWA);
    }

    private static List<TestimonyEmailSender.Request> wrap(
        List<Testimony_Documentation__c> recs,
        String fromAddress
    ) {
        return new List<TestimonyEmailSender.Request>{ buildRequest(recs, fromAddress) };
    }

    private static TestimonyEmailSender.Request buildRequest(
        List<Testimony_Documentation__c> recs,
        String fromAddress
    ) {
        TestimonyEmailSender.Request req = new TestimonyEmailSender.Request();
        req.records = recs;
        req.fromAddress = fromAddress;
        return req;
    }


    // ---------- send behaviour ----------

    @isTest
    static void bothPartiesReceiveEmail() {
        List<Testimony_Documentation__c> recs = new List<Testimony_Documentation__c>{
            fullRecord('interviewer@example.com', 'דנה',
                       'photographer@example.com', 'יוסי', 'בבית הניצול')
        };

        Test.startTest();
        List<String> results = TestimonyEmailSender.sendEmails(wrap(recs));
        Integer invocations = Limits.getEmailInvocations();
        Test.stopTest();

        System.assertEquals(1, invocations,
            'One sendEmail call should carry both the interviewer and photographer messages.');
        System.assertEquals(1, results.size(), 'One summary per input request.');
        System.assert(results[0].startsWith('Sent 2 of 2'),
            'Both messages should report success. Actual: ' + results[0]);
    }


    @isTest
    static void recordsAreGroupedPerRecipient() {
        // Same interviewer on both rows, two different photographers.
        List<Testimony_Documentation__c> recs = new List<Testimony_Documentation__c>{
            fullRecord('shared@example.com', 'דנה', 'photo1@example.com', 'יוסי', 'בבית הניצול'),
            fullRecord('shared@example.com', 'דנה', 'photo2@example.com', 'מיכל', 'יד ושם')
        };

        Test.startTest();
        List<String> results = TestimonyEmailSender.sendEmails(wrap(recs));
        Integer invocations = Limits.getEmailInvocations();
        Test.stopTest();

        System.assertEquals(1, invocations,
            'Grouping must not produce one sendEmail call per record.');
        System.assert(results[0].startsWith('Sent 3 of 3'),
            'Expected one grouped interviewer email plus two photographer emails. Actual: '
            + results[0]);
    }


    @isTest
    static void nullEmailAddressesSendNothing() {
        List<Testimony_Documentation__c> recs = new List<Testimony_Documentation__c>{
            fullRecord(null, null, null, null, 'יד ושם')
        };

        Test.startTest();
        List<String> results = TestimonyEmailSender.sendEmails(wrap(recs));
        Integer invocations = Limits.getEmailInvocations();
        Test.stopTest();

        System.assertEquals(0, invocations,
            'No addresses means sendEmail must not be called at all.');
        System.assertEquals('No recipients with an email address.', results[0]);
    }


    @isTest
    static void sparseRecordIsHandled() {
        // Exercises nullSafe(), the null branch of formatDateTime(), and the
        // non-"at home" path where address and city are blanked out.
        List<Testimony_Documentation__c> recs = new List<Testimony_Documentation__c>{
            buildRecord(new Map<String, Object>{
                'interview_email_interviewer__c' => 'sparse@example.com'
            })
        };

        Test.startTest();
        List<String> results = TestimonyEmailSender.sendEmails(wrap(recs));
        Integer invocations = Limits.getEmailInvocations();
        Test.stopTest();

        System.assertEquals(1, invocations,
            'A record with only an interviewer email should still produce an email.');
        System.assert(results[0].startsWith('Sent 1 of 1'), results[0]);
    }


    @isTest
    static void emptyCollectionIsHandled() {
        Test.startTest();
        List<String> results = TestimonyEmailSender.sendEmails(
            wrap(new List<Testimony_Documentation__c>())
        );
        Test.stopTest();

        System.assertEquals('No records supplied.', results[0]);
    }


    @isTest
    static void nullRecordCollectionIsHandled() {
        // A Flow can pass a Request whose record collection was never assigned.
        Test.startTest();
        List<String> results = TestimonyEmailSender.sendEmails(
            wrap(null, UNRESOLVABLE_OWA)
        );
        Test.stopTest();

        System.assertEquals('No records supplied.', results[0],
            'A null record collection must not throw.');
    }


    // ---------- sender address input ----------

    @isTest
    static void blankFromAddressStillSends() {
        List<Testimony_Documentation__c> recs = new List<Testimony_Documentation__c>{
            fullRecord('interviewer@example.com', 'דנה', null, null, 'בבית הניצול')
        };

        Test.startTest();
        List<String> results = TestimonyEmailSender.sendEmails(wrap(recs, null));
        Integer invocations = Limits.getEmailInvocations();
        Test.stopTest();

        System.assertEquals(1, invocations,
            'A Flow that supplies no sender address must still send from the running user.');
        System.assert(results[0].startsWith('Sent 1 of 1'), results[0]);
    }


    @isTest
    static void emptyStringFromAddressStillSends() {
        List<Testimony_Documentation__c> recs = new List<Testimony_Documentation__c>{
            fullRecord('interviewer@example.com', 'דנה', null, null, 'בבית הניצול')
        };

        Test.startTest();
        // A Flow text variable left unset arrives as '' rather than null.
        List<String> results = TestimonyEmailSender.sendEmails(wrap(recs, ''));
        Test.stopTest();

        System.assert(results[0].startsWith('Sent 1 of 1'),
            'An empty string must be treated the same as null. Actual: ' + results[0]);
    }


    @isTest
    static void unresolvableFromAddressFallsBackRatherThanFailing() {
        List<Testimony_Documentation__c> recs = new List<Testimony_Documentation__c>{
            fullRecord('interviewer@example.com', 'דנה', null, null, 'בבית הניצול')
        };

        Test.startTest();
        List<String> results = TestimonyEmailSender.sendEmails(
            wrap(recs, 'definitely-not-verified@example.invalid'));
        Integer invocations = Limits.getEmailInvocations();
        Test.stopTest();

        System.assertEquals(1, invocations,
            'An address with no matching OWA must fall back, not abort the send.');
        System.assert(results[0].startsWith('Sent 1 of 1'), results[0]);
    }


    @isTest
    static void eachRequestCarriesItsOwnSenderAddress() {
        // Two Flow invocations bulkified into one Apex call, each with a
        // different sender. Exercises the per-address cache holding two keys.
        List<Testimony_Documentation__c> first = new List<Testimony_Documentation__c>{
            fullRecord('a@example.com', 'דנה', null, null, 'יד ושם')
        };
        List<Testimony_Documentation__c> second = new List<Testimony_Documentation__c>{
            fullRecord('b@example.com', 'מיכל', null, null, 'יד ושם')
        };

        Test.startTest();
        List<String> results = TestimonyEmailSender.sendEmails(
            new List<TestimonyEmailSender.Request>{
                buildRequest(first, 'sender.one@example.invalid'),
                buildRequest(second, 'sender.two@example.invalid')
            });
        Test.stopTest();

        System.assertEquals(2, results.size(),
            'A summary must be returned for every request, not just the first.');
        System.assert(results[1].startsWith('Sent 1 of 1'),
            'The second request must be processed too. Actual: ' + results[1]);
    }


    @isTest
    static void repeatedAddressIsHandled() {
        // Three requests sharing a sender. The class caches the OWA lookup
        // per address; this asserts the observable result rather than the
        // query count, which is a transaction-wide figure that other org
        // automation contributes to.
        List<TestimonyEmailSender.Request> requests =
            new List<TestimonyEmailSender.Request>();

        for (Integer i = 0; i < 3; i++) {
            requests.add(buildRequest(
                new List<Testimony_Documentation__c>{
                    fullRecord('r' + i + '@example.com', 'דנה', null, null, 'יד ושם')
                },
                'shared.sender@example.invalid'
            ));
        }

        Test.startTest();
        List<String> results = TestimonyEmailSender.sendEmails(requests);
        Test.stopTest();

        System.assertEquals(3, results.size(), 'Every request must return a summary.');
        for (String r : results) {
            System.assert(r.startsWith('Sent 1 of 1'),
                'A repeated sender address must not affect the send. Actual: ' + r);
        }
    }


    // ---------- rendered content ----------

    @isTest
    static void bodyShowsTestimonyCodeNotInterviewNumber() {
        Testimony_Documentation__c rec = fullRecord(
            'interviewer@example.com', 'דנה', null, null, 'בבית הניצול');

        String html = TestimonyEmailSender.buildEmail(
            new List<Testimony_Documentation__c>{ rec }, 'photographer', 'דנה');

        System.assert(html.contains('T-6473'),
            'The קוד עדות column must render Testimony_Code_from_Case__c.');
        System.assert(!html.contains('<td>1</td>'),
            'InterviewNumber__c must not be rendered in place of the testimony code.');
    }


    @isTest
    static void interviewTimeRendersInIsraelTime() {
        // 07:30 UTC on 30 Aug 2026 is 10:30 in Jerusalem (UTC+3, DST).
        Testimony_Documentation__c rec = fullRecord(
            'interviewer@example.com', 'דנה', null, null, 'בבית הניצול');

        String html = TestimonyEmailSender.buildEmail(
            new List<Testimony_Documentation__c>{ rec }, 'photographer', 'דנה');

        System.assert(html.contains('30/08/2026, 10:30'),
            'Times must be converted to Asia/Jerusalem, not left in GMT. Body: ' + html);
    }


    @isTest
    static void addressIsHiddenWhenInterviewIsNotAtHome() {
        Testimony_Documentation__c atHome = fullRecord(
            'a@example.com', 'דנה', null, null, 'בבית הניצול');
        Testimony_Documentation__c atOffice = fullRecord(
            'b@example.com', 'דנה', null, null, 'יד ושם');

        String homeHtml = TestimonyEmailSender.buildEmail(
            new List<Testimony_Documentation__c>{ atHome }, 'photographer', 'דנה');
        String officeHtml = TestimonyEmailSender.buildEmail(
            new List<Testimony_Documentation__c>{ atOffice }, 'photographer', 'דנה');

        System.assert(homeHtml.contains('הרצל 10'),
            'Home interviews must show the street address.');
        System.assert(!officeHtml.contains('הרצל 10'),
            'Non-home interviews must not leak the survivor address.');
    }


    @isTest
    static void specialCharactersAreEscaped() {
        // A real surname in the data set contains an apostrophe: צ'רנומורדיק.
        Testimony_Documentation__c rec = buildRecord(new Map<String, Object>{
            'interview_email_interviewer__c' => 'x@example.com',
            'interview_last_name_victim__c'  => 'צ\'רנומורדיק & <b>',
            'Testimony_Code_from_Case__c'    => 'T-1'
        });

        String html = TestimonyEmailSender.buildEmail(
            new List<Testimony_Documentation__c>{ rec }, 'photographer', 'דנה');

        System.assert(html.contains('&amp;'), 'Ampersands must be escaped.');
        System.assert(!html.contains('<b>'), 'Raw markup must not survive into the body.');
    }


    @isTest
    static void partnerPhoneShowsTheOtherPartysNumber() {
        Testimony_Documentation__c rec = fullRecord(
            'interviewer@example.com', 'דנה',
            'photographer@example.com', 'יוסי', 'בבית הניצול');

        // An interviewer's email shows the photographer, and vice versa.
        String toInterviewer = TestimonyEmailSender.buildEmail(
            new List<Testimony_Documentation__c>{ rec }, 'photographer', 'דנה');
        String toPhotographer = TestimonyEmailSender.buildEmail(
            new List<Testimony_Documentation__c>{ rec }, 'interviewer', 'יוסי');

        System.assert(toInterviewer.contains('050-2222222'),
            'The interviewer should be given the photographer number.');
        System.assert(!toInterviewer.contains('050-1111111'),
            'The interviewer should not be shown their own number as the partner. Body: '
            + toInterviewer);

        System.assert(toPhotographer.contains('050-1111111'),
            'The photographer should be given the interviewer number.');
        System.assert(!toPhotographer.contains('050-2222222'),
            'The photographer should not be shown their own number as the partner. Body: '
            + toPhotographer);
    }


    @isTest
    static void survivorPhonesAreStillSeparateFromPartnerPhone() {
        Testimony_Documentation__c rec = fullRecord(
            'interviewer@example.com', 'דנה',
            'photographer@example.com', 'יוסי', 'בבית הניצול');

        String html = TestimonyEmailSender.buildEmail(
            new List<Testimony_Documentation__c>{ rec }, 'photographer', 'דנה');

        // The survivor's own numbers must not be displaced by the new column.
        System.assert(html.contains('02-1234567'), 'Survivor home phone missing.');
        System.assert(html.contains('050-1234567'), 'Survivor mobile missing.');
    }


    // ---------- activity logging ----------

    /**
     * Uses an existing Person Account rather than inserting one. The org's
     * "Account After Create & Update" flow fails on insert in a test context
     * because it creates a Campaign Member against a campaign that no test
     * data provides, so creating an account here is not possible without
     * disabling that automation.
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
            'Testimony_Code_from_Case__c'    => 'T-6473',
            'Documentation_Date__c'          => DateTime.newInstanceGmt(2026, 8, 30, 7, 30, 0),
            'interview_email_interviewer__c' => uniqueAddress,
            'interview_name_interviewer__c'  => 'מיה טלמון',
            'Interviewer__c'                 => interviewer.Id,
            'InterviewLocation__c'           => 'בבית הניצול'
        });

        Test.startTest();
        List<String> results = TestimonyEmailSender.sendEmails(
            wrap(new List<Testimony_Documentation__c>{ rec }, UNRESOLVABLE_OWA));
        Test.stopTest();

        List<EmailMessage> logged = [
            SELECT Id, Subject, ToAddress, FromAddress, Status, Incoming, HtmlBody
            FROM EmailMessage
            WHERE ToAddress = :uniqueAddress
        ];

        // Not assertEquals(1): the platform may log the outbound message itself,
        // in which case more than one record carries this address. What matters
        // here is that a log exists and carries our body and relation.
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
        System.assertEquals('ToAddress', relations[0].RelationType);

        Boolean bodyLogged = false;
        Boolean senderLogged = false;
        for (EmailMessage em : logged) {
            if (em.Status == '3' && !em.Incoming
                && em.HtmlBody != null && em.HtmlBody.contains('T-6473')) {
                bodyLogged = true;
                senderLogged = em.FromAddress == UserInfo.getUserEmail();
            }
        }
        System.assert(bodyLogged,
            'A log entry should carry the outbound body that was actually sent.');
        System.assert(senderLogged,
            'With no matching OWA the log must record the running user as sender, '
            + 'so the log matches what actually went out.');
    }


    @isTest
    static void failedSendsAreNotLogged() {
        // No account reference, so there is nothing to attach the log to.
        List<Testimony_Documentation__c> recs = new List<Testimony_Documentation__c>{
            fullRecord('interviewer@example.com', 'דנה', null, null, 'בבית הניצול')
        };

        Test.startTest();
        List<String> results = TestimonyEmailSender.sendEmails(wrap(recs));
        Test.stopTest();

        // Asserts on the class's own behaviour rather than a raw row count,
        // because the platform may log the outbound message independently.
        System.assert(results[0].contains('Emails logged: 0'),
            'Records without an interviewer account must not be logged by this class. Actual: '
            + results[0]);
    }
}