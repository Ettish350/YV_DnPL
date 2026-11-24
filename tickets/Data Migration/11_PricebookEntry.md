# Object: PricebookEntry

## Salesforce Object Details

| Property | Value |
|----------|--------|
| Object Label | Price Book Entry |
| API Name | PricebookEntry |
| Status | Draft |

The price of one product in one price book (Lior, 11.11.2026; Doc 2 processes 01, 02, 08). Legacy entity: `new_yv_pricelist_item`.

## Field Mapping

| Salesforce Label | Salesforce API Name | Translation | Field Type | Picklist Values | שם השדה במערכת הישנה | Old API Name | Unique | Section | Questions | Task | Status |
|-----------------|--------------------|-------------|------------|------------------|----------------------|--------------|--------|---------|-----------|------|--------|
| List Price | UnitPrice | מחיר | Currency(16,2) | | | | | | | Pricing (Lior 11.11.2026) | Exists |
| Comment | Comment__c | הערה | Text(255) | | הערה | new_comment | | תמחור | | Pricing (Lior 11.11.2026) | Built |
| Comment (English) | Comment_En__c | הערה באנגלית | Text(255) | | הערה אנגלית | new_comments_en | | תמחור | | Pricing (Lior 11.11.2026) | Built |

`Unit_Price_ILS__c` (Number(16,2), label "List Price ILS") already exists and is not changed. It is in the org metadata as created by Lior Pridor on 29.03.2026 and last changed by Tal Arviv on 05.11.2026. Its only reference is the active screen flow `Create_And_Update_Order_And_Product` (Case module: Case and Order quick actions), which shows it as the price column and copies it to `OrderItem.Unit_Price_ILS__c`. No Apex, LWC, Aura, layout, record page or report (187 checked) references it. Prices stay in the standard Currency field `UnitPrice` (Lior, 11.11.2026: single currency, nothing hard-codes the shekel sign).

### Page

The object has no Lightning record page; the default page shows `Price Book Entry` layout. A new section **תמחור** is added to that layout with Comment__c and Comment_En__c.

## Test records

## Automations
