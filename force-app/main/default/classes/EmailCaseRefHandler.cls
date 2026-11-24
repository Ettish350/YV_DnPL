global class EmailCaseRefHandler implements QuickAction.QuickActionDefaultsHandler {

    global EmailCaseRefHandler() { }

    global void onInitDefaults(QuickAction.QuickActionDefaults[] defaultsList) {
        if (defaultsList == null) return;

        for (QuickAction.QuickActionDefaults qad : defaultsList) {
            if (!(qad instanceof QuickAction.SendEmailQuickActionDefaults)) continue;

            QuickAction.SendEmailQuickActionDefaults sendDefaults =
                (QuickAction.SendEmailQuickActionDefaults) qad;

            EmailMessage em = (EmailMessage) sendDefaults.getTargetSObject();
            if (em == null) continue;

            Id contextId = sendDefaults.getContextId();
            if (contextId == null) continue;

            // Only operate when the context record is a Case
            String prefix = String.valueOf(contextId).substring(0, 3);
            if (prefix != '500') continue;

            Case parentCase;
            try {
                parentCase = [SELECT CaseNumber FROM Case WHERE Id = :contextId LIMIT 1];
            } catch (Exception e) {
                System.debug('EmailCaseRefHandler query failed: ' + e.getMessage());
                continue;
            }
            if (parentCase == null || String.isBlank(parentCase.CaseNumber)) continue;

            String marker = 'Ref: YV-' + parentCase.CaseNumber;
            String detectionPattern = 'YV-' + parentCase.CaseNumber;

            String html = em.HtmlBody == null ? '' : em.HtmlBody;
            if (!html.contains(detectionPattern)) {
                String htmlMarker =
                    '<div style="color:#999;font-size:11px;margin-top:8px;">'
                    + marker.escapeHtml4()
                    + '</div>';
                em.HtmlBody = html + htmlMarker;
            }

            String text = em.TextBody == null ? '' : em.TextBody;
            if (!text.contains(detectionPattern)) {
                em.TextBody = text + '\n\n' + marker;
            }

            System.debug('EmailCaseRefHandler injected marker for ' + parentCase.CaseNumber);
        }
    }
}