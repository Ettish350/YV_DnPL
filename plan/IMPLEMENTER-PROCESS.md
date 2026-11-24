# How we build: fields and automations per task

03.11.2026 (D-036). For Tal, Neev and Reut. Replaces the 28.10.2026 version (object by object, Stage A and Stage B), which is in git history.

## How we work now

Each of you owns one business process from the approved DD per sprint and builds it end to end in `yv-partial`. Your sprint tasks are of two kinds: the fields a step of the process needs, and the automations that run it.

For both, the work is a short conversation with Claude Code through a fixed command: Claude proposes, you review and decide, you build, you test. What you decided and built is written in the repo, next to the object it belongs to. Nobody pre-creates whole objects any more: an object file grows task by task, with only the fields and automations a real process needed.

You approve your own fields and automations (D-035, D-036). Questions go in the file, tagged with who decides: `[Lior]` technical, `[Neev]` GoMeddo and architecture, `[Tal]` portal.

## Fields

Example: task A1-2, school data on Account, for the portal booking.

1. Run `/fields-for-task A1-2 Account <what the step does>`. Claude reads the DD process, the legacy fields file and `yv-partial`, and proposes only the fields this step needs, each marked **Exists** (with its API name) or **To build**.
2. Review the table. Add, remove or change fields until it is right, then say it is approved.
3. Claude writes the approved rows into the object file, `tickets/Data Migration/<NN>_<Object>.md`, and creates the file if it does not exist yet.
4. Build the To build fields in `yv-partial` with Claude (translation, FLS, record page, `--dry-run` first), and create a test record that uses them.
5. Tell Claude it is built. It checks the org, sets those rows to **Built**, adds the test record under "Test records", and commits on branch `task/A1-2`. Open the pull request and merge it yourself.

**What the object file holds:** the same 10 columns as before (see `tickets/Data Migration/00-CONVENTIONS.md`), plus **Task** (the task that added the field) and **Status** (`Exists`, `To build`, `Built`, `Not built: open question`). Rows added by other tasks stay as they are. Legacy fields that no task has needed yet are simply not listed.

**Shared objects:** if the object file belongs to the other person's open task (for example Bundle, Bundle Item, Opportunity), change it through a pull request tagged to them, not a direct edit.

## Automations

Example: task A1-6, free slots in the portal calendar, DD code A11-03.

1. Run `/automation-spec A11-03 A1-6`. Claude reads the code's row in the DD, its process chapter and acceptance test, the object files it touches, and what already exists in `yv-partial`. It proposes a definition: trigger, what it does step by step, what it does not do, the tool (Flow, Apex, validation rule or configuration), and 3 to 6 test cases.
2. Review it. Change it until it is right, then say it is approved.
3. Claude creates the automation file and links it from every object file the automation touches.
4. Build it in `yv-partial` with Claude. Claude records the component names in the file.
5. Run the test cases yourself. The file is marked **Tested** only when all of them pass.

If the automation needs a field that does not exist, run `/fields-for-task` for it first.

**The automation file:** one per DD code, in `tickets/Data Migration/automations/<code>.md`, from the template in [automations/README.md](../tickets/Data%20Migration/automations/README.md): DD source, task, who built it, objects, the approved definition, the components in `yv-partial`, the test cases with results and dates, and questions. Each object file has an "Automations" list at the end that links to it.

## Testing and the morning check

You test your own work. A test counts when it is written in the automation file: the test record, the steps, the expected result, and the actual result with the date. Use the test records from your field tasks, so the test can be repeated.

Claude checks `yv-partial` against the repo every morning, read-only:

| What the file says | What the morning check confirms |
|---|---|
| A field row is `Built` | The field exists in `yv-partial` |
| An automation is `Built` | Its components exist and are active |
| An automation is `Tested` | Each test record shows the expected result |
| (anything) | Nothing new in `yv-partial` is missing from every file, so duplicate fields between you are caught |

A task is done when its rows or its file pass the check. The sprint is done when every task passes and Lior accepts the demo at the end of the sprint.

## How to start

Before your first session: `git pull` on `main`, so you have the two commands and the updated conventions. Open Claude Code in the repo folder.

**Tal: a high school books a 3-hour guided tour in the portal (Doc 2, processes 02 and 11)**

```text
/fields-for-task A1-2 Account A high school logs into the portal and is offered only the bundles for its classification and age group (Doc 2, process 02)
/fields-for-task A1-3 Bundle__c Each bundle carries its target audience so the portal can filter on it (Doc 2, processes 01 and 02)
/fields-for-task A1-4 B25__Resource__c The museum and classrooms have a location, opening hours per weekday and a capacity, used to show free slots (Doc 2, process 11)
/automation-spec A02-01 A1-5
/automation-spec A11-03 A1-6
/automation-spec A02-04 A1-7
/automation-spec V02-05 A1-8
/automation-spec A01-02 A1-9
```

In A1-6, also run `/automation-spec` for A11-04, V11-01 to V11-04, A11-01 and A11-02: one file each.

**Neev: the guide card (Doc 2, process 13)**

```text
/fields-for-task B0-2 Contact A guide is a Contact with the Guide record type, linked to a GoMeddo Staff record, with employment type, signed contract and its validity, languages and tax coordination (Doc 2, process 13)
/fields-for-task B0-3 Contact Each guide has dated statuses (Guide 25, regular guide and others) and a certification matrix of audiences and languages (Doc 2, process 13)
/automation-spec V13-03 B0-4
/automation-spec V13-05 B0-5
```

## Links

| What | Link |
|---|---|
| DD index, start here | [dd/INDEX.md](../dd/INDEX.md) |
| Process 02, online self-service order | [02-online-self-service-order.md](../dd/doc2-ordering-processes/06-processes/02-online-self-service-order.md) |
| Process 11, physical resources and museum | [11-physical-resources-museum.md](../dd/doc2-ordering-processes/06-processes/11-physical-resources-museum.md) |
| Process 13, guide availability and service providers | [13-guide-availability-service-providers.md](../dd/doc2-ordering-processes/06-processes/13-guide-availability-service-providers.md) |
| Process 09, guide and resource scheduling | [09-guide-resource-scheduling.md](../dd/doc2-ordering-processes/06-processes/09-guide-resource-scheduling.md) |
| Object files and their rules | [tickets/Data Migration/00-CONVENTIONS.md](../tickets/Data%20Migration/00-CONVENTIONS.md) |
| Automation files and the template | [tickets/Data Migration/automations/README.md](../tickets/Data%20Migration/automations/README.md) |
| The two commands | [fields-for-task](../.claude/commands/fields-for-task.md), [automation-spec](../.claude/commands/automation-spec.md) |
| Model register | [knowledge/model-register.md](../knowledge/model-register.md) |
| Binding rules | [A1](../knowledge/A1-binding-rules.md), [B1](../knowledge/B1-binding-rules.md) |
| Decisions | [D-035](../decisions/D-035-implementers-approve-their-own-research-and-build-without-lior.md), [D-036](../decisions/D-036-build-per-task-fields-and-automations-with-two-fixed-commands.md) |
| Your tasks | Jira, project YV20 |
