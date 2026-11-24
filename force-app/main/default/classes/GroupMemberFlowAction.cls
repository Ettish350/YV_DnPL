public class GroupMemberFlowAction {

    @InvocableMethod(label='Get All Group Member Emails (The Layer Peeler)' description='Deep-scans groups, sub-groups, roles, and managers layer by layer')
    public static List<List<String>> getEmailsFromGroups(List<Id> groupIds) {
        List<List<String>> finalResults = new List<List<String>>();
        
        // Load all roles for fast hierarchy lookup
        Map<Id, UserRole> roleMap = new Map<Id, UserRole>([SELECT Id, ParentRoleId FROM UserRole]);

        for (Id gid : groupIds) {
            Set<Id> userIdsFound = new Set<Id>();
            Set<Id> groupsToScan = new Set<Id>{gid}; 
            Set<Id> scannedGroups = new Set<Id>();
            Set<Id> roleIdsToScan = new Set<Id>();

            // --- Step 1: Peel layers of Groups and Roles ---
            while (!groupsToScan.isEmpty()) {
                Id currentGroupId = new List<Id>(groupsToScan)[0];
                groupsToScan.remove(currentGroupId);
                scannedGroups.add(currentGroupId);

                // Find direct Users or nested Groups
                for (GroupMember gm : [SELECT UserOrGroupId FROM GroupMember WHERE GroupId = :currentGroupId]) {
                    Id memberId = gm.UserOrGroupId;
                    String sType = memberId.getSObjectType().getDescribe().getName();

                    if (sType == 'User') {
                        userIdsFound.add(memberId);
                    } else if (sType == 'Group' && !scannedGroups.contains(memberId)) {
                        groupsToScan.add(memberId);
                    }
                }

                // Check if the group represents a Role or Subordinates
                for (Group g : [SELECT RelatedId, Type FROM Group WHERE Id = :currentGroupId AND RelatedId != null]) {
                    if (g.Type.contains('Role')) {
                        roleIdsToScan.add(g.RelatedId);
                        // Add all child roles if subordinates are included
                        if (g.Type.contains('Subordinates')) {
                            roleIdsToScan.addAll(findChildRoles(g.RelatedId, roleMap));
                        }
                    }
                }
            }

            // --- Step 2: Get Users from discovered Roles ---
            if (!roleIdsToScan.isEmpty()) {
                for (User u : [SELECT Id FROM User WHERE UserRoleId IN :roleIdsToScan AND IsActive = true]) {
                    userIdsFound.add(u.Id);
                }
            }

            // --- Step 3: Get Managers if Hierarchy is enabled ---
            Group mainGroup = [SELECT DoesIncludeBosses FROM Group WHERE Id = :gid LIMIT 1];
            if (mainGroup.DoesIncludeBosses && !userIdsFound.isEmpty()) {
                userIdsFound.addAll(findAllManagers(userIdsFound, roleMap));
            }

            // --- Step 4: Convert User IDs to Emails ---
            List<String> emailList = new List<String>();
            if (!userIdsFound.isEmpty()) {
                for (User u : [SELECT Email FROM User WHERE Id IN :userIdsFound AND IsActive = true]) {
                    emailList.add(u.Email);
                }
            }
            finalResults.add(emailList);
        }
        return finalResults;
    }

    // Helper: Find all roles below a specific role
    private static Set<Id> findChildRoles(Id parentId, Map<Id, UserRole> roleMap) {
        Set<Id> children = new Set<Id>();
        for (UserRole ur : roleMap.values()) {
            if (ur.ParentRoleId == parentId) {
                children.add(ur.Id);
                children.addAll(findChildRoles(ur.Id, roleMap));
            }
        }
        return children;
    }

    // Helper: Find all managers above the users in the hierarchy
    private static Set<Id> findAllManagers(Set<Id> userIds, Map<Id, UserRole> roleMap) {
        Set<Id> managerIds = new Set<Id>();
        Set<Id> currentRoleIds = new Set<Id>();
        for (User u : [SELECT UserRoleId FROM User WHERE Id IN :userIds AND UserRoleId != null]) {
            currentRoleIds.add(u.UserRoleId);
        }
        
        Set<Id> parentRoleIds = new Set<Id>();
        for (Id rid : currentRoleIds) {
            Id parent = roleMap.get(rid)?.ParentRoleId;
            while (parent != null) {
                parentRoleIds.add(parent);
                parent = roleMap.get(parent)?.ParentRoleId;
            }
        }
        
        if (!parentRoleIds.isEmpty()) {
            for (User u : [SELECT Id FROM User WHERE UserRoleId IN :parentRoleIds AND IsActive = true]) {
                managerIds.add(u.Id);
            }
        }
        return managerIds;
    }
}