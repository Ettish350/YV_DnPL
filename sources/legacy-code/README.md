# Legacy system code (Tahel, Dynamics, PDF generator, kiosk)

Source code of Yad Vashem's current orders system ("תה"ל", on Dynamics CRM), committed on 13.10.2026 under D-031. **Reference only.** It shows how the old system behaves today; it is not a spec. **The DD wins**: where the code and the approved Doc 1 or Doc 2 disagree, follow the DD and, if it matters, open a question Issue. Never port code from here.

Use it to answer "what does the old system actually do" questions: a rule the DD mentions only briefly, a calculation, a status flow, the meaning of a field in the YV fields workbook.

## What is here

| Folder | What it is | From |
|---|---|---|
| `dynamics-plugins/` | Dynamics CRM plugins (C#, `ReservationsPlugIn`): logic that runs on create and update of Activity Day, Budget Framework, Event Item, Event Registration, Event Reservation, Itinerary Group, Reservation, Institution (`RsvInstitution`), Service Provider and Service Provider Status | `OneDrive_1_8-19-2026/אוטומציות מהדיינמיקס - קוד` |
| `tahel-extracted/` | Extracted code of the Tahel web API (`YVReservationsAPI`): services for reservations, activity days, schedules, occupancy, resources, service providers, invoices; in `_round2/` online reservation, new reservation, payment (Pelecard), closed days, availability, budget frameworks, institutions, reports and more. `kiro__requirements.md` and `kiro__design.md` are an earlier attempt to document its business logic | `03 חומרים מהלקוח והמערכת הקיימת/קוד תה"ל שחולץ` |
| `pdf-word-generator/` | The service that produces confirmations, invoices, schedules, commemoration letters and reports as PDF and Word (C#) | `OneDrive_1_8-19-2026/קוד ליצירת קבצי PDF וקבצי WORD` |
| `kiosk/` | The entrance kiosk front end (Angular, TypeScript) | `OneDrive_1_8-19-2026/קוד של הקיוסק` |

The originals stay where they were on SharePoint. File names and folders are kept, so a path here maps to the same path in the original folder.

## What was left out or changed (D-031)

- **Left out**: build output and dependencies (`bin/`, `obj/`, `packages/`, `node_modules/`, `dist/`, `.angular/`, `.vs/`, `.github/`, `package-lock.json`), settings and publish files (`appsettings*.json`, `launchSettings.json`, `*.pubxml`, `*.user`), the signing key (`*.snk`), and sample PDFs and images.
- **Why the settings files are out**: `PdfCreator/appsettings.Development.json` holds a Yad Vashem domain account with its password in plain text, and the publish profiles point to internal servers. The same password was also written in `pdf-word-generator/Data/Repositories/HelperRepository.cs`.
- **Replaced with placeholders** (`<REDACTED-...>`): that password (2 places), a Google API key in `tahel-extracted/_round2/Services__Services__GoogleApiService.cs`, the private mobile numbers and email of the cafeteria operators in the confirmation-letter texts, and two private Gmail addresses in `WriteToLog.cs`.
- Kept: work emails of Yad Vashem staff and Yad Vashem's organisational addresses and office numbers (allowed by D-031).
- Text files were re-saved as UTF-8; nothing else in the code was changed.

**Action for Yad Vashem (Lior):** the domain password above and the Google API key were exposed in shared folders; Yad Vashem IT should change them.
