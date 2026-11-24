# Binding sources

This file says exactly which version of each source is binding, so every Claude session reads the same thing.

**The binding DDs are the files committed in this repo under `sources/dd/` (D-031).** They are byte-identical to the approved originals in the SharePoint binding folder: same size, same SHA-256 (table below). A copy anywhere else, including SharePoint, is binding only if its hash matches. `dd/` is a Markdown reading copy generated from the files in `sources/dd/`; it is not binding.

The YV fields workbook holds exported records with personal data (service providers, institutions, contacts), so it is **not** in git (D-031). Its only copy is in the SharePoint binding folder.

SharePoint binding folder, where the originals are kept (Malam Salesforce Team Site):
`General / פרויקטים פעילים (ar) / יד ושם / Order module claude code / _SharePoint Upload 2026-09-07 / Yad Vashem - מסמכי עבודה - 2026-09-07 / 3. Sales & Marketing - מכירות ושיווק / 01 אפיון / 00 מקורות מחייבים /`

Sync that folder to your computer (OneDrive "Sync" or "Add shortcut to My files") only if you need the YV fields workbook. For the DDs, use `sources/dd/`.

| Doc | Binding version | Binding copy | How to verify you have the right file |
|---|---|---|---|
| Doc 1: Introduction and Architecture (מסמך 1 - מבוא וארכיטקטורה) | **Approved by YV, 06.10.2026** | `sources/dd/אפיון מודול הזמנות מאושר חלק 1 מתוך 3 6-10-2026 .docx` (note the space before `.docx`) | 468,563 bytes, SHA-256 `d5eca50433dc0a2e6303c0f9b627bf2843cfd4e51e202898bc593127d00514c9`, 244 Word comments (all resolved). Title page reads "מסמך 1 מתוך 3" |
| Doc 2: Ordering Processes (מסמך 2 - תהליכי ההזמנות) | **Approved by YV, 06.10.2026** | `sources/dd/אפיון יד ושם מודול הזמנות חלק 2 מאושר 06-10 .docx` (note the space before `.docx`) | 2,500,636 bytes, SHA-256 `549c36d6407ba5f23c891aede5bef4ec85866513cc346e76a72197a0cc347dd5`, 758 Word comments (746 resolved). Title page reads "מסמך 2 מתוך 3" |
| YV fields workbook | Final list from YV (uploaded 16.09.2026) | SharePoint only: `מיפוי שדות ורשימות 2 1.xlsx`. Not in git: it holds personal data (D-031). It was committed under `sources/` on 13.10.2026 and removed from the tree the same day | 56 entities, 1,262 fields, 532 picklist values; size 5,430,244 bytes; SHA-256 fbe234d7f6162925a0c3a272bfe65bffc58d80efccde8e5680de7bb2679455cf |
| Doc 3: Events and Finance (מסמך 3 - אירועים וכספים) | **Approved by YV as final, file of 04.11.2026** (D-037, Lior 08.11.2026) | `sources/dd/מסמך 3 אירועים וכספים - מאושר על ידי יד ושם - 2026-11-04.docx` | 3,929,052 bytes, SHA-256 `e4d10ed6943fc8bff138e823b4b6b39319ad15d70f30add99b404f23023abeb9`, 235 Word comments (187 resolved), no tracked changes. Three screenshots in appendix 13 show real customer records and were replaced with a placeholder (D-031), so this copy does **not** match the original (4,135,013 bytes, SHA-256 `bb8dbd3e7dab3169f3eda7bf6c3ed278b553ef01ed5abfa190c869ee2192e62c`); the body (`word/document.xml`) is identical. Match against this row, not the original. Changes from V35: `sources/DD-CHANGES-DOC3-2026-11-04.md` |
| Legacy-fields file (YV fields workbook without records) | Made on 13.10.2026 from the binding YV fields workbook above | `sources/שדות מערכת ישנה.xlsx` (in git). Sheets "רשימת כל היישויות" and "שדות" are unchanged; every other sheet keeps only its header row, so no personal data. This is the source for the Data Migration workbook | 101,723 bytes, SHA-256 `c0edcc0d40b05c96de8b683a23099f2a78e6da13a3e11d8e1b538a7a8fe46b31` |

**Superseded, do not use:**

- Doc 3 V35 (`...מסמך 3 אירועים וכספים - V35 - 2026-10-28.docx`, sent to YV 28.10.2026) and every earlier Doc 3 draft (replaced by the approved file of 04.11.2026).
- `מסמך 1 - מבוא וארכיטקטורה - גרסה מחייבת - 2026-08-31.docx` and `...מסמך 1 מבוא וארכיטקטורה - סופי עם הערות הלקוח ומענה מלם - 2026-08-31 (1).docx` (replaced by the approved Doc 1 of 06.10.2026; moved to `גרסאות קודמות` on 06.10.2026).
- `אפיון יד ושם - מודול הזמנות - מסמך 2 - V9 ביקורת עקביות וניסוח - 2026-09-15.docx` and `...מסמך 2 - לאחר תיקונים וקריאה מודרכת - 2026-09-15 (1).docx` (replaced by the approved Doc 2 of 06.10.2026; moved to `גרסאות קודמות` on 06.10.2026).
- `מסמך 2 - תהליכי ההזמנות - גרסה מחייבת V4 - 2026-09-06.docx` (moved to `גרסאות קודמות` on 22.09.2026; replaced by V9).
- `...מסמך 2 תהליכי ההזמנות - V11 - נשלח ללקוח - 2026-08-27.docx` (what YV received before their 9.9 comments).
- `אפיון יד ושם  מודול הזמנות  מסמך 1 מבוא וארכיטקטורה גרסה סופית - RTL fixed.docx` at the project root. Despite its name it is the old full V3.
- Anything under `גרסאות קודמות`, or any DD copy whose SHA-256 does not match the table.

**If your file does not match the table, stop and ask Lior.** Do not work from a copy you found elsewhere.

The DDs are written in Hebrew. Read them in Hebrew. Chapter names below are given in English with the Hebrew heading so you can find them.

## Drafts (not binding)

None at present. Doc 3 was here as a draft (V28 to V35) from 26.10 to 08.11.2026 (`sources/drafts/doc3/`); the approved version is now in the binding table above.


## Legacy system code

The code of the current Tahel system (Dynamics plugins, the Tahel web API, the PDF and Word generator, the kiosk) is in `sources/legacy-code/`, committed 13.10.2026 with secrets and private contacts removed (D-031). Reference only: the DD wins. See `sources/legacy-code/README.md`.

## Doc 2 chapters used by the pilot

| English | Hebrew heading in Doc 2 | Used by |
|---|---|---|
| Entities, entity map | ישויות / מפת הישויות המשתתפות בתהליכי ההזמנות | both |
| Data model additions | השלמות למודל הנתונים הנדרשות לתהליכי ההזמנות | both |
| Open data-model issues | נושאים פתוחים במודל הנתונים | both |
| Building products, bundles and activity day templates | בניית מוצרים, חבילות ותבניות יום פעילות | A1 |
| Guide availability and service provider management | זמינות מדריכים וניהול נותני שירות | B1 |
| Service provider card (screen) | כרטיס נותן השירות (under מסכים מרכזיים) | B1 |
| Users, permissions and data migration | יוזרים, הרשאות והסבת נתונים | both |
| Security | אבטחת מידע | B1 |
| Fields | שדות | both |

## Markdown edition in the repo

The DDs are also in the repo as Markdown under `dd/`, generated with `scripts/dd_to_md.py` (Doc 1 and Doc 2 from the approved files on 06.10.2026; Doc 3 added on 08.11.2026). The binding files are the docx in `sources/dd/` (D-031), identical to the originals in SharePoint `01 אפיון/00 מקורות מחייבים` except Doc 3's three redacted screenshots:
- Doc 1: אפיון מודול הזמנות מאושר חלק 1 מתוך 3 6-10-2026 .docx
- Doc 2: אפיון יד ושם מודול הזמנות חלק 2 מאושר 06-10 .docx
- Doc 3: מסמך 3 אירועים וכספים - מאושר על ידי יד ושם - 2026-11-04.docx

What changed in the body text against the previous binding versions: `sources/DD-CHANGES-2026-10-06.md` (Doc 1 and Doc 2) and `sources/DD-CHANGES-DOC3-V35.md` and `sources/DD-CHANGES-DOC3-2026-11-04.md` (Doc 3).

Comment numbers (`c123`) are Word comment ids. They were renumbered in the approved files, so a comment number quoted from the old Markdown (before 06.10.2026) does not point to the same thread now.
