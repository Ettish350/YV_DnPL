/** PR1 | V01-03 | Tests for YvProductPricingController. */
@IsTest
private class YvProductPricingControllerTest {

    private static List<Pricebook2> makeBooks() {
        List<Pricebook2> books = new List<Pricebook2>{
            new Pricebook2(Name = 'PB A', IsActive = true, Valid_From__c = Date.today().addDays(-10), Valid_To__c = Date.today().addDays(300)),
            new Pricebook2(Name = 'PB B', IsActive = true, Valid_From__c = Date.today().addDays(-10), Valid_To__c = Date.today().addDays(300)),
            new Pricebook2(Name = 'PB Expired', IsActive = true, Valid_From__c = Date.today().addDays(-400), Valid_To__c = Date.today().addDays(-30))
        };
        insert books;
        return books;
    }

    private static Product2 makeProduct(Boolean isFree) {
        Product2 p = new Product2(Name = 'Test product', IsActive = true, Is_Free__c = isFree);
        insert p;
        return p;
    }

    private static void save(Product2 p, String pricesJson) {
        YvProductPricingController.savePrices(p.Id, p.Is_Free__c == true, pricesJson);
    }

    private static String json(List<Id> bookIds, List<Decimal> prices) {
        List<Map<String, Object>> rows = new List<Map<String, Object>>();
        for (Integer i = 0; i < bookIds.size(); i++) {
            rows.add(new Map<String, Object>{ 'pricebookId' => bookIds[i], 'price' => prices[i] });
        }
        return JSON.serialize(rows);
    }

    private static Integer entries(Id productId) {
        return [SELECT COUNT() FROM PricebookEntry WHERE Product2Id = :productId AND Pricebook2.IsStandard = false];
    }

    @IsTest
    static void listsStandardAndValidBooksOnly() {
        makeBooks();
        Product2 p = makeProduct(false);
        YvProductPricingController.PriceData d = YvProductPricingController.getPrices(p.Id);
        Integer custom = 0;
        for (YvProductPricingController.PriceRow r : d.prices) { if (!r.isStandard) { custom++; } }
        System.assertEquals(2, custom, 'expired price book is not offered');
    }

    @IsTest
    static void paidProductAtZeroSavesWithFlag() {
        List<Pricebook2> books = makeBooks();
        Product2 p = makeProduct(false);
        save(p, json(new List<Id>{ books[0].Id, books[1].Id }, new List<Decimal>{ 0, 40 }));
        System.assertEquals(true, [SELECT Has_Zero_Price__c FROM Product2 WHERE Id = :p.Id].Has_Zero_Price__c);
        System.assertEquals(2, entries(p.Id));
    }

    @IsTest
    static void paidProductWithPricesHasNoFlag() {
        List<Pricebook2> books = makeBooks();
        Product2 p = makeProduct(false);
        save(p, json(new List<Id>{ books[0].Id, books[1].Id }, new List<Decimal>{ 10, 40 }));
        System.assertEquals(false, [SELECT Has_Zero_Price__c FROM Product2 WHERE Id = :p.Id].Has_Zero_Price__c);
    }

    @IsTest
    static void standardPriceIsTheOneEntered() {
        List<Pricebook2> books = makeBooks();
        Id stdId = Test.getStandardPricebookId();
        Product2 p = makeProduct(false);
        save(p, json(new List<Id>{ stdId, books[0].Id }, new List<Decimal>{ 25, 40 }));
        System.assertEquals(25, [SELECT UnitPrice FROM PricebookEntry WHERE Product2Id = :p.Id AND Pricebook2Id = :stdId].UnitPrice);
        System.assertEquals(1, entries(p.Id));
    }

    @IsTest
    static void freeProductAtFiftyIsBlocked() {
        List<Pricebook2> books = makeBooks();
        Product2 p = makeProduct(true);
        try {
            save(p, json(new List<Id>{ books[0].Id }, new List<Decimal>{ 50 }));
            System.assert(false, 'should have been blocked');
        } catch (AuraHandledException e) {
            System.assertEquals(0, entries(p.Id));
        }
    }

    @IsTest
    static void freeProductSavesAtZeroInEveryBook() {
        List<Pricebook2> books = makeBooks();
        Product2 p = makeProduct(true);
        save(p, json(new List<Id>{ books[0].Id, books[1].Id }, new List<Decimal>{ null, 0 }));
        System.assertEquals(2, entries(p.Id));
        System.assertEquals(false, [SELECT Has_Zero_Price__c FROM Product2 WHERE Id = :p.Id].Has_Zero_Price__c);
    }

    @IsTest
    static void samePriceBookTwiceIsBlocked() {
        List<Pricebook2> books = makeBooks();
        Product2 p = makeProduct(false);
        try {
            save(p, json(new List<Id>{ books[0].Id, books[0].Id }, new List<Decimal>{ 10, 20 }));
            System.assert(false, 'should have been blocked');
        } catch (AuraHandledException e) {
            System.assertEquals(0, entries(p.Id));
        }
    }

    @IsTest
    static void checkingFreeZeroesEveryPriceAndSetsTheFlag() {
        List<Pricebook2> books = makeBooks();
        Product2 p = makeProduct(false);
        List<Id> ids = new List<Id>{ books[0].Id, books[1].Id };
        save(p, json(ids, new List<Decimal>{ 10, 40 }));
        YvProductPricingController.savePrices(p.Id, true, json(ids, new List<Decimal>{ 0, 0 }));
        System.assertEquals(true, [SELECT Is_Free__c FROM Product2 WHERE Id = :p.Id].Is_Free__c);
        System.assertEquals(0, [SELECT COUNT() FROM PricebookEntry WHERE Product2Id = :p.Id AND UnitPrice != 0]);
    }

    @IsTest
    static void freeCheckedWithPriceAboveZeroIsBlocked() {
        List<Pricebook2> books = makeBooks();
        Product2 p = makeProduct(false);
        try {
            YvProductPricingController.savePrices(p.Id, true, json(new List<Id>{ books[0].Id }, new List<Decimal>{ 50 }));
            System.assert(false, 'should have been blocked');
        } catch (AuraHandledException e) {
            System.assertEquals(false, [SELECT Is_Free__c FROM Product2 WHERE Id = :p.Id].Is_Free__c);
        }
    }

    @IsTest
    static void uncheckingFreeAllowsPrices() {
        List<Pricebook2> books = makeBooks();
        Product2 p = makeProduct(true);
        YvProductPricingController.savePrices(p.Id, false, json(new List<Id>{ books[0].Id }, new List<Decimal>{ 30 }));
        System.assertEquals(false, [SELECT Is_Free__c FROM Product2 WHERE Id = :p.Id].Is_Free__c);
        System.assertEquals(30, [SELECT UnitPrice FROM PricebookEntry WHERE Product2Id = :p.Id AND Pricebook2Id = :books[0].Id].UnitPrice);
    }

    @IsTest
    static void changeAndClearPrices() {
        List<Pricebook2> books = makeBooks();
        Product2 p = makeProduct(false);
        List<Id> ids = new List<Id>{ books[0].Id, books[1].Id };
        save(p, json(ids, new List<Decimal>{ 10, 40 }));
        save(p, json(ids, new List<Decimal>{ 15, null }));
        System.assertEquals(1, entries(p.Id), 'empty price removes the entry');
        System.assertEquals(15, [SELECT UnitPrice FROM PricebookEntry WHERE Product2Id = :p.Id AND Pricebook2Id = :books[0].Id].UnitPrice);
    }
}