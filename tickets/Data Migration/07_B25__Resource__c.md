# Object: B25__Resource__c

## Salesforce Object Details

| Property | Value |
|----------|--------|
| Object Label | Resource (GoMeddo managed object) |
| API Name | B25__Resource__c |
| Status | Draft |

Business process: Doc 2 process 11, physical resources, museum and exhibitions (`dd/doc2-ordering-processes/06-processes/11-physical-resources-museum.md`). The resource is the room, hall, museum, synagogue, exhibition or open space. Step 1: Yad Vashem defines the resource itself (name, type, location, capacity, opening hours and the behavior flags). Legacy entity: `new_resource`.

## Field Mapping

| Salesforce Label | Salesforce API Name | Translation | Field Type | Picklist Values | שם השדה במערכת הישנה | Old API Name | Unique | Section | Questions | Task | Status |
|-----------------|--------------------|-------------|------------|------------------|----------------------|--------------|--------|---------|-----------|------|--------|
| Resource Name | Name | שם משאב | Text(80) | | | | | פרטי משאב | | Process 11 | Exists |
| Resource Type | B25__Resource_Type__c | סוג משאב | Lookup(B25__Resource_Type__c) | | | | | פרטי משאב | | Process 11 | Exists |
| Default Capacity | B25__Capacity__c | קיבולת | Number | | | | | פרטי משאב | | Process 11 | Exists |
| Need Confirmed | Need_Confirmed__c | דורש אישור | Checkbox | | | | | אישור והתנהגות | | Process 11 | Exists |
| Can Have Refreshments | Can_Have_Refreshments__c | תומך כיבוד | Checkbox | | | | | שימוש וסידור | | Process 11 | Exists |
| Modify Arrangement Available | Modify_Arrangement_Available__c | תומך שינוי סידור | Checkbox | | | | | שימוש וסידור | | Process 11 | Exists |
| English name | Name_En__c | שם באנגלית | Text(255) | | שם באנגלית | new_name_en | | פרטי משאב | | Process 11 | Built |
| Location | Location__c | מיקום | Lookup(YV_Location__c) | | מיקום | new_location | | פרטי משאב | | Process 11 | Built |
| Comments | Comments__c | הערות | Long Text Area(32000) | | הערות | new_comments | | פרטי משאב | | Process 11 | Built |
| Default arrangement | Default_Arrangement__c | צורת סידור ברירת מחדל | Picklist | שורות;סביב שולחן;צורת ח';שורות קבוע;צורת ח' עם שולחנות;צורת ח' עם משענת יד;שורות עם משענת יד | צורת סידור - ברירת מחדל | new_arrangement | | שימוש וסידור | [Neev] GoMeddo has resource layouts (`B25__ResourceLayout__c`); should arrangements move there later? | Process 11 | Built |
| Microphone available | Has_Microphone__c | קיים מיקרופון | Checkbox | | קיים מיקרופון | new_has_microphone | | שימוש וסידור | | Process 11 | Built |
| AC after 18:00 | AC_After_18__c | מזגן אחרי 18:00 | Checkbox | | מזגן אחרי 18-00 | new_airconditioner_after_18_00 | | שימוש וסידור | | Process 11 | Built |
| Splittable | Is_Splittable__c | ניתן לפיצול | Checkbox | | האם ניתן לפיצול? | new_is_can_be_split | | אישור והתנהגות | | Process 11 | Built |
| Consolidable | Is_Consolidable__c | ניתן לאיחוד | Checkbox | | האם ניתן לאחד? | new_is_it_consolidable | | אישור והתנהגות | | Process 11 | Built |
| United resource | United_Resource__c | משאב מאוחד | Lookup(B25__Resource__c) | | משאב מאוחד | new_united_resource | | אישור והתנהגות | [Neev] `B25__Parent__c` already holds the building hierarchy and `B25__Resource_Grouping__c` links resources; is a separate lookup right? | Process 11 | Built |
| Two-mode resource | Is_Two_Mode__c | משאב דו-מצבי | Checkbox | | | | | אישור והתנהגות | [Implementer] From DD process 11 step 1 and V11-08; no legacy field. | Process 11 | Built |

### Proposed Lightning Page section grouping

Record page `Order_Resource_Record_Page` (label "משאב"), Dynamic Forms (the object supports it; the current page `Resource_Record_Page1` already uses field sections):
- **פרטי משאב**: Name, Name_En__c, B25__Resource_Type__c, Location__c, B25__Capacity__c, Comments__c
- **שימוש וסידור**: Default_Arrangement__c, Can_Have_Refreshments__c, Modify_Arrangement_Available__c, Has_Microphone__c, AC_After_18__c
- **אישור והתנהגות**: Need_Confirmed__c, Is_Splittable__c, Is_Consolidable__c, United_Resource__c, Is_Two_Mode__c
- **הגדרות GoMeddo**: the other fields of the old page `Resource_Record_Page1` (parent, description, active, timezone, layouts, entry minutes, day capacity, pricing), kept so nothing is lost
- **System Information** (standard): CreatedById, LastModifiedById, OwnerId, B25__Materialized_Path__c, B25__Booker25_Id__c

Related lists: availabilities (`B25__Availabilities2__r`), child resources (`B25__Children__r`), reservations (`B25__Reservations__r`). The GoMeddo components of the current page are kept: calendar, single calendar, occupancy.

Deployed to `yv-partial` on 11.11.2026 and activated as the org default (desktop and phone), replacing `Resource_Record_Page1`. The Sales and Sales Console apps still assign `Resource_Record_Page1` to four profiles (System Administrator, Standard, Standard User - Inquiry, Yad Vashem Profile); those assignments win over the org default and were left unchanged.

## Relationship Fields

| Field | Target Object | Matching Logic |
|--------|--------------|----------------|
| Location__c | YV_Location__c (exists in `yv-partial`) | Legacy `new_location` points to `new_activity_day_location`. |
| United_Resource__c | B25__Resource__c (self lookup) | Legacy `new_united_resource` points to `new_resource`. |

## Not in this task

Opening hours per weekday are GoMeddo availability records (`B25__Availability__c`), not fields: today only the root resource has hours (Sunday to Thursday 07:55 to 18:10, Friday 07:55 to 13:10). VIP lock (V11-10, A11-15) and the buffers (Reservation_Assign_Buffers, on the resource type) are automations, outside this task.

## Test records

## Automations
