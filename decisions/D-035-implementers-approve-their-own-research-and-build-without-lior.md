# D-035: Implementers approve their own data-model research and build it in yv-partial, without Lior's approval

| | |
|---|---|
| Status | **accepted** |
| Decided by | Lior Pridor |
| Date | 22.10.2026 |
| Challenges | none |

## Decision
For the data-model track (`tickets/Data Migration/`, D-030): objects, fields, record pages, translations and FLS.
- **Lior is not a reviewer or an approval gate.** No research or build waits for his approval.
- **Research (Stage A):** the implementer goes through every legacy field, decides for each whether it moves to the new object or not, and sets the object file to `Approved` when done. The implementer merges their own Stage A pull request into `main` (self-merge). No requested reviewer is needed.
- **Questions:** the implementer decides what the DD and the legacy file allow, and writes it as `[Implementer] <what was decided>`. A real question (the DD is silent or contradicts itself on something that changes the model) is tagged for the person who decides it (D-029) and does not hold the object: that field is marked `Not built: open question` and the rest goes ahead (partial approval).
- **Build (Stage B):** any implementer builds their approved fields and record page directly in `yv-partial`: retrieve the shared metadata first, `--dry-run`, then deploy. Then commit the metadata on the `data-migration/<object>-stage-b` branch and self-merge the pull request into `main`, so git matches the sandbox.
- **Only `yv-partial`.** Never production, never `sf data`. Everything outside the data-model track (automations, Apex, the portal, shared apps beyond the record page assignment) keeps the existing gate in `WORKFLOW.md`.

## Why
Lior, 22.10.2026: he has no time to review every object, and waiting for his approval stalled the data-model work. Building fields and pages is within every implementer's role.

## Affects
`tickets/Data Migration/00-CONVENTIONS.md`, `.claude/agents/data-migration-workbook.md`, `CLAUDE.md`, `WORKFLOW.md`, `plan/OPERATING-GUIDE.md`, `.claude/commands/weekly-progress.md`. Changes part of D-029 (Lior always a reviewer on research pull requests) and D-030 (`review` and `question` rows wait for Lior's approval) for this track.

## History
- 22.10.2026: accepted.
