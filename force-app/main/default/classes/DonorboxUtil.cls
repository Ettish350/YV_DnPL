/** Small helpers for reading Donorbox JSON and mapping values. No DML, no callouts. */
public with sharing class DonorboxUtil {

    // ── JSON access ───────────────────────────────────────────

    public static Map<String, Object> obj(Map<String, Object> m, String key) {
        if (m == null) return null;
        Object v = m.get(key);
        return v instanceof Map<String, Object> ? (Map<String, Object>) v : null;
    }

    /** Trimmed string, or null when missing/blank (Donorbox sometimes sends " "). */
    public static String str(Map<String, Object> m, String key) {
        if (m == null) return null;
        Object v = m.get(key);
        if (v == null) return null;
        String s = String.valueOf(v).trim();
        return s.length() == 0 ? null : s;
    }

    public static Decimal dec(Map<String, Object> m, String key) {
        String s = str(m, key);
        if (s == null) return null;
        s = s.replaceAll('[^0-9.\\-]', '');
        if (s.length() == 0) return null;
        try {
            return Decimal.valueOf(s);
        } catch (Exception e) {
            return null;
        }
    }

    public static Boolean bool(Map<String, Object> m, String key) {
        if (m == null) return false;
        Object v = m.get(key);
        if (v instanceof Boolean) return (Boolean) v;
        return v != null && String.valueOf(v).equalsIgnoreCase('true');
    }

    public static String normEmail(String email) {
        return String.isBlank(email) ? null : email.trim().toLowerCase();
    }

    /** "2026-10-08" or "2026-10-08T17:38:38.668Z" → calendar date in the running user's time zone. */
    public static Date toDate(String iso) {
        if (String.isBlank(iso)) return null;
        if (iso.length() == 10) return Date.valueOf(iso);
        Datetime dt = (Datetime) JSON.deserialize('"' + iso + '"', Datetime.class);
        return dt.date();
    }

    // ── Value mapping ─────────────────────────────────────────

    /**
     * Donorbox plan status → GiftCommitment.Status (this org's values).
     * "failed" in Donorbox means a card charge failed; that is a transaction matter, so the
     * commitment stays Active and the month's installment simply stays Unpaid.
     */
    public static String commitmentStatus(String s) {
        String v = s == null ? '' : s.toLowerCase();
        // paused → stays Active: Salesforce only allows Paused with a separate pause schedule, which the team doesn't use.
        if (v == 'cancelled' || v == 'canceled' || v == 'completed' || v == 'stopped' || v == 'ended') return 'Canceled';
        return 'Active';
    }

    /** Donorbox plan type → [TransactionPeriod, TransactionInterval]. */
    public static List<Object> cadence(String type) {
        String v = type == null ? '' : type.toLowerCase().replace('-', '').replace('_', '');
        if (v == 'weekly') return new List<Object>{ 'Weekly', 1 };
        if (v == 'biweekly' || v == 'fortnightly') return new List<Object>{ 'Weekly', 2 };
        if (v == 'quarterly') return new List<Object>{ 'Monthly', 3 };
        if (v == 'semiannually' || v == 'biannually') return new List<Object>{ 'Monthly', 6 };
        if (v == 'annually' || v == 'yearly' || v == 'annual') return new List<Object>{ 'Yearly', 1 };
        return new List<Object>{ 'Monthly', 1 };
    }

    /** GiftCommitmentSchedule.TransactionDay picklist: "1".."30" or "LastDay". */
    public static String transactionDay(Date d) {
        return d.day() >= 31 ? 'LastDay' : String.valueOf(d.day());
    }

    /** Street line 1 + line 2, without repeating line 2 when it's already in line 1. */
    public static String street(String line1, String line2) {
        if (line1 == null) return line2;
        if (line2 == null || line1.toLowerCase().contains(line2.toLowerCase())) return line1;
        return line1 + '\n' + line2;
    }

    /** "In memory of: …" block for Commemoration_Request__c, from Donorbox's `honor`. */
    public static String tribute(Map<String, Object> honor) {
        String honoree = str(honor, 'honoree_name');
        if (honoree == null) return null;
        List<String> lines = new List<String>();
        String kind = str(honor, 'dedication_type');
        lines.add((kind == null ? 'Dedication' : kind) + ': ' + honoree);
        String rName = str(honor, 'recipient_name');
        String rEmail = str(honor, 'recipient_email');
        if (rName != null || rEmail != null) {
            String notify = 'Notify: ' + (rName == null ? '' : rName)
                + (rEmail == null ? '' : ' <' + rEmail + '>');
            String how = str(honor, 'notify_type');
            lines.add(notify.trim() + (how == null ? '' : ' (' + how + ')'));
        }
        String addr = str(honor, 'recipient_address');
        if (addr != null) lines.add('Address: ' + addr);
        String msg = str(honor, 'recipient_message');
        if (msg != null) lines.add('Message: ' + msg);
        return String.join(lines, '\n').left(3000);
    }

    // ── Countries ─────────────────────────────────────────────

    private static final Map<String, String> COUNTRIES = new Map<String, String>{
        'US' => 'United States', 'USA' => 'United States', 'IL' => 'Israel', 'CA' => 'Canada',
        'GB' => 'United Kingdom', 'UK' => 'United Kingdom', 'AU' => 'Australia', 'NZ' => 'New Zealand',
        'FR' => 'France', 'DE' => 'Germany', 'IT' => 'Italy', 'ES' => 'Spain', 'PT' => 'Portugal',
        'NL' => 'Netherlands', 'BE' => 'Belgium', 'CH' => 'Switzerland', 'AT' => 'Austria', 'IE' => 'Ireland',
        'SE' => 'Sweden', 'NO' => 'Norway', 'DK' => 'Denmark', 'FI' => 'Finland', 'IS' => 'Iceland',
        'PL' => 'Poland', 'CZ' => 'Czech Republic', 'SK' => 'Slovakia', 'HU' => 'Hungary', 'RO' => 'Romania',
        'BG' => 'Bulgaria', 'GR' => 'Greece', 'HR' => 'Croatia', 'SI' => 'Slovenia', 'RS' => 'Serbia',
        'UA' => 'Ukraine', 'RU' => 'Russia', 'BY' => 'Belarus', 'MD' => 'Moldova', 'LT' => 'Lithuania',
        'LV' => 'Latvia', 'EE' => 'Estonia', 'GE' => 'Georgia', 'AM' => 'Armenia', 'AZ' => 'Azerbaijan',
        'TR' => 'Turkey', 'CY' => 'Cyprus', 'MT' => 'Malta', 'LU' => 'Luxembourg', 'MC' => 'Monaco',
        'MX' => 'Mexico', 'BR' => 'Brazil', 'AR' => 'Argentina', 'CL' => 'Chile', 'CO' => 'Colombia',
        'PE' => 'Peru', 'UY' => 'Uruguay', 'VE' => 'Venezuela', 'PA' => 'Panama', 'CR' => 'Costa Rica',
        'PR' => 'Puerto Rico', 'ZA' => 'South Africa', 'EG' => 'Egypt', 'MA' => 'Morocco', 'KE' => 'Kenya',
        'NG' => 'Nigeria', 'ET' => 'Ethiopia', 'AE' => 'United Arab Emirates', 'BH' => 'Bahrain',
        'JO' => 'Jordan', 'IN' => 'India', 'CN' => 'China', 'HK' => 'Hong Kong', 'TW' => 'Taiwan',
        'JP' => 'Japan', 'KR' => 'South Korea', 'SG' => 'Singapore', 'TH' => 'Thailand', 'PH' => 'Philippines',
        'VN' => 'Vietnam', 'MY' => 'Malaysia', 'ID' => 'Indonesia', 'KZ' => 'Kazakhstan', 'UZ' => 'Uzbekistan'
    };

    /** ISO code → full country name; anything that isn't a known code is returned unchanged. */
    /** Amount for names: 18$ for USD, otherwise 18 EUR. */
    public static String money(Decimal amount, String currencyCode) {
        String n = amount == null ? '0' : amount.stripTrailingZeros().toPlainString();
        String c = currencyCode == null ? 'USD' : currencyCode.toUpperCase();
        return c == 'USD' ? n + '$' : n + ' ' + c;
    }

    public static String countryName(String code) {
        if (code == null) return null;
        String name = COUNTRIES.get(code.toUpperCase());
        return name == null ? code : name;
    }
}