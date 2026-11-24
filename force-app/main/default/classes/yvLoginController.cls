/**
 * One login entry point for every type of external user, called by the guest-accessible LWC
 * yvLoginForm before the visitor is authenticated. The page sends the login type and the fields of
 * that type; this class works out the Salesforce username and calls Site.login.
 *
 * Types (add a type: add a constant and a case in resolveUsername):
 *   edu    institutionId + email + password. Same lookup as yvEduLoginController (Account by
 *          Institution_ID__c, then the Contact by email, whose Portal_Username__c is the username).
 *   agent  username + password. The username is the Salesforce username as typed.
 *
 * The type only decides which fields the page asks for and how the username is found. It grants
 * nothing: what the user can see and do after signing in comes from their own profile and
 * permission sets.
 *
 * Deliberately `without sharing`: the Guest User has no access to these records by design (see the
 * note in yvEduLoginController: a guest query against User cannot see rows). It never returns why a
 * login failed, so a caller cannot find out which institution IDs, emails or usernames exist.
 */
public without sharing class yvLoginController {
    public static final String TYPE_EDU = 'edu';
    public static final String TYPE_AGENT = 'agent';

    public class LoginResult {
        @AuraEnabled public Boolean success;
        @AuraEnabled public String redirectUrl;
    }

    @AuraEnabled
    public static LoginResult login(String loginType, Map<String, String> credentials, String startUrl) {
        LoginResult result = new LoginResult();
        result.success = false;

        if (credentials == null || String.isBlank(credentials.get('password'))) {
            return result;
        }

        String username = resolveUsername(loginType, credentials);
        if (username == null) {
            return result;
        }

        PageReference loginPage;
        try {
            loginPage = Site.login(username, credentials.get('password'), safeStartUrl(startUrl));
        } catch (Exception e) {
            loginPage = null;
        }
        if (loginPage != null) {
            result.success = true;
            result.redirectUrl = loginPage.getUrl();
        }
        return result;
    }

    @TestVisible
    private static String resolveUsername(String loginType, Map<String, String> credentials) {
        if (loginType == TYPE_EDU) {
            String institutionId = credentials.get('institutionId');
            String email = credentials.get('email');
            if (String.isBlank(institutionId) || String.isBlank(email)) {
                return null;
            }
            return findEduUsername(institutionId, email);
        }
        if (loginType == TYPE_AGENT) {
            String username = credentials.get('username');
            return String.isBlank(username) ? null : username.trim();
        }
        return null; // unknown type
    }

    private static String findEduUsername(String institutionId, String email) {
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

    // Only a path inside the site is accepted, so the page cannot send the user to another address.
    @TestVisible
    private static String safeStartUrl(String startUrl) {
        if (String.isBlank(startUrl) || !startUrl.startsWith('/') || startUrl.startsWith('//') || startUrl.contains('\\')) {
            return '/';
        }
        return startUrl;
    }
}