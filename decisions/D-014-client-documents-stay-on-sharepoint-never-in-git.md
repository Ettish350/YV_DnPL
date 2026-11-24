# D-014: Client documents stay on SharePoint, never in git

| | |
|---|---|
| Status | **reversed** by D-031 |
| Decided by | Lior Pridor; exception by Anna |
| Date | 22.09.2026 |
| Challenges | none |

## Decision
The DDs and the YV fields workbook live only in the SharePoint binding folder. The repo points to them (sources/SOURCES.md) and blocks .docx and .xlsx files.

## DD source / evidence
Lior's decision, 22.09.2026.

## Alternatives considered
DDs in the repo: simpler for Claude, but puts Yad Vashem documents on an external host.

## Exception (Anna, 13.10.2026)
The YV fields workbook (`מיפוי שדות ורשימות 2 1.xlsx`) is also committed under `sources/`, so every implementer and their Claude has it without SharePoint access. The DDs and every other client document stay on SharePoint. `.gitignore` carries a named exception for this one file only.

## Affects
sources/SOURCES.md, .gitignore. Means the shared reviewer must run on a machine with the SharePoint folder synced.

## History
- 22.09.2026: accepted.
- 13.10.2026: exception for the YV fields workbook, decided by Anna.
- 13.10.2026: reversed by D-031. Documents may be committed unless they hold personal information; the git copy of the DD is binding. The YV fields workbook holds exported records, so it is removed from the tree (Lior).
