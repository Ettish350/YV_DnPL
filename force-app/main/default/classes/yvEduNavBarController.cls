public with sharing class yvEduNavBarController {
    @AuraEnabled(cacheable=true)
    public static String getInstitutionName() {
        try {
            List<User> users = [
                SELECT ContactId
                FROM User
                WHERE Id = :UserInfo.getUserId()
                WITH USER_MODE
                LIMIT 1
            ];
            if (users.isEmpty() || users[0].ContactId == null) {
                return null;
            }

            List<Contact> contacts = [
                SELECT Account.Name
                FROM Contact
                WHERE Id = :users[0].ContactId
                WITH USER_MODE
                LIMIT 1
            ];
            if (contacts.isEmpty() || contacts[0].Account == null) {
                return null;
            }
            return contacts[0].Account.Name;
        } catch (Exception e) {
            return null;
        }
    }

    @AuraEnabled
    public static void updateLanguage(String languageLocaleKey) {
        try {
            update new User(Id = UserInfo.getUserId(), LanguageLocaleKey = languageLocaleKey);
        } catch (Exception e) {
            throw new AuraHandledException(e.getMessage());
        }
    }
}