# Sync: DM - Mikey

Status only. Disagreements, questions and needs from other epics go to GitHub Issues (see WORKFLOW.md).

Brief version I worked from: V4

## Ticket status
| Key | Status | Category | Est. h | Actual h | Review min | Rework h | Fixed by hand | Components actually built |
|---|---|---|---|---|---|---|---|---|
| Staff filter on Availability Month (phone with Lior and Neev, 18.11, not in Jira) | Code ready, not deployed | Automation | | | | | | GS_StaffFilterLogic_AvMonth and its test (new). GS_StaffFilterLogic (Neev) untouched, backed up in the branch |
| Availability step 1 (Lior, not in Jira) | Built in yv-partial | Data model | 2 to 3 (Lior's brief) | not logged | | | Admin Malam app tabs added by hand | Special_Day__c, Availability_Month__c, flow B2_Availability_Month_Set_Key, 2 layouts, 2 tabs, Hebrew translations, permission set and System Administrator FLS |
| A13-01 Availability Month builder (phone with Lior and Neev, 18.11, not in Jira) | Built in yv-partial | Automation | | | | | Button on list view, permission set ticks, Commitment__c delete and erase, Hebcal permission set assignment, schedule | Staff__c, Day_01_Note__c..Day_31_Note__c; A13_AvailabilityMonthBuilder, A13_AvailabilityMonthJob, A13_AvailabilityMonthSchedule, test class, LWC a13AvailabilityMonthBuilder, flow A13_Build_Availability_Months, button Build_Months |
| Staff filter on Availability Month (PR #52) | Built and switched on in yv-partial; 5 form tests passed 18.11 | Automation | | | | | Switch of Reservation Form Customizer Class | GS_StaffFilterLogic_AvMonth and test |

## Time log (write as you go, see PILOT-MEASUREMENT.md)
| Date | Ticket | Start | End |
|---|---|---|---|
| 18.11.2026 | Staff filter and A13-01 | not noted | |
| 19.11.2026 | A13-01 | | about 01:00 (first live run, noted afterwards) |
| 16.11.2026 | Availability step 1 | about 13:00 (Mikey's estimate, noted afterwards) | |
| 17.11.2026 | Availability step 1 | | 10:56 (last deploy) |
| 18.11.2026 | Staff filter on Availability Month | about 12:00 (not noted at the start) | |

Start time was not noted during the session; Mikey gave it on 17.11. Deploys to yv-partial ran 10:45 to 10:56 on 17.11.

## Incident, 17.11.2026 (fixed)
A Workbench deploy of `YadVashem_Order_Permission_Set` holding only the new objects replaced the whole permission set at 10:48. It removed 58 field permissions (Reservation, Resource, YV Location, Skill, Staff Skill, Budget Framework, Department, Section, Bundle Item Entry, Activity Day Subject, Price Book, Price Book Entry, Product), Activity Day Subject object access and the four Bundle record types. Restored at 10:55 from the Setup Audit Trail, each at its earlier level; the audit trail of the restore matches the removal one for one. Cause: the permission set was not retrieved from the org before the deploy (Data Migration conventions, Anna #13). The full current file is now in `tickets/Data Migration/metadata/Availability_Month__c/main/default/permissionsets/`. If you lost access to a field between 10:48 and 10:55, that was this.

## Requests for Lior or Anna
- Record in `knowledge/model-register.md` the availability model agreed with Lior and Neev: Availability Month is one record per guide per month, looks up the guide's GoMeddo Availability (commitment), and holds per day a status (global value set Availability_Day_Status), a start time and a duration (global value set Shift_Duration, 3 to 9 hours); Year and Month are text fields. It replaces the DD's per-day "Guide Availability" object and the brief's month-settings object. Details in `tickets/Data Migration/15_Availability_Month__c.md`.
- [Lior] One time range per day now (start and duration). The DD allows two ranges per day. Is one enough?

## Change, 17.11.2026 evening
Availability Month rebuilt: day picklists moved to global value sets, start time and duration per day added, Month changed from Date to Text, Year added. Old day fields and Month were deleted and erased in Setup; the test record AVM-00000 was deleted, and the org recycle bin was emptied (275 records, everyone's). The permission set was redeployed in full, including Anna's O1_Order_Defaults custom permission; checked afterwards, nothing lost.

- Staff filter step 4 rule, agreed 18.11.2026: available if the guide's Availability Month day is זמין, מחוייב or משובץ (משובץ: the GoMeddo reservations in step 5 decide) and Start plus Duration covers the reservation's hours. No record, other status, or no hours: not available. Please add to `knowledge/model-register.md` with the Availability Month entry.
- Model register: Availability Month now belongs to the guide (Staff__c); the commitment is only one source. Commitment__c was deleted (18.11.2026).
- Open Jira tasks for the staff filter and for A13-01 (both agreed by phone, 18.11.2026).

## Blocked
- Questions to Neev (day one of the brief): waiting for the question list from Lior's earlier message.

## Open Issues I raised
- #59 GS_StaffFilterLogic skips availability in the open form (for Neev)
- #61 Availability summary for Lior, Neev and Tal
