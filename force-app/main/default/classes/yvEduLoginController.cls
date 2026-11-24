/**
 * Called from the guest-accessible yvEduLoginForm LWC, before the visitor is authenticated.
 * Deliberately `without sharing` and skips FLS enforcement on the Account/Contact lookups below:
 * the Guest User profile has no access to these records by design, and resolving the Site.login
 * username is exactly the privileged operation this class exists to perform on the visitor's behalf.
 *
 * Resolves the username via Contact, not User: Salesforce doesn't support Guest-user Sharing
 * Rules on the User object at all, so a Guest-context query against User can never see a row
 * regardless of object/field permissions. Contact's sharing is "Controlled by Parent" in this
 * org, so once Account is Guest-visible (via a Guest User Sharing Rule), its Contacts are
 * automatically visible too - no separate Contact-side sharing rule is needed, only a Guest
 * "Read" object permission on Contact. Contact.Portal_Username__c holds the actual Salesforce
 * Username, set once by an admin when provisioning each person's portal access. Confirmed
 * end-to-end with a direct Username+Password baseline before building this lookup on top of it.
 *
 * Email (not institution ID) is the lookup key, so exactly one candidate is ever resolved - the
 * institution ID is only cross-checked against that Contact's Account afterwards. This also
 * means Site.login is only ever attempted once, against the one person who actually submitted
 * the form, never against other contacts under the same institution.
 *
 * It never returns *why* a login failed, to avoid letting a caller enumerate valid institution
 * IDs or registered emails.
 */
public without sharing class yvEduLoginController {
    public class LoginResult {
        @AuraEnabled
        public Boolean success;
        @AuraEnabled
        public String redirectUrl;
    }

    @AuraEnabled
    public static LoginResult login(String institutionId, String email, String password, String startUrl) {
        LoginResult result = new LoginResult();
        result.success = false;

        if (String.isNotBlank(institutionId) && String.isNotBlank(email) && String.isNotBlank(password)) {
            String username = findUsername(institutionId, email);
            if (username != null) {
                PageReference loginPage;
                try {
                    loginPage = Site.login(username, password, startUrl);
                } catch (Exception e) {
                    loginPage = null;
                }
                if (loginPage != null) {
                    result.success = true;
                    result.redirectUrl = loginPage.getUrl();
                }
            }
        }

        return result;
    }

    private static String findUsername(String institutionId, String email) {
        List<Account> accounts = [
            SELECT Id
            FROM Account
            WHERE Institution_ID__c = :institutionId
            LIMIT 1
        ];
        if (accounts.isEmpty()) {
            return null;
        }

        List<Contact> contacts = [
            SELECT Portal_Username__c
            FROM Contact
            WHERE AccountId = :accounts[0].Id AND Email = :email AND Portal_Username__c != null
            LIMIT 1
        ];
        return contacts.isEmpty() ? null : contacts[0].Portal_Username__c;
    }
}