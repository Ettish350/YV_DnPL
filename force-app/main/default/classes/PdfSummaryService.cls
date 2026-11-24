/**
 * PdfSummaryService
 *
 * Calls the Collect the Fragments PDF endpoint for a Case and writes the result
 * back onto the Case record.
 *
 * Chain:
 *   Case.Generate_PDF_Summary__c = true
 *     -> record-triggered Flow (after save, checkbox changed to true)
 *       -> PdfSummaryService.generate(caseIds)          [invocable]
 *         -> PdfSummaryService.Callout                  [queueable, async]
 *           -> POST https://payment.yadvashem.org/collect-fragments/pdfSummary/generate.php
 *             -> writes status/result fields back to the Case
 *
 * Why async: Salesforce does not allow callouts from a trigger's synchronous
 * context. Queueable also removes the user-facing wait, which matters because a
 * normal run takes ~7 seconds and larger Cases take longer.
 *
 * Named Credential expected: Collect_Fragments_PDF
 *   URL:  https://payment.yadvashem.org
 *   Auth: External Credential, Custom protocol, with a custom header
 *         Authorization = Bearer {!$Credential.<ExternalCredName>.EndpointToken}
 *
 * Custom fields expected on Case:
 *   Generate_PDF_Summary__c            Checkbox
 *   PDF_Summary_Status__c              Picklist (Queued / Success / Error)
 *   PDF_Summary_Error__c               Long Text Area (1000)
 *   PDF_Summary_ContentVersion_Id__c   Text (18)
 *   PDF_Summary_Generated_On__c        Date/Time
 */
public with sharing class PdfSummaryService {

    private static final String ENDPOINT_PATH =
        'callout:Collect_Fragments_PDF/collect-fragments/pdfSummary/generate.php';

    // The endpoint takes ~7s normally. 120s is the Apex maximum and costs nothing
    // when the call returns quickly.
    private static final Integer TIMEOUT_MS = 120000;

    // ------------------------------------------------------------------ Invocable
    @InvocableMethod(
        label='Generate Case PDF Summary'
        description='Queues a call to the external PDF summary endpoint for each Case.'
        category='Collect the Fragments'
    )
    public static void generate(List<Id> caseIds) {
        if (caseIds == null || caseIds.isEmpty()) {
            return;
        }

        // De-duplicate: a Flow can hand the same Id in more than once, and every
        // call uploads another PDF to the Case.
        Set<Id> unique = new Set<Id>(caseIds);

        // One job per Case, so a single failure doesn't take the batch with it.
        // Governor limit is 50 enqueued jobs per transaction.
        for (Id caseId : unique) {
            System.enqueueJob(new Callout(caseId));
        }
    }

    // ------------------------------------------------------------------ Queueable
    public class Callout implements Queueable, Database.AllowsCallouts {

        private final Id caseId;

        public Callout(Id caseId) {
            this.caseId = caseId;
        }

        public void execute(QueueableContext ctx) {

            Case result = new Case(Id = this.caseId);

            try {
                HttpRequest req = new HttpRequest();
                req.setEndpoint(ENDPOINT_PATH);
                req.setMethod('POST');
                req.setHeader('Content-Type', 'application/json');
                req.setTimeout(TIMEOUT_MS);
                req.setBody(JSON.serialize(new Map<String, Object>{
                    'case_id' => String.valueOf(this.caseId)
                }));

                HttpResponse res = new Http().send(req);

                Map<String, Object> body;
                try {
                    body = (Map<String, Object>) JSON.deserializeUntyped(res.getBody());
                } catch (Exception parseEx) {
                    // Non-JSON response, e.g. an Apache error page.
                    body = new Map<String, Object>();
                }

                // Apex has no Boolean.TRUE constant, and body.get('ok') is an
                // Object, so check the type before casting.
                Object okValue = body.get('ok');
                Boolean ok = res.getStatusCode() == 200
                    && okValue instanceof Boolean
                    && (Boolean) okValue;

                if (ok) {
                    result.PDF_Summary_Status__c            = 'Success';
                    result.PDF_Summary_ContentVersion_Id__c = (String) body.get('content_version_id');
                    result.PDF_Summary_Generated_On__c      = System.now();
                    result.PDF_Summary_Error__c             = null;
                } else {
                    result.PDF_Summary_Status__c = 'Error';
                    result.PDF_Summary_Error__c  = buildError(res, body);
                }

            } catch (Exception e) {
                // Timeout, DNS failure, Named Credential misconfiguration, etc.
                result.PDF_Summary_Status__c = 'Error';
                result.PDF_Summary_Error__c  = e.getTypeName() + ': ' + e.getMessage();
            }

            // Always clear the checkbox so the user can retry. The Flow's entry
            // criteria fires only when the checkbox changes to TRUE, so setting it
            // to FALSE here does not re-trigger anything.
            result.Generate_PDF_Summary__c = false;

            try {
                update result;
            } catch (DmlException dmlEx) {
                // Nothing sensible left to do — the callout may well have succeeded.
                // Log it so the run isn't silently lost.
                System.debug(LoggingLevel.ERROR,
                    'PdfSummaryService: failed to update Case ' + this.caseId
                    + ' :: ' + dmlEx.getMessage());
            }
        }

        /**
         * Endpoint failures return { ok:false, error:"<code>", request_id:"<id>" }.
         * The request_id is the key to finding the real cause in logs/generate.log
         * on the server, so it always goes into the message.
         */
        private String buildError(HttpResponse res, Map<String, Object> body) {
            String code = body.containsKey('error')
                ? String.valueOf(body.get('error'))
                : 'no_error_code';

            String requestId = body.containsKey('request_id')
                ? String.valueOf(body.get('request_id'))
                : 'none';

            String msg = 'HTTP ' + res.getStatusCode()
                + ' | error: ' + code
                + ' | request_id: ' + requestId;

            if (!body.containsKey('error')) {
                String raw = res.getBody();
                if (String.isNotBlank(raw)) {
                    msg += ' | body: ' + raw.abbreviate(300);
                }
            }
            return msg;
        }
    }
}