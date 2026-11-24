# Salesforce build conventions

Same for every epic. Anything not covered here: ask in a question Issue, do not improvise.

## Project layout
- Salesforce project file: `sfdx-project.json`. Each epic has its own package directory: `metadata/L0` (stubs and shared), `metadata/A1`, `metadata/B1`. New epics get a folder when they start.
- Inside an epic folder, standard source format: `main/default/objects/<Object>/fields/<Field>.field-meta.xml`, etc.
- API version: do not pin. The CLI uses the org's version.
- Org: always `--target-org yv-partial` (see `sources/ORGS.md`).

## Names
| Component | API name | Label |
|---|---|---|
| Custom object | English, underscores: `Service_Provider_Status__c` (as in `mapping/`) | Hebrew |
| Custom field | As in `mapping/` (D-013: drop `new_`, PascalCase, `__c`) | Hebrew, as in `mapping/` |
| Record type | English: `Guide` | Hebrew |
| Validation rule | `<Epic>_<DD code>_<Short>`: `A1_V01_02_Online_Notice` | same as API name |
| Flow | `<Epic>_<Object>_<Purpose>`: `A1_Product_Clone` | Hebrew |
| Permission set | `<Epic>_<Role>`: `A1_Catalog_Manager` | Hebrew |
| Lightning page | `<Object>_<Epic>_Record_Page` | Hebrew |

## Descriptions (required)
Every object, field, validation rule, flow and permission set gets a description that starts with the ticket key and DD code:
`A1-06 | V01-02 | Online product requires notice time.`
This is how the daily agent and the reviewer link org components to tickets.

## Access
- Field-level security: for the orders object migration, on the `YV Order user` profile and System Administrator (D-033); otherwise through the epic's permission set. Never edit any other profile. Profile and permission set questions go to Niv.
- New fields are not added to existing page layouts of the live Cases module. Use the epic's own Lightning page or record type layout.

## Do not touch
- Anything that belongs to the live Cases module (פניות): objects, fields, flows, layouts, permission sets. If an epic needs to change one of them, open a question Issue first.
- Managed package components (GoMeddo etc.): use, do not edit.

## Check the org before creating anything
Before creating any object or field, look at what already exists in `yv-partial` (read-only commands only):
```
sf sobject describe --sobject Contact --target-org yv-partial
sf org list metadata --metadata-type CustomObject --target-org yv-partial
```
- A field with the same API name or the same Hebrew label already exists: do not create a second one. Reuse it if it means the same thing, otherwise open a question Issue.
- An existing field belongs to the Cases module: do not change it. Question Issue.
- Record the result of the check in the ticket under "Components" (e.g. "checked Contact: no clash").

This matters most on Contact (B1): the Cases module already customized it.
