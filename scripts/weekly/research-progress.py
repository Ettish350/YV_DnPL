#!/usr/bin/env python3
"""Field-level progress of the data-model research (Stage A object files), for the weekly progress check.

Reads every object file `tickets/Data Migration/NN_*.md` on origin/main and on every other remote branch
(pushed work counts even without a pull request), and counts, per file and per branch, how many fields of
the legacy source entity the file has gone through: a legacy field is "gone through" when its system name
appears in the file's Field Mapping table (Old API Name) or anywhere under a "Dropped legacy fields" heading.
The legacy fields per entity come from the "שדות" sheet of `sources/שדות מערכת ישנה.xlsx`.

Writes sources/local/org-snapshot-<today>/_research.md and _research.json. Read-only on git (fetch only).
Usage: python3 scripts/weekly/research-progress.py
"""
import datetime
import json
import os
import re
import subprocess
import sys
from collections import defaultdict

import openpyxl

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
FOLDER = "tickets/Data Migration/"
LEGACY = os.path.join(ROOT, "sources", "שדות מערכת ישנה.xlsx")
OUT = os.path.join(ROOT, "sources", "local", f"org-snapshot-{datetime.date.today():%Y-%m-%d}")
TOKEN = re.compile(r"\b[a-z][a-z0-9]*_[a-z0-9_]+\b|\b[a-z]{4,}\b")


def git(*args):
    return subprocess.run(["git", "-C", ROOT, *args], capture_output=True, text=True).stdout


def legacy_fields():
    ws = openpyxl.load_workbook(LEGACY, read_only=True, data_only=True)["שדות"]
    rows = ws.iter_rows(values_only=True)
    head = next(rows)
    ent, name, label = head.index("שם היישות"), head.index("שם השדה - שם מערכת"), head.index("שם השדה")
    fields, labels = defaultdict(set), {}
    for r in rows:
        if r[ent] and r[name]:
            fields[r[ent]].add(str(r[name]).strip().lower())
            labels[(r[ent], str(r[name]).strip().lower())] = r[label]
    return fields, labels


def parse(text):
    """Status, API name, Old API Name tokens, dropped tokens, mapping rows, open questions, Not built rows."""
    status = re.search(r"\|\s*Status\s*\|\s*([^|]+)\|", text)
    api = re.search(r"\|\s*API Name\s*\|\s*([^|]+)\|", text)
    entity = re.search(r"Legacy entity:\s*`([^`]+)`", text)
    mapped, dropped, rows, questions, not_built = set(), set(), 0, 0, 0
    section, old_col, q_col = "", None, None
    for line in text.splitlines():
        if line.startswith("#"):
            section = line.lower()
            old_col = None
            continue
        if "dropped legacy" in section:
            dropped |= {t for t in TOKEN.findall(line.lower())}
            continue
        if not line.startswith("|"):
            continue
        cells = [c.strip() for c in line.strip().strip("|").split("|")]
        if "Old API Name" in cells:
            old_col, q_col = cells.index("Old API Name"), (cells.index("Questions") if "Questions" in cells else None)
            continue
        if old_col is None or set("".join(cells)) <= set("-: "):
            continue
        rows += 1
        if old_col < len(cells):
            mapped |= {t for t in re.split(r"[;,\s`]+", cells[old_col].lower()) if t}
        if q_col is not None and q_col < len(cells) and cells[q_col] and not cells[q_col].startswith("[Implementer]"):
            questions += 1
        if "not built" in line.lower() or "not created" in line.lower():
            not_built += 1
    return {
        "status": status.group(1).strip() if status else "?",
        "api_name": api.group(1).strip() if api else "?",
        "entity_hint": entity.group(1).strip() if entity else None,
        "mapped": mapped, "dropped": dropped, "rows": rows, "open_questions": questions, "not_built": not_built,
    }


def main():
    subprocess.run(["git", "-C", ROOT, "fetch", "-q", "--prune", "origin"])
    fields, labels = legacy_fields()
    refs = [r for r in git("for-each-ref", "--format=%(refname:short)", "refs/remotes/origin").split()
            if r not in ("origin", "origin/HEAD")]
    refs.sort(key=lambda r: (r != "origin/main", r))
    # A branch whose pull request is merged or closed is history (squash merges leave its commits off main).
    try:
        prs = json.loads(subprocess.run(["gh", "pr", "list", "--state", "all", "--limit", "200", "--json", "headRefName,state,number"],
                                        cwd=ROOT, capture_output=True, text=True).stdout or "[]")
    except Exception:
        prs = []
    pr_of = {p["headRefName"]: p for p in prs}
    refs = [r for r in refs if pr_of.get(r[len("origin/"):], {}).get("state", "OPEN") == "OPEN"]
    main_blobs = {}
    results = []
    for ref in refs:
        for line in git("ls-tree", ref, FOLDER).splitlines():
            meta, path = line.split("\t", 1)
            name = os.path.basename(path)
            if not re.match(r"\d\d_.*\.md$", name):
                continue
            blob = meta.split()[2]
            if ref == "origin/main":
                main_blobs[name] = blob
            elif main_blobs.get(name) == blob:
                continue  # same as main: nothing new on this branch
            elif not git("log", "--format=%h", f"origin/main..{ref}", "--", path).strip():
                continue  # not changed on this branch since it left main: an old copy, not new work
            text = git("show", f"{ref}:{path}")
            if "## Salesforce Object Details" not in text:
                continue  # a questions file, not an object file
            p = parse(text)
            seen = p["mapped"] | p["dropped"]
            entity = p["entity_hint"] if p["entity_hint"] in fields else \
                max(fields, key=lambda e: len(fields[e] & seen)) if seen else None
            if entity and not fields[entity] & seen and entity != p["entity_hint"]:
                entity = None
            total = len(fields[entity]) if entity else 0
            covered = fields[entity] & seen if entity else set()
            last = git("log", "-1", "--format=%ad|%an", "--date=short", ref, "--", path).strip().split("|")
            results.append({
                "ref": ref.replace("origin/", ""), "pr": pr_of.get(ref[len("origin/"):], {}).get("number"), "file": name, "object": p["api_name"], "status": p["status"],
                "legacy_entity": entity, "legacy_total": total, "legacy_done": len(covered),
                "legacy_mapped": len(fields[entity] & p["mapped"]) if entity else 0,
                "legacy_dropped": len(fields[entity] & p["dropped"]) if entity else 0,
                "legacy_left": sorted(labels.get((entity, f)) or f for f in fields[entity] - seen)[:15] if entity else [],
                "rows": p["rows"], "open_questions": p["open_questions"], "not_built": p["not_built"],
                "last_commit": last[0] if last[0] else "?", "author": last[1] if len(last) > 1 else "?",
            })
    os.makedirs(OUT, exist_ok=True)
    with open(os.path.join(OUT, "_research.json"), "w") as f:
        json.dump(results, f, ensure_ascii=False, indent=1)
    lines = [f"# Research progress {datetime.date.today():%Y-%m-%d}", "",
             "Legacy fields gone through = in the Field Mapping table (Old API Name) or under Dropped legacy fields. "
             "A branch is listed only for files changed on it and not yet on main. "
             "Open questions = non-empty Questions cells that are not `[Implementer]` decisions.", "",
             "| Branch | File | Object | Status | Legacy entity | Gone through | Mapped | Dropped | Rows | Open questions | Not built | Last commit | Author |",
             "|---|---|---|---|---|---|---|---|---|---|---|---|---|"]
    for r in results:
        pct = f"{r['legacy_done']}/{r['legacy_total']} ({100 * r['legacy_done'] // r['legacy_total']}%)" if r["legacy_total"] else "no legacy entity matched"
        lines.append(f"| {r['ref']}{' (PR #%s)' % r['pr'] if r['pr'] else ''} | {r['file']} | {r['object']} | {r['status']} | {r['legacy_entity'] or '?'} | {pct} | "
                     f"{r['legacy_mapped']} | {r['legacy_dropped']} | {r['rows']} | {r['open_questions']} | {r['not_built']} | "
                     f"{r['last_commit']} | {r['author']} |")
    with open(os.path.join(OUT, "_research.md"), "w") as f:
        f.write("\n".join(lines) + "\n")
    print(f"research: {os.path.relpath(OUT, ROOT)}/_research.md ({len(results)} file versions)")


if __name__ == "__main__":
    sys.exit(main())
