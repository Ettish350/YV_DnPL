# Bundle (`Bundle__c`): questions for Lior

Companion to [02_Bundle.md](02_Bundle.md). That file holds the full field mapping (77 rows) and the per-field notes. This file pulls every open point into one list, one question per row, so you can answer row by row.

**How to answer.**
- Every group has one fixed question and a fixed set of answers (shown at the top of the group).
- Every row has Anna's **Proposal** in the same vocabulary as the answers. Write **OK** to accept it, or write a different answer.
- Anything that is not one of the listed answers: write it in words.
- When you have answered, Anna updates `02_Bundle.md` to match. Only after that are the fields built (Stage B, after approval). Nothing is deployed.

"Proposal" is Anna's working view, not a DD fact. "Why we ask" quotes the DD where it says something. "Not in the DD" means a search of `dd/` found nothing.

Questions in total: 73 (A: 20, B: 27, C: 5, D: 9, E: 12).

---

## A. Where should this field live?

**Question for every row: does this field belong on the Bundle or on the Product?**
**Answers: Bundle / Product / Drop.**

Background: your reply c83 (Doc 1, 03-architecture.md, 23.9.2026) says the settings that affect scheduling, such as the required resource, the service provider type and the activity location, are defined on the product itself, and that expansion questions belong to the order request process. Comments c113 to c116 (Doc 2, process 01) leave the general split open: you asked for the full scenario before deciding which settings sit on the product and which on the Bundle.

"On Product2 today" means the Product2 draft ([01_Product2.md](01_Product2.md)) already has a matching field, so "Product" means moving the value there and not creating a field.

| # | Field | On Product2 today | Proposal | Why we ask | Answer |
|---|---|---|---|---|---|
| A1 | Tour code `TourCode__c` (קוד סיור) | no | Product | Product-level information | Drop (Lior, 19.10.2026) |
| A2 | Time before museum entry `PreMuseumTime__c` | no | Product | Product-level information | Bundle (Lior; pending YV) |
| A3 | Guide mandatory per group `IsGuideMandatoryPerGroup__c` | similar: `IsRequiredServiceProvider__c` | Product | Product-level information | Drop (Lior) |
| A4 | Headset needed `IsNeedHeadset__c` | no | Product | Product-level information | Product |
| A5 | Open for individual registration `IsOpenForIndividualRegistration__c` | no | Product | Product-level information | Drop (Lior) |
| A6 | Group name required `IsGroupNameRequired__c` | no | Product | Product-level information | Drop (Lior): an order-form rule by audience type |
| A7 | Product for order schedule line `ScheduleProduct__c` (lookup to `Product2`, legacy `new_yv_product`) | no | Product | Should sit on the product | Drop (Lior; see reply in xlsx; Neev to confirm) |
| A8 | Product for event schedule line `ScheduleProductEvent__c` (lookup to `Product2`) | no | Product | Same as A7 | Neev to answer (proposal: Drop) |
| A9 | Pricelist type `PricelistType__c` | no | Product | Product-level information. The DD manages pricing in price books (Doc 1 04-entities.md) and leaves Bundle pricing open (see E9) | Drop |
| A10 | Resource `Resource__c` (lookup to `B25__Resource__c`) | yes, `Resource__c` | Product | Your c83: the required resource is defined on the product | Product (Neev to confirm) |
| A11 | Activity location `ActivityDayLocation__c` (lookup to `YV_Location__c`) | yes, `Activity_Location__c` | Product | Your c83: the activity location is defined on the product | Product (Neev to confirm) |
| A12 | Resource duration (hours) `DurationOfHoursResource__c` | no (`DurationOfHours__c` exists) | none | The DD defines a default duration on the product ("משך זמן ברירת מחדל למוצר"). c113: the activity duration and the time the resource is held can differ per bundle for the same product | Neev to answer (proposal: Drop from Bundle) (pending YV) |
| A13 | Minimum business days to order `MinimumDays__c` | yes, same name | none | | Drop |
| A14 | Minimum participants per order `MinParticipants__c` | yes, `MinNumberOfParticipants__c` | none | | Drop |
| A15 | Maximum participants per order `MaxParticipants__c` | yes, `MaxNumberOfParticipants__c` | none | | Drop |
| A16 | Maximum visitors per group `GroupRegistrationMaxAmount__c` (merged, see C5) | no | none | | Product |
| A17 | Maximum groups per order `MaxGroups__c` | no | none | | Product |
| A18 | Maximum orders per day `MaxReservationsPerDay__c` | similar: `MaxProductsPerDay__c` (products, not orders) | none | | Product |
| A19 | Allow group registration `IsAllowGroupRegistration__c` | no | none | | Drop |
| A20 | Price descriptions `PriceDescriptionHe__c`, `PriceDescriptionEn__c`, `PriceDescriptionKioskHe__c`, `PriceDescriptionKioskEn__c` (4 fields, one answer for all) | no | none | | Drop |

---

## B. Does this field need to exist on the Bundle?

**Question for every row: keep this field on the Bundle, or drop it (not migrated)?**
**Answers: Keep / Drop.**

| # | Field | Proposal | Why we ask | Answer |
|---|---|---|---|---|
| B1 | Closed for registration `IsClosedForRegistration__c` | Drop | Not in the DD. The Bundle already has `Active_Bundle__c` | Drop |
| B2 | Additional order questions needed `NeedsMoreQuestions__c` | Drop | Your c83: expansion questions belong to the order request process, not the Bundle | Drop (Lior) |
| B3 | Additional questions type `MoreQuestionsAbout__c` | Drop | Depends on B2 | Drop |
| B4 | Suitable age `Age__c` (מתאים לגיל) | Drop | `AgeGroup__c` (שכבת גיל) already exists | Drop |
| B5 | Tag `Tag__c` | Drop | Not defined for the Bundle in the DD. The legacy workbook also has a many-to-many table `new_tour_type` to `new_tag` (see E12) | Drop |
| B6 | Sort weight, Hebrew and English `SortWeightHe__c`, `SortWeightEn__c` (one answer for both) | Drop | Not in the DD. Product2 already has one `SortWeight__c`. Only related text: c139 asks for control over display order in the price book | Drop |
| B7 | Kiosk sort weight `KioskSortWeight__c` | none | Kiosk appears in the DD only as a display channel of the product (Doc 2, process 01) | Drop |
| B8 | Total discount `Discount__c` | Drop | Discounts are entered at order level or order-line level, not on the Bundle (Doc 2 process 08: V08-01, A08-02) | Drop |
| B9 | Discount type, online system `DiscountType__c` | Drop | Same as B8 | Drop |
| B10 | Enable hours by default `EnableHoursByDefault__c` | none | Not in the DD. Regulation is defined in configuration records (dimension, audience, quota, dates, weekdays, hours window, location; default window 09:30 to 13:30; Doc 2 process 12) | Drop (Neev to confirm) |
| B11 | Opening hours by museum `OpeningHoursByMuseum__c` | none | Same as B10 | Drop (see reply in xlsx; Neev to confirm) |
| B12 | Museum hours regulation needed `IsNeedHoursRegulation__c` | none | Same as B10 | Drop |
| B13 | Guide hours regulation needed `IsNeedGuideHoursRegulation__c` | none | Same as B10 | Drop |
| B14 | Event (unified system) `EventReservations__c` | Drop | The relationship runs the other way: `Event__c` links to the Bundle (Doc 1 04-entities.md, "אירועים והרשמה"), so the lookup belongs on `Event__c` | Drop (Neev to confirm) |
| B15 | Event needed `IsNeedEvent__c` | Drop | Not in the DD. Follows B14 | Drop |
| B16 | Budget framework `BudgetFramework__c` (lookup) | none | The DD links the budget framework to the order and the event, not the Bundle (Doc 1 04-entities.md) | Drop (Neev to confirm) |
| B17 | Department `Department__c` (lookup) | none | The DD links the department to the order and the budget framework (Doc 1 04-entities.md; Doc 2 c47) | Drop (see reply in xlsx; Neev to confirm) |
| B18 | Product for ticket types display `DisplayTicketsProduct__c` (lookup) | none | In the DD, ticket types are price book entries by audience type (Doc 1 04-entities.md, c135 and c139), not a lookup from the Bundle | Drop (Neev to confirm) |
| B19 | Additional order characterization `AdditionalCharacterization__c` (אפיון נוסף להזמנה, 47 values: army units, programs, webinars) | none | Not in the DD. Doc 2 appendix reports list report 500.1.2 "by budget framework including order characterization, age group and language" (11-appendix-reports.md), which suggests an order-level field | Neev to answer (proposal: Drop from Bundle) |
| B20 | Promotion `IsPromotion__c` | none | Not in the DD | Drop |
| B21 | Show in calendar `IsForCalendarView__c` | none | Still relevant in the new system? | Drop |
| B22 | Text direction `TextDirection__c` (LTR/RTL) | none | Not in the DD | Drop |
| B23 | Display age and country `IsToDisplayAgeAndCountry__c` | none | Not in the DD. The API name says "age and country", the label only "country" | Drop |
| B24 | Show comments in online form `ShowCommentsOnWebsite__c` | none | Not in the DD | Drop |
| B25 | More details link, Hebrew and English `LinkToMoreDetailsHe__c`, `LinkToMoreDetailsEn__c` (one answer for both) | none | Still relevant in the new system? | Drop |
| B26 | Activity type for Tahel `TourTypeTahel__c` (self-lookup to `Bundle__c`) | none | Purpose not explained in the DD | Drop (see reply in xlsx; Neev to confirm) |
| B27 | Almog billing interface `AlmogInterface__c` (lookup) | none | Same stub lookup as Product2 (target `Almog_Interface__c`, not in `yv-partial`). Does it apply to the Bundle? | Drop (Neev to confirm) |

---

## C. Merges

**Question for every row: approve this merge of legacy fields into one Salesforce field?**
**Answers: Yes / No.** If No, write what to do instead (for example "keep separate checkboxes").

| # | Merge | Proposal | Why we ask | Answer |
|---|---|---|---|---|
| C1 | Legacy `new_category` (5 values) into the existing `Bundle_Type__c` (4 values) = 9 values: בר מצווה, חבילת ביקור, כנס, השתלמות, סיור מודרך, ביקור במוזיאון, חיזיון אורקולי, תיאטרון לגעת בזיכרון, אחר | Yes | The first four match the four Bundle record types in Doc 1 05-configurations.md. The DD says "the activity type is a field on the Bundle" without naming it (Doc 2, c185). See also E5 | No (see reply in xlsx; Neev to confirm) **Final (Anna, 11.11.2026): No merge. The legacy `new_category` is not loaded; `Bundle_Type__c` keeps its four values. Neev to confirm.** |
| C2 | Six weekday checkboxes into `Relevant_Days__c` (multi-select, ראשון to שישי) | Yes | Same pattern as Product2 `Relevant_Days__c` | Yes |
| C3 | Three show-on checkboxes (תהל, אתר, קיוסק) into `Display_Channels__c` (פנימי, מקוון, קיוסק) | Yes | The DD names internal and online channels for the Bundle and lists kiosk for the Product. Does kiosk apply to Bundles? | Yes (pending YV) |
| C4 | Four audience checkboxes (מוסדות, סוכנים, כוחות הבטחון, פרטיים) into `Available_For__c` | Yes | The values are the legacy field labels, not DD values. Overlaps with Target audience `TargetPopulation__c` | Yes, into one field |
| C5 | `new_group_registration_max_amount` and `new_max_participants_in_group` into `GroupRegistrationMaxAmount__c` | Yes | Two legacy fields with the same meaning | Yes |

---

## D. Assumptions to confirm

**Question for every row: is the assumption right?**
**Answers: OK, or write the correct value.**

| # | Item | Assumed | Answer |
|---|---|---|---|
| D1 | Length of `Bundle__c.Name` (Text 80) | No legacy name is longer than 80 characters | YES (Anna, 15.10.2026) |
| D2 | Is Tour code `TourCode__c` Unique? (only if it stays on the Bundle, see A1) | Not Unique (not stated in the DD) | OK |
| D3 | Is Legacy System ID `Legacy_System_Id__c` Unique? | Unique | YES (Anna, 15.10.2026) |
| D4 | Does the existing required checkbox `Active_Bundle__c` receive the legacy `statecode` / `statuscode`? | Yes: Active = checked | OK |
| D5 | Unit of `DifferenceBetweenArrivalToStart__c` (הפרש בין הגעה לכניסה) | Hours (legacy decimal) | OK |
| D6 | API names of the objects the lookups point to (only if B16 or B17 is kept) | `BudgetFramework__c` (given) and `Department__c` (assumed; the DD gives only the label מחלקה) | OK, not needed |
| D7 | Text lengths (the workbook gives none) | Text(255) for nvarchar, Long Text Area(32000) for ntext | YES (Anna, 15.10.2026) |
| D8 | Type of `LinkToMoreDetailsHe__c` and `LinkToMoreDetailsEn__c` (only if B25 is kept) | URL(255) | YES (Anna, 15.10.2026) |
| D9 | Meaning of "Watcher" in `NoteWatcherHe__c` and `NoteWatcherEn__c` (הערה לוואצ'ר) | Voucher | YES (Anna, 15.10.2026) |

---

## E. Model questions

**Question for every row: choose one of the listed options.**
**Answers: the option letter or text.** Where an option does not fit, write it in words.

| # | Question | Options | Proposal | Why we ask | Answer |
|---|---|---|---|---|---|
| E1 | Can one activity day hold more than one bundle? | Yes / No | none | The DD says an order can have several activity days, each with its own quote (Doc 1 03-architecture.md). Legacy `new_activity_day` has its own lookup to `new_tour_type` | Yes, no limit in the model (Neev to confirm) |
| E2 | After an order is placed, must the chosen bundle be kept? At which level? | None / Order / Day / Line | none | A bundle breaks down into order lines (Doc 1 04-entities.md, Doc 2 A01-02 and A01-03), and the DD has no field recording which bundle a line came from | Line (Neev to confirm); Superseded (Lior, 21.10.2026): the order keeps the bundle in the existing `Connected_Bundle__c`; its items are the Bundle's child `Bundle_Item__c` records, and `Connected_Bundle_Item__c` is not needed (PR #27 C11, C12; Bundle Item D3). |
| E3 | If kept (E2): how? | (a) junction object between the order and `Bundle__c` (working name `Connected Bundle`) / (b) lookup to `Bundle__c` on the activity day (legacy `new_activity_day_type`) / (c) lookup to `Bundle__c` on the order line / (d) a combination | none | Full text in `02_Bundle.md`, Open Question 18 | (c) lookup to Bundle__c on the order line (Neev to confirm); Superseded (Lior, 21.10.2026): the order keeps the bundle in the existing `Connected_Bundle__c`; its items are the Bundle's child `Bundle_Item__c` records, and `Connected_Bundle_Item__c` is not needed (PR #27 C11, C12; Bundle Item D3). |
| E4 | Hebrew labels: legacy wording or "חבילה"? | Keep "פעילות" (שם פעילות, משך פעילות, ...) / Switch to "חבילה" (שם חבילה, ...) | Switch | The DD says "אין להשוות חבילה לסוג פעילות" (Doc 2, c185). No API names or types change. Full text: Open Question 19 | Switch |
| E5 | `Bundle_Type__c`: how is the type modeled? | Picklist / Record types / Both | none | The DD lists four Bundle record types (Doc 1 05-configurations.md): כנס, השתלמות, חבילת ביקור, בר מצווה | Record types (see reply in xlsx; Neev to confirm) **Final (Anna, 11.11.2026): Record types for the four DD types; `Bundle_Type__c` stays with its four values. Load rule: every migrated bundle gets the record type חבילת ביקור. Neev to confirm.** **`Bundle_Type__c` stays with its four values (Anna, 11.11.2026); this replaces "Bundle_Type__c can go" in the xlsx reply.** |
| E6 | If record types are used for the four types: do the five legacy category values (see C1) stay in the picklist? | Yes / No | none | | Keep as a separate picklist only if the portal still filters by it; otherwise drop (Neev to confirm; pending YV) **Final (Anna, 11.11.2026): No. The five legacy categories are not kept as a picklist. A filter field is added later only if a portal task needs it (D-036). Neev to confirm.** |
| E7 | Gefen type `new_gefen_type` (8 values) is not carried over. What happens to it? | Drop / Move to the order | Drop | The DD says the Bundle has no Gefen field: "גפן הוא שדה מידע בלבד" (Doc 2 process 01, c121) | Drop |
| E8 | Legacy fields in the PICKLISTS and sample-data sheets but not in the "שדות" sheet, so not in the table: `new_grade_1` to `new_grade_12` (twelve Yes/No, would merge into one multi-select), `new_category_grounds`, `new_category_individuals`, `new_category_museum`, `new_category_special`, `new_division_router` (picklist), `new_is_allow_multi_times`, `new_is_show_in_mobile`, `statecode`, `statuscode`, `new_min_groups`. Add any? | None / list which | none | If any is added, the source must define it | None |
| E9 | Bundle pricing model | Own price / Discount on the sum of its products / Other | none | The DD leaves it open (Doc 1 04-entities.md, "נושא פתוח: אופן תמחור חבילה"). Affects A9, B8, B9 | Stays open |
| E10 | Publishing status: should the Bundle get a field like Product2 `Product_Status__c` (draft, published)? | Yes / No | none | Products and bundles are published to the online system by a dedicated mark with catalog manager approval (Doc 2 process 01, c176 and c177). The DD names no Bundle field | Yes |
| E11 | Mandatory, optional and no-cost products per Bundle (four many-to-many legacy tables to `new_yv_product`): handled in the Bundle Item file, not here? | Yes / No | Yes | They belong to Bundle Item | Yes (Neev to confirm) |
| E12 | Other many-to-many legacy tables to `new_tour_type`: `new_closed_days_and_activities`, `new_coupon`, `new_day_and_time`, `new_disable_products_daily_activities`, `new_language`, `new_spcific_date_and_hour`, `new_tag`, `new_yv_time`. Which need a Bundle-side field? | None / list which | none | For example a language list like Product2 `Available_Languages__c` | None (Neev to confirm) |

---

## What happens after your answers

1. Anna updates `02_Bundle.md`: rows dropped or moved, merges approved, answers written into the Questions column, counts re-checked by script. Status becomes approved.
2. Fields answered **Product** are added to the Product2 file as new rows (a separate change to `01_Product2.md`).
3. Stage B: the missing fields, the Excel tab and the proposed Lightning Record Page are prepared for approval. Nothing is deployed to the org.

---

## Answers from Lior, 18.10.2026

Answers are in the **Answer** column above. Tags in brackets:
- **Lior**: decided by Lior.
- **Neev to confirm** / **Neev to answer**: bundle architecture and relationship questions, also go to Neev (D-029).
- **pending YV**: needs Yad Vashem; Lior is collecting these in one document.
- **see reply in xlsx**: Lior asked a question on the row; the reply is in column O of `Bundle PR20 - questions and answers - V4 - 18.10.xlsx` (this folder), which also shows for every field its row in the YV fields sheet and what the old system used it for.

Rows without a tag are Lior's acceptance of the proposed answer.

## Answers from Lior, 19.10.2026

- **A1** (tour code): Drop (Lior).

Answers given in a Claude session and recorded here; the V4 xlsx of 18.10 is not updated, this file is the current list.
