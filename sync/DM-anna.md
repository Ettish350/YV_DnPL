# Sync: Data Migration - Anna

Status only. Disagreements, questions and needs from other epics go to GitHub Issues (see WORKFLOW.md).

Brief version I worked from: V4

## Object status
| Object file | Status | Note |
|---|---|---|
| 01_Product2.md | Approved, merged (PR #19, 11.11.2026) | Task and Status columns on every row (32 Built, 2 Not built). PR #1 closed. |
| 02_Bundle.md | Draft, Stage A, waiting for Lior | 77 rows. Questions for Lior in `02_Bundle-questions-for-Lior.md`: 73 in total, 5 answered by Anna (D1, D3, D7, D8, D9), 68 open. Stage B must not start. |
| 05_Activity_Day.md | Draft, Stage A | 58 rows (54 legacy fields, one merge, plus 5 DD or org fields). 13 cross-field open questions. Pull request to Lior not yet opened (gh is not installed here). |

## Time log (write as you go, see PILOT-MEASUREMENT.md)
| Date | Ticket | Start | End |
|---|---|---|---|
| 13.10.2026 | Data Migration: Activity Day, Stage A | about 14:00 | 14:45 |
| 12.11.2026 | YV20-221: Bundle record types and 4 Product2 fields (PR #44) | n/a | about 2 hours in total (Anna) |
| 16.11.2026 | YV20-223: Activity Day Subject object, 4 fields, page, test records | n/a | (hours to be filled in, Anna) |
| 17.11.2026 | YV20-226: O1 order defaults and budget framework rule (impact check, metadata in `metadata/O1/` deployed to yv-partial, PR #50, Done) | 15:18 | n/a; about 4 hours in total (Anna) |
| 18.11.2026 | YV20-227: O2 department on the order and budget framework filter (impact check, metadata in `metadata/O2/` deployed to yv-partial, tested, Jira comment posted) | about 11:10 | about 5 hours in total (Anna) |
| 18.11.2026 | YV20-228: O3 order number with the department prefix (OrderNumber__c formula, Order_Prefix__c required, deployed to yv-partial, tested) | about 15:45 | about 2 hours in total (Anna) |
| 18.11.2026 | YV20-229: O4 institution required for a school order (`Department__c.Institution_Required__c`, validation rule, `metadata/O4/`, deployed to yv-partial; question Issue #56) | n/a | about 1 hour in total (Anna) |
| 18.11.2026 | YV20-230: O5 online product requires notice time, minimum days and booking limit (`Product2.Online_Notice`, `metadata/A1/`, deployed to yv-partial, tested) | about 16:55 | about 1 hour in total (Anna) |
## Blocked
- 05_Activity_Day.md: no Lior answers yet; Stage B must not start.

## Open Issues I raised
- #56 (question, YV20-229): Account is required on the order page, against the per-department institution rule; bypass per order.

## Notes for Lior
- `05_Activity_Day.md` also changes references in `02_Bundle.md` and `04_Activity_Day_Subject.md` (file name `04_Activity_Day.md` became `05_Activity_Day.md`).
- Per D-029 the order and the activity day are Niv's entities; confirm the Data Migration file for this entity stays with this track.
- YV20-226 (O1), for the data migration: the validation rule `Budget_Framework_Required` (Order cannot be Closed Won without `Budget_Framework__c`) also blocks loading legacy orders that are already Closed Won. The rule must be deactivated before the order load and activated again after it (or the load must set a budget framework on every closed order). Please plan this in the migration task. Anna, 17.11.2026.
