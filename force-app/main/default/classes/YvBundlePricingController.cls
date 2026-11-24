/**
 * PR1 | A01-02 pricing rule | Bundle items and their price in each price book (Bundle_Item_Entry__c).
 * Used by the yvBundleItems LWC on the Bundle record page.
 * A new item starts at the product's list price in each price book (the LWC pre-fills it); the price in the bundle can differ.
 * One price per item per price book is enforced by the flow A1_Bundle_Item_Entry_Unique_Key (Unique_Key__c).
 */
public with sharing class YvBundlePricingController {

    public class Book {
        @AuraEnabled public Id pricebookId;
        @AuraEnabled public String name;
        @AuraEnabled public String pricelistType;
        @AuraEnabled public Date validFrom;
        @AuraEnabled public Date validTo;
    }

    public class Cell {
        @AuraEnabled public Id pricebookId;
        @AuraEnabled public Decimal price;      // price saved in the bundle (Bundle_Item_Entry__c), null when none
        @AuraEnabled public Decimal listPrice;  // regular price of the product in this price book, null when none
    }

    public class ItemRow {
        @AuraEnabled public Id itemId;
        @AuraEnabled public Id productId;
        @AuraEnabled public String productName;
        @AuraEnabled public Decimal quantity;
        @AuraEnabled public Decimal defaultOrder;
        @AuraEnabled public List<Cell> cells = new List<Cell>();
    }

    public class BundleData {
        @AuraEnabled public List<Book> books = new List<Book>();
        @AuraEnabled public List<ItemRow> items = new List<ItemRow>();
    }

    public class EntryInput {
        public Id pricebookId;
        public Decimal price;
    }

    public class ItemInput {
        public Id itemId;
        public Id productId;
        public Decimal quantity;
        public Decimal defaultOrder;
        public Boolean active;
        public List<EntryInput> prices;
    }

    /** Price books: active, non-standard, valid today, plus any the bundle already has a price in. Items: the active ones. */
    @AuraEnabled
    public static BundleData getBundleData(Id bundleId) {
        BundleData d = new BundleData();

        List<Bundle_Item__c> items = [SELECT Id, Product__c, Product__r.Name, Quantity__c, Default_Order__c
                                      FROM Bundle_Item__c WHERE Bundle__c = :bundleId AND Active__c = true
                                      ORDER BY Default_Order__c ASC NULLS LAST, CreatedDate ASC];
        Map<String, Decimal> saved = new Map<String, Decimal>();
        Set<Id> usedBooks = new Set<Id>();
        for (Bundle_Item_Entry__c e : [SELECT Bundle_Item__c, Price_Book__c, Price__c FROM Bundle_Item_Entry__c
                                       WHERE Bundle_Item__r.Bundle__c = :bundleId AND Bundle_Item__r.Active__c = true]) {
            if (e.Price_Book__c != null) {
                saved.put(key(e.Bundle_Item__c, e.Price_Book__c), e.Price__c);
                usedBooks.add(e.Price_Book__c);
            }
        }

        Date today = Date.today();
        for (Pricebook2 pb : [SELECT Id, Name, Pricelist_Type__c, Valid_From__c, Valid_To__c FROM Pricebook2
                              WHERE IsActive = true AND IsStandard = false
                              AND (((Valid_From__c = null OR Valid_From__c <= :today) AND (Valid_To__c = null OR Valid_To__c >= :today))
                                   OR Id IN :usedBooks)
                              ORDER BY Valid_From__c DESC NULLS LAST, Name]) {
            Book b = new Book();
            b.pricebookId = pb.Id;
            b.name = pb.Name;
            b.pricelistType = pb.Pricelist_Type__c;
            b.validFrom = pb.Valid_From__c;
            b.validTo = pb.Valid_To__c;
            d.books.add(b);
        }

        Set<Id> productIds = new Set<Id>();
        for (Bundle_Item__c i : items) { if (i.Product__c != null) { productIds.add(i.Product__c); } }
        Map<String, Decimal> listByKey = new Map<String, Decimal>();
        for (PricebookEntry pbe : [SELECT Product2Id, Pricebook2Id, UnitPrice FROM PricebookEntry
                                   WHERE Product2Id IN :productIds AND IsActive = true AND Pricebook2.IsStandard = false]) {
            listByKey.put(key(pbe.Product2Id, pbe.Pricebook2Id), pbe.UnitPrice);
        }

        for (Bundle_Item__c i : items) {
            ItemRow r = new ItemRow();
            r.itemId = i.Id;
            r.productId = i.Product__c;
            r.productName = i.Product__r.Name;
            r.quantity = i.Quantity__c;
            r.defaultOrder = i.Default_Order__c;
            for (Book b : d.books) {
                Cell c = new Cell();
                c.pricebookId = b.pricebookId;
                c.price = saved.get(key(i.Id, b.pricebookId));
                c.listPrice = listByKey.get(key(i.Product__c, b.pricebookId));
                r.cells.add(c);
            }
            d.items.add(r);
        }
        return d;
    }

    /**
     * Saves the items and their prices, all or nothing.
     * New item (no itemId): created. Existing item: quantity, order and active flag updated.
     * An empty price deletes the entry of that item and price book.
     */
    @AuraEnabled
    public static void saveBundle(Id bundleId, String itemsJson) {
        List<ItemInput> inputs = (List<ItemInput>) JSON.deserialize(itemsJson, List<ItemInput>.class);

        Map<Id, Bundle_Item__c> existing = new Map<Id, Bundle_Item__c>(
            [SELECT Id, Product__c, Quantity__c, Default_Order__c, Active__c FROM Bundle_Item__c WHERE Bundle__c = :bundleId]);
        Map<String, Bundle_Item_Entry__c> entries = new Map<String, Bundle_Item_Entry__c>();
        for (Bundle_Item_Entry__c e : [SELECT Id, Bundle_Item__c, Price_Book__c, Price__c FROM Bundle_Item_Entry__c
                                       WHERE Bundle_Item__r.Bundle__c = :bundleId]) {
            entries.put(key(e.Bundle_Item__c, e.Price_Book__c), e);
        }

        for (ItemInput i : inputs) { validate(i, existing); }

        Savepoint sp = Database.setSavepoint();
        try {
            List<Bundle_Item__c> toInsert = new List<Bundle_Item__c>();
            List<ItemInput> insertedInputs = new List<ItemInput>();
            List<Bundle_Item__c> toUpdate = new List<Bundle_Item__c>();
            for (ItemInput i : inputs) {
                Boolean active = i.active != false;
                if (i.itemId == null) {
                    toInsert.add(new Bundle_Item__c(Bundle__c = bundleId, Product__c = i.productId, Quantity__c = i.quantity,
                                                    Default_Order__c = i.defaultOrder, Active__c = active));
                    insertedInputs.add(i);
                } else {
                    Bundle_Item__c it = existing.get(i.itemId);
                    it.Quantity__c = i.quantity;
                    it.Default_Order__c = i.defaultOrder;
                    it.Active__c = active;
                    toUpdate.add(it);
                }
            }
            if (!toUpdate.isEmpty()) { update toUpdate; }
            if (!toInsert.isEmpty()) {
                insert toInsert;
                for (Integer n = 0; n < toInsert.size(); n++) { insertedInputs[n].itemId = toInsert[n].Id; }
            }

            List<Bundle_Item_Entry__c> toDelete = new List<Bundle_Item_Entry__c>();
            List<Bundle_Item_Entry__c> entryUpdates = new List<Bundle_Item_Entry__c>();
            List<Bundle_Item_Entry__c> entryInserts = new List<Bundle_Item_Entry__c>();
            for (ItemInput i : inputs) {
                if (i.active == false || i.prices == null) { continue; }
                for (EntryInput p : i.prices) {
                    Bundle_Item_Entry__c e = entries.get(key(i.itemId, p.pricebookId));
                    if (e == null) {
                        if (p.price != null) {
                            entryInserts.add(new Bundle_Item_Entry__c(Bundle_Item__c = i.itemId, Price_Book__c = p.pricebookId, Price__c = p.price));
                        }
                    } else if (p.price == null) {
                        toDelete.add(e);
                    } else if (e.Price__c != p.price) {
                        e.Price__c = p.price;
                        entryUpdates.add(e);
                    }
                }
            }
            if (!toDelete.isEmpty()) { delete toDelete; }
            if (!entryUpdates.isEmpty()) { update entryUpdates; }
            if (!entryInserts.isEmpty()) { insert entryInserts; }
        } catch (DmlException e) {
            Database.rollback(sp);
            throw aura(e.getDmlMessage(0));
        }
    }

    private static void validate(ItemInput i, Map<Id, Bundle_Item__c> existing) {
        if (i.itemId != null && !existing.containsKey(i.itemId)) { throw aura(System.Label.YvBundleErrItemNotInBundle); }
        if (i.itemId == null && i.productId == null) { throw aura(System.Label.YvBundleErrProductRequired); }
        if (i.quantity == null || i.quantity < 1 || i.quantity > 9999 || i.quantity != i.quantity.round()) {
            throw aura(System.Label.YvBundleErrQuantity);
        }
        Set<Id> seen = new Set<Id>();
        if (i.prices != null) {
            for (EntryInput p : i.prices) {
                if (!seen.add(p.pricebookId)) { throw aura(System.Label.YvPricingErrDuplicateBook); }
                if (p.price != null && p.price < 0) { throw aura(System.Label.YvPricingErrNegative); }
            }
        }
    }

    private static String key(Id a, Id b) {
        return String.valueOf(a) + '|' + String.valueOf(b);
    }

    private static AuraHandledException aura(String msg) {
        AuraHandledException e = new AuraHandledException(msg);
        e.setMessage(msg);
        return e;
    }
}