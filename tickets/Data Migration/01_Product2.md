# Object: Product2

## Salesforce Object Details

| Property | Value |
|----------|--------|
| Object Label | Product |
| API Name | Product2 |
| Status | Approved |


## Field Mapping

Mandatory columns per `Claude_Salesforce_Migration_Guidelines.md` (1-6), plus four added on request: **Old API Name** (the legacy system's field name, for Data Loader source-to-target mapping), **Unique** (whether the field should carry a Unique constraint), **Section** (proposed Lightning Page section for this field, see the grouping note below the table), and **Questions** (open questions for Lior, per field). Unique is left blank where neither the DD nor the mapping file states a requirement; do not infer one.

| Salesforce Label | Salesforce API Name | Translation | Field Type | Picklist Values | שם השדה במערכת הישנה | Old API Name | Unique | Section | Questions | Task | Status |
|-----------------|--------------------|-------------|------------|------------------|----------------------|--------------|--------|---------|-----------|------|--------|
| Almog billing interface | AlmogInterface__c | ממשק לאלמוג | Lookup | | ממשק לאלמוג | new_almog_interface | | System Information | | Product2 Stage A | Not built: open question |
| Description | Description | תיאור מוצר בעברית | Long Text Area(4000) | | תיאור מוצר בעברית | new_description_he | | General Info | | Product2 Stage A | Built |
| Product description (English) | DescriptionEn__c | תיאור מוצר באנגלית | Long Text Area(32000) | | תיאור מוצר באנגלית | new_description_en | | General Info | | Product2 Stage A | Built |
| Activity duration (hours) | DurationOfHours__c | משך פעילות (בשעות) | Number(18,2) | | משך פעילות (בשעות) | new_duration_of_hours | | Scheduling & Availability | | Product2 Stage A | Built |
| MOE discount allowed (online orders) | IsPossibleDiscountFromMoe__c | הזמנות מקוונות באינטרנט - אפשר הנחת משרד החינוך | Checkbox | | הזמנות מקוונות באינטרנט - אפשר הנחת משרד החינוך | new_is_possible_discount_from_moe | | General Info | | Product2 Stage A | Built |
| Required for itinerary | IsRequiredForItinerary__c | האם נדרש ללו"ז | Checkbox | | האם נדרש ללו"ז | new_is_required_for_itinerary | | Scheduling & Availability | | Product2 Stage A | Built |
| Resource required | IsRequiredResource__c | האם נדרש משאב | Checkbox | | האם נדרש משאב | new_is_required_resource | | Scheduling & Availability | | Product2 Stage A | Built |
| Service provider required | IsRequiredServiceProvider__c | האם נדרש נותן שירות | Checkbox | | האם נדרש נותן שירות | new_is_required_service_provider | | Scheduling & Availability | | Product2 Stage A | Built |
| Taxi required | IsRequiredTaxi__c | האם נדרש מונית | Checkbox | | האם נדרש מונית | new_is_required_taxi | | Scheduling & Availability | | Product2 Stage A | Built |
| Maximum participants | MaxNumberOfParticipants__c | מספר משתתפים מקסימלי | Number(18,0) | | מספר משתתפים מקסימלי | new_max_number_of_participants | | Scheduling & Availability | | Product2 Stage A | Built |
| Maximum items per day | MaxProductsPerDay__c | מקסימום פריטים ליום | Number(18,0) | | מקסימום פריטים ליום | new_max_products_per_day | | Scheduling & Availability | | Product2 Stage A | Built |
| Minimum participants | MinNumberOfParticipants__c | מספר משתתפים מינימלי | Number(18,0) | | מספר משתתפים מינימלי | new_min_number_of_participants | | Scheduling & Availability | | Product2 Stage A | Built |
| Minimum business days to order | MinimumDays__c | מינימום ימי עסקים להזמנה | Number(18,0) | | מינימום ימי עסקים להזמנה | new_minimum_days | | Scheduling & Availability | | Product2 Stage A | Built |
| Product name (Hebrew) | Name | שם המוצר בעברית | Text(255) | | שם המוצר בעברית | new_name | | General Info | | Product2 Stage A | Built |
| Product name (English) | NameEn__c | שם המוצר באנגלית | Text(255) | | שם המוצר באנגלית | new_name_en | | General Info | | Product2 Stage A | Built |
| Short name (English) | PaymentTitleEn__c | שם מקוצר באנגלית | Text(255) | | שם מקוצר באנגלית | new_payment_title_en | | General Info | | Product2 Stage A | Built |
| Short name (Hebrew) | PaymentTitleHe__c | שם מקוצר בעברית | Text(255) | | שם מקוצר בעברית | new_payment_title_he | | General Info | | Product2 Stage A | Built |
| Pricing method | PricingMethod__c | צורת תמחור | Picklist | ללא הכפלה;לפי קבוצה;לפי אדם | צורת תמחור | new_pricing_method | | General Info | | Product2 Stage A | Built |
| Product Catalog Type | Product_Catalog_Type__c [EXISTING CUSTOM] | סיווג מוצר | Picklist | הדרכה;הסעה;כיבוד;פעילויות נוספות;הסעה ניידות;הסעה סמינר;אוזניות;אודיו גייד;הצגה;סיור;חדר;טיסה;מלון;ארוחה | סיווג מוצר | new_product_classification | | General Info | Reuses the existing custom field `Product_Catalog_Type__c` instead of a new field, per `CONVENTIONS.md`. Picklist Values merges the DD-required set with the field's existing org values, de-duplicated. The field originally planned for this legacy field, before the org check found the existing field, was `ProductClassification__c`. | Product2 Stage A | Built |
| Relevant days | Relevant_Days__c | ימים רלוונטיים | Multi-Select Picklist | ראשון;שני;שלישי;רביעי;חמישי;שישי | יום ראשון רלוונטי;יום שני רלוונטי;יום שלישי רלוונטי;יום רביעי רלוונטי;יום חמישי רלוונטי;יום שישי רלוונטי | new_relevant_day_sunday;new_relevant_day_monday;new_relevant_day_tuesday;new_relevant_day_wednesday;new_relevant_day_thursday;new_relevant_day_friday | | Scheduling & Availability | | Product2 Stage A | Built |
| Resource | Resource__c [EXISTING CUSTOM] | | Lookup (to `B25__Resource__c`) | | | | | Scheduling & Availability | Per Lior (B1): stays for the GoMeddo link of a schedulable component (Doc 1 Product entity). The legacy category goes to the new row `ResourceType__c` below. Bundle decision (Anna, 11.11.2026, Bundle questions A10): the Bundle does not hold the resource; it is defined on the Product (Doc 1 03-architecture.md, c83). Neev to confirm. | Product2 Stage A | Built |
| Required resource type | ResourceType__c | סוג משאב | Picklist | מוזיאון;חדר השאלות הגדולות;אולם;כיתה/אולם;קמפוס;אחר;חדר ישיבות;עיר הבה"דים;חזרה לחיים;מיצג;החיים שלפני | סוג משאב | new_resource_type | | Scheduling & Availability | Per Lior (B1): the DD gives the product a required resource type ("סוג משאב נדרש"), so the category stays. Open for Neev (D-029): picklist, or a lookup to GoMeddo's own resource type? | Product2 Stage A | Built |
| Service provider type | ServiceProviderType__c | סוג נותן שירות | Picklist | מדריך;מרצה;עדות;עובד;בת שרות;מדריך פולין;חזן;שחקן;מורה דרך;רכז;צלם;מדריך עיר הבה"דים;מדריך מגמת הדרכה;מדריך הכשרת מורים;מדריכי ה-25;עדויות - צלם;עדויות - מראיין | סוג נותן שירות | new_service_provider_type | | Scheduling & Availability | | Product2 Stage A | Built |
| Display Channels | Display_Channels__c | ערוצי תצוגה | Multi-Select Picklist | פנימי;מקוון;קיוסק | | | | Display & Publishing | Mapping of `new_is_for_front_desk` (to קיוסק) and `new_display_on_counter` (to פנימי) is a working assumption; Lior is confirming it with YV. Not a build blocker. | Product2 Stage A | Built |
| Product SKU | StockKeepingUnit | מק"ט מוצר | Text(180) | | מק"ט מוצר | new_sku_product | | General Info | Deviates from approved mapping row 31 (was custom `SkuProduct__c`); please update the mapping file. Unique is the target (c285, one Priority SKU per product) but not marked until YV confirm no two products share a SKU. | Product2 Stage A | Built |
| Sort weight | SortWeight__c | משקל למיון | Number(18,0) | | משקל למיון | new_sort_weight | | Display & Publishing | | Product2 Stage A | Built |
| Subject type | SubjectType__c | סוג נושא | Picklist | ללא;רשימה;טקסט חופשי | סוג נושא | new_subject_type | | General Info | | Product2 Stage A | Built |
| product id | product_id__c [EXISTING CUSTOM] | מזהה מערכת ישנה | Text(1300) | | מזהה מערכת ישנה | new_yv_productId | | System Information | | Product2 Stage A | Built |
| Activity Location | Activity_Location__c | מקום פעילות | Lookup (to `YV_Location__c`) | | | | | Scheduling & Availability | Bundle decision (Anna, 11.11.2026, Bundle questions A11): the Bundle does not hold the activity location (`ActivityDayLocation__c` is not built on the Bundle); it is defined on the Product (Doc 1 03-architecture.md, c83). Neev to confirm. | Product2 Stage A | Built |
| Available Languages | Available_Languages__c | שפות זמינות | Multi-Select Picklist | | | | | General Info | Value list pending: Lior is asking YV. One global value set is planned, to serve guide languages later. | Product2 Stage A | Not built: open question |
| Minimum Notice Time | Minimum_Notice_Time__c | זמן התראה מזערי | Number(18,0) | | | | | Scheduling & Availability | Unit hours is a working assumption; Lior is confirming it with YV. Model register note (Neev): Doc 1 c279 sets notice time per activity type, Doc 2 online chapter says per audience. | Product2 Stage A | Built |
| Advance Booking Limit | Advance_Booking_Limit__c | מגבלת הזמנה מראש | Number(18,0) | | | | | Scheduling & Availability | Per Lior (C4): unit is days, default 365 (Doc 2 online chapter: the online horizon is up to 12 months for all audiences). | Product2 Stage A | Built |
| Status | Product_Status__c | סטטוס | Picklist | טיוטה;מפורסם | | | | Display & Publishing | | Product2 Stage A | Built |
| Recurring Product | Is_Recurring_Product__c | מוצר חוזר או חד פעמי | Checkbox | | | | | General Info | | Product2 Stage A | Built |
| Headset needed | IsNeedHeadset__c | האם דרוש אוזניות | Checkbox | | האם דרוש אוזניות | new_is_need_headset | | Scheduling & Availability | Lior, Bundle questions A4: Product. Moved here from the Bundle. | Bundle questions (YV20-196) | Built |
| Maximum visitors per group | GroupRegistrationMaxAmount__c | מספר מבקרים מקסימלי לקבוצה | Number(18,0) | | מספר מבקרים מקסימלי לקבוצה;מקסימום נרשמים בקבוצה | new_group_registration_max_amount;new_max_participants_in_group | | Scheduling & Availability | Lior, Bundle questions A16 and C5: Product; merges two legacy fields with the same meaning. Moved here from the Bundle. | Bundle questions (YV20-196) | Built |
| Maximum groups per order | MaxGroups__c | מקסימום קבוצות להזמנה | Number(18,0) | | מקסימום קבוצות להזמנה | new_max_groups | | Scheduling & Availability | Lior, Bundle questions A17: Product. Moved here from the Bundle. | Bundle questions (YV20-196) | Built |
| Maximum orders per day | MaxReservationsPerDay__c | מקסימום הזמנות ליום | Number(18,0) | | מקסימום הזמנות ליום | new_max_reservations_per_day | | Scheduling & Availability | Lior, Bundle questions A18: Product. Moved here from the Bundle. Product2 has `MaxProductsPerDay__c` (products, not orders). | Bundle questions (YV20-196) | Built |
| Free product | Is_Free__c | מוצר ללא תשלום | Checkbox | | | | | General Info | [Implementer] From DD V01-03; no legacy field. | Pricing (Lior 11.11.2026) | Built |
| Paid product priced at zero | Has_Zero_Price__c | מוצר בתשלום שתומחר באפס | Checkbox | | | | | General Info | [Implementer] From DD V01-03; read-only on the page, set by an automation later. | Pricing (Lior 11.11.2026) | Built |
| Registration fee product | Is_Registration_Fee__c | מוצר דמי הרשמה | Checkbox | | | | | General Info | [Implementer] From DD A02-05, A08-04, A08-05; no legacy field. | Pricing (Lior 11.11.2026) | Built |

### Proposed Lightning Page section grouping

Three new sections, plus the existing standard "System Information" section (not a new section; already present on the page):

- **General Info**: Name, NameEn__c, Description, DescriptionEn__c, PaymentTitleHe__c, PaymentTitleEn__c, StockKeepingUnit, Available_Languages__c, PricingMethod__c, Is_Free__c, Has_Zero_Price__c, Is_Registration_Fee__c, IsPossibleDiscountFromMoe__c, Product_Catalog_Type__c, SubjectType__c, Is_Recurring_Product__c (the page has no pricing section; the three pricing flags sit next to PricingMethod__c, added 11.11.2026)
- **Scheduling & Availability**: DurationOfHours__c, IsRequiredForItinerary__c, IsRequiredResource__c, IsRequiredServiceProvider__c, IsRequiredTaxi__c, ServiceProviderType__c, Resource__c, ResourceType__c, Relevant_Days__c, Activity_Location__c, MaxNumberOfParticipants__c, MinNumberOfParticipants__c, MaxProductsPerDay__c, MinimumDays__c, Minimum_Notice_Time__c, Advance_Booking_Limit__c IsNeedHeadset__c, GroupRegistrationMaxAmount__c, MaxGroups__c, MaxReservationsPerDay__c (added 11.11.2026, built in yv-partial 12.11.2026, YV20-221)
- **Display & Publishing**: Display_Channels__c, SortWeight__c, Product_Status__c
- **System Information** (existing standard section, not new): AlmogInterface__c, product_id__c

This grouping is a proposal for review, not a decision; the DD does not specify a Lightning Page layout. Reorder, rename, or split sections as needed before building the page.

## Relationship Fields

| Field | Target Object | Matching Logic |
|--------|--------------|----------------|
| AlmogInterface__c | Almog_Interface__c (stub, outside pilot) | Per Lior (E1): legacy `new_almog_interface` records are found by `new_almog_sub_code` (`BudgetFrameworkService.cs`). Migrate them into the stub with the legacy GUID as an external ID plus the sub code; `Product2.AlmogInterface__c` resolves by the legacy GUID. D-009 accepted with this extension. |
| Activity_Location__c | YV_Location__c (already exists in `yv-partial`) | Required by the DD (doc2 06-processes/01-products-bundles-templates.md entities table; doc2 04-entities.md defines `YV Location`). No legacy field or matching key exists for this lookup; the DD does not specify matching logic for a data load, only that the relationship is required going forward. |

## Open Questions

Per-field questions are in the **Questions** column of the Field Mapping table and in [01_Product2-questions-for-Lior.md](01_Product2-questions-for-Lior.md). Lior answered on 18.10.2026 (see Answers from Lior). Still pending, none blocks the build of other fields: A2.3 mapping, C2 value list, C3 unit and D1 Unique (all four waiting for YV), B1 picklist or lookup (Neev), F1 confirmation (Neev). Cross-field items:

- **Type field and Bundle**: answered (F1). The Bundle is modelled only by `Bundle__c` / `Bundle_Item__c`; `Type = Bundle` on Product2 is not used. Neev to confirm.
- **D-009 (stubs for lookup targets outside the pilot)**: accepted by Lior on 18.10.2026 with an extension: the stub holds the legacy ID and the sub code, not a name only.

## Answers from Lior

First round, given in the session on 2026-10-15. Numbers match [01_Product2-questions-for-Lior.md](01_Product2-questions-for-Lior.md).

- **A1**: Yes. `Relevant_Days__c` merges the six weekday checkboxes, values ראשון;שני;שלישי;רביעי;חמישי;שישי.
- **B2**: Yes, `product_id__c` already serves as the legacy ID. The field is reused (row `product_id__c [EXISTING CUSTOM]`) and `Legacy_System_Id__c` is dropped from this draft. Unique is left blank: the existing field is not Unique and no answer asked for it.
- **C1**: Yes. API name `Activity_Location__c` approved.
- **C2**: Yes. API name `Available_Languages__c` approved. The value list was not given and stays open.
- **C5**: Yes. `Product_Status__c` approved as a separate field next to `IsActive` (as proposed).
- **C6**: Yes. API name `Is_Recurring_Product__c` approved.

Second round, 18.10.2026, pull request #19 (Lior's comment):

- **A2.1** Y. Merge the three checkboxes into `Display_Channels__c`.
- **A2.2** Y. Values פנימי;מקוון;קיוסק.
- **A2.3** `new_show_on_website` to מקוון: Y (legacy `OnlineReservationService.cs` filters on it). `new_is_for_front_desk` to קיוסק and `new_display_on_counter` to פנימי: working assumption, being confirmed with YV. The picklist values do not change.
- **B1** (2) Keep both. `Resource__c` stays for the GoMeddo link; the category stays as a new picklist `ResourceType__c`. For Neev: picklist or GoMeddo lookup, and confirm "keep both".
- **C2** No list in the DD or the legacy file. Lior asks YV; one global value set is planned.
- **C3** Hours, Number(18,0), working assumption; being confirmed with YV. The field stays on Product (Doc 2 products chapter, V01-02).
- **C4** Days, Number(18,0), default 365 (Doc 2 online chapter: horizon up to 12 months for all audiences).
- **D1** N for now. Unique is the target (c285); marked once YV confirm that no two products share a SKU.
- **D2** No. A legacy SKU over 180 characters is unlikely; Text(180), no check.
- **E1** See the Relationship Fields table: legacy GUID as external ID plus sub code on the stub.
- **F1** Only `Bundle__c` / `Bundle_Item__c`; `Type = Bundle` is not used on Product2.
- **Legacy_System_Id__c** Not wanted on Product2; `product_id__c` is the legacy ID.

## Test records

Created by hand in `yv-partial` on 11.11.2026, task YV20-194 (sub-task YV20-197), so Tal can test the calendar and Neev the scheduling. Read back with SOQL on the same day. All are active, Standard Price Book, Display_Channels__c as shown.

| Name | Product2 Id | Product_Catalog_Type__c | DurationOfHours__c | PricingMethod__c | IsRequiredServiceProvider__c | ServiceProviderType__c | ResourceType__c | Activity_Location__c | Display_Channels__c | Standard Price Book entry Id | UnitPrice |
|---|---|---|---|---|---|---|---|---|---|---|---|
| בדיקה - הדרכה 3 שעות | 01tWm0000070yhmIAA | סיור | 3 | לפי קבוצה | true | (empty) | (empty) | (empty) | פנימי;מקוון | 01uWm000000kEpZIAU | 1000 |
| בדיקה - סיור במוזיאון | 01tWm0000070ykvIAA | סיור | 1.5 | ללא הכפלה | false | (empty) | מוזיאון | קמפוס יד ושם (a0RWm000001rGeZMAU) | פנימי | 01uWm000000kErBIAU | 600 |
| בדיקה - סדנה | 01tWm0000070yjJIAQ | הדרכה | 1 | ללא הכפלה | false | (empty) | כיתה/אולם | קמפוס יד ושם (a0RWm000001rGeZMAU) | פנימי | 01uWm000000kEnxIAE | 500 |
| בדיקה - הרצאה | 01tWm0000070ydlIAA | הדרכה | 1 | לפי קבוצה | true | מרצה | (empty) | (empty) | פנימי | 01uWm000000kEmLIAU | 600 |

Notes:
- Product2 has no "free component" field. Doc 2 V01-03: a free product is priced at zero. The museum tour and the workshop have a non-zero test price (600 and 500), so they behave as priced products in these tests.
- The first product has Product_Catalog_Type__c = סיור and no service provider type, although a service provider is required. Set only for testing.
