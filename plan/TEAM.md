# Team

Version 1, 12.10.2026. One row per person. The weekly progress check (`.claude/commands/weekly-progress.md`) uses this table to join Jira assignees, git authors, GitHub handles and the Salesforce users who created or modified metadata in `yv-partial` to one person, and writes its per-person sections in this order.

`?` = not matched with certainty. Lior or Anna fills it in.

| Name | Hebrew | Role | GitHub handle | Git author name(s) | Salesforce user (`yv-partial`) | Jira display name | Work this sprint (13.10 to 22.10.2026) |
|---|---|---|---|---|---|---|---|
| Lior Pridor | ליאור פרידור | PM. Reviews, approves, decides | `liorpridorrrrrrr` | `Lior Pridor <lior.pridor@gmail.com>`, `Lior Pridor <liorpridor@MacBook-Pro.local>` | Lior Pridor | Lior Pridor | Planning, review, approvals |
| Anna Pertzovski | אנה פרצובסקי | Delivery lead. Same repo rights as Lior | `AnnaPertzovski` | `Anna Pertzovski <Anna_p@malamteam.com>` | Anna Pertzovski | Anna Pertzovski | Catalog: product, bundle and bundle item, activity day subject (research, build, layouts). 68h |
| Niv (Neev) Doron | ניב דורון | Architect and implementer | `Neev1993` (invitation pending since 23.09.2026) | ? (no commits yet) | Neev Doron | Neev Doron | Bundle in the order (A01-02 flows, filters); scheduling: assignment, resource, hours regulation and closures; order and activity day (research, build, layouts). 155.5h |
| Or Rozner | אור רוזנר | Implementer | `Or-Rozner` | ? (no commits yet) | Or Rozner (not Or Ashbel, who is also a user in `yv-partial`) | Or Rosner (spelled with s in Jira) | Institutions: institution and institution characteristics, department (research, build, layouts). 33.5h |
| Tal Arviv | טל ארביב | Implementer, portal | `TalArvivMalam` (invitation pending since 22.09.2026) | ? (no commits yet) | Tal Arviv | Tal Arviv | Portal skeleton (site, design, RTL, home page, portal permissions, parent and child institution) and first booking components. 160h |
| Mikey (Miki) Linial | מיקי ליניאל | ? | `mikeylinial` (not a collaborator yet; one pending invitation of 22.09.2026 shows no handle) | ? | Mikey Linial | Mikey Linial | Not in this sprint. Donorbox sync in `yv-partial` (11.10.2026), outside the orders module |

Reviewers for this sprint's tasks are not yet assigned. Mapping rows marked `review` or `question` are approved by Lior; `high` rows are self-merged and built right away (D-030).

## Notes
- Tal Arviv is not Tal Shamrit (טל שמרית), who comments on the DD for Yad Vashem.
- `yv-partial` is shared with other Yad Vashem work (for example fundraising and Donorbox). Changes by other Salesforce users are reported under "Unplanned work" only when they touch objects of the orders module.
- A git author, PR author or Salesforce user that matches no row is reported with the raw name.
