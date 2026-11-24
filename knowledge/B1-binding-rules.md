# B1: binding rules from Doc 2 V9 that are easy to miss

Source: Doc 2 V9, chapter "זמינות מדריכים וניהול נותני שירות" (file `19-...` from `scripts/dd_to_text.py`), screen "כרטיס נותן השירות", and chapter "ישויות".

## Rules

| # | Rule | Where in V9 |
|---|---|---|
| R1 | "Service provider" is an umbrella: guides, lecturers, testimony givers, YV employees, photographers, cantors, actors, volunteers, freelancers. One person can hold one or more statuses | intro |
| R2 | Contact with the Guide record type is the master identity. GoMeddo Staff is the scheduling representation of the same person | para 2 |
| R3 | **Statuses are dated records**: status type, start date, end date. Status types named by V9 include "מדריך 25", "מדריך רגיל", national service volunteer, IDF education unit soldier. The status decides which products the person may deliver and affects payment | para 2; entity table row "סטטוס נותן שירות (רשומה מתוארכת)"; V13-03 |
| R4 | Contact fields named by V9: employment type (Malam employee / service provider / writers' fee), signed contract returned and its validity, YV employee indicator, YV full-time / part-time, job percentage, languages, tax coordination and its validity, attachments | entity table, Contact row |
| R5 | A service provider without a signed, valid service contract cannot be scheduled. Only exception: guides of the IDF bases city who received an extension, flagged as such (V13-07; enforcement is in B3) | para 3 |
| R6 | Certification matrix: which audiences (group types, e.g. high school, recruits, special education) and languages the guide may guide. The generic course gives the base matrix; more are added over time. Used as fixed filters in scheduling | para 4 |
| R7 | Contract days fields named by V9: weekday, hours scope (3 or 6), contract validity range (ten months a year for the "25" guides), contract product, entitlement on YV non-working days | entity table, contract days row |
| R8 | Sensitive data (ID number, personal rates, feedback visits and scores) is visible only to authorized training-department users, through field-level security (V13-05) | para "תיק המדריך"; V13-05 |
| R9 | A new guide is created with a dated status, matrix and languages, **or** a course registrant (Registration__c) who completed the guide course is converted to a service provider without creating a new record (A13-08) | step 1; entity table row "רישום" |

## Where this conflicts with the current mapping or plan (open, needs Lior and Niv)

| # | Finding | Affects |
|---|---|---|
| F1 | R3 confirms D-005: the DD itself defines dated status records. D-005 is a DD requirement, not only a proposal | D-005 |
| F2 | The status picklist must include "מדריך 25" and "מדריך רגיל", which are not among the 11 source checkboxes. The source keeps "מדריכי ה-25" as a Contact checkbox (`new_is_guide_with_tenure`); per R3 it should be a status value | mapping/B1-field-mapping.md, B1-02, B1-03 |
| F3 | R7 says contract days carry a **weekday** (singular). If each record is one weekday, D-006 (one multi-select field) is wrong for Contract_Days__c | D-006, B1-05 |
| F4 | A13-08 converts a `Registration__c` record. Registration__c is an events entity from Doc 3 and does not exist yet. B1-09 cannot be built or tested until it does | B1-09, dependency on Doc 3 |
| F5 | Compared with R4: languages is in the mapping (as a relationship). Missing from the source: tax coordination and its validity, signed contract validity. Signed contract, YV full-time, YV part-time and job percentage exist in the source but have no system name, so the mapping marks them "question" | B1-02, B1-08 |
