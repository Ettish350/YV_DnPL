# Object: Special_Day__c

## Salesforce Object Details

| Property | Value |
|----------|--------|
| Object Label | Special Day (יום מיוחד / ימים מיוחדים) |
| API Name | Special_Day__c |
| Owner | Mikey |
| Status | Per task (D-036) |

One record per special date for the whole year, not tied to a month: blocked days (fast days, team days, site closures, annual trips) and days that show a note. Filled by the schedulers, for days Yad Vashem defines itself (closures, visits, fast days). Holidays are not stored here: the month builder (A13-01) reads them from Hebcal directly (Mikey 18.11.2026). The guide availability form reads the dates that fall in its month. DD Doc 2, chapter 13 (V13-02, A13-01). New object, no legacy entity.

## Field Mapping

| Salesforce Label | Salesforce API Name | Translation | Field Type | Picklist Values | שם השדה במערכת הישנה | Old API Name | Unique | Section | Questions | Task | Status |
|-----------------|--------------------|-------------|------------|------------------|----------------------|--------------|--------|---------|-----------|------|--------|
| Special Day Name | Name | שם היום | Text(80) | | | | | General Info | | Availability step 1 (Lior 16.11.2026) | Built |
| Date | Date__c | תאריך | Date (required) | | | | | General Info | | Availability step 1 (Lior 16.11.2026) | Built |
| Day Type | Day_Type__c | סוג יום | Picklist | חסום; הערה | | | | General Info | | Availability step 1 (Lior 16.11.2026) | Built |
| Reason | Reason__c | סיבה | Picklist | יום צום; יום גיבוש; סגירת אתר; טיול שנתי; אחר | | | | General Info | | Availability step 1 (Lior 16.11.2026) | Built |
| Source | Source__c | מקור | Picklist | Hebcal; ידני | | | | General Info | [Implementer] Default ידני. | Availability step 1 (Lior 16.11.2026) | Built |
| Note (Hebrew) | Note_He__c | הערה (עברית) | Text(255) | | | | | Notes | | Availability step 1 (Lior 16.11.2026) | Built |
| Note (English) | Note_En__c | הערה (אנגלית) | Text(255) | | | | | Notes | | Availability step 1 (Lior 16.11.2026) | Built |

No `Legacy_ID__c`: new object with nothing to migrate. [Implementer]

### Page

No Lightning record page. The default page shows `Special Day Layout` (General Info, Notes, System Information), as on `13_Bundle_Item_Entry.md`. A tab "ימים מיוחדים" exists and is added by hand to the Admin Malam app.

### Access

Read, Create, Edit on `YadVashem_Order_Permission_Set` (no Delete) and the tab Visible; FLS Read/Edit on the permission set and System Administrator for every field except the required `Date__c` (D-033).

## Test records

| Name | Id | Task |
|---|---|---|
| name (18.11.2026, חסום, יום צום, ידני) | a2KWm000003PWUQMA4 | Availability step 1 |

## Automations

Read by the month builder (A13-01). No Hebcal job writes here (Mikey 18.11.2026). Tal's flow `Create_Holiday_Blocks` and `NC Hebcal API` already exist and are not touched.

- [A13-01](automations/A13-01.md): the month builder reads blocked and note days from here.
