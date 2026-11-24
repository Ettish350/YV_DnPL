# Object: Bundle_Item_Entry__c

## Salesforce Object Details

| Property | Value |
|----------|--------|
| Object Label | Bundle Item Entry |
| API Name | Bundle_Item_Entry__c |
| Owner | Tal (built for the portal) |
| Status | Draft |

The price of one bundle item in one price book. Lior, 11.11.2026 (Doc 1 4.2, open topic c139): the bundle price is the sum of its items, and an item's price can be overridden per price book here. Doc 2 processes 01, 02, 08. No legacy entity.

## Field Mapping

| Salesforce Label | Salesforce API Name | Translation | Field Type | Picklist Values | שם השדה במערכת הישנה | Old API Name | Unique | Section | Questions | Task | Status |
|-----------------|--------------------|-------------|------------|------------------|----------------------|--------------|--------|---------|-----------|------|--------|
| Bundle Item Entry Name | Name | מספר מחיר פריט | Auto Number (BIT-{0000}) | | | | | | | Pricing (Lior 11.11.2026) | Exists |
| Bundle Item | Bundle_Item__c | פריט חבילה | Master-Detail(Bundle_Item__c) | | | | | | | Pricing (Lior 11.11.2026) | Exists |
| Price Book | Price_Book__c | מחירון | Lookup(Pricebook2) | | | | | | | Pricing (Lior 11.11.2026) | Exists |
| Price | Price__c | מחיר | Currency(16,2) | | | | | | | Pricing (Lior 11.11.2026) | Exists |
| Unique key | Unique_Key__c | מפתח ייחודי | Text(40) | | | | Yes | | [Implementer] One price per bundle item per price book; filled by an automation later. | Pricing (Lior 11.11.2026) | Built |

The four existing fields were built by Tal and are not changed. Translations of their labels above are descriptive only; nothing is deployed for them.

### Page

The object has no Lightning record page; the default page shows `Bundle Item Entry Layout`. Unique_Key__c is added to that layout, read only (Tal's layout; nothing else changed).

## Relationship Fields

| Field | Target Object | Matching Logic |
|--------|--------------|----------------|
| Bundle_Item__c | Bundle_Item__c | Master-detail (Tal). |
| Price_Book__c | Pricebook2 | Lookup (Tal). |

## Test records

## Automations
