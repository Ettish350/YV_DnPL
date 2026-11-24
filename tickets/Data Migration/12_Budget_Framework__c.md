# Object: Budget_Framework__c

## Salesforce Object Details

| Property | Value |
|----------|--------|
| Object Label | Budget Framework |
| API Name | Budget_Framework__c |
| Status | Draft |

Built as a D-009 stub during the Opportunity work (PR #27, name only, one budget framework per order). Lior, 11.11.2026: the budget framework carries the price book. `Account.Price_Book__c` (built by Tal for the portal) is an interim; the price book will come from the order's budget framework once Yad Vashem sends the selection rule. Legacy entity: `new_budget_framework`.

## Field Mapping

| Salesforce Label | Salesforce API Name | Translation | Field Type | Picklist Values | שם השדה במערכת הישנה | Old API Name | Unique | Section | Questions | Task | Status |
|-----------------|--------------------|-------------|------------|------------------|----------------------|--------------|--------|---------|-----------|------|--------|
| Budget Framework Name | Name | שם מסגרת תקציבית | Text(80) | | | | | | | Pricing (Lior 11.11.2026) | Exists |
| Department | Department__c | מחלקה | Lookup(Department__c) | | | | | | [Implementer] Built by Tal (29.10.2026); not changed. | Pricing (Lior 11.11.2026) | Exists |
| Discount | Discount__c | הנחה | Percent(14,4) | | | | | | [Implementer] Built by Tal (29.10.2026); not changed. | Pricing (Lior 11.11.2026) | Exists |
| Price Book | Price_Book__c | מחירון | Lookup(Pricebook2) | | סוג מחירון | new_pricelist_type | | | [Implementer] Built by Tal (29.10.2026), the same meaning as asked; reused, not changed. The legacy field is a price list type picklist; the load maps its value and the activity year to the matching price book. | Pricing (Lior 11.11.2026) | Exists |

### Page

The object has no Lightning record page; the default page shows `Budget Framework Layout`, which already holds Price_Book__c, Department__c and Discount__c. Nothing was built for this object.

## Relationship Fields

| Field | Target Object | Matching Logic |
|--------|--------------|----------------|
| Price_Book__c | Pricebook2 | Existing (Tal). Legacy price list type plus activity year to the price book of that type and year. |
| Department__c | Department__c | Existing (Tal). |

## Test records

## Automations
