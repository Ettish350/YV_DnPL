trigger GiftTransactionTurnToPaid on GiftTransaction (before update) {
    for (GiftTransaction gt : Trigger.new) {
        if (gt.Turn_to_Paid__c == true && Trigger.oldMap.get(gt.Id).Turn_to_Paid__c == false) {
            gt.Status = 'Paid';
            gt.Turn_to_Paid__c = false;
        }
    }
}