# Object: B25__Reservation__c

## Salesforce Object Details

| Property | Value |
|----------|--------|
| Object Label | Reservation (GoMeddo managed object) |
| API Name | B25__Reservation__c |
| Status | Draft |

Business process: Doc 2 process 11, physical resources, museum and exhibitions (`dd/doc2-ordering-processes/06-processes/11-physical-resources-museum.md`). The reservation (מפגש) holds a resource for a date and time. Legacy entities: `new_event_item` (booking kind, hold reason, requester) and `new_reservation` (cancellation reason).

## Field Mapping

| Salesforce Label | Salesforce API Name | Translation | Field Type | Picklist Values | שם השדה במערכת הישנה | Old API Name | Unique | Section | Questions | Task | Status |
|-----------------|--------------------|-------------|------------|------------------|----------------------|--------------|--------|---------|-----------|------|--------|
| Status | B25__Status__c | סטטוס | Lookup(B25__Reservation_Status__c) | | | | | | | Process 11 | Exists |
| Quantity | B25__Quantity__c | מספר משתתפים | Number | | | | | | | Process 11 | Exists |
| Need Refreshments | Need_Refreshments__c | נדרש כיבוד | Checkbox | | | | | | | Process 11 | Exists |
| Need Arrangement | Need_Arrangement__c | נדרש שינוי סידור | Checkbox | | | | | | | Process 11 | Exists |
| Notes | B25__Notes__c | הערות | Long Text Area(32768) | | | | | | | Process 11 | Exists |
| Opportunity | B25__Opportunity__c | הזמנה | Lookup(Opportunity) | | | | | | | Process 11 | Exists |
| Booking kind | Booking_Kind__c | סוג שיבוץ | Picklist | שיבוץ;שריון;שיבוץ ליום שלם;שמירת מקום ללא הזמנה;איחוד משאבים | סוג שיבוץ | new_event_type | | שיבוץ משאבים | [Implementer] Room values only, default שיבוץ. GoMeddo reservation types hold audience types and are not used. | Process 11 | Built |
| Hold reason | Hold_Reason__c | סיבת השריון | Text(255) | | סיבת השמירה | new_reason_for_saving | | שיבוץ משאבים | | Process 11 | Built |
| Requested by | Requested_By__c | מבקש השריון | Text(255) | | מבקש השמירה | new_requesting_the_save | | שיבוץ משאבים | | Process 11 | Built |
| Cancel reason | Cancel_Reason__c | סיבת ביטול | Picklist | מס' משתתפים מועט;מזג אוויר;הקפצה;ביטול ביום הביקור;מצב בטחוני;קורונה;שביתה;הזמנה כפולה;הזמנה מהאינטרנט;נדחה למועד שטרם נקבע;הפך להשתלמות;נסיון;אחר;בוטל ע"י המזמין;העברה לאגף אחר;צהל- שינויי לוז;צהל- היסעים;נדחה לתאריך אחר;מלחמה;הפגנה | סיבה לביטול הפעילות | new_cancellation_reason | | שיבוץ משאבים | [Implementer] New global value set `Cancel_Reason`; `Opportunity.CancellationReason__c` has the same 20 values as local values. | Process 11 | Built |
| Usage mode | Usage_Mode__c | אופן שימוש | Picklist | כיתה;כניסה למוזיאון | | | | שיבוץ משאבים | [Implementer] From DD process 11 step 5 and V11-08; no legacy field. | Process 11 | Built |
| Special arrangement | Special_Arrangement__c | סידור מיוחד | Checkbox | | | | | שיבוץ משאבים | [Implementer] DD process 11 entities table lists it apart from Need_Arrangement__c. | Process 11 | Built |

### Proposed Lightning Page section grouping

The new fields go into one new section **שיבוץ משאבים** on the current record page `Reservation_Record_Page1`. The existing sections and the GoMeddo recurrence editor stay as they are. Deployed to `yv-partial` on 11.11.2026; the page is live for the profiles it is assigned to in the Sales and Sales Console apps.

## Relationship Fields

None added by this task.

## Test records

## Automations
