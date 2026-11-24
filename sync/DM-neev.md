# Sync: Data Migration - Neev

Status only. Disagreements, questions and needs from other epics go to GitHub Issues (see WORKFLOW.md).

Brief version I worked from: V4

## Object status
| Object file | Status | Note |
|---|---|---|
| 06_Opportunity.md | Approved (Neev, 21.10.2026). Stage B generated locally on branch data-migration/opportunity-stage-b, not pushed | Source `new_reservation`: 107 legacy rows (script count). Table now 92 rows, 25 legacy fields dropped or not loaded. Lior's answers of 21.10.2026 applied to the rows. No field question is open; approved by Neev on 21.10.2026. |

## Time log (write as you go, see PILOT-MEASUREMENT.md)
| Date | Ticket | Start | End |
|---|---|---|---|
| 18.10.2026 | Data Migration: Opportunity (Order), Stage A | about 12:50 | |

## Answers from Lior
- 21.10.2026: all PR #27 items answered (A1 to A7, B1 to B17, C3, C7, C11, C12, other drops). See "Lior's answers to pull request #27" in `06_Opportunity.md` and the V2 workbook. Changed from the proposals: B10 (order closes at approval, new Handling status), B13 (audience and age group as two fields), B14 (billing derives from Total to pay), B1 (multi-select, as Neev proposed).

## Open for Neev
- Totals settled (B14): Total price before discount and Total discount stay fields; Total to pay is a formula.
- Find out who created Desk_Name__c, Desk__c and Visit_End_Date__c (A2).
- Pull request to Anna on 02_Bundle.md: open question 18 (Connected Bundle objects), rename Legacy_System_Id__c to Legacy_ID__c, make the Bundle audience and age group lists global value sets.

## Blocked
- none

## Open Issues I raised
- none

## Notes
- Org alias `yv-partial` was set on the existing `vscodeOrg` login (same Yad Vashem partial sandbox, neevd@malamteam.com). No default org changed.
- Python 3.12 and gh installed on this machine; `gh auth login` still needed before a pull request can be opened.
- Appendix A (full field list) is not in the repo set; fields are grounded in the legacy file and the Doc 1 and Doc 2 chapters.

## Handoff for the next session (written 21.10.2026)

Read this first, then `tickets/Data Migration/06_Opportunity.md` (status Approved; section "Stage B" at the end) and pull request #27 with its comments.

### Where things stand
- **Stage A** of Opportunity (Order): approved by Neev on 21.10.2026. Branch `data-migration/opportunity-stage-a`, pushed, pull request #27 open (reviewers Lior and Anna). Lior answered every item on 21.10.2026 and the answers are applied.
- **Stage B**: generated on branch `data-migration/opportunity-stage-b`, committed locally only (last commit d99da33), **not pushed, no pull request**. Contents: `tickets/Data Migration/metadata/Opportunity/` (72 fields, 3 stub objects, 2 global value sets, translations, profile permissions, record page, proposals for existing components) and the workbook tab "הזמנה-Opportunity". Nothing is deployed to any org.
- Neev owns the order entity (D-029) and is the one who approves. Lior and Anna review in the pull requests.

### Decisions still needed from Neev before the Stage B pull request is final
1. **Address differs from institution** (`IsAddressDifferentFromInstitution__c`, type Address): drop it, now that the activity location is free text (built as Text 255)?
2. **Order number**: per-department prefix (needs a prefix field on the Department stub, then a formula) or a plain Auto Number? `OrderSequence__c` (format {0000000}, confirmed) is built; the `OrderNumber__c` formula is not.
3. **Primary contact details on the page**: `Opportunity.ContactId` has no relationship name, so the page cannot pull the Contact email or phone through it. Custom Contact lookup, or Opportunity Contact Roles? The second contact is done through `SecondContact__c`.
4. The `YV Order user` profile does not exist in `yv-partial`; it must be created (Niv or Anna, D-033) before its permissions can deploy.

### Other open tasks
- Pull request to Anna on `02_Bundle.md`: question 18 (no new junction object; `Connected_Bundle__c` already links the order to the Bundle; `Connected_Bundle_Item__c` not needed), rename `Legacy_System_Id__c` to `Legacy_ID__c`, make the Bundle audience and age group lists global value sets. Not opened yet.
- Age group (`AgeGroup__c`) is not built until those Bundle value sets exist.
- Waiting for Lior to confirm that `Connected_Bundle_Item__c` is not needed (comment on #27).
- Find out who created `Desk_Name__c`, `Desk__c`, `Visit_End_Date__c`.
- Ask the YV dev team to add `createdon` to the legacy export.
- Fill the end time in the time log above when work on the ticket stops.

### Update 21.10.2026 (later in the same session)
Decisions by Neev, applied locally (uncommitted, not pushed):
1. `IsAddressDifferentFromInstitution__c` dropped (DD has no rule for it); moved to Dropped legacy fields.
2. `OrderNumber__c` ON HOLD until each department's prefix is known; `OrderSequence__c` stays. Workbook column removed.
3. Primary contact details: empty section "Primary Contact Details" added to `Order_Opportunity_Record_Page`; Neev adds the Contact fields himself. No custom field.
4. `YV Order user` profile file removed from Stage B (profile does not exist in `yv-partial`). Per Neev, all field FLS is on System Administrator only for now; `YadVashem_Order_Permission_Set` holds object permissions (see `06_Opportunity.md`), and new fields are added to it from now on.
Workbook tab now has 83 field columns. Remaining: Bundle PR to Anna, AgeGroup, `Connected_Bundle_Item__c` confirmation, push and Stage B PR when Neev says.

### Working agreements (this session)
- Never deploy. Deploy only to `yv-partial`, only when Lior or Anna says so in the session; retrieve shared metadata (profiles, Orders app, Order record type) right before every deploy; `--dry-run` first.
- Show Neev everything before any push or pull request; a push or pull request notifies Lior.
- Neev's decisions are recorded as notes with the tag `[Implementer]` and "Per Neev (date)"; dropped fields go in "Dropped legacy fields". No em-dashes. English in the repo.
- When Neev gives a view on a field, check the DD first and say how sure I am; do not drop a DD-required field without a challenge Issue.

### Machine setup (persists)
- Org alias `yv-partial` is set on the existing `vscodeOrg` login (neevd@malamteam.com, the Yad Vashem partial sandbox). Do not use `TeamCad` (another client).
- `gh` is logged in as NeevDoronMalam. Python 3.12 is installed (run it by full path, the Windows alias does not work).
- The scripts that generated the Stage B files were in the session scratchpad and may be gone; if so, edit the files directly or regenerate by hand from the table in `06_Opportunity.md`.

### To resume
Say: "Continue Stage B for Opportunity, read the handoff in sync/DM-neev.md."
