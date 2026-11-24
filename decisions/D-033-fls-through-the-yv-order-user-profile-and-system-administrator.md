# D-033: Field-level security through the YV Order user profile and System Administrator

| | |
|---|---|
| Status | **accepted, amended 21.10.2026** |
| Decided by | Anna Pertzovski (Issue #16); profile licence and routing by Lior Pridor |
| Date | 14.10.2026 |
| Challenges | none |

## Decision
- A new profile, `YV Order user`, is created from scratch with the Salesforce user licence. It is not cloned from an existing profile (Lior, #16).
- For every object and field created in the object migration (`tickets/Data Migration/`), field-level security is set identically on `YV Order user` and on System Administrator, in the same deploy step. Required fields are skipped, because they cannot take FLS.
- A permission set is created only if one turns out to be needed.
- Profiles are shared metadata in `yv-partial`: retrieve them from the org right before every deploy, never deploy an older copy (#13, 6d).
- Questions about profiles and permission sets go to Niv (D-029).

## Amendment, 21.10.2026 (Neev, approved by Lior, PR #28)
- Field-level security for the object migration goes through the permission set `YadVashem_Order_Permission_Set` and System Administrator, not through the `YV Order user` profile. New fields are added to the permission set from now on.
- The `YV Order user` profile is still the users' profile (created from scratch, Salesforce user licence); it does not carry the field permissions. It does not exist in `yv-partial` yet (21.10.2026).
- The line "A permission set is created only if one turns out to be needed" no longer applies.

## Replaces
For the object migration of the orders module: the rule in `CONVENTIONS.md` "Field-level security only through the epic's permission set. Never edit profiles", and the rule in `tickets/Data Migration/00-CONVENTIONS.md` that set FLS on System Administrator only.

## DD source / evidence
Anna's decision in Issue #16 (item 6 of `meetings/2026-10-13-sprint-1-handoff.md`, Issue #13); Lior's answer of 14.10.2026.

## Affects
`CONVENTIONS.md`, `tickets/Data Migration/00-CONVENTIONS.md`, D-029.

## History
- 14.10.2026: accepted.
- 21.10.2026: amended, FLS through the permission set (PR #28).
