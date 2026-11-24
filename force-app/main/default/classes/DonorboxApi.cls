/**
 * Reads from the Donorbox API through the "Donorbox" Named Credential
 * (Basic auth: org email + API key, set up in Setup, never in code).
 */
public with sharing class DonorboxApi {
    public static final String BASE = 'callout:Donorbox/api/v1/';
    public static final Integer PAGE_SIZE = 100; // Donorbox maximum

    public class DonorboxException extends Exception {}

    /** Walks every page of an endpoint, oldest first. */
    public static List<Map<String, Object>> getAll(String endpoint, Map<String, String> params) {
        List<Map<String, Object>> rows = new List<Map<String, Object>>();
        for (Integer page = 1; ; page++) {
            if (Limits.getCallouts() >= Limits.getLimitCallouts()) {
                throw new DonorboxException('Reached the Salesforce callout limit while reading ' + endpoint
                    + '. Use a smaller date range.');
            }
            String url = BASE + endpoint + '?order=asc&per_page=' + PAGE_SIZE + '&page=' + page;
            for (String k : params.keySet()) {
                url += '&' + EncodingUtil.urlEncode(k, 'UTF-8') + '=' + EncodingUtil.urlEncode(params.get(k), 'UTF-8');
            }
            HttpRequest req = new HttpRequest();
            req.setEndpoint(url);
            req.setMethod('GET');
            req.setHeader('Accept', 'application/json');
            req.setTimeout(60000);
            HttpResponse res = new Http().send(req);
            if (res.getStatusCode() != 200) {
                throw new DonorboxException('Donorbox ' + endpoint + ' returned HTTP ' + res.getStatusCode()
                    + ': ' + String.valueOf(res.getBody()).left(300));
            }
            List<Object> pageRows = (List<Object>) JSON.deserializeUntyped(res.getBody());
            for (Object o : pageRows) {
                rows.add((Map<String, Object>) o);
            }
            if (pageRows.size() < PAGE_SIZE) {
                break;
            }
        }
        return rows;
    }
}