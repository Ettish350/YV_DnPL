@isTest
private class GroupMemberFlowActionTest {

    private static Integer seq = 0;

    private static User newUser(Id roleId) {
        seq++;
        String key = 'gmfa' + String.valueOf(Math.abs(Crypto.getRandomInteger())) + seq;
        return new User(
            LastName = 'Tester',
            Alias = 'gmfa' + seq,
            Email = key + '@example.com',
            Username = key + '@gmfatest.example.com',
            ProfileId = UserInfo.getProfileId(),
            UserRoleId = roleId,
            EmailEncodingKey = 'UTF-8',
            LanguageLocaleKey = 'en_US',
            LocaleSidKey = 'en_US',
            TimeZoneSidKey = 'America/Los_Angeles'
        );
    }

    private static Group newGroup(Boolean includeBosses) {
        seq++;
        String key = 'gmfa' + String.valueOf(Math.abs(Crypto.getRandomInteger())) + seq;
        return new Group(Name = key, DeveloperName = key, Type = 'Regular',
                         DoesIncludeBosses = includeBosses);
    }

    private static UserRole newRole(Id parentId) {
        seq++;
        String key = 'gmfa' + String.valueOf(Math.abs(Crypto.getRandomInteger())) + seq;
        UserRole r = new UserRole(Name = key, DeveloperName = key, ParentRoleId = parentId);
        insert r;
        return r;
    }


    @isTest
    static void testDirectAndNestedMembers() {
        User u1 = newUser(null);
        User u2 = newUser(null);
        insert new List<User>{ u1, u2 };

        Group child = newGroup(false);
        Group parent = newGroup(false);
        insert new List<Group>{ child, parent };

        insert new List<GroupMember>{
            new GroupMember(GroupId = child.Id,  UserOrGroupId = u1.Id),
            new GroupMember(GroupId = parent.Id, UserOrGroupId = u2.Id),
            new GroupMember(GroupId = parent.Id, UserOrGroupId = child.Id)
        };

        Test.startTest();
        List<List<String>> result =
            GroupMemberFlowAction.getEmailsFromGroups(new List<Id>{ parent.Id });
        Test.stopTest();

        Set<String> emails = new Set<String>(result[0]);
        System.assertEquals(2, emails.size(), 'Expected direct + nested member');
        System.assert(emails.contains(u1.Email), 'Nested member missing');
        System.assert(emails.contains(u2.Email), 'Direct member missing');
    }


    @isTest
    static void testRoleAndSubordinates() {
        UserRole top = newRole(null);
        UserRole bottom = newRole(top.Id);

        User topUser = newUser(top.Id);
        User bottomUser = newUser(bottom.Id);
        insert new List<User>{ topUser, bottomUser };

        // Salesforce auto-creates role groups; they cannot be inserted manually.
        Group ras = [SELECT Id FROM Group
                     WHERE RelatedId = :top.Id AND Type = 'RoleAndSubordinates' LIMIT 1];

        Test.startTest();
        List<List<String>> result =
            GroupMemberFlowAction.getEmailsFromGroups(new List<Id>{ ras.Id });
        Test.stopTest();

        Set<String> emails = new Set<String>(result[0]);
        System.assert(emails.contains(topUser.Email), 'Role holder missing');
        System.assert(emails.contains(bottomUser.Email), 'Subordinate missing');
    }


    @isTest
    static void testIncludeBosses() {
        UserRole top = newRole(null);
        UserRole bottom = newRole(top.Id);

        User boss = newUser(top.Id);
        User staff = newUser(bottom.Id);
        insert new List<User>{ boss, staff };

        Group g = newGroup(true);
        insert g;
        insert new GroupMember(GroupId = g.Id, UserOrGroupId = staff.Id);

        Test.startTest();
        List<List<String>> result =
            GroupMemberFlowAction.getEmailsFromGroups(new List<Id>{ g.Id });
        Test.stopTest();

        Set<String> emails = new Set<String>(result[0]);
        System.assert(emails.contains(staff.Email), 'Member missing');
        System.assert(emails.contains(boss.Email), 'Manager missing');
    }


    @isTest
    static void testInactiveAndEmpty() {
        User active = newUser(null);
        User inactive = newUser(null);
        insert new List<User>{ active, inactive };

        Group g = newGroup(false);
        Group empty = newGroup(false);
        insert new List<Group>{ g, empty };

        insert new List<GroupMember>{
            new GroupMember(GroupId = g.Id, UserOrGroupId = active.Id),
            new GroupMember(GroupId = g.Id, UserOrGroupId = inactive.Id)
        };

        inactive.IsActive = false;
        update inactive;

        Test.startTest();
        List<List<String>> result =
            GroupMemberFlowAction.getEmailsFromGroups(new List<Id>{ g.Id, empty.Id });
        Test.stopTest();

        Set<String> emails = new Set<String>(result[0]);
        System.assertEquals(2, result.size(), 'One list per input Id');
        System.assert(emails.contains(active.Email), 'Active member missing');
        System.assert(!emails.contains(inactive.Email), 'Inactive user should be excluded');
        System.assertEquals(0, result[1].size(), 'Empty group should return no emails');
    }
}