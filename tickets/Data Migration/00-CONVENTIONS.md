# Migration workbook conventions

Read this before building any object file. From 14.10.2026 this folder is the data-model track for research and build (D-030). Content changes (object files, metadata) are made only through Claude Code; Copilot is used for generic git tasks only (pull, push). This is in addition to `Claude_Salesforce_Migration_Guidelines.md` (the general process) and `CLAUDE.md`/`CONVENTIONS.md` (the repo-wide rules); where they conflict, ask, don't guess.

## From 03.11.2026: build per task (D-036)

This section overrides Stage A and Stage B below wherever they differ. The rest of this file (sources, columns, field-level rules, build rules, deploying) still applies.

- **Scope is one task, not one object.** Run `/fields-for-task <task> <Object> <step>` in Claude Code. It proposes only the fields that step of the process needs. You approve, add or remove, build them in `yv-partial` with a test record, and the rows go into this object's file. Do not go through every legacy field of the object; legacy fields no task has needed are simply not listed yet.
- **Two more columns** after Questions in every object file: **Task** (the task key that added the row, e.g. `A1-2`) and **Status** (`Exists`, `To build`, `Built`, `Not built: open question`). Rows added by other tasks stay as they are.
- **Two lists at the end of every object file:** "Test records" (record Name, Id and task key) and "Automations" (one line per automation file that reads or writes the object).
- **File status:** a file created from now on has Status `Per task (D-036)`. Existing `Draft` or `Approved` files keep their rows; new rows are added with Task and Status.
- **Automations:** run `/automation-spec <DD code> <task>`. One file per DD code in `automations/<code>.md`, from the template in `automations/README.md`. Metadata for automations goes under `metadata/automations/<code>/main/default/`.
- **Branch:** `task/<task key>`, pushed at the end of every work session. Open a pull request into `main` and merge it yourself (D-035, D-036).
- **Done:** a field row is done when it is `Built` and exists in `yv-partial`. An automation is done when its file is `Tested`, its components are active in `yv-partial`, and its test records show the expected results. The old scoring of a `Draft` by legacy fields gone through no longer applies.
- How the whole cycle runs, with examples: `plan/IMPLEMENTER-PROCESS.md`.

## Stage A: Prepare the migration workbook

## Sources

Build every object file from two sources only:
- The legacy-system fields file `שדות מערכת ישנה.xlsx` in `sources/` (committed in git; see `sources/SOURCES.md`) for legacy field names, types, and picklist value lists. It is the YV fields workbook with all records removed (D-031). Sheets "רשימת כל היישויות" (entities) and "שדות" (all fields, including a PicklistValues column) are complete. Every other sheet keeps only its header row, so it shows the column names of that legacy table and nothing else. The "PICKLISTS" sheet is empty apart from its header; take picklist values from the "PicklistValues" column of "שדות". There is no sample data: if a primary key's format or a value cannot be read from "שדות", write it in Questions. Do not use the original workbook (it holds personal data and stays on SharePoint).
- The DD (אפיון) for business rules, required entities, and process context.

Do not use `mapping/<epic>-field-mapping.md` as a source for this workbook, for any epic. Verify standard field types/lengths and existing custom fields against the live org (`sf sobject describe --sobject <Object> --target-org yv-partial`, read-only). Never deploy; never run `sf data` commands.

## Columns

Mandatory 6 columns, in this order, never reordered: Salesforce Label, Salesforce API Name, Translation, Field Type, Picklist Values, שם השדה במערכת הישנה. Four more are appended after them: Old API Name, Unique, Section, Questions.

| Column | Content |
|---|---|
| Salesforce Label | Required and always in English. Use a readable label that describes the same field as the Salesforce API Name; it does not need to repeat API-name syntax. |
| Salesforce API Name | The field's valid Salesforce API name, aligned with the Salesforce Label. Standard fields are written plainly (e.g. `Name`). |
| Translation | Required for every field and written in Hebrew. If an equivalent field exists in the legacy system, use its Hebrew field name (שם השדה במערכת הישנה) exactly; otherwise, provide an accurate Hebrew translation of the Salesforce Label. |
| Field Type | The type only (e.g. `Text(255)`, `Long Text Area(4000)`, `Picklist`). No provenance notes or parenthetical commentary. |
| Picklist Values | The actual values only, semicolon-separated. Nothing else: no inline notes, no placeholders, no parenthetical explanations. If values are not yet decided, leave the cell blank. |
| שם השדה במערכת הישנה | The legacy system's original Hebrew field label, preserved exactly as it appears in the source. Never cleaned up, shortened, or rephrased; it is a historical record, not a proposed value. |
| Old API Name | The legacy system's internal field name (e.g. `new_description_en`), for Data Loader source-to-target mapping. |
| Unique | Whether the field should carry a Unique constraint. Leave blank unless the DD states a requirement or the user explicitly instructs it. Never infer uniqueness. |
| Section | The proposed Lightning Page section for this field. Keep it to a small number of broadly named sections (e.g. General Info, plus two or three more specific to the object), not a narrow section per concept. Use the existing standard "System Information" section for system/integration-type fields rather than inventing a new one. The Created By and Last Modified By fields must remain in the "System Information" section. List the final grouping under a "Proposed Lightning Page section grouping" note below the table. This is a proposal for review, not a decision; the DD does not specify page layout. |
| Questions | An open question about this specific field, starting with who decides (D-029): `[Lior]` technical, `[Anna]` general, `[Niv]` GoMeddo. A point the implementer decided alone is written as `[Implementer] <what was decided>`. **Leave the cell empty when there is no specific question; do not fill every row.** Keep the text minimal: one short question, no background or explanation beyond what the person needs to answer. The only place for commentary, caveats, and flagged deviations tied to a specific row. |

## Field-level rules

- **Hebrew/English duplicate pairs**: wherever a legacy entity has a Hebrew and an English version of the same field, and a standard Salesforce field is available, the standard field takes the Hebrew value and a custom field takes the English value.
- **Standard-field reuse**: before planning a new custom field, check the live org for a standard field that already serves the same purpose. Verify type and length; never guess.
- **Existing custom-field reuse**: also check the live org for an existing custom field that may already cover the same concept. If its picklist values differ from what's required, do not resolve the difference unilaterally; merge the value lists into one deduplicated union and flag the field in Questions.
- **Merge value lists, don't pick one**: when a required value list differs from an already-existing picklist's value list, Picklist Values holds the union of both, de-duplicated.
- **Many-to-one legacy fields get one row**: when several legacy fields map to a single Salesforce field, write one row, not one per legacy field. List every legacy field name together, semicolon-separated, in both "שם השדה במערכת הישנה" and "Old API Name".
- **Never invent values**: if the sources are silent on something (a picklist's values, a uniqueness requirement, a field's existence), say so in Questions. Do not supply a plausible-looking answer.
- **Relationships require updating both objects**: when a field on one object must relate to another object, update the Markdown file and Excel worksheet for the child object to include the relationship field. Check that the referenced parent object is documented as well; if the relationship requires a change to its definition, update its Markdown file and worksheet too. Keep both object files and worksheets consistent.

### Before and during the review (13.10.2026, Lior)
- Start an object by reviewing its general goal and its role in the business process (the DD chapter). Each field is understood in relation to that process.
- A legacy field that is dropped (no future use) is not deleted from the record: list it under a "Dropped legacy fields" heading in the object file, one line per field with its legacy name, Old API Name and the reason, tagged `[Implementer]`. It can be added later if needed.
- A field that belongs on an object whose file is part of another implementer's open task is changed through a pull request tagged to that implementer, not by a direct edit. The lookup target object must exist before the lookup field is deployed.

## Stage A: finishing the research (D-035, 22.10.2026)

Status of an object file: `Draft`, then `Approved`. Lior is not a reviewer or a gate (D-035). While it is `Draft`, push the branch at the end of every work session; the checks see nothing that is only on a laptop. (Scoring a draft by legacy fields gone through was replaced by D-036 on 03.11.2026.)
1. Decide every field yourself from the DD and the legacy file. Write a decision you made as `[Implementer] <what was decided>` in the Questions column.
2. A real question (the DD is silent or contradicts itself on something that changes the model) is tagged for the person who decides it (D-029). It does not hold the object: mark that field `Not built: open question` and go on. When the answer comes, a later pull request updates the file and the field is built.
3. When every field is decided or marked, set the Status to `Approved`, open a pull request from `data-migration/<object>-stage-a` into `main` and merge it yourself. It contains only the object file(s) in this folder and `sync/DM-<name>.md`. The description lists any open questions as one numbered list with their tags.
4. Write any decision that affects other objects into `knowledge/model-register.md`, and tell the implementer of that object.

## Stage B: Build the approved object in Salesforce

Start Stage B once the object file is `Approved` (D-035). Any implementer builds; no approval is needed. Fields marked `Not built: open question` are left out (partial approval, D-029).

1. No worksheet tab is needed in `Migration - Objects and fields.xlsx` (Lior, 28.10.2026): the approved object Markdown file is the record, and one shared workbook edited by several implementers cannot be merged in git.
2. Create a Lightning Record Page for the object. Name the page `Order-<Object Name>- Record Page` (replace `<Object Name>` with the object's name). Assign it only to the System Administrator profile in the Orders app.
3. Identify which fields from the approved Markdown file are missing in the sandbox. Generate metadata for those fields and deploy it to `yv-partial` (see "Deploying to yv-partial" below).
4. For every field created, set field-level security (FLS) to Read and Edit, identically on the System Administrator and `YV Order user` profiles (D-033).
5. Place the fields on the Lightning Record Page in the sections specified in the worksheet's Section column.
6. Before proceeding, confirm that every relationship is recorded in the child object's Markdown file and worksheet, that any required parent-object changes are recorded in its Markdown file and worksheet, and that System Administrator and `YV Order user` have Read and Edit FLS for every created field.

Deploy only to `yv-partial`, never to production, and never run `sf data`. The deploy needs no approval (D-035); git must match the sandbox afterwards (see "Handing off" below).

### Where Stage B files live

- Metadata for an object: `tickets/Data Migration/metadata/<Object>/main/default/` (fields, objectTranslations, flexipages, profiles).
- Changes to existing components that are not deployed by default (an existing field's picklist values, a shared app): `tickets/Data Migration/metadata/<Object>/_proposed-changes-to-existing-fields/` and `_proposed-changes-to-existing-components/`. Never place them under `main/default/`.
- Do not use `metadata/<epic>/` for this workbook.

### Build rules decided during Product2

- Field labels are English (40 characters maximum) with the Hebrew value as a Translation Workbench translation (`objectTranslations`, language `iw`).
- Standard fields that are not in the Section column (for example `ProductCode`, `Family`, `Type`, `IsActive`, `SpecificationType` on Product2) go in the General Info section.
- If a Lookup field's target object does not yet exist in the system, do not create the target object just to unblock the field. Do not generate the Lookup field; mark it in the object's Markdown file as "Not created: target object `<Object API Name>` does not yet exist" and list it in the PR questions.
- A field whose Lookup target or value list is not yet decided, or a multi-select picklist with no values, is not generated. Record the reason in the object's Markdown file and list it in the PR questions.
- FLS is deployed as `profiles/Admin.profile-meta.xml` and the `YV Order user` profile file, each containing only `fieldPermissions` (Read and Edit). Skip required fields, which cannot take FLS (D-033). Profile and permission set questions go to Niv.
- Every object gets an External ID field named `Legacy_ID__c`, the same name on all objects, for the data load (Anna, #13).
- List views are not part of the object and field migration; they are created in later stages, as the DD requires (Anna, #13).
- Shared metadata in `yv-partial` (profiles, the Orders app, permission sets) is retrieved from the org right before every deploy. Never deploy an older copy (Anna, #13).
- Assigning the record page to the System Administrator profile in the Orders app is done by retrieving the current `Orders.app` from the org and adding one `profileActionOverrides` entry. Do not deploy an older copy.

### Deploying to yv-partial

The implementer deploys their own approved object to `yv-partial`, no approval needed (D-035). Never production.
1. Use `sourceApiVersion` 68.0. With 62.0 the record page fails on `flexipage:tabset`.
2. `sf` fails on paths containing spaces (`Data Migration`). Copy `main/` to a folder with no spaces plus an `sfdx-project.json`, and deploy from there.
3. Always run `--dry-run` first, then the real deploy.

### Handing off

1. Commit everything under `tickets/Data Migration/` on a branch named `data-migration/<object>-stage-b` and push it. Never push to `main` directly.
2. Open a pull request into `main` and merge it yourself (D-035), so `main` matches what is deployed in `yv-partial`.
3. The pull request description lists: what was added, what is deployed to `yv-partial`, what was not generated or not deployed and why, and any open questions, each with its tag (one numbered list).
4. Move the Jira task to Ready for testing sandbox.
5. `gh` may not be installed. If so, open the pull request in the browser from the link printed by `git push`.
6. Do not edit `Migration - Objects and fields.xlsx` (step 1 of Stage B).
