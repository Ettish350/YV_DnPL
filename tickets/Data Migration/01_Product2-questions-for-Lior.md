# Product (`Product2`): questions for Lior

Companion to [01_Product2.md](01_Product2.md). That file holds the full field mapping (34 rows) and the per-field notes. This file pulls every open point into one list you can answer row by row.

**Status: all questions answered by Lior on 18.10.2026 (pull request #19). The answers are in the "Answers from Lior" section of `01_Product2.md`. Four items wait for Yad Vashem (A2.3, C2, C3, D1) and two for Neev (B1, F1).**

**How to answer.** Write your answer in the **Answer** column of each row (or comment on the row in the PR). Short codes are enough. When you have answered, Anna updates `01_Product2.md` to match, and only then are the fields built (Stage B, after approval).

Codes: **Y** / **N** = yes / no. Anything else: write it in words.

**Proposal** is the working view of the draft, not a DD fact. **Evidence** quotes the source where it says something. "Not in the DD" means a search of `dd/` found nothing.

Questions open: 11 (A: 3, B: 1, C: 3, D: 2, E: 1, F: 1). The numbers are the same as in `01_Product2.md`, so there are gaps: A1, B2, C1, C5 and C6 were answered by Lior and are recorded in the "Answers from Lior" section of that file. C2 keeps only its value list.

---

## A. Merges to approve

One merge is still to approve (A1 is answered). Answer each line Y or N; if N, write what to change.

| # | Question | Evidence | Answer (Y/N) |
|---|---|---|---|
| A2.1 | Merge the three legacy checkboxes `new_show_on_website` (הראה באתר), `new_is_for_front_desk` (האם למבואה?) and `new_display_on_counter` (האם להציג בדלפק) into one multi-select picklist `Display_Channels__c` | The DD names one Product-level concept: "מנהל הקטלוג בוחר את סוג המוצר ואת ערוצי התצוגה (הזנה פנימית, מערכת מקוונת, קיוסק)" (Doc 2, 06-processes/01-products-bundles-templates.md). Same pattern as A1 | Y |
| A2.2 | The picklist values are פנימי;מקוון;קיוסק | Taken from the same DD sentence. Doc 2 names them הזנה פנימית, מערכת מקוונת, קיוסק; the short values are fine | Y |
| A2.3 | Each checkbox maps to one value: `new_show_on_website` to מקוון, `new_is_for_front_desk` to קיוסק, `new_display_on_counter` to פנימי (order corrected by Lior) | Our reading of the labels, not stated in the DD. The legacy online service filters on `new_show_on_website` (`OnlineReservationService.cs`) | `new_show_on_website` to מקוון: Y. The other two: working assumption, Lior is confirming with YV (not a build blocker) |

---

## B. Field that already exists in `yv-partial`

| # | Field | Question | Answer |
|---|---|---|---|
| B1 | Resource `Resource__c` (existing custom lookup to `B25__Resource__c`, a managed package, likely GoMeddo), replacing the planned `ResourceType__c` | The legacy `new_resource_type` is a Picklist of resource categories (מוזיאון;חדר השאלות הגדולות;אולם;כיתה/אולם;קמפוס;אחר;חדר ישיבות;עיר הבה"דים;חזרה לחיים;מיצג;החיים שלפני), in the "שדות" sheet of `מיפוי שדות ורשימות 2 1.xlsx`. The lookup points to an actual resource record, not a category. Which one? (1) The lookup replaces the category: drop the picklist. (2) Keep both: the lookup and a new picklist for the category. (3) Other | (2) Keep both. The DD gives the product a required resource type; `Resource__c` stays for the GoMeddo link. For Neev: picklist or GoMeddo lookup? |

---

## C. New fields that come from the DD (no legacy source)

No legacy field, so there is nothing to migrate. API names are approved; these questions are about values and units.

| # | Field | Question | Evidence | Answer |
|---|---|---|---|---|
| C2 | Available Languages `Available_Languages__c` (multi-select picklist, API name approved) | Give the value list | The DD gives no list of languages: "לכל מוצר יוגדרו שפות זמינות, רלוונטי בעיקר למוצרי הדרכה" | No list in the DD or the legacy file. Lior asks YV; one global value set is planned so it can serve guide languages later |
| C3 | Minimum Notice Time `Minimum_Notice_Time__c` (Number 18,0) | What is the unit? Hours is assumed, not stated | V01-02 requires "זמן התראה מזערי" for a product shown online, separate from the minimum business days already in `MinimumDays__c` | Hours, Number(18,0), working assumption; Lior is confirming the unit with YV. Field stays on Product |
| C4 | Advance Booking Limit `Advance_Booking_Limit__c` (Number 18,0) | What is the unit (days?) | V01-02 requires "מגבלת הזמנה מראש", how far ahead a product can be booked. It is a third requirement next to C3 and `MinimumDays__c` | Days, Number(18,0), default 365 (Doc 2 online chapter: horizon up to 12 months for all audiences) |

---

## D. SKU

| # | Question | Answer |
|---|---|---|
| D1 | `StockKeepingUnit` (standard field, source `new_sku_product`): mark it Unique? Not stated in the DD or the mapping; SKU is a common candidate | N for now. c285 (one Priority SKU per product) makes Unique the target; marked once YV confirm no two products share a SKU |
| D2 | `StockKeepingUnit` is Text(180). Is any legacy SKU value longer than 180 characters? | No. Unlikely; keep Text(180), no check |

---

## E. Stub lookup

| # | Question | Answer |
|---|---|---|
| E1 | `AlmogInterface__c` (lookup to `Almog_Interface__c`, which is not in `yv-partial`) is a stub under D-009 (status `proposed`). The mapping row has confidence `review`. What is the matching logic from the legacy Almog key (`new_almog_interface`) to the stub record? Neither the DD nor the mapping says | Legacy `new_almog_interface` records are found by `new_almog_sub_code` (`BudgetFrameworkService.cs`). Migrate them into the stub with the legacy GUID as external ID plus the sub code; the lookup resolves by the legacy GUID. D-009 accepted with this extension |

---

## F. Model question

| # | Question | Answer |
|---|---|---|
| F1 | The standard `Type` picklist on `Product2` already contains the value `Bundle`. Is the Bundle modelled as a `Type` value on Product2, or only as the separate `Bundle__c` / `Bundle_Item__c` objects, or both? This is the open F1/F2 question in `A1-OBJECT-MODEL.md`. It affects the Bundle file as well | Only `Bundle__c` / `Bundle_Item__c`; do not use `Type = Bundle` (Doc 1 Bundle entity). For Neev to confirm |

---

## What happens after your answers

1. Anna updates `01_Product2.md` (units and values written in, answers added to the "Answers from Lior" list, counts re-checked by script) and sets Status to approved.
2. The mapping file gets the rows it is missing (`product_id__c` for `new_yv_productId`, `StockKeepingUnit` instead of `SkuProduct__c`).
3. Fields marked **P** in the Bundle questions are added to this file as new rows.
4. Stage B: the missing fields, the Excel tab and the proposed Lightning Record Page are prepared for approval. Nothing is deployed to the org.
