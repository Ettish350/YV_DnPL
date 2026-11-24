# Orders module, Yad Vashem: brief for epic owners and their Claude

**Version:** V6 | **Date:** 06.10.2026 | **Owner:** Lior Pridor | **Supersedes:** V1 of this brief (Hebrew, 22.09) and the Jira plan "תוכנית Jira - אפוסים וספרינטים למסמכים 1+2" of 06.09, which is no longer valid.

Changes in V6: Doc 1 and Doc 2 replaced by the versions approved by Yad Vashem on 06.10.2026 (`sources/SOURCES.md`, changes in `sources/DD-CHANGES-2026-10-06.md`). The DD is now frozen for the build.
Changes in V5: epic codes renamed so each letter is one owner's area (`plan/EPICS.md`): old C1 is now A3, old A3 is now A4, old C5 is now A5, old C2/C3/C4 are now C1/C2/C3.
Changes in V4: master sandbox is `yv-partial` (D-016); tickets live in the repo, not Jira (D-017); build conventions (`CONVENTIONS.md`) and pilot measurement (`PILOT-MEASUREMENT.md`) added.
Changes in V3: decisions are now recorded one per file in `decisions/` (D-001 to D-015) and can be challenged through GitHub Issues; sync files are status only.
Changes in V2: English; Doc 2 reference updated to V9 (15.09.2026); field mapping moved to `mapping/`; sync moved to git.

---

## 0. Instructions for the Claude reading this

This brief exists so that every epic owner's Claude works from the same base as Lior's Claude. Read it in full before doing anything.

**Your sources, in order of precedence:**

1. **Doc 2, Ordering Processes, approved 06.10.2026.** Binding for processes, validation rules (V codes), automations (A codes) and the entity map.
2. **Doc 1, Introduction and Architecture, approved 06.10.2026.** General data model, configuration, permissions, interfaces.
3. **`decisions/`, this brief and `mapping/`.** The decision register governs; this brief explains it; the mapping applies it.

Exact file names and how to verify them are in `sources/SOURCES.md`. If your copy does not match, stop.

If this brief or a decision contradicts the DD, the DD wins. Do not decide it yourself: open a challenge Issue (section 9) and carry on with the rest.

**Rules that do not change:**

- **No deployment to the org.** Every Claude Code prompt ends with: "Do not deploy to the org. Generate the metadata and show it for approval." Deployment only with Lior's explicit approval.
- **Do not invent fields.** A field that is not in `mapping/` and not in the DD is not built. If a field seems missing, that is a question, not a decision.
- **Where the DD is silent, write "question"** and do not fill the gap from general Salesforce knowledge.
- **Language:** communication within the team, tickets, sync files and this repo are in English. Anything delivered to Yad Vashem is in Hebrew, RTL. Salesforce labels are Hebrew because the org is Hebrew.
- **No em-dashes** in any output.
- **Verify before asserting.** Before writing "not in the DD" or "missing", search the whole document.

---

## 1. What the sources really are

**The YV fields workbook** is the **Dynamics 2016 source model**, not a Salesforce design. The signs: EntityLogicalName and ObjectTypeCode columns and the `new_` prefix. Its size:

| Measure | Count |
|---|---|
| Entities | 56 |
| Fields | 1,262 |
| Lookup fields | 195 |
| Many-to-many relationships | 46 |
| Picklist values | 532 |
| Tables marked for data migration | 24 |

**So:** fields cannot be built "as is". Each entity is translated to a target object and each field to a target field, following the DD architecture. That translation is the mapping in `mapping/`.

---

## 2. Target architecture

From Doc 2, chapter "Entities", section "entity map" (מפת הישויות המשתתפות בתהליכי ההזמנות):

| Business entity | Salesforce object |
|---|---|
| Product | Product2 |
| Price book | Price Book |
| Bundle | Bundle and Bundle items |
| Activity day template | Bundle |
| Order | Opportunity |
| Activity day | GoMeddo Reservation (parent) |
| Schedule line | GoMeddo Reservation (child) |
| Resource | GoMeddo Resource |
| Staff | GoMeddo Staff |
| Service provider | Contact, Guide record type |
| Activity location | YV Location |
| Group | Campaign |
| Budget framework, department, certification matrix, contract days, coupon | Custom objects |

GoMeddo is installed in the build org, `yv-partial`, the partial copy sandbox that is the master sandbox (D-016, `sources/ORGS.md`). The GoMeddo question is settled in the DD.

---

## 3. How the epics were built

### 3.1 Principle: cut by the graph, not by the text (D-001)

Coupling between parts of the module is not in the process chapters. It is in the **lookup fields** of the fields workbook. We built a graph (entity = node, lookup = edge) and grouped entities into clusters with few links between them. Each cluster became a track, and each track got one owner.

### 3.2 Two kinds of dependency

- **Structural:** an epic needs another epic's object to exist, so it has something to point at. **Solved** by creating a stub early (an object with a name only). Does not dictate order.
- **Behavioral:** an epic needs another epic's logic to work (e.g. the scheduling engine reads availability). **Not solved** by a stub, and dictates order.

### 3.3 Structure: shared gate, 3 areas, cross-cutting

| Code | Epic | Area and owner | Wave | Depends on | Doc 2 chapter (Hebrew heading) |
|---|---|---|---|---|---|
| L0 | Environment, users, stubs and mapping gate | shared | 0 | - | - |
| **A1** | **Products, bundles and activity day templates** | A, Anna | **1, pilot** | L0 | בניית מוצרים, חבילות ותבניות יום פעילות |
| A2 | Price books, ticket types, discount eligibility | A, Anna | 2 | A1 | בניית מוצרים (price book validity), ישויות |
| A3 | Manual order intake, groups and schedule | A, Anna | 2 | L0 | קליטת הזמנה ידנית |
| A4 | Quote, discounts and approval | A, Anna | 3 | A2, A3 | הצעת מחיר, הנחות ואישור |
| A5 | Regulation and locking | A, Anna | 3 | A3, A1 | ויסות ונעילה רב-רמתית |
| **B1** | **Service provider card, certifications and statuses** | B, Niv | **1, pilot** | L0 | זמינות מדריכים וניהול נותני שירות |
| B2 | Monthly availability and hour regulation | B, Niv | 2 | B1 | זמינות מדריכים וניהול נותני שירות |
| B3 | Guide and resource assignment | B, Niv | 3 | B2, A3 | שיבוץ מדריכים ומשאבים |
| B4 | External tours, double tours, distance | B, Niv | 4 | B3 | הדרכות חוץ, הדרכות כפולות וחישוב מרחק וזמן |
| B5 | Physical resources, museum and exhibitions | B, Niv | 4 | B3 | שיבוץ משאבים פיזיים, מוזיאון ותערוכות |
| C1 | Institution identification, classification, routing | C, Or | 2 | L0 | זיהוי, סיווג וניתוב מוסד לרכזת |
| C2 | Army, security forces and delegations | C, Or | 3 | A3, C1 | הזמנות צבא, כוחות הביטחון ומשלחות |
| C3 | Online ordering and kiosk | C, Or | 4 | A3, C1, A2 | הזמנה מקוונת עצמאית, לקוח פרטי ומוסד |
| X1 | Reminders and proactive communication | cross | 5 | A3, A4 | תזכורות ותקשורת יזומה |
| X2 | Reports and dashboards | cross | 5 | all business epics | דוחות ודשבורדים |
| X3 | Cross-cutting validations, duplicates, Hebrew calendar | cross | 5 | L0 | וולידציות, אבטחת מידע ותבניות תקשורת במערכת |
| X4 | Permissions and data migration | cross | 5 | all business epics | יוזרים, הרשאות והסבת נתונים |
| X5 | Official visits and educational mobile units | cross | 5 | A3 | ביקורים רשמיים ותיעוד ביקורי תורמים; ניידות חינוכיות |

Each letter is one owner's area, from spec to working demo: A catalog and the order (Anna), B people and resources (Niv), C channels (Or). `plan/EPICS.md` explains every epic in plain words and for Claude.

### 3.4 Friction points between tracks

- **B3 depends on A3:** scheduling points at the activity day and the order. The main friction point between tracks B and C.
- **A4 depends on A3:** quote lines depend on the activity day. Both are Anna's, so this seam has no handoff.
- **C3 (online) is the most connected epic** (A3, C1, A2), so it comes late.
- **Cross-document risk:** `new_event_reservations` (unified events) has the most incoming lookups (17) and belongs to **Doc 3**. **Pilot decision (D-008): the relationship is deferred.** Do not build the object and do not create lookups to it.

---

## 4. Why A1 and B1 are the pilot (D-002)

1. **Independent of each other.** Not one lookup between them. Two owners work in parallel without waiting.
2. **At the head of their tracks.** Each is the foundation of its track, so the work is not thrown away.
3. **Different in nature.** A1 is catalog: many fields, little logic. B1 is a card with a structural change, a merge into a standard object and field security. Comparing them says more about the working method than two similar epics would.

| | A1 | B1 |
|---|---|---|
| Owner | Anna | Niv |
| Doc 2 chapter | בניית מוצרים, חבילות ותבניות יום פעילות | זמינות מדריכים וניהול נותני שירות; screen כרטיס נותן השירות |
| Source entities | 7 | 6 |
| Source fields | 144 | 86 |
| Fields actually built | about 104 (23 display fields move to C3, 18 weekday checkboxes become 3 fields, 2 dropped) | about 57 (13 map to standard Contact fields, 11 role checkboxes become 1 field, 6 weekday checkboxes become 1 field, 1 dropped) |

---

## 5. How fields are handled

### 5.1 Who does what (D-003, D-004)

1. **Claude proposes** a target object per entity and a target field per field. The proposal is in `mapping/`.
2. **The architect reviews and approves.** He does not map from scratch. He starts from rows marked "review" or "question". The rest is mechanical and can be approved as a batch.
3. **Each epic owner builds the fields of their own epic,** after the mapping is approved. There is no shared layer that builds all fields up front. This replaces the earlier "Epic 0" plan in which one team built all 230 fields.
4. **Changes to the field list** go through Lior's Claude, which regenerates `mapping/` with `scripts/build_mapping_md.py`. Do not edit `mapping/` by hand. Request the change in your sync file.

### 5.2 What the mapping gate (L0) contains

L0 is the gate both pilot epics start from:

- Approval of the entity mapping for the 13 pilot entities.
- A decision on the two structural changes (section 6).
- **Stubs** for the 11 lookup targets outside the pilot (`mapping/entities.md`).
- A separate user per builder in the org. Without it the daily agent cannot tell who built what.
- One frozen DD version for the pilot.

### 5.3 Translation rules applied by the mapping (D-013)

| Source (Dynamics) | Proposed target | Note |
|---|---|---|
| nvarchar | Text(255) | Builder narrows length from the data. No architect needed |
| ntext | Long Text Area(32000) | |
| bit / boolean | Checkbox | |
| picklist | Picklist | Values from the PICKLISTS sheet, checked against data at build time |
| int | Number(18,0) | |
| decimal | Number(18,2) | |
| money | Currency(16,2) | |
| datetime | Date, or Date/Time when the name contains time or reminder | |
| lookup | Lookup to the mapped target | Target outside the pilot becomes a stub or a deferred relationship |

**API name:** strip `new_`, PascalCase, add `__c`. Example: `new_max_participants` becomes `MaxParticipants__c`.

**Confidence:** "high" = mechanical. "review" = needs the architect. "question" = the DD does not decide.

---

## 6. The two structural changes (awaiting the architect)

### 6.1 Service provider statuses (D-005)

The source has 11 checkboxes on the service provider (`new_rsv_status_is_*`): actor, cantor, coordinator, guide, lecturer, national service, photographer, Poland guide, testimony, tour guide, YV employee.

**Proposal:** fold them into one picklist, `Status_Type__c`, on `Service_Provider_Status__c`, with a start date and an end date.

**Why:** Doc 2 defines "dated statuses" (סטטוסים מתוארכים) and their effect on scheduling. With checkboxes you cannot answer when someone stopped being a guide, which is exactly what the B3 scheduling engine asks.

**Load impact:** every active checkbox becomes a status record. The number of status records loaded must equal the number of checked boxes in the source.

### 6.2 Weekdays (D-006)

Six weekday checkboxes repeat on Product2, Bundle, Activity_Window__c and Contract_Days__c.

**Proposal:** one Multi-Select Picklist per object (`Relevant_Days__c`, `Days_Of_Week__c`, `Contract_Days_Of_Week__c`).

**Why:** simpler reports and availability formulas. The load translates six flags into values.

---

## 7. Scope: what belongs to the pilot and what does not

**Rule (D-010): a component belongs to the epic where it is enforced, not the chapter where it is written.**

### 7.1 A1

| Code | What | Belongs to |
|---|---|---|
| A01-01 | Dynamic forms when creating a product | **A1** |
| A01-11 | Clone product | **A1** |
| A01-12 | Clone activity type | **A1** |
| V01-02 | Online product requires notice time and minimum business days | **A1** |
| V01-03 | Free product only at price zero | **A1** |
| A01-02, A01-03, A01-05, A01-07, A01-10 | Act on an order | A3 |
| A01-04, A01-06, V01-04 | Invoice, discount engine, quote | A4 |
| V01-05 | Scheduling by product definition | B3 |
| V01-06 | Resource tied to a location | B5 |

**The 23 activity-type display fields** (kiosk names, price explanations, sort weights, display tab, text alignment and so on) are moved from A1 to C3, since nobody uses them in the pilot. They are marked "move to C3" in `mapping/A1-field-mapping.md`. **This is a proposal pending Lior.** If they stay, A1 grows by about a quarter.

### 7.2 B1

| Code | What | Belongs to |
|---|---|---|
| V13-03 | Status requires validity dates | **B1** |
| V13-05 | Sensitive fields (ID number, rates) not visible to everyone | **B1** |
| V13-07 | Signed agreement as a condition for scheduling | **B1** builds the fields, **B3** enforces at scheduling |
| A13-08 | Convert a course registrant into a service provider | **B1** |
| V13-01, V13-02, V13-09, A13-01 to A13-06, A13-10 | Availability form and absences | B2 |
| V13-04, V13-06, A13-13 | Enforced on a scheduling record | B3 |
| A13-07 | Weekly work schedule email | X1 |

**Of the 56 service provider fields in the source:** 13 map to standard Contact fields (first name, last name, email, phones, address, title, notes) and no parallel custom fields may be created; the 11 role checkboxes move to the status object; the full name is dropped because it is computed; 31 are built as custom fields on Contact.

(Correction to the 16.09 ticket book: it said 45 fields merge into standard Contact fields. 45 is the number of fields that stay on Contact, not the number of merges.)

---

## 8. How to build the tickets for your epic

### 8.1 Order inside an epic (same in every epic, D-011)

| Step | What | Why in this order |
|---|---|---|
| 1 | Objects and fields | From `mapping/`, after the mapping is approved |
| 2 | Page layout and list views | Without a screen there is nowhere to enter data and nothing to test against |
| 3 | Validation rules | **Before automations.** A validation added after an automation breaks it |
| 4 | Automations: record-triggered, then screen, then scheduled | A scheduled flow built first can only be tested the next day |
| 5 | Reference data load | This is where real mapping mistakes surface |
| 6 | Permission set | Otherwise you only test as admin |
| 7 | End-to-end acceptance test | An epic closes on a working path, not on a pile of components |
| 8 | Update the "Components" field of every ticket | The API names of what was actually built. Without it the daily agent cannot cross-check, and the epic does not close |

Any deviation from this order is a decision recorded in the ticket.

### 8.2 Ticket template

Each ticket has these fields, in this order:

| Field | Content |
|---|---|
| Key | `A1-01`, `B1-01`, etc. |
| Title | Short, what gets built |
| DD source | Doc 2 chapter and code (V or A), or a row in `mapping/` |
| Business context | Why it is needed, one or two sentences |
| What to build | Build description |
| Components | Planned: `objects:`, `fields:`, `validation rules:`, `Flow:`. **Updated to what was actually built** |
| Depends on | Ticket keys, or "mapping approved" / "architect approval" |
| Acceptance criteria | Numbered, testable |
| Claude Code prompt | Always ends with "Do not deploy to the org. Generate the metadata and show it for approval." |
| Human build hours | Estimate for a person without Claude |
| Actual hours | Empty, filled at close |
| Status | To Do / In Progress / Done |

Tickets are written as markdown in `tickets/<epic>/`, one file per epic (`tickets/A1/tickets.md`). The repo is the master for tickets (D-017); Jira is not updated.

Every ticket also carries a **Category** (fields/objects, layout, validation, automation, data load, permissions, testing) for the pilot measurement (`PILOT-MEASUREMENT.md`).

### 8.3 Your steps, Claude

1. Read the relevant Doc 2 chapter in full, including its validation table, automation table and implementation spec.
2. Build a coverage table: every V and A code in the chapter and the ticket or epic it belongs to. Compare with section 7. Log every difference in your sync file.
3. Compare your tickets with the baseline in 8.4. It is a starting point, not the answer. If the DD requires a ticket that is missing, add it. If a baseline ticket has no DD source, flag it.
4. Write the tickets in the 8.2 template with an hours total.
5. Never write a prompt that deploys.

### 8.4 Baseline (starting point to verify)

**A1: Products, bundles and activity day templates (Anna)**

| Key | Title | Depends on | Hours |
|---|---|---|---|
| A1-01 | Product2: catalog fields (33 source fields) | mapping approved | 6 |
| A1-02 | Bundle: activity type fields, without the display fields | A1-01 | 10 |
| A1-03 | Five supporting objects: YV_Location__c, Product_Subject__c, Tag__c, Activity_Window__c, Coupon__c | A1-01 | 5 |
| A1-04 | Weekdays as a multi-select picklist | A1-02, A1-03, architect approval | 3 |
| A1-05 | A01-01: dynamic forms on product creation | A1-01 | 6 |
| A1-06 | V01-02, V01-03: catalog validations | A1-01, A1-05 | 4 |
| A1-07 | A01-11: clone product (screen flow and quick action) | A1-01, A1-06 | 5 |
| A1-08 | A01-12: clone activity type | A1-02, A1-07 | 5 |
| A1-09 | Load catalog data (4 tables) | A1-04, A1-08 | 8 |
| A1-10 | Permission set Catalog_Manager | A1-09 | 3 |
| A1-11 | End-to-end acceptance test | A1-10 | 4 |
| | **Total** | | **59** |

**B1: Service provider card, certifications and statuses (Niv)**

| Key | Title | Depends on | Hours |
|---|---|---|---|
| B1-01 | Contact: Guide record type and the 13 standard-field mappings | mapping approved | 4 |
| B1-02 | Contact: the 31 custom fields | B1-01 | 6 |
| B1-03 | Service_Provider_Status__c and folding the 11 checkboxes; V13-03 | B1-02, architect approval | 6 |
| B1-04 | Certifications, categories and junction object (many-to-many) | B1-03 | 5 |
| B1-05 | Contract_Days__c | B1-04 | 3 |
| B1-06 | Pickup_Station__c (first find out how many of the 61,496 are active) | B1-05 | 3 |
| B1-07 | V13-05: sensitive field security | B1-02, B1-03 | 4 |
| B1-08 | V13-07: signed agreement fields (enforcement in B3) | B1-07 | 3 |
| B1-09 | A13-08: convert registrant to service provider (record-triggered flow) | B1-03, B1-04 | 6 |
| B1-10 | Load service providers (2,047) and statuses | B1-09 | 8 |
| B1-11 | Permission set Service_Provider_Manager and acceptance test | B1-10 | 4 |
| | **Total** | | **52** |

---

## 9. Working together: decisions, challenges, status

The full process is in `WORKFLOW.md`. In short:

- **Decisions** live in `decisions/`, one file each, with DD source and status. The brief refers to them by ID.
- **Challenges** to any decision, including Lior's, are GitHub Issues with the `challenge` template and evidence. Draft them, show your owner, post on approval. Do not build around a decision you think is wrong.
- **Questions and needs from other epics** are GitHub Issues with the `question` template.
- **Status** (tickets, hours, blocked) goes in your sync file.
- **The shared reviewer** comments on every pull request and posts a daily report in `reviews/`. It is advisory. Lior decides.

## 10. Open items that may affect you

| Topic | What is open | Affects |
|---|---|---|
| DD version | Settled: Doc 1 and Doc 2 approved by YV on 06.10.2026. 12 Doc 2 comment threads are still unresolved in Word (1 awaiting a Malam reply, `dd/_meta/open-comments.md`) | both |
| Two structural changes | Architect decision (section 6) | A1-04, B1-03, B1-05, B1-10 |
| 54 mapping rows to review | Architect approval | both |
| 23 display fields | Stay in A1 or move to C3 | A1-02 |
| Product_Subject__c, Tag__c, Activity_Window__c | Custom object, or picklist / Topic / GoMeddo Availability | A1-03 |
| Pickup stations | How many of 61,496 are active | B1-06 |
| new_event_reservations | Deferred to Doc 3, timing to decide | both, through lookups that are not built |
| WBS | Block 1 (data model) is priced at 153h; extrapolating from the pilot suggests about 500h. Block 11 (data migration) is priced at 0h against 24 tables to migrate | planning, not build |
