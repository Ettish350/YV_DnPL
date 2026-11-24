# A1: Products, Bundles and Activity Day Templates

**Status:** Draft for review  
**Owner:** Anna  
**Source baseline:** `brief/epic-owners-brief.md`, section 8.4, verified against Doc 2 V9 and `knowledge/A1-binding-rules.md`  
**Total estimated human hours:** 59  
**Note:** This is a working draft. Rows marked with open questions or architect approval must not be built until the relevant decision is approved.

**Field label convention for approval:** Every Salesforce field must have an English API-facing label and a Hebrew translation. The English label is the field label used in metadata, and the Hebrew translation is added through Translation Workbench metadata. The ticket must list both values for every field.

## A1-01: Product2 catalog fields

- **Category:** fields/objects
- **מקור באפיון:** [Doc 2, תהליכים עסקיים › בניית מוצרים, חבילות ותבניות יום פעילות](../../dd/doc2-ordering-processes/06-processes/01-products-bundles-templates.md), headings `תיאור התהליך`, `שלבי התהליך` (steps 1-2), and `ישויות ושדות בתהליך` (row `מוצר (Product)`); supporting entity definition: [Doc 1, ישויות › קטלוג ותמחור › מוצר (Product)](../../dd/doc1-intro-architecture/04-entities.md). Field list and build eligibility come from `mapping/A1-field-mapping.md`, entity `new_yv_product`.
- **Business context:** המוצר הוא פריט הקטלוג ומקור כללי השיבוץ ברמת המוצר. הוא מגדיר את סטטוס נותן השירות הנדרש, המשאב, המיקום, שיטת התמחור, משך הפעילות ומגבלות ההזמנה המקוונת. Product is the catalog item and the source of product-level scheduling rules. It defines the required provider status, resource, location, pricing method, duration and online ordering constraints.
- **What to build:** Create the approved Product2 custom fields from the A1 mapping. Include product names and descriptions, SKU, duration, participant limits, pricing method, product classification, required provider/resource/itinerary flags, resource type, service provider type, display flags and sort weight. Include only fields approved by the architect. Add DD-required fields that are confirmed in the mapping or by an approved decision, including product type, available languages, minimum notice time, lifecycle status and recurring/one-time status. For every field, define an English label and a Hebrew translation, and list both in the ticket.
- **Components:** Planned: `object: Product2`; `fields: Product2 fields from A1 mapping`; `record type/status configuration if approved`
- **Depends on:** Mapping approved; decisions F1-F6 resolved or explicitly scoped; read-only org clash check completed
- **Acceptance criteria:**
  1. Every approved A1 Product2 field exists with the API name, Hebrew label and type from the mapping.
  2. Every Product2 field has an English metadata label and a Hebrew Translation Workbench value listed in the ticket.
  3. No field is created for a row marked `question`, `review`, `drop` or `move to C3` until approved.
  4. The product stores the scheduling attributes needed by B3, without adding a coordinator or owner field.
  5. A product can represent a recurring product without creating a new product for every year.
  6. The Components field is updated with the actual API names after build.
- **QA ידני:**
- **Claude Code prompt:** Create only the approved Product2 metadata for A1 from `mapping/A1-field-mapping.md` and the confirmed Doc 2 requirements. Check the target org for existing fields before creating anything. Do not invent fields, do not modify Cases components or managed package components, and do not deploy to the org. Generate the metadata and show it for approval.
- **Human build hours:** 6
- **Actual hours:**
- **Status:** To Do

## A1-02: Bundle catalog fields

- **Category:** fields/objects
- **מקור באפיון:** [Doc 2, תהליכים עסקיים › בניית מוצרים, חבילות ותבניות יום פעילות](../../dd/doc2-ordering-processes/06-processes/01-products-bundles-templates.md), headings `תיאור התהליך` and `שלבי התהליך` (step 3), and `ישויות ושדות בתהליך` (rows `חבילה (Bundle, Custom)` and `פריט חבילה (Bundle Item, Custom)`). Supporting entity definition: [Doc 1, ישויות › קטלוג ותמחור › חבילה (Bundle, Custom)](../../dd/doc1-intro-architecture/04-entities.md). Field list and scope exclusions come from `mapping/A1-field-mapping.md`, entity `new_tour_type`.
- **Business context:** חבילה היא אוסף מסודר של מוצרים. סוג הפעילות הוא שדה בחבילה, אך החבילה אינה תבנית יום הפעילות. פריטי החבילה קובעים את תוכן ההזמנה והצעת המחיר. A Bundle is an ordered collection of products. The activity type is a field on the Bundle, but the Bundle is not the activity day template. Bundle items determine order and quote content.
- **What to build:** Create the approved Bundle fields from the A1 mapping, including audience, age, category, limits, registration rules, pricing and cancellation settings, operational flags, resource and department lookups, and product relationships. Exclude the 23 display fields proposed for C3 until Lior rules on D-007. Do not place Gefen on the Bundle unless the open conflict is resolved in writing. For every field, define an English label and a Hebrew translation, and list both in the ticket.
- **Components:** Planned: `object: Bundle`; `fields: approved Bundle fields`; `lookups: approved targets and L0 stubs`
- **Depends on:** A1-01; mapping approved; architect decision on F1, F2 and F6; D-007 status confirmed
- **Acceptance criteria:**
  1. Every approved Bundle field exists with the mapped API name, Hebrew label and type.
  2. Every Bundle field has an English metadata label and a Hebrew Translation Workbench value listed in the ticket.
  3. Activity type is represented as a Bundle field and is not treated as a separate synonym for Bundle.
  4. Bundle fields do not describe the activity day template or create a lookup to deferred `new_event_reservations`.
  5. The 23 display fields are either excluded and recorded for C3, or included only after an approved scope decision.
  6. Bundle item dependencies are documented for the later Bundle Item implementation.
- **QA ידני:**
- **Claude Code prompt:** Create only the approved Bundle metadata and fields from the A1 mapping. Preserve the distinction between Bundle, activity type and activity day template. Exclude unresolved review/question rows and the deferred `new_event_reservations` relationship. Do not deploy to the org. Generate the metadata and show it for approval.
- **Human build hours:** 10
- **Actual hours:**
- **Status:** To Do

## A1-03: Supporting catalog objects

- **Category:** fields/objects
- **מקור באפיון:** [Doc 2, ישויות](../../dd/doc2-ordering-processes/04-entities.md), heading `מפת הישויות המשתתפות בתהליכי ההזמנה`, rows `מקום פעילות` and `קופון`; and [Doc 1, ישויות › שיבוץ ומשאבים (GoMeddo) › מקום פעילות (YV Location)](../../dd/doc1-intro-architecture/04-entities.md). Doc 2, [בניית מוצרים, חבילות ותבניות יום פעילות](../../dd/doc2-ordering-processes/06-processes/01-products-bundles-templates.md), heading `שלבי התהליך` (step 2), requires linking a product to a topic list or free text, but does not specify a separate object for that list.
- **Business context:** המיקום והקופון מופיעים כישויות במפת הישויות של תהליכי ההזמנות. דרישת קישור מוצר לנושאים קיימת, אך האפיון אינו קובע שהיא תמומש באמצעות אובייקט נפרד.
- **What to build:** Create only `YV_Location__c` and `Coupon__c` with their approved mapped fields and relationships. Do not create `Product_Subject__c`, `Tag__c` or `Activity_Window__c` because the DD does not explicitly require them as separate entities. Keep product-to-topic-list behavior unresolved until its data model is approved. Do not create the deferred relationship to `new_event_reservations`. For every object and field, define an English label and a Hebrew translation, and list both in the ticket.
- **Components:** Planned: `objects: YV_Location__c, Coupon__c`; `fields: approved mapped fields for these objects`; `lookups: approved relationships only`
- **Depends on:** Approved mappings for `YV_Location__c` and `Coupon__c`; D-008/D-009 scope confirmed; read-only org clash checks completed
- **Acceptance criteria:**
  1. `YV_Location__c` and `Coupon__c` are created with Hebrew labels and descriptions beginning with the ticket key and DD source.
  2. Every created object and field has an English metadata label and a Hebrew Translation Workbench value listed in the ticket.
  3. Each created object contains only its approved mapped fields.
  4. `Product_Subject__c`, `Tag__c` and `Activity_Window__c` are not created by this ticket.
  5. The product-to-topic-list requirement is not silently assigned to a new object before the data model is approved.
  6. No object or field is created for deferred `new_event_reservations`.
- **QA ידני:**
- **Claude Code prompt:** Generate only `YV_Location__c` and `Coupon__c` with their approved mapped fields. Do not create `Product_Subject__c`, `Tag__c` or `Activity_Window__c`. Do not decide how the product-to-topic-list requirement is modeled without approval. Do not deploy to the org. Generate the metadata and show it for approval.
- **Human build hours:** 5
- **Actual hours:**
- **Status:** To Do

## A1-04: Weekday multi-select fields

- **Category:** fields/objects
- **מקור באפיון:** No DD heading explicitly requires consolidating weekday checkboxes. The source of the proposed consolidation is proposed decision D-006 and the weekday rows in `mapping/A1-field-mapping.md` for Product2 and Bundle. The related DD context is [Doc 2, תהליכים עסקיים › בניית מוצרים, חבילות ותבניות יום פעילות](../../dd/doc2-ordering-processes/06-processes/01-products-bundles-templates.md), headings `שלבי התהליך` and `ישויות ושדות בתהליך`; neither specifies this conversion.
- **Business context:** שישה שדות סימון חוזרים של ימי השבוע מוצעים כשדה רב-ערכי אחד בכל אובייקט, כדי לפשט דוחות, נוסחאות זמינות וטעינת נתונים. Six repeated weekday checkboxes are proposed as one multi-select picklist per object to simplify reporting, availability formulas and data loading.
- **What to build:** After architect approval, create `Relevant_Days__c` on Product2 and Bundle with the approved six weekday values. Define an English label and a Hebrew translation for each field, and list both in the ticket. Do not create the six source checkbox fields.
- **Components:** Planned: `fields: Product2.Relevant_Days__c`; `fields: Bundle.Relevant_Days__c`
- **Depends on:** A1-02, A1-03, D-006 approved
- **Acceptance criteria:**
  1. Each approved multi-select field on Product2 and Bundle has exactly the approved weekday values.
  2. Each multi-select field has an English metadata label and a Hebrew Translation Workbench value listed in the ticket.
  3. The six source checkbox fields are not created as parallel Salesforce fields.
  4. A sample load maps each source checkbox combination to the correct selected values.
  5. The final Components field names the actual fields created.
- **QA ידני:**
- **Claude Code prompt:** After D-006 is approved, create the two mapped multi-select picklist fields on Product2 and Bundle with the approved Hebrew values. Do not create the six source checkbox fields. Do not deploy to the org. Generate the metadata and show it for approval.
- **Human build hours:** 3
- **Actual hours:**
- **Status:** To Do

## A1-05: A01-01 dynamic product creation forms

- **Category:** layout
- **מקור באפיון:** [Doc 2, תהליכים עסקיים › בניית מוצרים, חבילות ותבניות יום פעילות](../../dd/doc2-ordering-processes/06-processes/01-products-bundles-templates.md), heading `שלבי התהליך` (steps 1-2), heading `כללי ולידציה` (V01-02), heading `אוטומציות` (A01-01), and `אוטומציות מרכזיות, מפרט מימוש`, implementation specification `תהליך הקמת מוצר בשלבים, מטיוטה לפרסום` (steps 1-3 and acceptance test).
- **Business context:** בעת יצירת מוצר יש להציג רק את השדות הרלוונטיים לסוג המוצר ולערוצי התצוגה שנבחרו, ולאפשר שמירת טיוטה והשלמתה במועד מאוחר יותר. Product creation must show only fields relevant to the selected product type and display channels, while allowing a draft to be saved and completed later.
- **What to build:** Use the dependency matrix below as the visibility baseline. Implement only rows fully specified by the DD; do not infer fields or conditions for rows marked unresolved.

| Controlling field | Selected value | Fields shown as a dependency |
|---|---|---|
| Product type (API name not defined in the DD or current A1 mapping) | Each product-type value | The DD requires type-specific fields but does not enumerate the values or the fields for each value. Unresolved; obtain an approved matrix from Lior. `ProductClassification__c` is mapped as product classification, not as the product-type controller. |
| Display channel (DD concept; API name not confirmed. The mapping has separate source flags `ShowOnWebsite__c`, `DisplayOnCounter__c`, and `IsForFrontDesk__c`, not a confirmed unified channel field) | Internal only (`פנימי`) | Do not show or require online-publication fields. No catalog-manager approval; available for internal order use. The complete internal-only field list is not specified. |
| Display channel (DD concept; API name not confirmed) | Online (`מקוון`) | Online-specific fields include minimum notice time (API name not mapped), minimum business days (`MinimumDays__c` in the mapping), and advance-booking limit (API name not mapped). Online publication fields are not itemized in the DD. |
| Display channel (DD concept; API name not confirmed) | Kiosk (`קיוסק`) | Kiosk-only fields. The DD does not enumerate them or confirm how they map to the separate source flags; unresolved. |
| Price book availability (controlling field/API name not defined for this screen) | No price book | Do not show or require pricing fields, per the implementation specification. The relationship to the universal requirement that every product has a price value, including zero, needs clarification for products without a price book. |
| Price book availability (controlling field/API name not defined for this screen) | Has a price book | Show the relevant pricing fields; the exact field list and conditions are not specified in the DD and require approval. |

- **Questions for Lior:** Please provide/approve this matrix in the exact form `controlling field | selected value | fields shown`, including actual API names where decided. Specify product-type values and their fields; identify kiosk-only and online-publication fields; confirm how combinations of display channels work; identify the price-book controller and pricing fields; and clarify how a product without a price book satisfies the DD requirement that every product has a price value. Also specify the visibility/requiredness conditions for provider status, resource, location, duration, languages, itinerary, group classification and topics.
- **Components:** Planned: `Lightning Page: Product2`; `Dynamic Forms`; `visibility rules`
- **Depends on:** A1-01; confirmed Product2 fields; Lior-approved field visibility and requiredness matrix for product type and display channel
- **Acceptance criteria:**
  1. Selecting product type and display channel changes the visible field groups according to the approved matrix.
  2. A product without a price book does not require pricing fields, while every product still has a price value.
  3. A kiosk product displays only its approved kiosk fields.
  4. An online product requires its approved online fields, including the V01-02 advance-notice settings, before publication.
  5. An internal-only product does not require online publication fields or catalog-manager approval.
  6. A draft can be saved and completed gradually before online publication.
  7. No field-specific visibility or requiredness condition outside the approved matrix is implemented.
  8. Manual QA results are recorded in the `QA ידני` field.
- **QA ידני:**
- **Claude Code prompt:** Configure the Product2 Lightning page and Dynamic Forms using only the confirmed A1 fields and the Doc 2 implementation specification. Do not invent publication rules and do not deploy to the org. Generate the metadata and show it for approval.
- **Human build hours:** 6
- **Actual hours:**
- **Status:** To Do

## A1-06: V01-02 and V01-03 catalog validation rules

- **Category:** validation
- **מקור באפיון:** [Doc 2, תהליכים עסקיים › בניית מוצרים, חבילות ותבניות יום פעילות](../../dd/doc2-ordering-processes/06-processes/01-products-bundles-templates.md), heading `כללי ולידציה`, rows `V01-02` and `V01-03`.
- **Business context:** מוצרים מקוונים זקוקים להגדרות זמן התראה מראש. מוצרים ללא תשלום חייבים להיות מתומחרים באפס, ואילו מוצרים בתשלום שתומחרו בכוונה באפס צריכים להישאר אפשריים עם סימון מתאים. Online products need advance-notice settings. Free products must be priced at zero, while intentionally zero-priced paid products remain possible with an indicator.
- **What to build:** Create `A1_V01_02_Online_Notice` and `A1_V01_03_Zero_Price` using the exact approved conditions and Hebrew error messages from Doc 2. Ensure the rules do not block valid drafts or intentionally zero-priced paid products.
- **Components:** Planned: `validation rules: A1_V01_02_Online_Notice, A1_V01_03_Zero_Price`
- **Depends on:** A1-01, A1-05; final wording of V01-03 approved
- **Acceptance criteria:**
  1. An online product missing the required notice settings cannot be saved or published as specified.
  2. A free product with a non-zero price is rejected.
  3. A paid product intentionally priced at zero is saved and receives the approved indicator.
  4. Error messages match the approved Hebrew wording.
  5. Draft behavior is not accidentally blocked.
- **QA ידני:**
- **Claude Code prompt:** Create the two Product2 validation rules from V01-02 and V01-03 in Doc 2. Use the approved conditions and exact Hebrew messages. Preserve valid drafts and intentionally zero-priced paid products. Do not deploy to the org. Generate the metadata and show it for approval.
- **Human build hours:** 4
- **Actual hours:**
- **Status:** To Do

## A1-07: A01-11 clone product

- **Category:** automation
- **מקור באפיון:** [Doc 2, תהליכים עסקיים › בניית מוצרים, חבילות ותבניות יום פעילות](../../dd/doc2-ordering-processes/06-processes/01-products-bundles-templates.md), heading `אוטומציות` (row `A01-11`); supporting behavior is described under `תיאור התהליך` (catalog duplication paragraph).
- **Business context:** מנהלי קטלוג צריכים ליצור במהירות מוצרים דומים, בלי לבנות מחדש ידנית את תנאי השיבוץ והקישורים. Catalog managers need to create similar products quickly without manually rebuilding scheduling conditions and links.
- **What to build:** Create a screen flow and Product2 quick action that opens a new draft with the approved source conditions and links, including scheduling settings, required resource, price books, display channels and discount rules. Create the new record only on save. Do not copy values that must be unique, such as the product name or SKU.
- **Components:** Planned: `Flow: A1_Product_Clone`; `Quick Action: Product2 clone`
- **Depends on:** A1-01, A1-06; approved clone field list
- **Acceptance criteria:**
  1. Clone opens an editable new draft and does not create a record on button click alone.
  2. Approved conditions and links are copied.
  3. Unique product identity fields are blank or require new values.
  4. Cancelling the flow creates no new product.
  5. The new product remains subject to the same validation and publication rules.
- **QA ידני:**
- **Claude Code prompt:** Build the Product2 clone screen flow and quick action according to A01-11. Use an explicit approved field-copy list and create the new record only on save. Do not deploy to the org. Generate the metadata and show it for approval.
- **Human build hours:** 5
- **Actual hours:**
- **Status:** To Do

## A1-08: A01-12 clone activity type

- **Category:** automation
- **מקור באפיון:** [Doc 2, תהליכים עסקיים › בניית מוצרים, חבילות ותבניות יום פעילות](../../dd/doc2-ordering-processes/06-processes/01-products-bundles-templates.md), heading `אוטומציות` (row `A01-12`); supporting descriptions are under `תיאור התהליך` (catalog duplication paragraph), `שלבי התהליך` (step 3), and `ישויות ושדות בתהליך` (rows `חבילה` and `פריט חבילה`). The DD calls for cloning an activity type, but its target data model is unresolved: the DD says activity type is a field on Bundle and also says Bundle is not the activity type.
- **Business context:** האפיון דורש שכפול של סוג פעילות, כולל הרכב המוצרים וההגדרות שלו. הוא אינו קובע שהרשומה המשוכפלת היא Bundle: סוג הפעילות מתואר כשדה בחבילה, וההכרעה לגבי מיפוי `new_tour_type` ל־Bundle עדיין פתוחה (F1/F2).
- **What to build:** After Lior approves the target model, implement A01-12 on the approved activity-type record. Copy all fields of the source record, required products, paid and free optional products per channel, and the approved schedule-building configuration. Create the new record only when the user saves. Do not assume the source or target is Bundle, or that the activity-day template is Bundle.
- **Components:** Planned after target-model approval: `clone action/flow: approved activity-type object (API name TBD)`; `fields and relationships: approved A01-12 clone list`
- **Depends on:** A1-02, A1-07; Lior approval resolving F1/F2 and identifying the source record/object, relationships and clone field list
- **Acceptance criteria:**
  1. The clone opens an editable draft and creates no record until save.
  2. After the target model is approved, all source-record fields and required/optional paid/free products per channel are copied according to A01-12.
  3. Schedule-building configuration is copied only after its owning entity and relationships are approved.
  4. The source record and related records are unchanged.
  5. The implementation does not equate activity type with Bundle or activity-day template without an approved decision.
- **QA ידני:**
- **Questions for Lior:** Which record/object is the source and target of A01-12 cloning? How does it relate to Bundle, Bundle Items, the activity-day template and its subject? Please resolve F1/F2 and approve the complete field, relationship and schedule-configuration copy list before implementation.
- **Claude Code prompt:** After the A01-12 target model and copy list are approved, build the clone action/flow on that approved object. Copy only the approved fields and relationships, and create the new record only on save. Do not equate activity type with Bundle or the activity-day template. Do not deploy to the org. Generate the metadata and show it for approval.
- **Human build hours:** 5
- **Actual hours:**
- **Status:** To Do

## A1-09: Catalog reference data load

- **Category:** data load
- **מקור באפיון:** [Doc 2, ישויות](../../dd/doc2-ordering-processes/04-entities.md), heading `מפת הישויות המשתתפות בתהליכי ההזמנה`; and [Doc 2, תהליכים עסקיים › בניית מוצרים, חבילות ותבניות יום פעילות](../../dd/doc2-ordering-processes/06-processes/01-products-bundles-templates.md), headings `שלבי התהליך` (steps 1-3) and `ישויות ושדות בתהליך`. The actual source rows and load values come from the YV fields workbook and `mapping/A1-field-mapping.md`, not from a DD heading.
- **Business context:** נדרש קטלוג עובד כדי לבדוק את תהליך ההזמנה ולחשוף שגיאות מיפוי לפני שאפוסים אחרים יהיו תלויים בו. A working catalog is needed to test ordering and to expose mapping errors before the next epics depend on it.
- **What to build:** Prepare and validate load files for products, activity types/Bundles, product subjects and activity locations. Load in dependency order and translate source weekday checkboxes into the approved multi-select values. Report accepted and rejected rows with reasons.
- **Components:** Planned: `data: product source table`; `data: activity type source table`; `data: product subject source table`; `data: activity location source table`; `load report`
- **Depends on:** A1-04, A1-08; approved mapping; reference objects available; no deployment by this ticket
- **Acceptance criteria:**
  1. Each approved source table has a validated load file.
  2. Row counts and rejected-row reasons are reported.
  3. Lookups are populated in dependency order.
  4. Weekday flags are translated correctly.
  5. No deferred `new_event_reservations` data or relationship is loaded.
- **QA ידני:**
- **Claude Code prompt:** Prepare and validate A1 catalog load files from the approved mapping. Start with records that have no dependencies, document row counts and rejects, and do not perform an org data load unless separately approved. Do not deploy to the org. Generate the metadata and show it for approval.
- **Human build hours:** 8
- **Actual hours:**
- **Status:** To Do

## A1-10: Catalog Manager permission set

- **Category:** permissions
- **מקור באפיון:** [Doc 2, תהליכים עסקיים › יוזרים, הרשאות והסבת נתונים](../../dd/doc2-ordering-processes/06-processes/15-users-permissions-migration.md), heading `תיאור התהליך`, paragraph `קבוצות ההרשאה (Permission Sets)` (includes `ניהול קטלוג ומחירון`). Naming and metadata-description requirements are project conventions in `CONVENTIONS.md`, not DD requirements.
- **Business context:** יש לבדוק את A1 כמשתמשת בעלת תפקיד מנהלת קטלוג ולא רק כמנהלת מערכת. A1 must be tested as a Catalog Manager, not only as a System Administrator.
- **What to build:** Create `A1_Catalog_Manager` with object, field, tab and relevant action permissions for the approved A1 catalog components. Use field-level security through the permission set and do not edit profiles.
- **Components:** Planned: `Permission Set: A1_Catalog_Manager`; `object permissions`; `field permissions`; `tab visibility`
- **Depends on:** A1-09; final component list; org clash check
- **Acceptance criteria:**
  1. A user with only the permission set can create and manage approved catalog records.
  2. A user without the permission set cannot perform the catalog-manager actions.
  3. Sensitive or unresolved fields are not granted accidentally.
  4. No profile is edited.
  5. The permission set description links to this ticket and its DD sources.
- **QA ידני:**
- **Claude Code prompt:** Create `A1_Catalog_Manager` for the approved A1 components using field-level security in the permission set. Do not edit profiles, Cases components or managed package components. Do not deploy to the org. Generate the metadata and show it for approval.
- **Human build hours:** 3
- **Actual hours:**
- **Status:** To Do

## A1-11: End-to-end acceptance test

- **Category:** testing
- **מקור באפיון:** [Doc 2, תהליכים עסקיים › בניית מוצרים, חבילות ותבניות יום פעילות](../../dd/doc2-ordering-processes/06-processes/01-products-bundles-templates.md), headings `שלבי התהליך` (steps 1-7), `כללי ולידציה` (rows `V01-02`, `V01-03`), and `אוטומציות` (rows `A01-11`, `A01-12`); also `אוטומציות מרכזיות, מפרט מימוש`, implementation specification `תהליך הקמת מוצר בשלבים, מטיוטה לפרסום` (acceptance test).
- **Business context:** A1 הושלם רק כאשר מנהלת קטלוג יכולה ליצור ולהשתמש בנתיב קטלוג עובד, ולא רק כאשר קובצי המטא-דאטה קיימים. A1 is complete only when a Catalog Manager can create and use a working catalog path, not when metadata files merely exist.
- **What to build:** Execute and document a manual acceptance path: create a product as a draft, complete the approved fields, apply the publication path where relevant, clone the product, create or clone a Bundle, verify Bundle Items and catalog visibility, and validate the relevant failure cases.
- **Components:** Planned: `manual test script`; `test data`; `acceptance results`; `updated Components fields on A1 tickets`
- **Depends on:** A1-01 through A1-10; all required decisions approved; test user available
- **Acceptance criteria:**
  1. An incomplete online product remains a draft and is not exposed online.
  2. A completed product follows the approved publication path and is available only in selected channels.
  3. Product cloning copies approved conditions and creates a new record only on save.
  4. Bundle cloning preserves approved required and optional product categories.
  5. V01-02 and V01-03 pass both positive and negative tests.
  6. Manual QA results are recorded in every ticket's `QA ידני` field.
  7. Every ticket's Components field contains the actual built API names.
- **QA ידני:**
- **Claude Code prompt:** Do not generate deployment metadata for this ticket. Prepare a manual acceptance-test script and result template for the approved A1 path. Do not deploy to the org. Generate the metadata and show it for approval.
- **Human build hours:** 4
- **Actual hours:**
- **Status:** To Do

## Open questions and blockers to resolve before implementation

- F1/F2: Bundle, activity type and activity day template ownership and relationships.
- F3: Which subject entity owns the activity day template.
- F5: Product fields required by Doc 2 but absent from the current mapping.
- F6: Gefen must not be treated as a Bundle field unless the conflict is resolved.
- D-006: Weekday consolidation requires architect approval.
- D-007: Scope of the 23 display fields must be ruled on before A1-02.
- Open Doc 2 items: Product2 versus Bundle responsibilities, paid/free/optional rules by channel, Catalog Manager role, draft mandatory fields, recurring-product publication mechanism and SmartTicket interface ownership.
