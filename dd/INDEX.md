# Orders module DD, Markdown edition: legend for Claude Code

Read this file first. It tells you what is in this folder, which file answers which question, and how to treat what you find.

## 1. What this is

The three binding detail-design (DD) documents of the Yad Vashem orders module, converted from Word to Markdown with `scripts/dd_to_md.py` and split along the Word navigation pane (heading levels). Text, tables and review threads are converted mechanically from the docx, not retyped. Doc 1 and Doc 2 are the versions approved by Yad Vashem on 06.10.2026 (converted 06.10.2026). Doc 3 is the version Yad Vashem approved as final (file of 04.11.2026; D-037, converted 08.11.2026).

| Doc | Folder | Source file (`sources/dd/`) |
|---|---|---|
| Doc 1, Introduction and Architecture | `doc1-intro-architecture/` | אפיון מודול הזמנות מאושר חלק 1 מתוך 3 6-10-2026 .docx |
| Doc 2, Ordering Processes | `doc2-ordering-processes/` | אפיון יד ושם מודול הזמנות חלק 2 מאושר 06-10 .docx |
| Doc 3, Events and Finance | `doc3-events-finance/` | מסמך 3 אירועים וכספים - מאושר על ידי יד ושם - 2026-11-04.docx |

Doc 3's appendix 13 screenshots are placeholders in the git copy (customer records, D-031). Changes from V35, the last version the team saw as a draft: `sources/DD-CHANGES-DOC3-2026-11-04.md`.

The Word files are the master. **Do not edit these .md files by hand.** A change goes into the docx and the set is regenerated (`python3 scripts/dd_to_md.py "<Doc 1>" "<Doc 2>" "<date>" "<Doc 3>" "<Doc 3 label>"`, Lior only). Body changes against the previous binding versions: `sources/DD-CHANGES-2026-10-06.md`. If you think the text is wrong, log it in your sync file.

## 2. Precedence

1. **Doc 2** binds processes, validation rules (V codes), automations (A codes) and the entity map used by the processes.
2. **Doc 1** binds the general data model, configuration, permission model and architecture.
3. **Doc 3** binds the events and finance processes (V and A codes 14 to 24, including 17B), their entities and screens, and the module-wide profiles and permissions (chapter 8) and interfaces (chapter 9). Where Doc 3 and Doc 1 or Doc 2 disagree, check `knowledge/dd-corrections.md` first; anything not ruled there is logged, not decided.
4. The epic owners brief and `mapping/` come after the DD. If they contradict the DD, the DD wins: log it, do not decide it.

Body text is binding. **Review comments are not** (section 6).

## 3. Layout

```
INDEX.md                         this legend
_meta/toc.md                     full heading tree of all three docs, linked
_meta/open-comments.md           review threads still awaiting a Malam reply
assets/d1/, assets/d2/, assets/d3/  diagrams and screenshots from the docx (PNG, open with Read)
doc1-intro-architecture/         one file per chapter (Heading 1)
doc2-ordering-processes/         one file per chapter; 06-processes/ split per process (Heading 2)
doc3-events-finance/             one file per chapter; 05-processes/ split per process (Heading 2)
```

Heading levels inside each file follow Word: `#` is the chapter (or the process, inside `06-processes/` and `05-processes/`). Every file opens with a header block: source, breadcrumb, the V/A codes it contains, and previous/next links.

Every process file in `06-processes/` (and, mostly, in Doc 3's `05-processes/`) has the same internal sections, in this order: תיאור התהליך (description), שלבי התהליך (steps), ישויות ושדות בתהליך (entities and fields), כללי ולידציה (V codes table), אוטומציות (A codes table), אוטומציות מרכזיות, מפרט מימוש (implementation spec: trigger, steps and acceptance test per automation). Some short processes have only some of these.

## 4. Where to look, by epic

| Epic | Scope | Read |
|---|---|---|
| L0 | Environment, users, stubs, mapping gate | [doc1-intro-architecture/04-entities.md](doc1-intro-architecture/04-entities.md)<br>[doc2-ordering-processes/04-entities.md](doc2-ordering-processes/04-entities.md) |
| A1 | Products, bundles, activity day templates | [06-processes/01-products-bundles-templates.md](doc2-ordering-processes/06-processes/01-products-bundles-templates.md) |
| A2 | Price books, ticket types, discount eligibility | [06-processes/01-products-bundles-templates.md](doc2-ordering-processes/06-processes/01-products-bundles-templates.md)<br>[doc2-ordering-processes/04-entities.md](doc2-ordering-processes/04-entities.md)<br>[doc1-intro-architecture/04-entities.md](doc1-intro-architecture/04-entities.md) |
| A3 | Quote, discounts and approval | [06-processes/08-quote-discounts-approval.md](doc2-ordering-processes/06-processes/08-quote-discounts-approval.md) |
| B1 | Service provider card, certifications, statuses | [06-processes/13-guide-availability-service-providers.md](doc2-ordering-processes/06-processes/13-guide-availability-service-providers.md)<br>[doc2-ordering-processes/05-screens.md](doc2-ordering-processes/05-screens.md) |
| B2 | Monthly availability and hour regulation | [06-processes/13-guide-availability-service-providers.md](doc2-ordering-processes/06-processes/13-guide-availability-service-providers.md) |
| B3 | Guide and resource scheduling | [06-processes/09-guide-resource-scheduling.md](doc2-ordering-processes/06-processes/09-guide-resource-scheduling.md)<br>[doc2-ordering-processes/05-screens.md](doc2-ordering-processes/05-screens.md) |
| B4 | External tours, double tours, distance | [06-processes/10-external-double-tours-distance.md](doc2-ordering-processes/06-processes/10-external-double-tours-distance.md) |
| B5 | Physical resources, museum, exhibitions | [06-processes/11-physical-resources-museum.md](doc2-ordering-processes/06-processes/11-physical-resources-museum.md) |
| C1 | Manual order intake, groups, schedule | [06-processes/03-manual-order-intake.md](doc2-ordering-processes/06-processes/03-manual-order-intake.md)<br>[doc2-ordering-processes/04-entities.md](doc2-ordering-processes/04-entities.md) |
| C2 | Institution identification, classification, routing | [06-processes/04-institution-identification-routing.md](doc2-ordering-processes/06-processes/04-institution-identification-routing.md) |
| C3 | Army, security forces, delegations | [06-processes/05-army-security-delegations.md](doc2-ordering-processes/06-processes/05-army-security-delegations.md) |
| C4 | Online self-service ordering | [06-processes/02-online-self-service-order.md](doc2-ordering-processes/06-processes/02-online-self-service-order.md)<br>[doc2-ordering-processes/10-forms.md](doc2-ordering-processes/10-forms.md) |
| C5 | Multi-level regulation and locking | [06-processes/12-regulation-locking.md](doc2-ordering-processes/06-processes/12-regulation-locking.md) |
| X1 | Reminders and proactive communication | [06-processes/14-reminders-proactive-communication.md](doc2-ordering-processes/06-processes/14-reminders-proactive-communication.md)<br>[doc2-ordering-processes/12-appendix-communication-templates.md](doc2-ordering-processes/12-appendix-communication-templates.md) |
| X2 | Reports and dashboards | [doc2-ordering-processes/08-reports-dashboards.md](doc2-ordering-processes/08-reports-dashboards.md)<br>[doc2-ordering-processes/11-appendix-reports.md](doc2-ordering-processes/11-appendix-reports.md) |
| X3 | Cross-cutting validations, duplicates, Hebrew calendar | [doc2-ordering-processes/07-validations-security-templates.md](doc2-ordering-processes/07-validations-security-templates.md) |
| X4 | Permissions and data migration | [06-processes/15-users-permissions-migration.md](doc2-ordering-processes/06-processes/15-users-permissions-migration.md)<br>[doc1-intro-architecture/03-architecture.md](doc1-intro-architecture/03-architecture.md) |
| X5 | Official visits, educational mobile units | [06-processes/06-official-visits-donors.md](doc2-ordering-processes/06-processes/06-official-visits-donors.md)<br>[06-processes/07-educational-mobile-units.md](doc2-ordering-processes/06-processes/07-educational-mobile-units.md) |

The epics above cover Doc 1 and Doc 2. Doc 3 work is assigned by Jira (YV20) task; for an events or finance step, start from the process file in [doc3-events-finance/05-processes/](doc3-events-finance/05-processes/00-overview.md) and the entities in [doc3 03-entities](doc3-events-finance/03-entities.md).

Rule from the brief: a component belongs to the epic where it is **enforced**, not the chapter where it is written. Search all three docs before concluding something is outside your epic.

## 5. Where to look, by code or entity

**V/A codes** follow the pattern `V03-05` / `A03-05`. The prefix number is the process. Codes 01 to 13 and 25 are in Doc 2; codes 14 to 24 and 17B (courses and trainings) are in Doc 3. Doc 3 also cites some Doc 2 codes (for example V08, V09, A02); the definition is always in the file listed below.

The numbering has gaps in the source itself (for example V01-01, V02-01 and A09-04 do not exist in this version). A missing number is not a conversion error; do not invent it.

| Prefix | Codes present | Defined in |
|---|---|---|
| A01 | A01-01, A01-02, A01-03, A01-04, A01-05, A01-06, A01-07, A01-08, A01-10, A01-11, A01-12 | [01-products-bundles-templates.md](doc2-ordering-processes/06-processes/01-products-bundles-templates.md) |
| V01 | V01-02, V01-03, V01-04, V01-05, V01-06 | [01-products-bundles-templates.md](doc2-ordering-processes/06-processes/01-products-bundles-templates.md) |
| A02 | A02-01, A02-02, A02-03, A02-04, A02-05, A02-06, A02-07, A02-08, A02-09, A02-10, A02-11 | [02-online-self-service-order.md](doc2-ordering-processes/06-processes/02-online-self-service-order.md) |
| V02 | V02-02, V02-03, V02-05, V02-06, V02-07, V02-08, V02-09 | [02-online-self-service-order.md](doc2-ordering-processes/06-processes/02-online-self-service-order.md) |
| A03 | A03-01, A03-02, A03-03, A03-04, A03-05, A03-06, A03-07, A03-08, A03-09, A03-10 | [03-manual-order-intake.md](doc2-ordering-processes/06-processes/03-manual-order-intake.md) |
| V03 | V03-01, V03-02, V03-03, V03-04, V03-05, V03-06, V03-07 | [03-manual-order-intake.md](doc2-ordering-processes/06-processes/03-manual-order-intake.md) |
| A04 | A04-01, A04-02, A04-03, A04-04, A04-05, A04-06, A04-07 | [04-institution-identification-routing.md](doc2-ordering-processes/06-processes/04-institution-identification-routing.md) |
| V04 | V04-01, V04-02, V04-03, V04-04 | [04-institution-identification-routing.md](doc2-ordering-processes/06-processes/04-institution-identification-routing.md) |
| A05 | A05-01, A05-02, A05-03, A05-04, A05-05, A05-06, A05-07, A05-08, A05-09, A05-10, A05-11 | [05-army-security-delegations.md](doc2-ordering-processes/06-processes/05-army-security-delegations.md) |
| V05 | V05-01, V05-02, V05-04, V05-05, V05-06 | [05-army-security-delegations.md](doc2-ordering-processes/06-processes/05-army-security-delegations.md) |
| A07 | A07-01, A07-02, A07-03, A07-04, A07-05, A07-06 | [07-educational-mobile-units.md](doc2-ordering-processes/06-processes/07-educational-mobile-units.md) |
| A08 | A08-01, A08-02, A08-03, A08-04, A08-05, A08-06, A08-07 | [08-quote-discounts-approval.md](doc2-ordering-processes/06-processes/08-quote-discounts-approval.md) |
| V08 | V08-01, V08-02, V08-03, V08-04, V08-05 | [08-quote-discounts-approval.md](doc2-ordering-processes/06-processes/08-quote-discounts-approval.md) |
| A09 | A09-01, A09-02, A09-03, A09-05, A09-06, A09-07, A09-08, A09-09, A09-10, A09-11, A09-12, A09-13, A09-14, A09-15 | [09-guide-resource-scheduling.md](doc2-ordering-processes/06-processes/09-guide-resource-scheduling.md) |
| V09 | V09-01, V09-02, V09-03, V09-04, V09-05 | [09-guide-resource-scheduling.md](doc2-ordering-processes/06-processes/09-guide-resource-scheduling.md) |
| A10 | A10-01, A10-02, A10-03, A10-04, A10-05, A10-06, A10-07, A10-08, A10-09, A10-10, A10-11 | [10-external-double-tours-distance.md](doc2-ordering-processes/06-processes/10-external-double-tours-distance.md) |
| V10 | V10-01, V10-02, V10-03 | [10-external-double-tours-distance.md](doc2-ordering-processes/06-processes/10-external-double-tours-distance.md) |
| A11 | A11-01, A11-02, A11-03, A11-04, A11-05, A11-06, A11-07, A11-08, A11-09, A11-10, A11-11, A11-12, A11-13, A11-14, A11-15 | [11-physical-resources-museum.md](doc2-ordering-processes/06-processes/11-physical-resources-museum.md) |
| V11 | V11-01, V11-02, V11-03, V11-04, V11-05, V11-06, V11-07, V11-08, V11-09, V11-10 | [11-physical-resources-museum.md](doc2-ordering-processes/06-processes/11-physical-resources-museum.md) |
| A12 | A12-01, A12-02, A12-03, A12-04, A12-05, A12-06, A12-08, A12-09, A12-10, A12-11, A12-12 | [12-regulation-locking.md](doc2-ordering-processes/06-processes/12-regulation-locking.md) |
| V12 | V12-01, V12-02, V12-03, V12-04, V12-05, V12-06, V12-07 | [12-regulation-locking.md](doc2-ordering-processes/06-processes/12-regulation-locking.md) |
| A13 | A13-01, A13-02, A13-03, A13-04, A13-05, A13-06, A13-07, A13-08, A13-10, A13-13 | [13-guide-availability-service-providers.md](doc2-ordering-processes/06-processes/13-guide-availability-service-providers.md) |
| V13 | V13-01, V13-02, V13-03, V13-04, V13-05, V13-06, V13-07, V13-09 | [13-guide-availability-service-providers.md](doc2-ordering-processes/06-processes/13-guide-availability-service-providers.md) |
| A14 | A14-01, A14-02, A14-03, A14-05, A14-06, A14-07, A14-08, A14-09, A14-13, A14-14, A14-16 | [01-open-public-events-tours-shows.md](doc3-events-finance/05-processes/01-open-public-events-tours-shows.md) |
| V14 | V14-01, V14-02, V14-03, V14-04, V14-05, V14-06, V14-08, V14-10, V14-11 | [01-open-public-events-tours-shows.md](doc3-events-finance/05-processes/01-open-public-events-tours-shows.md) |
| A15 | A15-01, A15-02, A15-03, A15-04, A15-05, A15-06 | [02-smartticket-event-sync.md](doc3-events-finance/05-processes/02-smartticket-event-sync.md) |
| V15 | V15-01, V15-02 | [02-smartticket-event-sync.md](doc3-events-finance/05-processes/02-smartticket-event-sync.md) |
| A16 | A16-01, A16-02, A16-03, A16-05, A16-07, A16-10, A16-11, A16-13, A16-15, A16-16, A16-17, A16-18 | [03-teacher-conferences-sessions-tracks.md](doc3-events-finance/05-processes/03-teacher-conferences-sessions-tracks.md) |
| V16 | V16-01, V16-03, V16-04, V16-06, V16-07, V16-11, V16-12 | [03-teacher-conferences-sessions-tracks.md](doc3-events-finance/05-processes/03-teacher-conferences-sessions-tracks.md) |
| A17 | A17-01, A17-02, A17-04, A17-05, A17-09, A17-10, A17-11 | [04-seminars.md](doc3-events-finance/05-processes/04-seminars.md) |
| A17B | A17B-01, A17B-02, A17B-03, A17B-06, A17B-07, A17B-09, A17B-10 | [05-courses-trainings.md](doc3-events-finance/05-processes/05-courses-trainings.md) |
| V17 | V17-02, V17-03, V17-04, V17-05, V17-08, V17-09 | [04-seminars.md](doc3-events-finance/05-processes/04-seminars.md) |
| V17B | V17B-01, V17B-03, V17B-04, V17B-06, V17B-07 | [05-courses-trainings.md](doc3-events-finance/05-processes/05-courses-trainings.md) |
| A18 | A18-01, A18-02, A18-03, A18-05, A18-06, A18-08, A18-11 | [06-professional-development.md](doc3-events-finance/05-processes/06-professional-development.md) |
| V18 | V18-01, V18-02, V18-03 | [06-professional-development.md](doc3-events-finance/05-processes/06-professional-development.md) |
| A19 | A19-01, A19-02, A19-04, A19-05, A19-06, A19-07, A19-08, A19-09, A19-10 | [07-attendance-arrival.md](doc3-events-finance/05-processes/07-attendance-arrival.md) |
| V19 | V19-01, V19-03, V19-04, V19-05, V19-06 | [07-attendance-arrival.md](doc3-events-finance/05-processes/07-attendance-arrival.md) |
| A20 | A20-01, A20-02, A20-03, A20-04, A20-05, A20-06, A20-07, A20-08, A20-09, A20-10, A20-11, A20-12 | [08-kiosk-entry-ordering.md](doc3-events-finance/05-processes/08-kiosk-entry-ordering.md) |
| V20 | V20-01, V20-02, V20-03, V20-04, V20-05, V20-06, V20-07, V20-08 | [08-kiosk-entry-ordering.md](doc3-events-finance/05-processes/08-kiosk-entry-ordering.md) |
| A21 | A21-01, A21-02, A21-03, A21-04, A21-10, A21-11, A21-12, A21-13, A21-15, A21-16, A21-19, A21-20, A21-21 | [09-almog-invoicing-billing.md](doc3-events-finance/05-processes/09-almog-invoicing-billing.md) |
| V21 | V21-01, V21-02, V21-04, V21-06, V21-07, V21-14, V21-16, V21-17 | [09-almog-invoicing-billing.md](doc3-events-finance/05-processes/09-almog-invoicing-billing.md) |
| A22 | A22-01, A22-03, A22-04, A22-06, A22-07, A22-08, A22-09, A22-10, A22-11, A22-13, A22-14, A22-15 | [10-private-customer-cancel-change.md](doc3-events-finance/05-processes/10-private-customer-cancel-change.md) |
| V22 | V22-01, V22-02, V22-03, V22-04, V22-09, V22-11, V22-12, V22-13 | [10-private-customer-cancel-change.md](doc3-events-finance/05-processes/10-private-customer-cancel-change.md) |
| A23 | A23-01, A23-02, A23-03, A23-05, A23-06, A23-07, A23-08, A23-09, A23-11, A23-12, A23-13 | [11-institutional-cancel-credit-refund.md](doc3-events-finance/05-processes/11-institutional-cancel-credit-refund.md) |
| V23 | V23-01, V23-02, V23-03, V23-04, V23-06, V23-08, V23-09, V23-13, V23-14 | [11-institutional-cancel-credit-refund.md](doc3-events-finance/05-processes/11-institutional-cancel-credit-refund.md) |
| A24 | A24-01, A24-02, A24-04, A24-05, A24-06, A24-08, A24-09, A24-11, A24-12 | [12-guide-provider-payment-malam-file.md](doc3-events-finance/05-processes/12-guide-provider-payment-malam-file.md) |
| V24 | V24-01, V24-02, V24-04, V24-05, V24-07, V24-08, V24-09, V24-11 | [12-guide-provider-payment-malam-file.md](doc3-events-finance/05-processes/12-guide-provider-payment-malam-file.md) |
| A25 | A25-01, A25-02, A25-03, A25-04, A25-05, A25-06, A25-07, A25-08, A25-09, A25-10 | [14-reminders-proactive-communication.md](doc2-ordering-processes/06-processes/14-reminders-proactive-communication.md) |
| V25 | V25-01, V25-02, V25-03, V25-04, V25-05, V25-06, V25-07, V25-08 | [14-reminders-proactive-communication.md](doc2-ordering-processes/06-processes/14-reminders-proactive-communication.md) |

**Entities.** The general definition of each entity is in [doc1 04-entities](doc1-intro-architecture/04-entities.md): ארגון ולקוח (Account); איש קשר (Contact / Person Account); מוצר (Product); מחירון (Price Book ו-Price Book Entry); חבילה (Bundle, Custom); פריט חבילה (Bundle Item, Custom); מסגרת תקציבית (Custom); מחלקה (Custom); הזמנה (Opportunity); פריט הזמנה (Opportunity Product); אירועים והרשמה; מפגש (GoMeddo Reservation); משאב (GoMeddo Resource); צוות (GoMeddo Staff); משתתף במפגש (Reservation Contact); מקום פעילות (YV Location); נותן שירות ומדריך (Contact, סוג רשומה מדריך); מטריצת הסמכות (Custom); זמינות מדריך (Custom); מישוב (Custom); ימי חוזה (Custom); חיוב ותשלום לקוח (Custom); תשלום מדריך (Custom); הזמנת רכש (Custom); חשבון כספי (Custom). The process-level entity map (order structure: activity day, group, schedule line) and the data-model completions are in [doc2 04-entities](doc2-ordering-processes/04-entities.md). The events and finance entities (event, registration, financial account and its lines, receipt, hotel allocation, shuttle, pickup point and others) are in [doc3 03-entities](doc3-events-finance/03-entities.md), with a legend in [doc3 14-appendix-entity-legend](doc3-events-finance/14-appendix-entity-legend.md). Field-level mapping is in `mapping/`, not here. נספח א (the field mapping appendix) is not final and is not part of this set.

## 6. Review comments

All three Word files carry the client's review threads and Malam's replies. They are kept because they show where the text is still in discussion. In the approved files almost all threads are resolved (Doc 1: 244 of 244; Doc 2: 746 of 758; Doc 3: 187 of 235). Read the replies: they carry decisions. Comment numbers were renumbered in the approved files; an old `cN` from before 06.10.2026 does not point to the same thread.

- In the body, `⟦💬 c283⟧` marks the anchor of a thread. The thread itself is at the bottom of the same file, under "הערות סקירה בפרק זה".
- Status per thread: **סגורה** (resolved in Word), **נענתה על ידי מלם** (last message is from Malam), **ממתינה למענה מלם** (last message is from the client; Malam has not answered yet).
- **How to use them:** the body text is what you build. If a comment on the paragraph you are building contradicts the body, or its thread is ממתינה למענה מלם, treat the point as an open question: log it in your sync file and build the rest. Do not implement what a comment asks for unless the body text says it.
- All open threads in one list: [_meta/open-comments.md](_meta/open-comments.md) (1 thread).

## 7. Known gaps (do not fill them from general knowledge)

- **Doc 2 points forward to Doc 3 by chapter name** (for example הפקת חשבון וחיוב לקוח מול אלמוג, סמינרים, כנסי מורים). Those references are now linked to the Doc 3 files where the name matches; where a reference is not linked, find the chapter in [doc3 05-processes](doc3-events-finance/05-processes/00-overview.md).
- **Interfaces.** Doc 2 points to "פרק הממשקים שבמסמך 1", but Doc 1 has no interfaces chapter. The interfaces are in [doc3 09-interfaces](doc3-events-finance/09-interfaces.md), with the finance-system endpoints in [doc3 11-appendix-finance-interface-endpoints](doc3-events-finance/11-appendix-finance-interface-endpoints.md); "רכיבים ומערכות" in [doc1 03-architecture](doc1-intro-architecture/03-architecture.md) is the overview.
- **Permissions.** Doc 2 points to "פרק ההרשאות שבמסמך 1". The profiles and permissions matrix is in [doc3 08-profiles-permissions](doc3-events-finance/08-profiles-permissions.md); Doc 1 has the model ("מודל ההרשאות והרישוי" in [doc1 03-architecture](doc1-intro-architecture/03-architecture.md)) and named permission sets also appear in [15-users-permissions-migration](doc2-ordering-processes/06-processes/15-users-permissions-migration.md).
- **Diagrams** are images. Their content is not searchable as text; open the PNG when a chapter depends on one.

## 8. How to search

- By code: `grep -rn "A03-07" .` The definition is the table row whose first cell is the code.
- By Hebrew term, as it appears in the DD: `grep -rn "יום פעילות" .` Salesforce API names appear in English inside the Hebrew text (`Opportunity`, `GoMeddo Reservation`).
- **Before writing "not in the DD" or "missing", search all three folders.** Several past "missing" claims turned out to be in another chapter.

## 9. Rules for every task

- No deployment to the org. Every Claude Code prompt ends with: "Do not deploy to the org. Generate the metadata and show it for approval."
- Do not invent fields. A field not in `mapping/` and not in the DD is a question.
- Where the DD is silent, write "question".
- Team output (tickets, sync files, this repo) is in English; Salesforce labels stay Hebrew.
- No em-dashes in any output.

## 10. File map

| File | Chapter | V/A codes | Threads awaiting Malam |
|---|---|---|---|
| [doc1-intro-architecture/00-front-matter.md](doc1-intro-architecture/00-front-matter.md) | עמוד שער ותקציר מנהלים | 0 |  |
| [doc1-intro-architecture/01-goals.md](doc1-intro-architecture/01-goals.md) | מטרות | 0 |  |
| [doc1-intro-architecture/02-current-state.md](doc1-intro-architecture/02-current-state.md) | מצב קיים | 0 |  |
| [doc1-intro-architecture/03-architecture.md](doc1-intro-architecture/03-architecture.md) | ארכיטקטורת פתרון | 0 |  |
| [doc1-intro-architecture/04-entities.md](doc1-intro-architecture/04-entities.md) | ישויות | 0 |  |
| [doc1-intro-architecture/05-configurations.md](doc1-intro-architecture/05-configurations.md) | קונפיגורציות מערכת | 0 |  |
| [doc1-intro-architecture/06-diagrams.md](doc1-intro-architecture/06-diagrams.md) | תרשימי ישויות וזרימה | 0 |  |
| [doc2-ordering-processes/00-front-matter.md](doc2-ordering-processes/00-front-matter.md) | עמוד שער ותקציר מנהלים | 0 |  |
| [doc2-ordering-processes/01-goals.md](doc2-ordering-processes/01-goals.md) | מטרות | 0 |  |
| [doc2-ordering-processes/02-current-state.md](doc2-ordering-processes/02-current-state.md) | מצב קיים | 0 |  |
| [doc2-ordering-processes/03-architecture.md](doc2-ordering-processes/03-architecture.md) | ארכיטקטורת פתרון | 0 |  |
| [doc2-ordering-processes/04-entities.md](doc2-ordering-processes/04-entities.md) | ישויות | 0 |  |
| [doc2-ordering-processes/05-screens.md](doc2-ordering-processes/05-screens.md) | מסכים מרכזיים | 0 |  |
| [doc2-ordering-processes/06-processes/00-overview.md](doc2-ordering-processes/06-processes/00-overview.md) | תהליכים עסקיים | 0 |  |
| [doc2-ordering-processes/06-processes/01-products-bundles-templates.md](doc2-ordering-processes/06-processes/01-products-bundles-templates.md) | בניית מוצרים, חבילות ותבניות יום פעילות | 16 |  |
| [doc2-ordering-processes/06-processes/02-online-self-service-order.md](doc2-ordering-processes/06-processes/02-online-self-service-order.md) | הזמנה מקוונת עצמאית, לקוח פרטי ומוסד | 18 |  |
| [doc2-ordering-processes/06-processes/03-manual-order-intake.md](doc2-ordering-processes/06-processes/03-manual-order-intake.md) | קליטת הזמנה ידנית | 17 |  |
| [doc2-ordering-processes/06-processes/04-institution-identification-routing.md](doc2-ordering-processes/06-processes/04-institution-identification-routing.md) | זיהוי, סיווג וניתוב מוסד לרכזת | 11 |  |
| [doc2-ordering-processes/06-processes/05-army-security-delegations.md](doc2-ordering-processes/06-processes/05-army-security-delegations.md) | הזמנות צבא, כוחות הביטחון ומשלחות | 16 |  |
| [doc2-ordering-processes/06-processes/06-official-visits-donors.md](doc2-ordering-processes/06-processes/06-official-visits-donors.md) | ביקורים רשמיים ותיעוד ביקורי תורמים | 0 |  |
| [doc2-ordering-processes/06-processes/07-educational-mobile-units.md](doc2-ordering-processes/06-processes/07-educational-mobile-units.md) | ניידות חינוכיות | 6 | 1 |
| [doc2-ordering-processes/06-processes/08-quote-discounts-approval.md](doc2-ordering-processes/06-processes/08-quote-discounts-approval.md) | הצעת מחיר, הנחות ואישור | 12 |  |
| [doc2-ordering-processes/06-processes/09-guide-resource-scheduling.md](doc2-ordering-processes/06-processes/09-guide-resource-scheduling.md) | שיבוץ מדריכים ומשאבים | 19 |  |
| [doc2-ordering-processes/06-processes/10-external-double-tours-distance.md](doc2-ordering-processes/06-processes/10-external-double-tours-distance.md) | הדרכות חוץ, הדרכות כפולות וחישוב מרחק וזמן | 14 |  |
| [doc2-ordering-processes/06-processes/11-physical-resources-museum.md](doc2-ordering-processes/06-processes/11-physical-resources-museum.md) | שיבוץ משאבים פיזיים, מוזיאון ותערוכות | 25 |  |
| [doc2-ordering-processes/06-processes/12-regulation-locking.md](doc2-ordering-processes/06-processes/12-regulation-locking.md) | ויסות ונעילה רב-רמתית | 18 |  |
| [doc2-ordering-processes/06-processes/13-guide-availability-service-providers.md](doc2-ordering-processes/06-processes/13-guide-availability-service-providers.md) | זמינות מדריכים וניהול נותני שירות | 18 |  |
| [doc2-ordering-processes/06-processes/14-reminders-proactive-communication.md](doc2-ordering-processes/06-processes/14-reminders-proactive-communication.md) | תזכורות ותקשורת יזומה | 18 |  |
| [doc2-ordering-processes/06-processes/15-users-permissions-migration.md](doc2-ordering-processes/06-processes/15-users-permissions-migration.md) | יוזרים, הרשאות והסבת נתונים | 0 |  |
| [doc2-ordering-processes/07-validations-security-templates.md](doc2-ordering-processes/07-validations-security-templates.md) | וולידציות, אבטחת מידע ותבניות תקשורת במערכת | 0 |  |
| [doc2-ordering-processes/08-reports-dashboards.md](doc2-ordering-processes/08-reports-dashboards.md) | דוחות ודשבורדים | 0 |  |
| [doc2-ordering-processes/09-fields.md](doc2-ordering-processes/09-fields.md) | שדות | 0 |  |
| [doc2-ordering-processes/10-forms.md](doc2-ordering-processes/10-forms.md) | טפסים | 0 |  |
| [doc2-ordering-processes/11-appendix-reports.md](doc2-ordering-processes/11-appendix-reports.md) | נספח הדוחות | 0 |  |
| [doc2-ordering-processes/12-appendix-communication-templates.md](doc2-ordering-processes/12-appendix-communication-templates.md) | נספח תבניות התקשורת | 0 |  |
| [doc2-ordering-processes/13-appendix-yv-materials.md](doc2-ordering-processes/13-appendix-yv-materials.md) | נספחי חומרי יד ושם | 0 |  |
| [doc3-events-finance/00-front-matter.md](doc3-events-finance/00-front-matter.md) | עמוד שער ותקציר מנהלים | 0 |  |
| [doc3-events-finance/01-goals.md](doc3-events-finance/01-goals.md) | מטרות | 0 |  |
| [doc3-events-finance/02-architecture.md](doc3-events-finance/02-architecture.md) | ארכיטקטורת פתרון | 0 |  |
| [doc3-events-finance/03-entities.md](doc3-events-finance/03-entities.md) | ישויות | 0 |  |
| [doc3-events-finance/04-screens.md](doc3-events-finance/04-screens.md) | מסכים מרכזיים | 0 |  |
| [doc3-events-finance/05-processes/00-overview.md](doc3-events-finance/05-processes/00-overview.md) | תהליכים עסקיים | 0 |  |
| [doc3-events-finance/05-processes/01-open-public-events-tours-shows.md](doc3-events-finance/05-processes/01-open-public-events-tours-shows.md) | אירועים פתוחים לקהל, סיורים, הצגות וחיזיון | 20 |  |
| [doc3-events-finance/05-processes/02-smartticket-event-sync.md](doc3-events-finance/05-processes/02-smartticket-event-sync.md) | פתיחת אירוע בסמארטיקט וסנכרונו לסיילספורס | 8 |  |
| [doc3-events-finance/05-processes/03-teacher-conferences-sessions-tracks.md](doc3-events-finance/05-processes/03-teacher-conferences-sessions-tracks.md) | כנסי מורים, ניהול מפגשים ומסלולים אישיים | 19 |  |
| [doc3-events-finance/05-processes/04-seminars.md](doc3-events-finance/05-processes/04-seminars.md) | סמינרים | 13 |  |
| [doc3-events-finance/05-processes/05-courses-trainings.md](doc3-events-finance/05-processes/05-courses-trainings.md) | קורסים והכשרות | 12 |  |
| [doc3-events-finance/05-processes/06-professional-development.md](doc3-events-finance/05-processes/06-professional-development.md) | השתלמויות | 10 | 1 |
| [doc3-events-finance/05-processes/07-attendance-arrival.md](doc3-events-finance/05-processes/07-attendance-arrival.md) | ניהול נוכחות בפועל וסימון הגעה | 14 |  |
| [doc3-events-finance/05-processes/08-kiosk-entry-ordering.md](doc3-events-finance/05-processes/08-kiosk-entry-ordering.md) | כניסה והזמנה בעמדות קיוסק | 20 |  |
| [doc3-events-finance/05-processes/09-almog-invoicing-billing.md](doc3-events-finance/05-processes/09-almog-invoicing-billing.md) | הפקת חשבון וחיוב לקוח מול אלמוג | 21 |  |
| [doc3-events-finance/05-processes/10-private-customer-cancel-change.md](doc3-events-finance/05-processes/10-private-customer-cancel-change.md) | ביטול ושינוי הזמנה של לקוח פרטי | 20 |  |
| [doc3-events-finance/05-processes/11-institutional-cancel-credit-refund.md](doc3-events-finance/05-processes/11-institutional-cancel-credit-refund.md) | ביטול, זיכוי והחזר ללקוחות מוסדיים | 20 |  |
| [doc3-events-finance/05-processes/12-guide-provider-payment-malam-file.md](doc3-events-finance/05-processes/12-guide-provider-payment-malam-file.md) | תשלום מדריכים ונותני שירות וקובץ מלם | 17 |  |
| [doc3-events-finance/06-reports-dashboards.md](doc3-events-finance/06-reports-dashboards.md) | דוחות ודשבורדים | 0 |  |
| [doc3-events-finance/07-forms.md](doc3-events-finance/07-forms.md) | טפסים | 0 |  |
| [doc3-events-finance/08-profiles-permissions.md](doc3-events-finance/08-profiles-permissions.md) | פרופילים והרשאות | 0 |  |
| [doc3-events-finance/09-interfaces.md](doc3-events-finance/09-interfaces.md) | ממשקים | 0 |  |
| [doc3-events-finance/10-appendix-registration-handling-fees.md](doc3-events-finance/10-appendix-registration-handling-fees.md) | נספח דמי ההרשמה והטיפול | 0 |  |
| [doc3-events-finance/11-appendix-finance-interface-endpoints.md](doc3-events-finance/11-appendix-finance-interface-endpoints.md) | נספח נקודות הקצה של ממשק המערכת הכספית | 0 |  |
| [doc3-events-finance/12-appendix-yv-materials.md](doc3-events-finance/12-appendix-yv-materials.md) | נספחי חומרי יד ושם | 0 |  |
| [doc3-events-finance/13-appendix-legacy-account-screens.md](doc3-events-finance/13-appendix-legacy-account-screens.md) | נספח דוגמאות מסכי החשבונות במערכת הקיימת | 0 |  |
| [doc3-events-finance/14-appendix-entity-legend.md](doc3-events-finance/14-appendix-entity-legend.md) | נספח מקרא ישויות | 0 |  |
