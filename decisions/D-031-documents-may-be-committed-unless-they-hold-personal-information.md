# D-031: Documents may be committed unless they hold personal information; the git copy of the DD is binding

| | |
|---|---|
| Status | **accepted** |
| Decided by | Lior Pridor |
| Date | 13.10.2026 |
| Reverses | D-014 |
| Challenges | none |

## Decision
- Client documents (the DDs, Yad Vashem workbooks) and our own Word and Excel files **may be committed** to this repo, **unless they contain personal information**.
- **Personal information** = data about real people as private individuals: names together with ID numbers, private phone numbers, private emails, addresses, dates of birth, or any other detail of visitors, guides, customers, contacts or staff records; and any **exported records** from a system (Dynamics, Salesforce or other), even partial. Such files never go into git.
- **Not personal information**: names, work emails, work phones and roles of people acting in their project role (Yad Vashem staff as document authors or commenters, the Malam team, owners in a plan), and organisational contact details (for example `visit@yadvashem.org.il`, Yad Vashem office numbers, the organisation's registration number).
- **Before committing** a document, check it: search for emails, phone numbers and nine-digit numbers, and look at every sheet of a workbook. If any sheet holds records of people, do not commit the file. A copy with those sheets removed may be committed instead, under a name that says so.
- **The binding DD is the git copy.** The approved Doc 1 and Doc 2 are committed under `sources/dd/`. Those files are binding. They must be byte-identical to the originals in the SharePoint binding folder: same size and SHA-256 as listed in `sources/SOURCES.md`. If a copy anywhere does not match that hash, it is not binding. `dd/` (Markdown) is a reading copy generated from the git files; it is not binding.
- A new DD version is committed by Lior or Anna through a PR that also updates `sources/SOURCES.md` and regenerates `dd/`.

## DD source / evidence
Lior's decision, 13.10.2026, with his ruling of the same day that work names of project staff are not personal information and that the git copy of the DD is binding.

## Alternatives considered
D-014 (nothing client-side in git): every implementer and every Claude session needed the SharePoint folder synced, and work was done twice on a wrong local copy (CLAUDE.md, mistakes already made).
Any real name counts as personal information: would bar the DDs, whose Word comments are signed by Yad Vashem and Malam staff.

## Found when this decision was made
The YV fields workbook (`sources/מיפוי שדות ורשימות 2 1.xlsx`, committed on 13.10.2026 under the D-014 exception) holds exported records: service providers (about 2,000 rows with emails, phones and ID-like numbers), institutions and travel agents (about 3,400 rows with emails and phones), and contact details in other sheets. Under this decision it may not be in git. It is removed from the tree by the PR that introduces D-031 and stays on SharePoint. It remains in the git history until the history is cleaned (Lior's decision, separate).

## Affects
`.gitignore`, `sources/SOURCES.md`, `sources/dd/`, CLAUDE.md (start of session, hard rules), WORKFLOW.md, REVIEWER.md, README.md, D-030, `plan/OPERATING-GUIDE.md`, `scripts/README.md`.

## History
- 13.10.2026: accepted. Reverses D-014. The YV fields workbook removed from the tree.
