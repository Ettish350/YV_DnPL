# D-017: The repo is the master for tickets

| | |
|---|---|
| Status | **reversed** by D-028 |
| Decided by | Lior Pridor |
| Date | 22.09.2026 |
| Challenges | none |

## Decision
Tickets live in `tickets/<epic>/` in this repo. Jira (YV20) is not updated by owners or their Claude for this build.

## DD source / evidence
Lior's decision, 22.09.2026.

## Alternatives considered
Jira as master: two places to update, and Claude cannot reach Jira from every session.

## Affects
tickets/, brief 8.2, CLAUDE.md.

## History
- 22.09.2026: accepted.
- 12.10.2026: reversed by D-028. Jira is the master for tasks.
