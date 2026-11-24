# A1: binding rules from Doc 2 V9 that are easy to miss

Source: Doc 2 V9, chapter "בניית מוצרים, חבילות ותבניות יום פעילות" (file `07-...` from `scripts/dd_to_text.py`) and chapter "ישויות". V9 already contains the decisions of the 14.9 meeting with Yad Vashem; many of them sit in resolved comments, so read the chapter with comments.

## Rules

| # | Rule | Where in V9 |
|---|---|---|
| R1 | Two catalog levels. Product = anything orderable (tour, testimony, lecture, room, transport, food). Bundle = a set of products at a defined price, split into order lines when ordered | chapter intro, para 2 |
| R2 | **Activity type is a field on the Bundle. The Bundle is not the activity type.** "אין להשוות חבילה לסוג פעילות" | intro para 2; entity table rows "חבילה"; implementation spec step 2 |
| R3 | **The activity day template is linked to the day's subject (נושא), not to the Bundle.** The Bundle decides the order and quote lines; the subject's template decides the schedule lines. The template can be edited inside an order without changing the original | process steps 4 and 8; entity table row "תבנית יום פעילות"; chapter "ישויות", section "מבנה ההזמנה" |
| R4 | The subject is an entity, not a picklist (14.9: about 344 subjects, each linked to a department, each with its own schedule template). Choosing a subject is optional. Which Dynamics entity this is: see F3 | 14.9 decisions; process step 8 |
| R5 | The product is the scheduling rules engine: which provider status may deliver it, where, and which resource it needs. A product without a location can be used anywhere | para 3 |
| R6 | **No coordinator or owner on the product or product type.** The coordinator is linked to the order only | para 3; product row in entity table |
| R7 | Product fields named by V9: product type, display channels (internal, online, kiosk), classification (requires guide), allowed provider status, resource type, location, pricing method (one-off / per group / per participant), available languages, minimum notice time, minimum business days for online, status (draft / published), recurring or one-time | entity table, product row |
| R8 | A recurring product (e.g. Holocaust Day tour, Friday tour) is one product with dates added, never a new product per year. A price change adds a price book. "Recurring" is unrelated to online display | product row |
| R9 | Only products and bundles for the online channel go through approval (catalog manager), draft to published. Internal-only products are usable at once | step 7; implementation spec |
| R10 | Required fields depend on product type and channel, they are not uniform. A product without a price book needs no pricing fields. A product marked for kiosk shows the kiosk fields | implementation spec step 1 |
| R11 | Gefen is information only: not on the Bundle, no automation (A01-08 cancelled), no effect on price or choices. Only effect: shown on documents and passed to Almog invoice notes (A01-04, in A3) | step 3; A01-04; A01-08 |
| R12 | Every product carries a price value. Free product: price 0 only. Paid product priced 0 on purpose: saved with an indicator, not blocked (V01-03) | V01-03 |
| R13 | Clone product and clone activity type copy all conditions and links; the new record is created only on save, not on click (A01-11, A01-12) | A01-11, A01-12 |
| R14 | Price = product x budget framework (the budget framework carries the price book). Audience and location are conditions of the budget framework, not of the product | para 4 (built in A2/A3) |

## Where this conflicts with the current mapping (open, needs Lior and Niv)

| # | Finding | Affects |
|---|---|---|
| F1 | `new_tour_type` (88 fields) is mapped 1:1 to Bundle. R2 says the Bundle is not the activity type, and R3 says the template is not the Bundle either. Which target holds the tour-type fields needs a decision | mapping/entities.md, A1-02, D-007 |
| F2 | V9 is internally inconsistent: the entity map row says "activity day template = the Bundle with its items", while the process chapter (R3) says schedule lines come from a template linked to the subject, not from the Bundle | brief section 2, A1-02 |
| F3 | **Which entity is "the subject" is not settled.** Dynamics has two: day topics `new_activity_day_subject` ("שמות/נושאי יום העיון", 53 records; name, summary, pedagogical objectives, student preparation; linked from the activity day; marked for migration; **not** in the A1 source list) and product topics `new_product_subject` ("נושאים למוצר", 344 records, name only, linked from the schedule item; in A1). The 14.9 count "about 344" matches the product topics; V9 ties the template to the **day** subject. The template itself has no Dynamics source. Question for Niv | A1 scope, A1-03 |
| F4 | Whichever entity is the subject, R4 says it stays an entity: the "could be a picklist" option on `Product_Subject__c` is closed | mapping/entities.md |
| F5 | Product2 fields required by R7 that are **not** in the source mapping: available languages, status (draft / published), recurring or one-time, display channels as one field (source has separate website / counter / front-desk checkboxes), product type, minimum notice time, location | A1-01, A1-05 |
| F6 | `new_gefen_type` is mapped onto Bundle. R11 says Gefen is not on the Bundle | mapping/A1-field-mapping.md row "סוג הגפן" |

Fields the DD requires but the source lacks are allowed: the rule "do not invent fields" means not in the mapping **and** not in the DD.

## Still open with Yad Vashem (unresolved comments in V9, and 14.9 open items)

- #95: sync with SmartTicket for products. Which system is master?
- #98: which settings live on the product and which on the Bundle (also 14.9 open item).
- #121: mandatory, paid optional and free optional products, split by channel.
- #160: who is the "catalog manager".
- #162: fields should not be mandatory; depends on whether the product has a price book.
- 14.9 open items: default template for an order without a subject; final wording of the zero-price validation (V01-03); publishing mechanism for recurring products with the digital team.

Build around these as "question" until answered. Do not decide them.
