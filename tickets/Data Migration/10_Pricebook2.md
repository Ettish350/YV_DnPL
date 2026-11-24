# Object: Pricebook2

## Salesforce Object Details

| Property | Value |
|----------|--------|
| Object Label | Price Book |
| API Name | Pricebook2 |
| Status | Draft |

Lior, 11.11.2026: one price book per price list type per activity year, with validity dates. The budget framework carries the price book (`Budget_Framework__c.Price_Book__c`). Doc 2 processes 01, 02, 08. Legacy entity: `new_yv_pricelist`.

## Field Mapping

| Salesforce Label | Salesforce API Name | Translation | Field Type | Picklist Values | שם השדה במערכת הישנה | Old API Name | Unique | Section | Questions | Task | Status |
|-----------------|--------------------|-------------|------------|------------------|----------------------|--------------|--------|---------|-----------|------|--------|
| Price Book Name | Name | שם מחירון | Text(255) | | Name | new_name | | | | Pricing (Lior 11.11.2026) | Exists |
| Active | IsActive | פעיל | Checkbox | | | | | | | Pricing (Lior 11.11.2026) | Exists |
| Description | Description | תיאור | Text(255) | | | | | | | Pricing (Lior 11.11.2026) | Exists |
| Pricelist type | Pricelist_Type__c | סוג מחירון | Picklist | קבוצה ישראל;קבוצה חו"ל;כוחות הבטחון;גמלאים;ציבורי;פנימי - יד ושם;פרטי;ג'נסיס;פנימי - הנצחה;ביקורים רשמיים | סוג מחירון | new_pricelist_type | | תמחור | [Implementer] New global value set `Pricelist_Type`, shared with `Bundle__c.PricelistType__c` when it is built. | Pricing (Lior 11.11.2026) | Built |
| Valid from | Valid_From__c | בתוקף מתאריך | Date | | | | | תמחור | [Implementer] Pricebook2 has no standard ValidFrom in `yv-partial`; no legacy field. | Pricing (Lior 11.11.2026) | Built |
| Valid to | Valid_To__c | בתוקף עד תאריך | Date | | תאריך פקיעת מחיר | new_price_end_date | | תמחור | [Implementer] Pricebook2 has no standard ValidTo in `yv-partial`; the legacy field is a datetime, loaded as a date. | Pricing (Lior 11.11.2026) | Built |

### Page

The object has no Lightning record page; the default page shows `Price Book Layout`. A new section **תמחור** is added to that layout with Pricelist_Type__c, Valid_From__c and Valid_To__c.

## Not in this task

Legacy `new_yv_pricelist` also has a location (`new_activity_day_location`) and a supplier (`new_supplier`). Price by location, hour or quantity is an open question to Tal (see `knowledge/model-register.md`); no field is built for it.

## Test records

## Automations
