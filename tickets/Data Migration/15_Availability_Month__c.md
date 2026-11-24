# Object: Availability_Month__c

## Salesforce Object Details

| Property | Value |
|----------|--------|
| Object Label | Availability Month (חודש זמינות / חודשי זמינות) |
| API Name | Availability_Month__c |
| Owner | Mikey |
| Status | Per task (D-036) |

One record per guide per month: for each day, the guide's availability status, start time and duration. It belongs to the guide (Staff__c). The month builder (A13-01) fills it from three sources: the guide's commitment (GoMeddo Availability on the Staff, if any) for obligated days, Hebcal for Shabbat and holidays, and Special Day for days Yad Vashem blocks. Most guides have no commitment. DD Doc 2, chapter 13 (A13-01, A13-02, V13-09).

**Form display (Mikey, 17.11.2026):** every day of the month shows in one of three ways: open and on the commitment; open but not on the commitment (selectable, greyed out); blocked or unavailable.

**Differs from the brief and the DD (agreed with Lior and Neev, 16 to 17.11.2026, not yet in `knowledge/model-register.md`):**
- Lior's brief had Availability Month as one record per month for all guides (status, send date, close date, notes). Dropped: too many objects for what it held.
- The DD (chapter 13 entities) has a custom "Guide Availability" object, one record per guide per day with two time ranges. Replaced by 3 fields per day on this object (status, start time, duration): one range per day, not two.
Requested in `sync/DM-mikey.md` for Lior or Anna to record in the model register.

## Field Mapping

| Salesforce Label | Salesforce API Name | Translation | Field Type | Picklist Values | שם השדה במערכת הישנה | Old API Name | Unique | Section | Questions | Task | Status |
|-----------------|--------------------|-------------|------------|------------------|----------------------|--------------|--------|---------|-----------|------|--------|
| Availability Month Number | Name | מספר חודש זמינות | Auto Number (AVM-{00000}) | | | | | General Info | | Availability step 1 (Lior 16.11.2026) | Built |
| Staff | Staff__c | נותן שירות | Lookup(B25__Staff__c), required | | | | | General Info | [Implementer] Whose month it is. Replaces Commitment__c: the commitment is only one source the builder reads, like Hebcal and Special Day (Mikey 18.11.2026). | A13-01 | Built |
| Commitment | Commitment__c | התחייבות | Lookup(B25__Availability__c) | | | | | General Info | [Implementer] Made optional, then deleted by hand in Setup (Mikey 18.11.2026). The builder finds the commitment itself. | A13-01 | Deleted |
| Year | Year__c | שנה | Text(4), required | | | | | General Info | [Implementer] Filled automatically by the feeder (later task). | Availability step 1 (Mikey 17.11.2026) | Built |
| Month | Month__c | חודש | Text(2), required | | | | | General Info | [Implementer] Month number 1 to 12. Filled automatically by the feeder (later task). Was a Date field until 17.11.2026. | Availability step 1 (Mikey 17.11.2026) | Built |
| Month Key | Month_Key__c | מפתח חודש | Text(30) | | | | Yes | General Info | [Implementer] Commitment Id + Year + MM, filled by flow; blocks two records for the same guide and month. | Availability step 1 (Lior 16.11.2026) | Built |
| Last Guide Update | Last_Guide_Update__c | עדכון אחרון של המדריך | Date/Time | | | | | General Info | [Implementer] DD: indication that the guide updated the availability. Filled by the form (later task). | Availability step 1 (Lior 16.11.2026) | Built |
| Day 1 ... Day 31 | Day_01__c ... Day_31__c | יום 1 ... יום 31 | Picklist, global value set Availability_Day_Status (31 fields) | זמין; מחוייב; לא זמין; משובץ; שבת/חג; יום חסום | | | | Days | | Availability step 1 (Mikey 17.11.2026) | Built |
| Day 1 Start ... Day 31 Start | Day_01_Start__c ... Day_31_Start__c | יום 1 שעת התחלה ... | Time (31 fields) | | | | | Days | [Mikey] Hours, not shifts: to be confirmed. | Availability step 1 (Mikey 17.11.2026) | Built |
| Day 1 Duration ... Day 31 Duration | Day_01_Duration__c ... Day_31_Duration__c | יום 1 משך ... | Picklist, global value set Shift_Duration (31 fields) | 3; 3.5; 4; ... 8.5; 9 (hours) | | | | Days | | Availability step 1 (Mikey 17.11.2026) | Built |
| Day 1 Note ... Day 31 Note | Day_01_Note__c ... Day_31_Note__c | הערה יום 1 ... הערה יום 31 | Text(80) (31 fields) | | | | | Days | [Implementer] Filled by the month builder: holiday name from Hebcal, Special Day reason or note, or שבת. Read by the Titan form, which cannot read a second object (Mikey 18.11.2026). | A13-01 | Built |

Months shorter than 31 days leave the last fields empty. No `Legacy_ID__c`: new object. [Implementer]

### Page

No Lightning record page. The default page shows `Availability Month Layout`: General Info (two columns) and Days (status, start, duration per day; 1 to 16 left, 17 to 31 right). Tab "חודשי זמינות", added by hand to the Admin Malam app.

### Access

Read, Create, Edit on `YadVashem_Order_Permission_Set` (no Delete) and the tab Visible; FLS Read/Edit on the permission set and System Administrator for every field except the required `Staff__c`, `Year__c` and `Month__c` (D-033). The 31 note fields are ticked on the permission set by hand in Setup (Object Settings), not deployed: a permission set deploy replaces the set.

### Global value sets

- `Availability_Day_Status` (Availability Day Status): זמין, מחוייב, לא זמין, משובץ, שבת/חג, יום חסום.
- `Shift_Duration` (Shift Duration): 3 to 9 in half hours.

## Relationship Fields

| Field | Parent | Notes |
|---|---|---|
| Staff__c | B25__Staff__c (GoMeddo) | Lookup, required, delete Restrict; related list `Availability_Months`. Added for A13-01. |
| Commitment__c | B25__Availability__c (GoMeddo, Neev) | Removed for A13-01 (optional, then deleted by hand). |

## Test records

| Name | Id | Task |
|---|---|---|
| AVM-00000 | a2JWm0000002iRZMAY | Deleted 17.11.2026 to change Month from Date to Text |
| AVM-00001 | a2JWm0000002ibFMAQ | Filter test 18.11.2026; deleted 18.11.2026 before Staff__c was added |
| (first record created by the A13-01 builder, December 2026) | pending | A13-01 |

The Month Key flow worked on the first model (`a11Wm000000TEFhIAO-2026-11`).

## Automations

- `B2_Availability_Month_Set_Key` (record-triggered, before save, create and update, active): fills `Month_Key__c` from Staff, Year and Month (Commitment until 18.11.2026). Recreated 17.11.2026 when Month became text. Metadata under `metadata/Availability_Month__c/main/default/flows/`.
- [A13-01](automations/A13-01.md): builds every active guide's month from the commitment, Hebcal and Special Days.
