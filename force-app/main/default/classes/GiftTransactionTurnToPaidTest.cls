@isTest
private class GiftTransactionTurnToPaidTest {
    
    @isTest
    static void testTurnToPaid() {
        GiftTransaction gt = new GiftTransaction(
            Name = 'Test GT',
            PaymentMethod = 'Cash',
            OriginalAmount = 100,
            Turn_to_Paid__c = false
        );
        insert gt;
        
        gt.Turn_to_Paid__c = true;
        gt.TransactionDate = Date.today();
        update gt;
        
        GiftTransaction result = [SELECT Status, Turn_to_Paid__c FROM GiftTransaction WHERE Id = :gt.Id];
        System.assertEquals('Paid', result.Status);
        System.assertEquals(false, result.Turn_to_Paid__c);
    }
    
    @isTest
    static void testTurnToPaidAgain() {
        GiftTransaction gt = new GiftTransaction(
            Name = 'Test GT 2',
            PaymentMethod = 'Cash',
            OriginalAmount = 200,
            Turn_to_Paid__c = false
        );
        insert gt;
        
        gt.Turn_to_Paid__c = true;
        gt.TransactionDate = Date.today();
        update gt;
        
        GiftTransaction result = [SELECT Status, Turn_to_Paid__c FROM GiftTransaction WHERE Id = :gt.Id];
        System.assertEquals('Paid', result.Status);
        System.assertEquals(false, result.Turn_to_Paid__c);
    }
}