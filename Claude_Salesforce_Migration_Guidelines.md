# Salesforce Data Migration Workbook Generator - Claude Instructions

## Goal

Create a Salesforce Data Migration Workbook that will serve as the master source for data migration from a legacy system into Salesforce.

The process must be controlled, iterative, and approval-based.

In this project the approval is the implementer's own (the user in the session). Lior is not a reviewer or a gate, and the approved object is built in `yv-partial` without further approval (D-035, 22.10.2026).

---

# Core Working Principles

1. Work on one Salesforce Object at a time.
2. Never add an object directly into the final Excel workbook.
3. First create a dedicated Markdown document for review.
4. Wait for user approval.
5. Only after approval update the Excel workbook.
6. Maintain full traceability between Salesforce fields and legacy system fields.
7. Preserve Hebrew content and translations.
8. Use Salesforce metadata as the source of truth whenever available.

---

# Deliverables

## Deliverable 1 - Object MD Document

Create a dedicated MD file for every Salesforce Object.

Example:

- 01_Account.md
- 02_Contact.md
- 03_Opportunity.md
- 04_Campaign.md

Status values:

- Draft
- In Review
- Approved

---

## Deliverable 2 - Final Excel Workbook

Only approved objects should be added.

Workbook rules:

- One worksheet per Salesforce Object.
- Worksheet name = Salesforce Object Label.
- Do not create worksheets for unapproved objects.

---

# Mandatory Worksheet Columns

Every worksheet must contain the following columns in the exact order:

1. Salesforce Label
2. Salesforce API Name
3. Translation
4. Field Type
5. Picklist Values
6. שם השדה במערכת הישנה

Do not change the order.

---

# Field Collection Rules

Collect all relevant fields.

Include:

- Standard Fields
- Custom Fields
- Required Fields
- Integration Fields
- External ID Fields
- Reference Fields
- Migration Supporting Fields

Exclude unless requested:

- Formula Fields
- Rollup Summary Fields
- System Audit Fields
- Deprecated Fields

---

# Field Type Rules

Populate Salesforce Field Type exactly.

Examples:

- Text
- Text Area
- Long Text Area
- Number
- Percent
- Currency
- Date
- DateTime
- Email
- Phone
- URL
- Checkbox
- Picklist
- Multi-Select Picklist
- Lookup
- Master-Detail
- Formula
- Auto Number

---

# Picklist Handling

For Picklist and Multi-Select Picklist fields:

- Document all available values.
- Preserve original value spelling.
- Separate values using semicolons.

Example:

Active;Inactive;Pending;Cancelled

For non-picklist fields leave blank.

---

# Translation Rules

Provide a business-friendly Hebrew translation.

Requirements:

- Clear terminology.
- Consistent wording.
- Suitable for business users.
- Reuse previously approved translations.

---

# Legacy Field Mapping

Column:

שם השדה במערכת הישנה

Rules:

- Record source field names exactly.
- Preserve legacy naming.
- Leave blank when unknown.
- Add open questions if mapping is missing.

---

# Lookup and Relationship Fields

For Lookup or Master-Detail fields document:

- Target Object
- Matching Logic
- External ID if applicable

Example:

Account Lookup → Match by Legacy Account ID

---

# Required Validation Checks

Before requesting approval validate:

- No duplicate Salesforce fields.
- API name exists.
- Field type populated.
- Picklist values documented.
- Required fields identified.
- Relationship fields identified.
- Hebrew translation completed.

---

# MD Template

```md
# Object: [Object Name]

## Salesforce Object Details

| Property | Value |
|----------|--------|
| Object Label | |
| API Name | |
| Migration Priority | |
| Status | Draft |

## Field Mapping

| Salesforce Label | Salesforce API Name | Translation | Field Type | Picklist Values | שם השדה במערכת הישנה |
|-----------------|--------------------|-------------|------------|------------------|----------------------|

## Relationship Fields

| Field | Target Object | Matching Logic |
|--------|--------------|----------------|

## Open Questions

- Item 1
- Item 2

## Validation Summary

- Fields Reviewed: Yes/No
- Picklists Reviewed: Yes/No
- Lookups Reviewed: Yes/No

## Status

Draft / In Review / Approved
```

---

# Approval Workflow

Step 1:
Create object MD.

Step 2:
Present document.

Step 3:
Receive comments.

Step 4:
Update document.

Step 5:
Receive approval.

Step 6:
Add worksheet into Excel workbook.

Step 7:
Continue with next object.

---

# Excel Workbook Quality Standards

The final workbook must:

- Support Salesforce Data Loader usage.
- Support migration workshops.
- Support business validation.
- Support technical validation.
- Support future maintenance.

The workbook should be readable by both business users and Salesforce administrators.
