# Build schedule, Doc 1 + Doc 2

**Version 4, 12.10.2026. Epic codes follow `plan/EPICS.md`.** From the sprint of 13.10.2026 the master for sprint content, assignments and hours is the plan workbook (edited on SharePoint, `06 תכנון וספרינטים`; the approved version is in `plan/workbooks/`, readable as `plan/implementation-plan.md` and `plan/sprints/`), one sprint tab per sprint (D-028, D-029, D-032). Tasks are in Jira (YV20). This file keeps the calendar rules, the epic order and the risks; the sprint outline below is the V3 proposal and is reference only.

Rebuilt after every sprint from actual hours (`PILOT-MEASUREMENT.md`).

## Team

Epic codes and what each one covers: `plan/EPICS.md`.
Three full-time people. Each sprint everyone has one epic they **own** (spec, tickets, decisions) and may also **implement** part of someone else's epic. Ownership rotates by sprint.

Who is who, with GitHub and Jira names and this sprint's work: `plan/TEAM.md`.

| Person | Main area |
|---|---|
| Anna | Area A: catalog, pricing and the order itself (A1 to A5). Delivery lead: creates the Jira tasks |
| Niv | Area B: service providers, assignment and resources (B1 to B5). Also architect |
| Or Rozner | Area C: channels, institutions, army, online and kiosk (C1 to C3) |
| Tal Arviv | Portal (Experience Cloud): site, design, RTL, portal permissions, booking components |
| Lior | Reviews, approves, decides. Merging a pull request is the deploy approval |

## Calendar
- Israeli working week, Sunday to Thursday. Sukkot 26.09 to 03.10: decisions and reading only.
- **Sprints are two weeks, Sunday to Thursday** (D-029). Thursday morning Lior and Anna finalize the next sprint tab; Thursday afternoon Anna opens the approved tasks in Jira and holds a 30-minute team review; Sunday is kickoff.
- **Weekly progress report: every Thursday 07:30**, before the planning meeting (`scripts/weekly/`).
- Current sprint, an exception: Tuesday 13.10 to Thursday 22.10.2026. Next planning and team review: Thursday 22.10. Next sprint: Sunday 25.10 to Thursday 05.11.2026.
- Until 12.10.2026 sprints were 1.5 weeks; the V3 outline below still uses those dates.
- Hanukkah, 04.12 to 12.12, is a normal working period but school holidays cut availability. Treat sprint 7 as a lighter sprint.

## Rule: nothing waits for Doc 3
Doc 3 (events and finance) is still being written and approved. No epic in this plan may block on it.
- A ticket whose acceptance needs a Doc 3 object goes to `backlog/doc3-dependent.md` and the epic closes without it.
- Known cases: B1-09 (converting a course registrant, needs Registration__c), invoice generation and Almog billing in A4, invoice-based reminders in X1, every lookup to unified events (D-008), and registration flows in the portal.
- Where the DD points at a Doc 3 object, build the field and leave the relationship out, or use a stub object with a name only. Record it in the ticket.

## Data-model work: two tasks per entity (D-030)
From plan V9 (edited 13.10.2026), data-model work is two tasks per entity or entity group: **research** (the entity's section in `mapping/`; `high` rows self-merged, `review` and `question` rows approved by Lior) and **build and page layouts** (the object if needed, the fields, and the page layouts including Hebrew labels). Field counts and field types are no longer separate tasks.
Hours: 3h per object plus 0.25h per field, plus the page layouts as planned in the workbook; of the first part, 5 minutes per field (minimum 1h per entity) is research and the rest is build.

## Sprint 13.10 to 22.10.2026 (plan V9)
| Implementer | Work | Hours |
|---|---|---|
| Anna | Catalog: product, bundle and bundle item, activity day subject (research, build, layouts) | 68 |
| Niv | Bundle in the order (A01-02 flows, filters); scheduling: assignment, resource, hours regulation and closures; order and activity day (research, build, layouts) | 155.5 |
| Or | Institutions: institution and institution characteristics, department (research, build, layouts) | 33.5 |
| Tal | Portal skeleton (site, design, RTL, home page, portal permissions, parent and child institution) and first booking components | 160 |

Reviewers not yet assigned. Pricing and budget frameworks moved to the next sprint.

## Sprints (V3 outline, 23.09.2026, reference only)
Epic order and content as proposed in V3. Dates and assignments are superseded by the plan workbook.

| # | Dates | Anna | Niv | Or | What is included |
|---|---|---|---|---|---|
| 1 | 04.10 to 13.10 | **A1** products, bundles, day templates | **B1** provider card, certifications, statuses | **C1** institution identification and routing, plus portal environment | A1: catalog objects and fields, dynamic forms (A01-01), V01-02 and V01-03, clone product and clone activity type, catalog data load, permission set. B1: Guide record type, custom fields, dated statuses (V13-03), certifications, contract days, pickup stations, sensitive-field security (V13-05). C1: institution, institution characteristics, classification and routing to a coordinator; Experience Cloud site created with guest access, no business logic |
| 2 | 14.10 to 22.10 | **A3a** manual intake, the data structure | **B2** monthly availability | C1 finish, portal catalog page | A3a: order, activity day, groups, schedule lines on GoMeddo parent and child, attachments, order screen layout. B2: monthly availability form, availability records, absences, hour regulation for guides. Or: portal shows the catalog read-only from A1 |
| 3 | 25.10 to 03.11 | **A3b** intake automations | **A2** price books (Niv implements with Anna) | **C3a** online ordering, first slice | A3b: bundle to order lines (A01-02), schedule lines from the day subject (A01-03), quantity by pricing method (A01-05), location change (A01-07), activity type on the order (A01-10). A2: price books, price book entries, ticket types, discount eligibility, coupons. C3a: guest order, institution identification, order created from the portal |
| 4 | 04.11 to 12.11 | **A4** quote, discounts and approval | **B3a** scheduling engine, first slice | C3b online pricing and payment | A4: quote lines, discount pairs (A01-06), approval; **no invoicing, no Almog**. B3a: assignment records, availability and certification filters, manual assignment, D01 (assignments are never released automatically). C3b: price shown from the budget framework, Pelecard payment, order confirmation |
| 5 | 15.11 to 24.11 | **A5** regulation and locking | **B3b** scheduling engine, second slice | **C2** army, security forces, delegations | A5: museum and guiding regulation, multi-level locking, closures. B3b: suggested assignment, conflicts, V01-05 and V13-06, contract-day indication. C2: purchase order control, templates by age group, budget frameworks for security bodies |
| 6 | 25.11 to 03.12 | **X3** cross-cutting validations | **B4** external and double tours | C3c kiosk and forms | X3: duplicate checks, required fields, Hebrew calendar, closing days. B4: distance and travel time, double tours (D02), the order of hours completion. C3c: kiosk display fields (the 23 fields, if they moved here), Titan forms |
| 7 | 06.12 to 15.12 | **X5** official visits and educational mobile units | **B5** physical resources, museum, exhibitions | Portal hardening | Lighter sprint (Hanukkah). X5: official visits, VIP suspension, mobile units. B5: room and resource assignment, museum capacity, V01-06 |
| 8 | 16.12 to 24.12 | **X1** proactive communication | **X2a** reports | X2b dashboards | X1: order-status reminders and communication templates; **no invoice-based reminders**. X2: report families, daily operations report, operational views, dashboards |
| 9 | 27.12 to 05.01 | **X4a** permissions | **X4b** data migration, first half | X4c data migration, second half | X4: permission sets per role, sharing rules, field-level security; migration of the 24 marked tables, in dependency order, with row counts reconciled |
| 10 | 06.01 to 14.01 | End-to-end run, track A and C | End-to-end run, track B | End-to-end run, portal | Full path: catalog to order to schedule to assignment to regulation. Every gap becomes a ticket in this sprint |
| 11 | 17.01 to 26.01 | Fixes and rework | Fixes and rework | Fixes and rework | Buffer from the measured overrun of sprints 1 to 9 |
| 12 | 27.01 to 04.02 | Acceptance preparation | Acceptance preparation | Acceptance preparation | Test scripts, training data, the acceptance plan with Yad Vashem. Acceptance itself was priced separately in the WBS (235 hours) |

## Capacity, stated plainly
The 06.09 plan priced Docs 1+2 at 2,231 hours assuming four implementers at 30 net hours a week. This plan has three people. The V3 outline of twelve sprints of 1.5 weeks is about 18 weeks; at three people and roughly 25 net hours a week that is about 1,350 hours of build.

The gap closes only if the measured saving per category is real and holds. We will know after sprints 1 and 2. If it does not hold, the choices are a fourth implementer, less scope, or a later date. Do not present these dates outside the team before sprint 2 closes.

## Meetings

| When | What | Output |
|---|---|---|
| End of each sprint (Thursday), 90 min | Demo in the org of what closed, hours against estimate, open challenges, then each owner presents the process of their next epic from the DD | Next sprint agreed, decisions written to the repo the same day |
| Thursday morning | Lior and Anna finalize the next sprint tab, from the 07:30 weekly report | Sprint tab final |
| Thursday afternoon, 30 min | Anna: Jira tasks opened, team review of the sprint | Each implementer knows their tasks |
| Mid-sprint, 30 min | Blockers only | Blockers cleared or escalated |
| Epic kickoff, 30 min, before tickets are written | The owner presents the DD chapter and the coverage table: every V and A code and where it lands | Scope agreed before any ticket exists |
| Epic close | Acceptance test in the org, Components updated in every ticket, hours logged | Epic closed and measured |
| Architect hours, weekly, 45 min | Mapping rows marked review or question, open challenges | Decisions recorded in `decisions/` |

## What to watch

1. **Never refresh the `yv-partial` sandbox.** A refresh wipes everything built. Agree this with whoever administers the org.
2. **Yad Vashem inputs on the critical path:** price logic (product x budget framework), regulation values, closed lists for group characteristics, day subjects and their templates, active pickup stations. Ask at least one sprint before the epic that needs them.
3. **The DD is frozen.** Doc 1 and Doc 2 were approved by YV on 06.10.2026. Later changes come as change requests; log them and regenerate `dd/`.
4. **Review capacity.** Three people producing metadata that only Lior approves. Review minutes are measured per pull request; if they grow, review caps the build rate.
5. **WBS holes.** Block 1 (data model) is priced at 153 hours against an extrapolation of about 500. Block 11 (data migration) is priced at 0 against 24 tables. Sprint 9 will expose this.
6. **The portal is thin in the DD.** Doc 2 covers online ordering as a process; Experience Cloud design decisions were handed to the Malam architect and are not all settled. Or should raise them as questions in sprint 1, not in sprint 3.
