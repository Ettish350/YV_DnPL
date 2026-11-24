# D-041: The schedule template is chosen by bundle and subject

| | |
|---|---|
| Status | **accepted** |
| Decided by | Lior Pridor |
| Date | 16.11.2026 |
| Challenges | none |

## Decision
The schedule template is a separate object: template lines (תבנית שורות לו"ז), one record per line, with bundle, subject, product, sequence and duration. When the coordinator picks the subject on the activity day, the system finds the template lines of the order's bundle and the chosen subject, offers to apply them, and creates one child reservation per line under the activity day, linked to the product, without hours. Bundle items stay the order and quote lines; they do not change with the subject.

This confirms Lior's catalog model of 9.11.2026.

## Deviation from the DD (known)
Doc 2 process 01, rule 4: "תבנית שורות הלו"ז נגזרת מנושא יום הפעילות ולא מהחבילה". Here the template is chosen by bundle plus subject, because the same subject needs a different schedule in a 3-hour and a 6-hour visit. Internal ruling; Yad Vashem is not told proactively (Lior, 15.10.2026, scope communication rule).

## What does not change (Doc 2 process 01, rules 4 and 8)
Template lines are guiding lines only (opening, discussion, workshop, museum tour); a lecture is not part of the template; only products already in the quote can be used (V01-04); the coordinator can remove lines; "create template" copies one group's schedule to all groups of the activity day. A subject does not change the price unless a coordinator approves it (D-025).

## Affects
`knowledge/model-register.md` row 12 (closes the source conflict). Build: package 1 (Neev, GoMeddo), after D-040's open point.

## History
- 16.11.2026: accepted.
