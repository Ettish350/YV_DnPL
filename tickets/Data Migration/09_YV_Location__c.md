# Object: YV_Location__c

## Salesforce Object Details

| Property | Value |
|----------|--------|
| Object Label | YV Location |
| API Name | YV_Location__c |
| Status | Draft |

Business process: Doc 2 process 11 (`dd/doc2-ordering-processes/06-processes/11-physical-resources-museum.md`). The activity location filters the rooms; the DD names "מיקום, היררכיית מבנה וקומה". Legacy source: building and floor are fields of `new_resource`.

## Field Mapping

| Salesforce Label | Salesforce API Name | Translation | Field Type | Picklist Values | שם השדה במערכת הישנה | Old API Name | Unique | Section | Questions | Task | Status |
|-----------------|--------------------|-------------|------------|------------------|----------------------|--------------|--------|---------|-----------|------|--------|
| Building | Building__c | בניין | Text(255) | | בנין | new_building | | | [Lior] Exists as Text; the legacy field is a picklist (בית הספר;מנהלה;ארכיון). Convert it? | Process 11 | Exists |
| Floor | Floor__c | קומה | Picklist | -2;-1;Ground 1;Ground 2;Ground 3;Ground 4;Ground 5;Ground 6+;קומה ראשונה;קומה שנייה;קומה שלישית | קומה | new_floor | | | [Lior] Existing values differ from the legacy ones (קומה ראשונה;קומה שנייה;קומה שלישית). Add them? | Process 11 | Exists |

Both fields already exist in `yv-partial` and are already on the page layout `YV Location Layout`. The object has no Lightning record page; the default page shows the layout. Nothing was built for this object.

## Test records

## Automations
