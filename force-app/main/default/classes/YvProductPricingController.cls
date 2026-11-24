/**
 * PR1 | V01-03 | Read and save the price of one product in each price book (standard price book included).
 * Used by the yvProductPrices LWC on the Product record page.
 */
public with sharing class YvProductPricingController {

    public class PriceRow {
        @AuraEnabled public Id pricebookId;
        @AuraEnabled public Boolean isStandard;
        @AuraEnabled public String pricebookName;
        @AuraEnabled public String pricelistType;
        @AuraEnabled public Date validFrom;
        @AuraEnabled public Date validTo;
        @AuraEnabled public Decimal price;
    }

    public class PriceData {
        @AuraEnabled public Boolean isFree = false;
        @AuraEnabled public List<PriceRow> prices = new List<PriceRow>();
    }

    public class PriceInput {
        public Id pricebookId;
        public Decimal price;
    }

    /** The standard price book, the active price books valid today, and any price book the product is already priced in. */
    @AuraEnabled
    public static PriceData getPrices(Id productId) {
        PriceData d = new PriceData();
        d.isFree = [SELECT Is_Free__c FROM Product2 WHERE Id = :productId].Is_Free__c == true;
        Map<Id, Decimal> current = new Map<Id, Decimal>();
        for (PricebookEntry pbe : [SELECT Pricebook2Id, UnitPrice FROM PricebookEntry WHERE Product2Id = :productId]) {
            current.put(pbe.Pricebook2Id, pbe.UnitPrice);
        }
        Date today = Date.today();
        Set<Id> pricedIn = current.keySet();
        for (Pricebook2 pb : [SELECT Id, Name, IsStandard, Pricelist_Type__c, Valid_From__c, Valid_To__c FROM Pricebook2
                              WHERE IsActive = true
                              AND (IsStandard = true OR ((Valid_From__c = null OR Valid_From__c <= :today) AND (Valid_To__c = null OR Valid_To__c >= :today))
                                   OR Id IN :pricedIn)
                              ORDER BY IsStandard DESC, Valid_From__c DESC NULLS LAST, Name]) {
            PriceRow r = new PriceRow();
            r.pricebookId = pb.Id;
            r.isStandard = pb.IsStandard;
            r.pricebookName = pb.Name;
            r.pricelistType = pb.Pricelist_Type__c;
            r.validFrom = pb.Valid_From__c;
            r.validTo = pb.Valid_To__c;
            r.price = current.get(pb.Id);
            d.prices.add(r);
        }
        return d;
    }

    /**
     * Saves the Free product flag and the prices. V01-03: a free product can only be saved at price 0, a paid product at 0 is saved and flagged.
     * An empty price removes the entry of a non-standard price book. All or nothing.
     */
    @AuraEnabled
    public static void savePrices(Id productId, Boolean makeFree, String pricesJson) {
        List<PriceInput> inputs = (List<PriceInput>) JSON.deserialize(pricesJson, List<PriceInput>.class);
        Product2 product = [SELECT Id, Is_Free__c FROM Product2 WHERE Id = :productId];
        Boolean isFree = makeFree == true;
        product.Is_Free__c = isFree;
        Id standardId = standardPricebookId();

        Map<Id, Decimal> wanted = new Map<Id, Decimal>();
        Set<Id> cleared = new Set<Id>();
        for (PriceInput pi : inputs) {
            if (wanted.containsKey(pi.pricebookId) || cleared.contains(pi.pricebookId)) { throw aura(System.Label.YvPricingErrDuplicateBook); }
            if (pi.price != null && pi.price < 0) { throw aura(System.Label.YvPricingErrNegative); }
            // V01-03: a free product can only be saved at price 0.
            if (isFree && pi.price != null && pi.price != 0) { throw aura(System.Label.YvPricingErrFreeNotZero); }
            if (pi.price != null || isFree) {
                wanted.put(pi.pricebookId, pi.price == null ? 0 : pi.price);
            } else {
                cleared.add(pi.pricebookId);
            }
        }

        Map<Id, PricebookEntry> existing = new Map<Id, PricebookEntry>();
        for (PricebookEntry pbe : [SELECT Id, Pricebook2Id, UnitPrice, IsActive FROM PricebookEntry WHERE Product2Id = :productId]) {
            existing.put(pbe.Pricebook2Id, pbe);
        }

        Savepoint sp = Database.setSavepoint();
        try {
            // Salesforce needs the standard price book entry before any other one. If it is left empty, it is created at 0.
            if (!existing.containsKey(standardId) && !wanted.containsKey(standardId) && !wanted.isEmpty()) {
                wanted.put(standardId, 0);
            }
            List<PricebookEntry> toInsert = new List<PricebookEntry>();
            List<PricebookEntry> toUpdate = new List<PricebookEntry>();
            if (wanted.containsKey(standardId) && !existing.containsKey(standardId)) {
                insert new PricebookEntry(Pricebook2Id = standardId, Product2Id = productId, UnitPrice = wanted.get(standardId), IsActive = true);
            }
            for (Id pbId : wanted.keySet()) {
                PricebookEntry e = existing.get(pbId);
                if (e == null) {
                    if (pbId != standardId) {
                        toInsert.add(new PricebookEntry(Pricebook2Id = pbId, Product2Id = productId, UnitPrice = wanted.get(pbId), IsActive = true));
                    }
                } else if (e.UnitPrice != wanted.get(pbId) || !e.IsActive) {
                    e.UnitPrice = wanted.get(pbId);
                    e.IsActive = true;
                    toUpdate.add(e);
                }
            }
            List<PricebookEntry> toDelete = new List<PricebookEntry>();
            for (Id pbId : cleared) {
                if (pbId != standardId && existing.containsKey(pbId)) { toDelete.add(existing.get(pbId)); }
            }
            if (!toDelete.isEmpty()) { delete toDelete; }
            if (!toUpdate.isEmpty()) { update toUpdate; }
            if (!toInsert.isEmpty()) { insert toInsert; }

            // Has_Zero_Price__c: a paid product with at least one entry at 0 (the standard price book is a technical entry, not counted).
            Boolean hasZero = false;
            if (!isFree) {
                hasZero = [SELECT COUNT() FROM PricebookEntry WHERE Product2Id = :productId AND UnitPrice = 0 AND Pricebook2.IsStandard = false] > 0;
            }
            product.Has_Zero_Price__c = hasZero;
            update product;
        } catch (DmlException e) {
            Database.rollback(sp);
            throw aura(e.getDmlMessage(0));
        }
    }

    private static AuraHandledException aura(String msg) {
        AuraHandledException e = new AuraHandledException(msg);
        e.setMessage(msg);
        return e;
    }

    private static Id standardPricebookId() {
        if (Test.isRunningTest()) { return Test.getStandardPricebookId(); }
        return [SELECT Id FROM Pricebook2 WHERE IsStandard = true LIMIT 1].Id;
    }
}