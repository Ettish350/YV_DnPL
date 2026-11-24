public class TestimonyEmailSender {

    private static final String LOGO_URL =
        'https://yadvashemtheworldholocaustremembr2.my.salesforce.com/servlet/servlet.ImageServer'
        + '?id=015hd00000LeIqM&oid=00DJ9000000RreK&lastMod=1787484378000';

    // Keyed by address. A key present with a null value means the lookup ran
    // and found nothing, so a missing OWA is not re-queried on every call.
    private static Map<String, Id> owaIdByAddress = new Map<String, Id>();

    /**
     * Input wrapper. An @InvocableMethod accepts exactly one List parameter,
     * so passing both the records and the sender address requires a wrapper
     * with an @InvocableVariable per input.
     */
    public class Request {
        @InvocableVariable(
            label='Testimony Records'
            description='The Testimony_Documentation__c records to summarise.'
            required=true)
        public List<Testimony_Documentation__c> records;

        @InvocableVariable(
            label='From Email Address'
            description='Verified Org-Wide Email Address to send from. If blank, or if no matching OWA exists, mail sends from the running user.'
            required=false)
        public String fromAddress;
    }

    /**
     * Holds one outbound email: the address, who it is for, which testimony
     * records it covers, and the rendered bodies. Kept in the same order as
     * the message list so send results can be matched back for logging.
     */
    private class Recipient {
        String emailAddress;
        String displayName;
        Boolean isInterviewer;
        String htmlBody;
        String textBody;
        List<Testimony_Documentation__c> records = new List<Testimony_Documentation__c>();
    }

    @InvocableMethod(label='Send Testimony Emails')
    public static List<String> sendEmails(List<Request> requests) {
        List<String> results = new List<String>();

        for (Request req : requests) {
            results.add(processCollection(req.records, req.fromAddress));
        }
        return results;
    }

    private static String processCollection(
        List<Testimony_Documentation__c> records,
        String fromAddress
    ) {
        if (records == null || records.isEmpty()) {
            return 'No records supplied.';
        }

        Map<String, Recipient> recipientsByEmail = new Map<String, Recipient>();

        for (Testimony_Documentation__c rec : records) {
            addToRecipient(recipientsByEmail, rec.interview_email_interviewer__c,
                           rec.interview_name_interviewer__c, true, rec);
            addToRecipient(recipientsByEmail, rec.interview_email_photographer__c,
                           rec.interview_name_photographer__c, false, rec);
        }

        if (recipientsByEmail.isEmpty()) {
            return 'No recipients with an email address.';
        }

        // Stable ordering so sendResults[i] maps back to recipients[i].
        List<Recipient> recipients = recipientsByEmail.values();
        List<Messaging.SingleEmailMessage> emails = new List<Messaging.SingleEmailMessage>();

        for (Recipient r : recipients) {
            // isInterviewer means the recipient IS the interviewer, so the
            // partner column shown to them is the photographer.
            String partnerType = r.isInterviewer ? 'photographer' : 'interviewer';
            r.htmlBody = buildEmail(r.records, partnerType, r.displayName);
            r.textBody = buildPlainText(r.records, partnerType, r.displayName);
            emails.add(buildEmailMessage(r.emailAddress, r.htmlBody, r.textBody, fromAddress));
        }

        Integer sent = 0;
        List<String> failures = new List<String>();
        List<Recipient> delivered = new List<Recipient>();

        // allOrNothing = false so one bad address does not block the rest.
        Messaging.SendEmailResult[] sendResults = Messaging.sendEmail(emails, false);

        for (Integer i = 0; i < sendResults.size(); i++) {
            if (sendResults[i].isSuccess()) {
                sent++;
                delivered.add(recipients[i]);
            } else {
                String addr = recipients[i].emailAddress;
                String msg = sendResults[i].getErrors()[0].getMessage();
                failures.add(addr + ': ' + msg);
                System.debug(LoggingLevel.ERROR,
                    'Testimony email failed for ' + addr + ' - ' + msg);
            }
        }

        Integer logged = logEmails(delivered, fromAddress);

        String summary = 'Sent ' + sent + ' of ' + emails.size()
                       + '. Emails logged: ' + logged + '.';
        if (!failures.isEmpty()) {
            summary += ' Failures: ' + String.join(failures, ' | ');
        }
        System.debug(LoggingLevel.INFO, summary);
        return summary;
    }

    private static void addToRecipient(
        Map<String, Recipient> recipientsByEmail,
        String emailAddr,
        String displayName,
        Boolean isInterviewer,
        Testimony_Documentation__c rec
    ) {
        if (String.isBlank(emailAddr)) {
            return;
        }
        // Key on address + role: the same person could appear as interviewer
        // on one record and photographer on another.
        String key = emailAddr.toLowerCase() + '|' + String.valueOf(isInterviewer);

        Recipient r = recipientsByEmail.get(key);
        if (r == null) {
            r = new Recipient();
            r.emailAddress = emailAddr;
            r.displayName = displayName;
            r.isInterviewer = isInterviewer;
            recipientsByEmail.put(key, r);
        }
        r.records.add(rec);
    }

    /**
     * Logs each sent message as an EmailMessage, so it renders in the
     * activity timeline as a readable email rather than a task.
     *
     * One EmailMessage per message actually sent — not one per testimony
     * record — so the log reflects what the recipient received. It is
     * attached to the Person Account through an EmailMessageRelation on
     * PersonContactId, which is what places it on their activity timeline.
     */
    private static Integer logEmails(List<Recipient> delivered, String fromAddress) {
        if (delivered.isEmpty()) {
            return 0;
        }

        Set<Id> accountIds = new Set<Id>();
        for (Recipient r : delivered) {
            for (Testimony_Documentation__c rec : r.records) {
                Id acctId = r.isInterviewer ? rec.Interviewer__c : rec.Photographer__c;
                if (acctId != null) {
                    accountIds.add(acctId);
                }
            }
        }

        if (accountIds.isEmpty()) {
            return 0;
        }

        Map<Id, Id> contactIdByAccountId = new Map<Id, Id>();
        for (Account a : [
            SELECT Id, PersonContactId
            FROM Account
            WHERE Id IN :accountIds AND IsPersonAccount = true
        ]) {
            if (a.PersonContactId != null) {
                contactIdByAccountId.put(a.Id, a.PersonContactId);
            }
        }

        // The logged sender must match what actually went out: the OWA only
        // if one resolved, otherwise the running user.
        String loggedFrom = getOwaId(fromAddress) != null
            ? fromAddress
            : UserInfo.getUserEmail();

        List<EmailMessage> messages = new List<EmailMessage>();
        List<Id> contactPerMessage = new List<Id>();
        List<String> addressPerMessage = new List<String>();

        for (Recipient r : delivered) {
            Id acctId = r.isInterviewer
                ? r.records[0].Interviewer__c
                : r.records[0].Photographer__c;
            Id contactId = contactIdByAccountId.get(acctId);

            if (contactId == null) {
                System.debug(LoggingLevel.WARN,
                    'No PersonContactId for account ' + acctId
                    + '; skipping email log for ' + r.emailAddress);
                continue;
            }

            EmailMessage em = new EmailMessage();
            em.Subject     = 'סידור עבודה לראיונות עדות בשבוע הבא';
            em.HtmlBody    = r.htmlBody;
            em.TextBody    = r.textBody;
            em.FromAddress = loggedFrom;
            em.FromName    = 'Yad Vashem';
            em.ToAddress   = r.emailAddress;
            em.MessageDate = Datetime.now();
            em.Incoming    = false;
            em.Status      = '3'; // Sent

            // RelatedToId deliberately not set: EmailMessage.RelatedToId is a
            // restricted polymorphic field and rejects Testimony_Documentation__c
            // with FIELD_INTEGRITY_EXCEPTION. The link to the recipient is made
            // through EmailMessageRelation below, which is what places the
            // message on their account timeline.

            messages.add(em);
            contactPerMessage.add(contactId);
            addressPerMessage.add(r.emailAddress);
        }

        if (messages.isEmpty()) {
            return 0;
        }

        Integer inserted = 0;
        List<EmailMessageRelation> relations = new List<EmailMessageRelation>();

        Database.SaveResult[] saveResults = Database.insert(messages, false);
        for (Integer i = 0; i < saveResults.size(); i++) {
            if (saveResults[i].isSuccess()) {
                inserted++;
                relations.add(new EmailMessageRelation(
                    EmailMessageId  = saveResults[i].getId(),
                    RelationId      = contactPerMessage[i],
                    RelationAddress = addressPerMessage[i],
                    RelationType    = 'ToAddress'
                ));
            } else {
                System.debug(LoggingLevel.ERROR,
                    'EmailMessage insert failed: '
                    + saveResults[i].getErrors()[0].getMessage());
            }
        }

        if (!relations.isEmpty()) {
            Database.SaveResult[] relResults = Database.insert(relations, false);
            for (Database.SaveResult sr : relResults) {
                if (!sr.isSuccess()) {
                    System.debug(LoggingLevel.ERROR,
                        'EmailMessageRelation insert failed: '
                        + sr.getErrors()[0].getMessage());
                }
            }
        }

        return inserted;
    }

    /**
     * Resolves an address to a verified Org-Wide Email Address Id.
     * Returns null when the address is blank or has no matching OWA in this
     * org, in which case the caller falls back to the running user.
     */
    private static Id getOwaId(String address) {
        if (String.isBlank(address)) {
            return null;
        }

        if (!owaIdByAddress.containsKey(address)) {
            Id resolved = null;

            List<OrgWideEmailAddress> owas = [
                SELECT Id
                FROM OrgWideEmailAddress
                WHERE Address = :address
                LIMIT 1
            ];
            if (!owas.isEmpty()) {
                resolved = owas[0].Id;
            } else {
                System.debug(LoggingLevel.ERROR,
                    'No Org-Wide Email Address found for ' + address
                    + '. Emails will send from the running user.');
            }
            owaIdByAddress.put(address, resolved);
        }
        return owaIdByAddress.get(address);
    }

    @TestVisible
    private static String buildEmail(
        List<Testimony_Documentation__c> records,
        String partnerType,
        String recipientName
    ) {
        String partnerLabel = partnerType == 'photographer' ? 'צלם' : 'מראיין';

        String html = '<!DOCTYPE html><html dir="rtl" lang="he"><body '
            + 'style="font-family: Arial, sans-serif; direction: rtl;">';

        html += '<div style="text-align: center; margin-bottom: 20px;">';
        html += '<img src="' + LOGO_URL + '" alt="Yad Vashem" style="max-height: 80px;" />';
        html += '</div>';

        html += '<p>היי ' + esc(recipientName)
             + ', להלן טבלת הראיונות המתוכננים עבורך לשבוע הבא</p>';

        html += '<table border="1" cellpadding="8" cellspacing="0" '
             + 'style="border-collapse:collapse; width:100%; text-align:right;">';
        html += '<tr style="background-color:#f2f2f2; font-weight:bold;">';
        html += '<th>קוד עדות</th>';
        html += '<th>מועד ראיון</th>';
        html += '<th>שם משפחה</th>';
        html += '<th>שם פרטי</th>';
        html += '<th>מיקום הראיון</th>';
        html += '<th>רחוב</th>';
        html += '<th>עיר</th>';
        html += '<th>טלפון בבית</th>';
        html += '<th>טלפון נייד</th>';
        html += '<th>ארץ מוצא</th>';
        html += '<th>שנת לידה</th>';
        html += '<th>' + partnerLabel + '</th>';
        html += '<th>טלפון ' + partnerLabel + '</th>';
        html += '<th>הערות</th>';
        html += '<th>הערות פנימיות</th>';
        html += '</tr>';

        for (Testimony_Documentation__c rec : records) {
            Boolean atHome = rec.InterviewLocation__c == 'בבית הניצול';
            html += '<tr>';
            html += '<td>' + esc(rec.Testimony_Code_from_Case__c) + '</td>';
            html += '<td>' + formatDateTime(rec.Documentation_Date__c) + '</td>';
            html += '<td>' + esc(rec.interview_last_name_victim__c) + '</td>';
            html += '<td>' + esc(rec.interview_first_name_victim__c) + '</td>';
            html += '<td>' + esc(rec.InterviewLocation__c) + '</td>';
            html += '<td>' + (atHome ? esc(rec.Interview_Address__c) : '') + '</td>';
            html += '<td>' + (atHome ? esc(rec.Interview_City__c) : '') + '</td>';
            html += '<td>' + esc(rec.interview_phone_home__c) + '</td>';
            html += '<td>' + esc(rec.interview_phone_mobile__c) + '</td>';
            html += '<td>' + esc(rec.interview_country_of_origin__c) + '</td>';
            html += '<td>' + esc(rec.interview_year_of_birth__c) + '</td>';
            html += '<td>' + (partnerType == 'photographer'
                        ? esc(rec.interview_name_photographer__c)
                        : esc(rec.interview_name_interviewer__c)) + '</td>';
            html += '<td>' + (partnerType == 'photographer'
                        ? esc(rec.interview_phone_photographer__c)
                        : esc(rec.interview_phone_interviewer__c)) + '</td>';
            html += '<td>' + esc(rec.Description__c) + '</td>';
            html += '<td>' + esc(rec.Internal_Notes__c) + '</td>';
            html += '</tr>';
        }

        html += '</table></body></html>';
        return html;
    }

    private static String buildPlainText(
        List<Testimony_Documentation__c> records,
        String partnerType,
        String recipientName
    ) {
        String partnerLabel = partnerType == 'photographer' ? 'צלם' : 'מראיין';

        String body = 'היי ' + nullSafe(recipientName)
                    + ', להלן הראיונות המתוכננים עבורך לשבוע הבא:\n\n';

        for (Testimony_Documentation__c rec : records) {
            Boolean atHome = rec.InterviewLocation__c == 'בבית הניצול';
            body += 'קוד עדות: ' + nullSafe(rec.Testimony_Code_from_Case__c) + '\n';
            body += 'מועד: ' + formatDateTime(rec.Documentation_Date__c) + '\n';
            body += 'ניצול: ' + nullSafe(rec.interview_first_name_victim__c) + ' '
                              + nullSafe(rec.interview_last_name_victim__c) + '\n';
            body += 'מיקום: ' + nullSafe(rec.InterviewLocation__c) + '\n';
            if (atHome) {
                body += 'כתובת: ' + nullSafe(rec.Interview_Address__c) + ', '
                                  + nullSafe(rec.Interview_City__c) + '\n';
            }
            body += 'נייד: ' + nullSafe(rec.interview_phone_mobile__c) + '\n';
            body += partnerLabel + ': ' + (partnerType == 'photographer'
                        ? nullSafe(rec.interview_name_photographer__c)
                        : nullSafe(rec.interview_name_interviewer__c)) + '\n';
            body += 'טלפון ' + partnerLabel + ': ' + (partnerType == 'photographer'
                        ? nullSafe(rec.interview_phone_photographer__c)
                        : nullSafe(rec.interview_phone_interviewer__c)) + '\n';
            body += '\n';
        }
        return body;
    }

    private static Messaging.SingleEmailMessage buildEmailMessage(
        String toAddress,
        String htmlBody,
        String plainBody,
        String fromAddress
    ) {
        Messaging.SingleEmailMessage email = new Messaging.SingleEmailMessage();
        email.setToAddresses(new List<String>{ toAddress });

        Id owaId = getOwaId(fromAddress);
        if (owaId != null) {
            email.setOrgWideEmailAddressId(owaId);
        }

        email.setSubject('סידור עבודה לראיונות עדות בשבוע הבא');
        email.setHtmlBody(htmlBody);
        email.setPlainTextBody(plainBody);
        return email;
    }

    private static String formatDateTime(DateTime dt) {
        if (dt == null) return '';
        return dt.format('dd/MM/yyyy, HH:mm', 'Asia/Jerusalem');
    }

    private static String esc(Object val) {
        return val != null ? String.valueOf(val).escapeHtml4() : '';
    }

    private static String nullSafe(Object val) {
        return val != null ? String.valueOf(val) : '';
    }
}