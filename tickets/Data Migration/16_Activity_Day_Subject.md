# Object: Activity_Day_Subject__c

## Salesforce Object Details

| Property | Value |
|----------|--------|
| Object Label | Activity Day Subject |
| Plural Label | Activity Day Subjects |
| API Name | Activity_Day_Subject__c |
| Hebrew Label | נושא יום העיון |
| Owner | Anna (YV20-223) |
| Status | Per task (D-036) |
| Task | YV20-223 |

The subject of an activity day (נושא יום העיון), chosen per order. It is its own entity, separate from the Bundle and from `Product_Subject__c` (D-018, Lior's catalog model of 9.11.2026). The schedule template is linked to the subject, not to the Bundle (Doc 2, process 01, rule 4; comments c122, c143, c145, c194).

The DD does not define this entity: it has no row in the entities tables and no field list. It names only the concept "נושא יום הפעילות" as the key of the schedule template. The object and its four fields come from D-018 and the legacy file. The API name was left to Stage A by D-018 and is set here.

Legacy entity: `new_activity_day_subject` ("שמות /נושאי יום העיון", 53 records, marked for migration, code 10236). This task creates the object only. The links to it come in later tasks.

## Field Mapping

| Salesforce Label | Salesforce API Name | Translation | Field Type | Picklist Values | שם השדה במערכת הישנה | Old API Name | Unique | Section | Questions | Task | Status |
|-----------------|--------------------|-------------|------------|------------------|----------------------|--------------|--------|---------|-----------|------|--------|
| Activity Day Subject Name | Name | נושא יום העיון | Text(80) | | נושא יום העיון | new_name | | General Info | [Implementer] Standard Name is capped at 80 characters. The legacy length is not in the fields file, so this is unconfirmed against the 53 records. | YV20-223 | Built |
| Description | Description__c | תקציר | Long Text Area(32000) | | תקציר | new_description | | General Info | [Implementer] The DD does not mention this field; it comes from the legacy entity and D-018. | YV20-223 | Built |
| Pedagogical objectives | Pedagogical_Objectives__c | מטרות פדגוגיות | Long Text Area(32000) | | מטרות פדגוגיות | new_pedagogical_objectives | | General Info | [Implementer] The DD does not mention this field; it comes from the legacy entity and D-018. | YV20-223 | Built |
| Student preparation | Student_Preparation__c | סיכומים לגבי הכנת התלמידים | Long Text Area(32000) | | סיכומים לגבי הכנת התלמידים | new_student_preparation | | General Info | [Implementer] The DD does not mention this field; it comes from the legacy entity and D-018. | YV20-223 | Built |
| Legacy ID | Legacy_ID__c | מזהה מערכת ישנה | Text(255), External ID | | | | | System Information | [Implementer] Added because every object gets `Legacy_ID__c` for the data load (00-CONVENTIONS, build rules). Not Unique, since no rule asks for it. | YV20-223 | Built |

### Proposed Lightning Page section grouping

- **General Info:** Name, Description, Pedagogical objectives, Student preparation.
- **System Information:** Legacy ID, Created By, Last Modified By.

Record page: `Order-Activity Day Subject- Record Page` (API `Order_Activity_Day_Subject_Record_Page`), assigned to System Administrator in the Orders app. The object has a custom tab (`Activity_Day_Subject__c`, created in the org by Anna) and is in the Orders app navigation. Read, create and edit for the object and its fields are in `YadVashem_Order_Permission_Set`; System Administrator has full access through `Admin.profile-meta.xml`.

## Relationship Fields

None built in this task.

### Relationships, later

Two legacy lookups point to this entity. They are not built here, and no field is added to Bundle__c, Opportunity, B25__Reservation__c or any other existing object.

| Legacy field | Legacy entity | Old API Name | Meaning | Where it lands |
|---|---|---|---|---|
| נושא יום העיון (שם/נושא יום העיון) | new_activity_day | new_activity_day_subject | The subject of the activity day | The activity day (GoMeddo parent reservation), later task |
| נושא הארוע | new_event_reservations | new_event_subject_lu | The subject of an event (Doc 3 events) | The Doc 3 event reservation, later task |

Also later, in package 1: the schedule template line object (bundle plus subject). Not built here.

Related legacy field, not a lookup to this entity: `new_activity_day.new_activity_day_subject_other` ("שם/נושא יום העיון - אחר", free text for a subject outside the list). No task has needed it yet, so it is not listed.

## Test records

Created by hand in `yv-partial` by Anna, task YV20-223. Test data, not catalog subjects: the legacy file holds no records, so the content was written for testing and the Legacy ID carries a `TEST-` prefix.

| Record Name | Id | Task |
|---|---|---|
| בדיקה - הנוער בשואה, עדויות ומשמעות | a2IWm0000004zCzMAI | YV20-223 |

## Automations

None.
