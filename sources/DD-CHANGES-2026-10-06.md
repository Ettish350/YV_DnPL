# DD body changes, approved versions of 06.10.2026

Doc 1 of 31.08.2026 to approved 06.10.2026. Doc 2 V9 of 15.09.2026 to approved 06.10.2026. Written for the epic owners: read your epic's row in "Impact by epic" and the re-check list at the end. Nothing here is decided; open a question or challenge Issue where a change hits your work.

Method: body-only diffs (comment markers and threads stripped), word-level comparison per paragraph. Pure whitespace, stray-space and link-only changes are listed only where they could break tooling. Unchanged chapters: Doc 1 `00-front-matter`, `02-current-state`, `06-diagrams`; Doc 2 `01-goals`, `06-processes/00-overview`, `06-processes/06-official-visits-donors`, `09-fields`, `11-appendix-reports`.

Code inventory (script over old vs new files): **new** A09-14, A09-15. **Voided** A13-04 (row kept, text "בוטל"). No code removed.

## dd/doc1-intro-architecture/01-goals.md
- Structural only: the "שיבוץ חכם של מדריכים ומשאבים" goal is split out into its own paragraph. No wording change.

## dd/doc1-intro-architecture/03-architecture.md
- Heading "נותני שירות וכספים" renamed "נותני שירות"; new section "### חיוב וכספים" holds the customer-billing sentence (Almog, later Priority, Pelecard) and adds: billing processes are separate from service-provider management and payment.
- Audience paragraph: added that one product or activity type can serve many audiences, no separate product per audience is required, and audience fit and activity-type fit are "שני נושאים נפרדים".

## dd/doc1-intro-architecture/04-entities.md
- Product: old text said a "רכז אחראי" field would be added to the order item (Opportunity Product). New: "הרכז האחראי מקושר להזמנה בלבד, ולא למוצר או לפריט ההזמנה". The available-languages field on the product is unchanged.
- Budget framework: sentence "המסגרת משמשת ציר חיוב ונראות נפרד מהיררכיית הלקוח" replaced by "אין קשר בין הלקוח לבין המסגרת התקציבית", the framework derives from order conditions and is not a customer attribute.
- Department: new sentence, every department is linked to its wing ("אגף") for reporting by wing.
- Registration__c: a paid registration links to an Opportunity used for payment and billing only; explicitly "אין בכך קשר להזמנת ביקור", most registrations are unrelated to a visit order.

## dd/doc1-intro-architecture/05-configurations.md
- Scheduling overlap: "חיווי תוספות שעות וביטול זמן למשבץ" replaced by a hard rule: "חסימת שיבוץ חופף של מדריך" across guiding, travel, cancellation time and minimum completion hours; warning only for two activities outside YV on the same day.
- Minimum online notice paragraph: moved into the validation-rules paragraph, text unchanged.
- Cancellation reason: mandatory reason now scoped to cancellations made by YV. New: self-cancellation online is allowed only for a one-day order and sets a fixed reason "בוטל ע"י המזמין במקוונת" on order and day; manual cancellation of an order in TAHEL applies its reason to all days; cancelling a single day records the reason on that day only and the order does not become cancelled.
- Link to the forms chapter added (link only).

## dd/doc2-ordering-processes/00-front-matter.md
- Portal self-service audience: "ללקוחות, לסוכנים ולמדריכים" -> "ללקוחות ולסוכנים" (guides removed from the portal).

## dd/doc2-ordering-processes/02-current-state.md
- Regulation today: no longer "managed in external files ... not a system rule"; now "קיים ... ומנוהל לפי קובצי ויסות מוגדרים".

## dd/doc2-ordering-processes/03-architecture.md
- Almog row: "דרך שכבת ביניים" -> "ממשק ה-API הפנימי של יד ושם, המתממשק כיום לאלמוג ובהמשך לפריוריטי".

## dd/doc2-ordering-processes/04-entities.md
- Group size: TAHEL changes from indication only above 35 to block: "חריגה מ-35 משתתפים נחסמת, ורק בעלי הרשאה יכולים לחרוג". (50 for Taglit and abroad frameworks unchanged.)
- Link to Doc 1 entities removed (link only).

## dd/doc2-ordering-processes/05-screens.md
- "לוח מרכז ההזמנות" renamed "לוח תפעול הזמנות". Description rewritten: for all users handling orders (coordinators and the order center), shows new online orders, changed or cancelled orders, orders missing details, routing status and open tasks, handled from the board. Old claim that it replaces phone and email handling reversed: "הלוח אינו מחליף את הטיפול הטלפוני".
- Customer 360 screen moved from the service-provider card section to a new "## כרטיס הלקוח" section (text unchanged). "תצוגה יומית והעברת שיבוץ בין הזמנות" heading moved (structural).

## dd/doc2-ordering-processes/06-processes/01-products-bundles-templates.md
- Step 2: new rule, in TAHEL the group schedule lines must not overlap for time-bound products, considering each product's duration (example: show 12:00 for one hour, museum tour from 13:00 only).

## dd/doc2-ordering-processes/06-processes/02-online-self-service-order.md
- New paragraph "עריכת הזמנה במערכת המקוונת": customer edits own order even after payment; any cost change requires extra payment; can change date and time, participants, add services (audio guide); general public only if no extra group is needed, education groups may add a group in some cases; subject to museum and guided-group regulation; editable activity types and scope come from YV as configuration; money mechanics in Doc 3.
- Removed: "הכרעת ספק ופלטפורמת הפרונט הציבורי פתוחה" (vendor and front-end platform no longer stated as open).
- Portal licensing: now "רישוי הפורטל, שייקבע מול יד ושם" (was in the open-issues document).
- Removed: "סוכני תגלית מקבלים תצוגת היתכנות של מדריך ומשאבים".
- Private customer editing: may choose any time slot; moving from a cheap to an expensive hour is allowed and the registration and handling fee difference is charged (was: only alternative cheap hours shown).
- Login: institution or agent logs in with a personal user (email) and password, the user is tied to a specific institution, since "הח"פ אינו מזהה חד-ערכי" when parent and child institutions share it (was: company number and password).
- Budget framework table row: the framework decides whether to issue an automatic invoice, institution attributes decide when; an institution can be flagged "no automatic invoice", and that flag overrides both (was: invoice issuing and timing not set by the framework).
- V02-05: 35-participant limit now enforced in TAHEL too, only authorized users may exceed it (was indication only in TAHEL).
- V02-08: private customer editing may choose any hour; moving to an expensive hour "מחייב תשלום הפרש" (was: alternative hours in the paid price band only).
- New-institution form steps: the notification to the institution-opening team is also sent when a customer card is opened manually in TAHEL. Acceptance test wording fix.
- Cross-references to "פרקי הכספים" now point to Doc 3.

## dd/doc2-ordering-processes/06-processes/03-manual-order-intake.md
- Accounting interface reference now "פרק הפקת חשבון וחיוב לקוח מול אלמוג שבמסמך 3" (was open-issues document).
- Date change with assignments: service-provider assignments are kept, the order is flagged "דרוש טיפול בשיבוצים", removal is manual only (A09-09). Old text allowed the change only after the coordinator removed providers manually.
- A03-05: the automatic notice (with contract-day indication) is sent only "כאשר השינוי בוצע במערכת המקוונת"; a change made directly in TAHEL needs no automatic notice.
- Cancellations reference now points to the scheduling chapter and Doc 3 (was "processes 22 to 23").

## dd/doc2-ordering-processes/06-processes/04-institution-identification-routing.md
- Routing scope narrowed: runs on intake from the online system only. New: an order opened manually in TAHEL (by the coordinator or the department logistics coordinator) "אינה עוברת ניתוב", the responsible coordinator is set at opening.
- Visibility: "כל רכזת רואה ... של מחלקתה בלבד" replaced by default views filtered by department "ללא הגבלה על הנראות".
- Invoice: budget framework decides whether to issue automatically; timing is set by institution attributes ("וההסדר החוזי" removed).
- A04-05 and steps: board renamed "לוח תפעול הזמנות" (rename only).

## dd/doc2-ordering-processes/06-processes/05-army-security-delegations.md
- Link removed from the guide-payment reference (link only, no text change).

## dd/doc2-ordering-processes/06-processes/07-educational-mobile-units.md
- New sentence: the mobile units tab has "שדות בסיסיים בלבד (ללא תהליכים יעודיים)".
- Finance reference now "במסמך 3".

## dd/doc2-ordering-processes/06-processes/08-quote-discounts-approval.md
- Discounts at three levels: quote line, activity day and order (was line and order).
- Discount types replaced: line discount is always percent, order discount is always an amount; activity-day discount type "ייקבע לאחר אישורכם". Removed types: Ministry of Education (20 per participant) and "מהשואה לתקומה" (30 per participant).
- New: the support marker (budget source) is set at order level and cascades to day and lines; supporter name editable at each level.
- New paragraph: payment status at three levels (line, day, order) rolling up; examples "שולם חלקית", "הופק חשבון חלקי"; any price change recalculates payment status.
- A08-02: discount calculation "אחוז בשורת הצעת מחיר, סכום בהזמנה". Steps and entity row updated to match. Acceptance test now "הנחה של 10 אחוז ... של 1,000 ... 900".
- A08-05: stray space only.

## dd/doc2-ordering-processes/06-processes/09-guide-resource-scheduling.md
- **New A09-14**: cancelling an order with assigned providers directly in TAHEL: non-provider resources released automatically; warning and confirmation; keeping provider assignments needs double confirmation, since the provider is entitled to payment.
- **New A09-15**: assigning a contract-day guide to a tour shorter than the contract-day hours: payment is topped up to contract hours, with an indication at assignment.
- Date or time change: museum slot is recreated automatically by availability when changed online, released and rebooked manually when changed in TAHEL (was manual only). The email is sent only when the change was online.
- A09-09: museum slot "נקבע מחדש לפי היום והשעה שהלקוח בוחר"; manual release applies only to changes made directly in TAHEL. Steps: in TAHEL, selection beyond museum capacity "מותרת למנהלים ולבעלי הרשאה מיוחדת בלבד" (was allowed for anyone).
- Cancellation acceptance test: email sent only for online cancellations.

## dd/doc2-ordering-processes/06-processes/10-external-double-tours-distance.md
- Payment hours are now stored on the assignment record as hours to pay, alongside original activity hours (was computed behind the scenes at payroll).
- New: in some cases, tours only, the guide is paid less than ordered: the assignment product is swapped for a shorter tour product and payment derives from it.
- A10-08, A10-09: link removed only.

## dd/doc2-ordering-processes/06-processes/11-physical-resources-museum.md
- Step 11 and A11-08: above 35 participants is now blocked, authorized users may exceed (was indication only); exception up to 50 for Taglit and abroad frameworks restated in step 11.

## dd/doc2-ordering-processes/06-processes/12-regulation-locking.md
- Guided-tour regulation now also covers "בית ווהלין".
- Step 6 rewritten: a department with regulation may use only its allocated quota, a department without regulation is unlimited; two weeks before, unused balance opens to all departments. Online: blocked at quota. TAHEL: warning plus explicit acknowledgment button, only authorized users may exceed. Events are excluded except an event type YV will approve (per-event flag). Each department and each product or activity type carries an "included in regulation" flag; orders by IDF education unit soldiers are excluded via a flag on their budget framework.
- Locking steps: new option to block an activity type for a date or date range.

## dd/doc2-ordering-processes/06-processes/13-guide-availability-service-providers.md
- **A13-04 voided**: "בוטל: אין טופס ייעודי לימי חופשה"; absences per A13-05.
- Vacation days via a dedicated vacation form removed throughout (description and step 10).
- Entity row "רשומת תיק אישי (Custom)" removed. New sentence: the guide file is "אינו תיק אישי במובן של משאבי אנוש". Step 14: "רשומות התיק האישי" replaced by "המישובים".
- Link removed from the guide-payment reference.

## dd/doc2-ordering-processes/06-processes/14-reminders-proactive-communication.md
- New paragraph: send the schedule from an order or event to assigned providers (by day, order or event), user picks recipients and may add emails; fixed editable template; attachments (confirmation, schedule, invoice) sent to the contact with a prepared message per attachment type. Formatted file output depends on a third-party document tool.
- Order confirmation: fixed text segments kept in about three versions by order content and location, assigned automatically.
- New cross-cutting rule: automatic change or cancellation messages only when made online (private customers and agents); changes in TAHEL trigger none. Internal team email likewise limited to online changes.
- A25-07: emails only "רק כאשר הביטול או השינוי בוצעו במערכת המקוונת"; references now point to the scheduling chapter and Doc 3.

## dd/doc2-ordering-processes/06-processes/15-users-permissions-migration.md
- Permission model reversed: old text replaced full transparency with a Role hierarchy (record visibility) plus profiles. New: "שומר על השקיפות הקיימת כיום", everyone sees everything except sensitive service-provider data (ID, personal rates, feedback); distinction is by actions only, configurable per user.
- Step 3: per role only the allowed actions are defined; all users see all data except sensitive provider data.
- Business Unit row: "היררכיית Role ... בתוספת כללי שיתוף" -> "נראות לכלל המשתמשים, ללא הגבלה".
- Section coordinator profile: "נראות מוגבלת להזמנות המדור" removed.
- Sensitive data: the personal-file tab ("התיק האישי") removed from field-level security.
- Migration: old "לא מבוצעות הסבות עבר" removed; now the migration scope, "לרבות הזמנות פתוחות ונתוני עבר, ייקבע בדיון ייעודי". The figure "61,495 תחנות איסוף" removed.

## dd/doc2-ordering-processes/07-validations-security-templates.md
- Customer identification: identifiers no longer "חד-ערכיים"; new: institution code or company number may be shared by parent and child institutions, the orderer then picks the institution from a list.
- Active Trail templates sentence moved (structural).

## dd/doc2-ordering-processes/08-reports-dashboards.md
- PDF branding: Excel needs no branding; a PDF with the YV logo "מותנית בכלי הפקת מסמכים של צד שלישי" (was: PDF will carry the logo).
- Report families table: "קהל היעד" column removed (5 rows).
- New: reports are delivered within Malam's quota "50 דוחות לכלל המערכת", beyond reports defined in dedicated sections, prioritized by YV.

## dd/doc2-ordering-processes/10-forms.md
- New list of forms received from YV on 22.9.2026: 18 entries (visit order, recurring event, kiosk, event registrations, seminars, conferences, Ready to print, poster contest, torch lighters, guide availability, institution opening, Holocaust Day ceremony). Event, conference and seminar forms go to Doc 3; field mapping per form to come from YV.
- Guide availability form: no longer reports vacation days; absences (vacation, sickness, reserve duty) are recorded as dedicated assignments.

## dd/doc2-ordering-processes/12-appendix-communication-templates.md
- "## אישור הזמנה" heading moved (structural).

## dd/doc2-ordering-processes/13-appendix-yv-materials.md
- Stray space inside the URL only.

## Impact by epic

Codes are the new codes from `plan/EPICS.md`; the `dd/INDEX.md` section 4 code is in parentheses where it differs.

| Epic | Changes that touch it |
|---|---|
| L0 | Department linked to wing (new relationship); budget framework has no link to the customer; Registration__c to Opportunity is payment only; coordinator on order only (not order item) |
| A1 | Coordinator not on product or order item (confirms R6); new no-overlap rule for time-bound schedule lines (step 2, may belong to A3 per D-010); one product may serve many audiences; V01-02 text unchanged |
| A2 | Discount types reduced to percent (line) and amount (order); MoE and "מהשואה לתקומה" per-participant types removed |
| A3 (C1) | Date change keeps provider assignments, flag + manual removal; A03-05 notice only for online changes; group-size block above 35 |
| A4 (A3) | Three discount levels, fixed types, day-level type pending YV; support marker cascade; three-level payment status; A08-02 and acceptance test |
| A5 (C5) | Step 6 regulation rewrite (quota per department, flags on department, product, event type, budget framework; online block vs TAHEL acknowledgment); Beit Wohlin; date-range blocking of activity type |
| B1 | Personal-file entity removed; A13-04 voided; sensitive data = ID, rates, feedback |
| B2 | Vacation form removed; guide availability form reports availability only; guides removed from portal audience |
| B3 | New A09-14, A09-15; A09-09 museum slot online vs TAHEL; overlap now a hard block (Doc 1 config); capacity override for managers only; Taglit agents lose feasibility view |
| B4 | Payment hours stored on assignment; shorter tour product swap for reduced pay |
| B5 | A11-08 and step 11 block above 35 |
| C1 (C2) | Routing only for online intake; no visibility restriction; invoice whether/when split; board renamed |
| C3 (C4) | Online order editing paragraph; personal-user login; V02-05, V02-08; portal licensing; new-institution notification also on manual card; online self-cancel one-day only; forms list |
| X1 | Change and cancel messages only for online changes (A25-07); schedule send from order or event; confirmation text versions |
| X2 | 50-report quota; audience column removed; PDF logo conditional; operations board rename and scope; customer 360 section |
| X3 | Identifiers not unique (parent and child institutions); cancellation reason rules (Doc 1 config) |
| X4 | Full transparency, no Role hierarchy or sharing rules; migration scope (open orders, history) to be decided |
| X5 | Mobile units: basic fields only, no dedicated processes |

## Repo statements to re-check (flags only, nothing decided)

1. `knowledge/A1-binding-rules.md` R6: confirmed and strengthened (order item also excluded). The old Doc 1 sentence adding "רכז אחראי" to the order item is gone, so no remaining DD inconsistency. `tickets/A1/tickets.md` A1-01 criterion 4 stays valid.
2. `knowledge/A1-binding-rules.md` R14 ("Audience and location are conditions of the budget framework"): re-read against the new Doc 1 audience sentence (audience fit vs activity-type fit are separate) and "no link between customer and budget framework".
3. `knowledge/A1-binding-rules.md` header and all knowledge files cite "Doc 2 V9" and `dd_to_text.py` file numbers: the binding source is now the 06.10.2026 approved version.
4. `tickets/A1/tickets.md` A1-06, A1-05, A1-11 (V01-02): DD text of V01-02 unchanged; no action.
5. `knowledge/B1-binding-rules.md` R8: sensitive set now ID, personal rates, feedback; personal-file tab no longer exists. Check `mapping/B1-field-mapping.md` and B1 tickets (folder `tickets/B1/` is empty) for any personal-file object or vacation-form field.
6. `plan/EPICS.md` B2 ("Monthly availability form ... absences ... as zero-hour assignments"): consistent with the new text; any vacation-days field on the availability form is now out.
7. `plan/SCHEDULE.md` sprint 9 X4 ("permission sets per role, sharing rules"): the DD now keeps full visibility; sharing rules and Role-based visibility may be out of scope.
8. `brief/epic-owners-brief.md` line 48 and `plan/EPICS.md` X4 ("migration of the 24 marked tables"): the DD now says migration scope, including open orders and history, is to be decided in a dedicated discussion.
9. `plan/EPICS.md` A4 and `brief/epic-owners-brief.md` (A01-06 "discount pairs", discount engine in A4): discount types and levels changed; re-check the discount-pair logic and A2 "discount eligibility" scope.
10. `decisions/D-009` (stubs incl. Department__c, Budget_Framework__c): Department now needs a wing link; check whether the stub or L0 needs a wing target.
11. `decisions/D-010` (ownership list): new A09-14, A09-15 need an owner (enforced in B3?); the new schedule-overlap rule in chapter 01 step 2 is enforced on order schedule lines, not in the catalog.
12. `plan/EPICS.md` X5 and `plan/SCHEDULE.md` sprint 7 ("mobile units"): DD now limits mobile units to a basic-fields tab.
13. `plan/SCHEDULE.md` sprint 6 C3c ("Titan forms") and C3 scope: the new 18-form list sends event, conference and seminar forms to Doc 3.
14. `plan/SCHEDULE.md` sprint 7 "Portal hardening" and any guide-portal work: guides are no longer a portal audience (Doc 2 front matter).
