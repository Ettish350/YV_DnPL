/** PR1 | A01-02 pricing rule | Tests for YvBundlePricingController. */
@IsTest
private class YvBundlePricingControllerTest {

    private static Bundle__c bundle;
    private static List<Pricebook2> books;
    private static Product2 product;

    private static void setup() {
        bundle = new Bundle__c(Name = 'Test bundle');
        for (Schema.PicklistEntry e : Bundle__c.Audience_Category__c.getDescribe().getPicklistValues()) {
            if (e.isActive()) { bundle.Audience_Category__c = e.getValue(); break; }
        }
        insert bundle;

        books = new List<Pricebook2>{
            new Pricebook2(Name = 'PB A', IsActive = true, Valid_From__c = Date.today().addDays(-10), Valid_To__c = Date.today().addDays(300)),
            new Pricebook2(Name = 'PB B', IsActive = true, Valid_From__c = Date.today().addDays(-10), Valid_To__c = Date.today().addDays(300)),
            new Pricebook2(Name = 'PB Expired', IsActive = true, Valid_From__c = Date.today().addDays(-400), Valid_To__c = Date.today().addDays(-30))
        };
        insert books;

        product = new Product2(Name = 'Test product', IsActive = true);
        insert product;
        insert new PricebookEntry(Pricebook2Id = Test.getStandardPricebookId(), Product2Id = product.Id, UnitPrice = 120, IsActive = true);
        insert new List<PricebookEntry>{
            new PricebookEntry(Pricebook2Id = books[0].Id, Product2Id = product.Id, UnitPrice = 120, IsActive = true),
            new PricebookEntry(Pricebook2Id = books[1].Id, Product2Id = product.Id, UnitPrice = 150, IsActive = true)
        };
    }

    private static String item(Id itemId, Id productId, Decimal qty, Boolean active, Decimal priceA, Decimal priceB) {
        return JSON.serialize(new List<Map<String, Object>>{
            new Map<String, Object>{
                'itemId' => itemId, 'productId' => productId, 'quantity' => qty, 'defaultOrder' => 1, 'active' => active,
                'prices' => new List<Map<String, Object>>{
                    new Map<String, Object>{ 'pricebookId' => books[0].Id, 'price' => priceA },
                    new Map<String, Object>{ 'pricebookId' => books[1].Id, 'price' => priceB }
                } } });
    }

    private static Bundle_Item__c firstItem() {
        return [SELECT Id, Active__c, Quantity__c FROM Bundle_Item__c WHERE Bundle__c = :bundle.Id LIMIT 1];
    }

    @IsTest
    static void newItemIsSavedWithAPriceDifferentFromList() {
        setup();
        YvBundlePricingController.saveBundle(bundle.Id, item(null, product.Id, 2, true, 110, 150));
        Bundle_Item__c it = firstItem();
        System.assertEquals(2, it.Quantity__c);
        System.assertEquals(2, [SELECT COUNT() FROM Bundle_Item_Entry__c WHERE Bundle_Item__c = :it.Id]);
        System.assertEquals(110, [SELECT Price__c FROM Bundle_Item_Entry__c WHERE Bundle_Item__c = :it.Id AND Price_Book__c = :books[0].Id].Price__c);

        YvBundlePricingController.BundleData d = YvBundlePricingController.getBundleData(bundle.Id);
        System.assertEquals(2, d.books.size(), 'expired price book is not a column');
        System.assertEquals(1, d.items.size());
        System.assertEquals(110, d.items[0].cells[0].price);
        System.assertEquals(120, d.items[0].cells[0].listPrice);
    }

    @IsTest
    static void editPriceAndClearOne() {
        setup();
        YvBundlePricingController.saveBundle(bundle.Id, item(null, product.Id, 1, true, 110, 150));
        Bundle_Item__c it = firstItem();
        YvBundlePricingController.saveBundle(bundle.Id, item(it.Id, product.Id, 1, true, 100, null));
        System.assertEquals(1, [SELECT COUNT() FROM Bundle_Item_Entry__c WHERE Bundle_Item__c = :it.Id], 'empty price removes the entry');
        System.assertEquals(100, [SELECT Price__c FROM Bundle_Item_Entry__c WHERE Bundle_Item__c = :it.Id].Price__c);
    }

    @IsTest
    static void removedItemIsMarkedInactiveAndNotListed() {
        setup();
        YvBundlePricingController.saveBundle(bundle.Id, item(null, product.Id, 1, true, 110, 150));
        Bundle_Item__c it = firstItem();
        YvBundlePricingController.saveBundle(bundle.Id, item(it.Id, product.Id, 1, false, 110, 150));
        System.assertEquals(false, [SELECT Active__c FROM Bundle_Item__c WHERE Id = :it.Id].Active__c);
        System.assertEquals(0, YvBundlePricingController.getBundleData(bundle.Id).items.size());
    }

    @IsTest
    static void invalidInputIsBlocked() {
        setup();
        List<String> bad = new List<String>{
            item(null, product.Id, 0, true, 10, 10),      // quantity
            item(null, product.Id, 1.5, true, 10, 10),    // not whole
            item(null, product.Id, 1, true, -5, 10),      // negative
            item(null, null, 1, true, 10, 10)             // no product
        };
        for (String json : bad) {
            try {
                YvBundlePricingController.saveBundle(bundle.Id, json);
                System.assert(false, 'should have been blocked: ' + json);
            } catch (AuraHandledException e) {
                System.assertEquals(0, [SELECT COUNT() FROM Bundle_Item__c WHERE Bundle__c = :bundle.Id]);
            }
        }
    }

    @IsTest
    static void samePriceBookTwiceInOneItemIsBlocked() {
        setup();
        String json = JSON.serialize(new List<Map<String, Object>>{
            new Map<String, Object>{ 'productId' => product.Id, 'quantity' => 1, 'active' => true,
                'prices' => new List<Map<String, Object>>{
                    new Map<String, Object>{ 'pricebookId' => books[0].Id, 'price' => 10 },
                    new Map<String, Object>{ 'pricebookId' => books[0].Id, 'price' => 20 } } } });
        try {
            YvBundlePricingController.saveBundle(bundle.Id, json);
            System.assert(false, 'should have been blocked');
        } catch (AuraHandledException e) {
            System.assertEquals(0, [SELECT COUNT() FROM Bundle_Item__c WHERE Bundle__c = :bundle.Id]);
        }
    }
}