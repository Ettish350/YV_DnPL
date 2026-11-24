# Epics: what they are, who owns them, and when they are built

Version 2, 12.10.2026 (sections 3 and 4 updated for D-029 and D-030; the epics themselves are unchanged from version 1). **Proposal.** Owners review it with their own Claude against the DD and this repo and challenge anything, through a challenge Issue.

Every epic below has two descriptions:
- **For people**: what it means for Yad Vashem, in plain words.
- **For Claude**: which DD chapter it comes from, the main objects, the codes, what it depends on, what is deferred.

---

## 1. What the letters mean

The letter is the **area one person owns**, from the spec to a working demo. The number is the order inside that area.

| Letter | Area | Owner | In plain words |
|---|---|---|---|
| **A** | Catalog and the order | Anna | What Yad Vashem sells, what it costs, and the order a coordinator captures |
| **B** | People and resources | Niv | Who delivers the activity, when they are available, and which rooms and equipment are used. Niv is also the architect for the whole team |
| **C** | Channels | Or Rozner | How an order reaches us from outside: institutions, the army, the website and the kiosk |
| **X** | Cross-cutting | Split at the end | Permissions, reports, migration, reminders. These are not processes, so they are not really epics |
| **L0** | Foundations | Shared | Environment, users, stub objects, and the approved field mapping |

Earlier drafts used A, B and C as dependency clusters rather than ownership. That is why manual order intake was once called C1. It is now A3, because it sits on the order.

### Old code to new code

| Old | New | Epic |
|---|---|---|
| C1 | **A3** | Manual order intake, groups and schedule |
| A3 | **A4** | Quote, discounts and approval |
| C5 | **A5** | Regulation and locking |
| C2 | **C1** | Institution identification, classification and routing |
| C3 | **C2** | Army, security forces and delegations |
| C4 | **C3** | Online ordering and kiosk |

A1, A2, B1 to B5 and X1 to X5 are unchanged.

---

## 2. The epics

### L0: foundations (shared, before sprint 1)
**For people:** the groundwork everyone needs: a login for each builder, the approved list of objects and fields, and empty placeholder objects so nothing waits on another area.
**For Claude:** approve `mapping/entities.md` and the 54 flagged rows; create stubs for the 11 out-of-slice lookup targets; one Salesforce user per builder; freeze one DD version. See brief section 5.2.

### A1: products, bundles and activity day templates (Anna)
**For people:** the catalog. A catalog manager creates a product (a tour, a testimony, a room, transport), prices it, decides which channels show it, and publishes it. Nothing can be ordered before this exists.
**For Claude:** Doc 2, chapter "בניית מוצרים, חבילות ותבניות יום פעילות". Product2, Bundle, YV_Location__c, Product_Subject__c, Tag__c, Activity_Window__c, Coupon__c. A01-01 dynamic forms, A01-11 and A01-12 clone, V01-02 and V01-03. Open findings F1 to F6 in `knowledge/A1-binding-rules.md` must be answered first.

### A2: price books, ticket types and discount eligibility (Anna)
**For people:** the same product costs different amounts for a school, a soldier or a private visitor. This is where those price lists live, along with who is entitled to a discount.
**For Claude:** Doc 2, "בניית מוצרים" (price book validity) and the entities chapter. Price Book, Price Book Entry, ticket types, discount eligibility, coupons. Price is product times budget framework: the budget framework carries the price book. Codes assigned at kickoff from the chapter tables.

### A3: manual order intake, groups and schedule (Anna, was C1)
**For people:** a coordinator takes an order by phone or email: who is coming, on what date, how many groups, and the timetable for the day. This is the heart of the module.
**For Claude:** Doc 2, "קליטת הזמנה ידנית". Opportunity, GoMeddo parent reservation (activity day), child reservations (schedule lines), Campaign (group), attachments. A01-02 bundle to order lines, A01-03 schedule lines from the day subject, A01-05 quantity by pricing method, A01-07 location change, A01-10 activity type on the order. Depends on L0 only, and blocks A4, B3 and C2.

### A4: quote, discounts and approval (Anna, was A3)
**For people:** the price offer the customer receives, the discounts on it, and the approval that turns it into a confirmed order.
**For Claude:** Doc 2, "הצעת מחיר, הנחות ואישור". Quote lines, discount pairs (A01-06), approval. **No invoicing and no Almog**: those are Doc 3, see `backlog/doc3-dependent.md`. Depends on A2 and A3.

### A5: regulation and locking (Anna, was C5)
**For people:** Yad Vashem cannot take unlimited visitors. Museum entries, guided tours and opening hours have caps, and some dates are closed. This enforces them.
**For Claude:** Doc 2, "ויסות ונעילה רב-רמתית". Hour regulation, activity windows, closures. The numeric values are configuration supplied by YV, not spec. Depends on A3 and A1.

### B1: service provider card, certifications and statuses (Niv)
**For people:** the guide's file: who they are, what they are certified to guide, in which languages, from which date, and whether they have a signed contract.
**For Claude:** Doc 2, "זמינות מדריכים וניהול נותני שירות" and the service provider card screen. Contact with the Guide record type, Service_Provider_Status__c (dated), Certification__c and its junction, Contract_Days__c, Pickup_Station__c. V13-03, V13-05, V13-07 (fields only). A13-08 is deferred, it needs Registration__c from Doc 3. See `knowledge/B1-binding-rules.md`.

### B2: monthly availability and hour regulation (Niv)
**For people:** every month guides say which days they can work. This collects that and feeds the assignment engine.
**For Claude:** same chapter as B1. Monthly availability form (ID number login), availability records, absences (sickness, vacation, reserve duty as zero-hour assignments), guide hour regulation. Depends on B1.

### B3: guide and resource assignment (Niv)
**For people:** putting the right guide on the right group, with the right room. The most complex logic in the module.
**For Claude:** Doc 2, "שיבוץ מדריכים ומשאבים". Assignments on GoMeddo, filters by availability, certification and language, D01 (an assignment is never released automatically), V01-05, V13-06, contract-day indication. Depends on B2 and A3.

### B4: external tours, double tours, distance (Niv)
**For people:** guiding outside Yad Vashem, two tours in one day, and the travel time between them.
**For Claude:** Doc 2, "הדרכות חוץ, הדרכות כפולות וחישוב מרחק וזמן". D02: double tours are allowed, including over eight hours. Google Maps for distance. Depends on B3.

### B5: physical resources, museum and exhibitions (Niv)
**For people:** rooms, halls and the museum itself: what is free when, and who booked it.
**For Claude:** Doc 2, "שיבוץ משאבים פיזיים, מוזיאון ותערוכות". GoMeddo Resource, capacity, V01-06 (a resource tied to a location). Unlike guides, physical resources are released automatically. Depends on B3.

### C1: institution identification, classification and routing (Or, was C2)
**For people:** an order arrives from a school. The system recognizes the institution, classifies it, and routes it to the right coordinator, instead of someone knowing it by heart.
**For Claude:** Doc 2, "זיהוי, סיווג וניתוב מוסד לרכזת". Account, institution characteristics, Department__c, routing. Depends on L0. Feeds C2 and C3.

### C2: army, security forces and delegations (Or, was C3)
**For people:** the army orders differently: a purchase order, its own budget, its own templates.
**For Claude:** Doc 2, "הזמנות צבא, כוחות הביטחון ומשלחות". Purchase order, budget frameworks for security bodies, templates by age group. Depends on A3 and C1.

### C3: online ordering and kiosk (Or, was C4)
**For people:** a customer orders on the website by themselves, pays, and gets a confirmation. Plus the kiosk at the entrance.
**For Claude:** Doc 2, "הזמנה מקוונת עצמאית, לקוח פרטי ומוסד". Experience Cloud, guest ordering, price from the budget framework, Pelecard payment, Titan forms, kiosk display fields. The most connected epic: depends on A3, C1 and A2. Registration flows are deferred to Doc 3. Experience Cloud design decisions are open with the Malam architect: raise them in sprint 1.

### X1 to X5: cross-cutting (split at the end)
**For people:** the things that touch everything: reminders to customers, reports and dashboards, duplicate checks and the Hebrew calendar, permissions, migrating the old data, and official visits.
**For Claude:** X1 reminders and communication (invoice-based reminders deferred to Doc 3), X2 reports and dashboards, X3 cross-cutting validations and the Hebrew calendar, X4 permissions and migration of the 24 marked tables, X5 official visits and educational mobile units. They depend on the business epics and are built in sprints 6 to 9.

---

## 3. How the sprints work

A sprint is two weeks, Sunday to Thursday (D-029). Planning is on Thursday, kickoff on Sunday, and the weekly progress report runs every Thursday at 07:30. An epic takes one or two sprints, and a sprint may hold one epic or the tail of one plus the start of the next. Sprint content, assignments and hours live in the plan workbook (SharePoint, `06 תכנון וספרינטים`, V9 from 12.10.2026); tasks live in Jira (D-028).

Each sprint every person **owns** one epic (its spec, tickets, questions and acceptance) and may also **implement** part of someone else's. Ownership of an area does not change; what you build this sprint does.

Around every epic:
1. **Kickoff (30 min), before any ticket is written.** The owner presents the DD chapter and the coverage table: every validation and automation code in the chapter, and where each one lands.
2. **Build**, in the order set in the brief: fields, screen, validations, automations, data, permissions, end-to-end test. For the data model this is two tasks per entity (section 4).
3. **Close.** The acceptance test runs in the org, the Components field of every ticket is updated, hours are logged.

The sprint calendar rules are in `plan/SCHEDULE.md`; the sprints themselves are in the plan workbook, readable in `plan/implementation-plan.md` and `plan/sprints/` (D-032).

---

## 4. Data-model tasks per entity (D-030)

Every entity or entity group in an epic gets two Jira tasks:

| Task | Done when | Hours |
|---|---|---|
| **Research**: the fields Yad Vashem sent, mapped to the new architecture | All rows of the entity's `## <source entity> -> <target> (N fields)` section are on `main` and none is still `question`. Each `question` row is a GitHub Issue | 5 min per field, minimum 1h per entity |
| **Build and page layouts**: the object if needed, the fields, and the layout or Lightning page, including Hebrew labels | Every `carry over` row with a custom API name exists in `yv-partial` after an approved PR, and the layout or Lightning page of the target object is merged and deployed | 3h per object plus 0.25h per field, less the research share, plus the layouts as planned in the workbook |

The implementer's Claude writes the mapping sections of the implementer's own entities: `high` rows in the implementer's own PR, self-merged and built right away; `review` and `question` rows in a separate PR, built only after Lior approves (D-030, D-004).
