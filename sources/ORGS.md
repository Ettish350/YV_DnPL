# Salesforce orgs

| Alias | What it is | Used for | Who connects |
|---|---|---|---|
| `yv-partial` | Partial copy sandbox. **The master sandbox** (D-016) | All pilot build and testing | Every builder, with their own user |
| Production | Live org (Cases module is live there) | **Never connected from this repo** | Nobody |

## Connecting (once per person)
```
sf org login web --instance-url <sandbox My Domain URL, ask Lior or Anna> --alias yv-partial
```
Use your own sandbox user, never a shared one. The daily agent identifies who built what by user.

Do not run `sf config set --global`. Always pass `--target-org yv-partial` explicitly.

## Data
A partial copy sandbox holds real Yad Vashem records, including sensitive fields. Use metadata commands only (describe, list, retrieve). Any `sf data` command needs Lior's or Anna's approval (enforced in `.claude/settings.json`).
