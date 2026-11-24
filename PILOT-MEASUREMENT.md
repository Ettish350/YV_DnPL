# Measuring the pilot

The pilot (A1, B1) exists to answer one question: **where does this way of working save hours, and how many.** The answer is a range per kind of work, not one global percentage.

## What is recorded for every ticket

| Field | Who records it | When |
|---|---|---|
| Category: fields/objects, layout, validation, automation, data load, permissions, testing | Owner's Claude, from the ticket | When the ticket is written |
| Estimate: human hours without Claude | Ticket ("Human build hours") | When the ticket is written, before work starts. Never changed afterwards |
| Actual hands-on hours | Owner's Claude, from start and end times noted **during** the session | End of each session |
| Senior review minutes | The reviewer (Lior or architect), in the pull request | When reviewing |
| Rework hours | Owner's Claude | If the ticket had to be redone after review |
| What had to be fixed by hand | Owner's Claude, one line | End of ticket |

Rules:
- Hours are logged at the time, not reconstructed later. At the start of work on a ticket Claude notes the time in the sync file; at the end it notes the time again.
- Review time counts as cost. A ticket built fast that needed an hour of senior review did not save that hour.
- The shared reviewer cross-checks the logged hours against commit times and org change times, and flags gaps.

## Where it lives
The ticket table in `sync/<epic>-<name>.md` (columns below). Nothing is kept outside the repo.

| Key | Category | Est. h | Actual h | Review min | Rework h | Fixed by hand |
|---|---|---|---|---|---|---|

## How it is read
- Per category: actual (plus review and rework) divided by estimate. Compared within the same owner, so differences between people do not distort it.
- Reported as a range per category, e.g. "fields/objects: 30 to 45 percent of the estimate".
- A1 against B1 shows whether the saving depends on the kind of epic.
