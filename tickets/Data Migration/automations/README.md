# Automation files

One file per DD automation or validation code (`A11-03.md`, `V11-01.md`), created by `/automation-spec` (D-036). The file is the record of what the automation does, who built it, what it is built with in `yv-partial`, and how it was tested. Every object file it touches links to it under "Automations".

Status: `Approved` (definition agreed by the implementer), then `Built` (exists in `yv-partial`), then `Tested` (every test case passed).

## Template

```markdown
# <code>: <one-line name, in English>

| | |
|---|---|
| DD | <Doc 2 / Doc 3>, <process>, row <code> ([chapter](../../../dd/doc2-ordering-processes/06-processes/<file>.md), or `dd/doc3-events-finance/05-processes/<file>.md` for Doc 3) |
| Task | <Jira key or sprint task, e.g. A1-6> |
| Built by | <name> |
| Objects | <API names it reads or writes> |
| Status | Approved / Built / Tested |

## Definition (approved <date>)
- **Trigger:**
- **What it does:**
  1.
- **Reads and writes:**
- **Not in scope:**
- **Tool:** <Flow / Apex / validation rule / configuration>, because <one line>

## Implementation
| Component | Type | API name | Version |
|---|---|---|---|

Metadata in git: `tickets/Data Migration/metadata/automations/<code>/`

## Test cases
| # | Test record | Steps | Expected | Result and date |
|---|---|---|---|---|
| 1 | | | | |

## Questions
- [Lior] / [Neev] / [Tal] <one short question>
```
