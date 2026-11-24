#!/usr/bin/env python3
"""Scores every sprint task the same way for the weekly check and the daily status (OPERATING-GUIDE sections 4 and 5,
D-030, .claude/commands/weekly-progress.md steps 3a to 3d), so the two emails always agree.

Inputs, in <dir> (sources/local/weekly or sources/local/daily), written by the headless Claude session:
- jira-<date>.json: {"process": {"key", "summary"} or null, "tasks": [{key, assignee, summary, description,
  jira_status, original_estimate_h}]} (step 2). With no sprint Process the planned tasks have keys plan-1, plan-2, ...
- judgments-<date>.json: [{key, percent (100, 50 or 0), evidence, components: ["<MetadataType>:<fullName>", ...]}]
  for the portal and other tasks (step 3d), the only judged scores.
And, from the scripts that run before Claude: sources/local/org-snapshot-<date>/_inventory/, _pages.json, _research.json,
plan/sprints/<start>.md, plan/TEAM.md, mapping/entities.md and the object files (git show, read-only).

Writes <dir>/tasks-<date>.json (the daily schema: assignee, key, summary, jira_status, original_estimate_h, percent,
evidence, plus rule: the section of the guide that produced the score), <dir>/scores-<date>.json (sprint dates,
working days left, status per person, Score disputed, page mismatches) and, with --report, the report file with
section 9 filled in and a marker where Claude writes sections 1 to 8.

Rules, as written in the definition:
- Research: the object file's Status, most advanced of origin/main, an open PR or a pushed branch. Draft = 60 x legacy
  fields gone through / the legacy entity's fields, rounded down; Draft with no legacy entity = 40;
  "In review" scores as Draft (D-035); Approved = 100; no file = 0. Several objects: the average.
- Build with an approved object file: the custom API-name rows (not Not built) found in the inventory, averaged with the
  page score. Without one (the fallback): the share of the components the task names (summary, description, sprint
  tab row) that exist, averaged with the page score. A custom object that is a stub (no custom field, no record page,
  no edited layout) gets no credit.
- Page, per object, from _pages.json: assigned in the Orders app = 100; a record page built or changed by an orders
  implementer but not assigned, or only a page layout a person edited = 50; nothing = 0. Several objects: the average.
- Portal and other tasks: Claude's judgment, but when it differs from the previous run and no listed component was
  created or modified since the previous snapshot, the previous score is kept and the task goes under Score disputed.
- Hours left = estimate x (1 - percent / 100). Status per person: blocked if an open task's evidence says blocked;
  else at risk if hours left > working days left x 6; else on track. No sprint tasks, no status.

Usage: python3 scripts/weekly/score-tasks.py --dir sources/local/weekly [--report reports/weekly/progress-<date>.md]
       [--date YYYY-MM-DD] [--run weekly|daily] [--root <repo>] [--prev <tasks.json>]
Read-only on git and the org. Never writes to Jira.
"""
import argparse
import datetime as dt
import glob
import json
import math
import os
import re
import subprocess
import sys

HOURS_PER_DAY = 6
ORDERS_IMPLEMENTERS = ("Anna", "Niv", "Or", "Tal")  # guide section 5, Layouts
API = re.compile(r"\b[A-Za-z][A-Za-z0-9_]*__c\b")
OBJ_FIELD = re.compile(r"\b([A-Za-z][A-Za-z0-9_]*)\.([A-Za-z][A-Za-z0-9_]*__c)\b")
RESEARCH = re.compile(r"מחקר השדות|research", re.I)
BUILD = re.compile(r"בניית|ושדות|השדות|עמוד|לייאאוט|layout|lightning page", re.I)
BUILDS_OBJECT = re.compile(r"בניית (?:ה?אובייקט|שני האובייקטים)|אובייקטים ושדות")
NUMBER_WORDS = {"two": 2, "three": 3, "שני": 2, "שלושה": 3}
RULE = {
    "research": "guide 5 Research (D-030; weekly-progress 3a)",
    "build": "guide 5 Build + Layouts, approved object file (weekly-progress 3b, 3c)",
    "fallback": "guide 5 Build fallback + Layouts, no approved object file (weekly-progress 3b, 3c)",
    "judged": "guide 5 Portal and other tasks, judged (weekly-progress 3d)",
    "kept": "guide 5 Portal and other tasks, judged; previous score kept, disputed (weekly-progress 3d)",
    "unjudged": "guide 5 Portal and other tasks, not judged this run; previous score kept (weekly-progress 3d)",
}


def load(path, default=None):
    if not path or not os.path.exists(path):
        return default
    with open(path, encoding="utf-8") as f:
        return json.load(f)


def git(root, *args):
    return subprocess.run(["git", "-C", root, *args], capture_output=True, text=True).stdout


def md_rows(text, header_has):
    """Rows of the markdown tables whose header has the column header_has, as dicts."""
    out, head = [], None
    for line in text.splitlines():
        if not line.startswith("|"):
            head = None
            continue
        cells = [c.strip() for c in line.strip().strip("|").split("|")]
        if head is None:
            head = cells if header_has in cells else False
            continue
        if head and not set("".join(cells)) <= set("-: "):
            out.append(dict(zip(head, cells)))
    return out


def fnorm(name):
    """Field or object name without a managed namespace prefix, lower case."""
    name = name.lower()
    return name.split("__", 1)[1] if name.count("__") >= 2 else name


def floor1(x):
    return math.floor(x * 10 + 1e-9) / 10


# ---------- context ----------

class Ctx:
    def __init__(self, root, date):
        self.root, self.date = root, date
        snap = os.path.join(root, "sources", "local", f"org-snapshot-{date}")
        self.snap = snap
        self.inv = {}
        for p in glob.glob(os.path.join(snap, "_inventory", "*.json")):
            self.inv[os.path.basename(p)[:-5]] = (load(p, {}) or {}).get("result") or []
        self.objects = {fnorm(x["fullName"]): x for x in self.inv.get("CustomObject", [])}
        self.fields = {}
        for x in self.inv.get("CustomField", []):
            o, f = x["fullName"].split(".", 1)
            self.fields[(fnorm(o), fnorm(f))] = x
        self.pages = load(os.path.join(snap, "_pages.json"))
        self.research = load(os.path.join(snap, "_research.json"), [])
        self.team = team(root)
        self.implementers = {r["sf"] for r in self.team if r["name"].split()[0] in ORDERS_IMPLEMENTERS}
        self.entities = entities(root)
        self.tab = sprint_tab(root, date)

    def custom_fields_of(self, obj):
        return [k for k in self.fields if k[0] == fnorm(obj)]

    def object_exists(self, obj):
        return fnorm(obj) in self.objects

    def page_info(self, obj):
        return (self.pages or {}).get(obj) or next((v for k, v in (self.pages or {}).items() if k.lower() == obj.lower()), None)


def team(root):
    rows = md_rows(open(os.path.join(root, "plan", "TEAM.md"), encoding="utf-8").read(), "Jira display name")
    out = []
    for r in rows:
        clean = lambda s: re.sub(r"\s*\(.*?\)", "", s.replace("`", "")).strip()
        out.append({"name": r["Name"], "first": r["Name"].split()[0], "jira": clean(r["Jira display name"]),
                    "sf": clean(r["Salesforce user (`yv-partial`)"])})
    return out


def entities(root):
    out = {}
    for r in md_rows(open(os.path.join(root, "mapping", "entities.md"), encoding="utf-8").read(), "Source entity"):
        t = r.get("Proposed target", "")
        if re.fullmatch(r"[A-Za-z][A-Za-z0-9_]*", t):
            out[r["Source entity"]] = t
    return out


def sprint_tab(root, date):
    files = sorted(f for f in glob.glob(os.path.join(root, "plan", "sprints", "*.md")) if re.search(r"/\d{4}-\d{2}-\d{2}\.md$", f))
    files = [f for f in files if os.path.basename(f)[:10] <= date] or files
    if not files:
        return {"file": None, "rows": [], "start": None, "end": None}
    text = open(files[-1], encoding="utf-8").read()
    dates = re.findall(r"(\d{2})\.(\d{2})\.(\d{4})", text.splitlines()[0])
    d = lambda t: dt.date(int(t[2]), int(t[1]), int(t[0]))
    rows = [r for r in md_rows(text, "Task") if r.get("Task")]
    return {"file": os.path.relpath(files[-1], root), "rows": rows, "start": d(dates[0]), "end": d(dates[-1])}


def sprint_dates(ctx, jira):
    """The dates in the sprint Process summary; with no Process, the approved sprint tab (D-029, D-032)."""
    s = ((jira or {}).get("process") or {}).get("summary") or ""
    m = re.search(r"\((\d{1,2})/(\d{1,2})-(\d{1,2})/(\d{1,2})\)", s)
    year = int(ctx.date[:4])
    if m:
        a, b, c, e = map(int, m.groups())
        return dt.date(year, b, a), dt.date(year, e, c), s
    return ctx.tab["start"], ctx.tab["end"], ctx.tab["file"]


def working_days_left(today, start, end, after_noon):
    """Sunday to Thursday days from the later of today and the sprint start to the sprint end, both included.
    The daily runs at 17:00, after the day's work, so it counts from tomorrow (Lior, 21.10.2026)."""
    first = max(today + dt.timedelta(1) if after_noon else today, start)
    return sum(1 for i in range((end - first).days + 1) if (first + dt.timedelta(i)).weekday() in (6, 0, 1, 2, 3))


# ---------- task to sprint tab row and objects ----------

def tab_row(ctx, summary):
    rows = ctx.tab["rows"]
    s = summary.strip()
    for r in rows:
        if r["Task"].strip() == s:
            return r
    for r in rows:
        if s in r["Task"] or r["Task"] in s:
            return r
    pre = s.split(":")[-1].strip()
    return next((r for r in rows if r["Task"].split(":")[-1].strip() == pre), None)


def entity_prefix(summary):
    return summary.split(":")[0].strip() if ":" in summary else summary.strip()


def research_row(ctx, summary):
    pre = entity_prefix(summary)
    return next((r for r in ctx.tab["rows"] if r["Task"].startswith(pre + ":") and RESEARCH.search(r["Task"])), None)


def entity_targets(ctx, summary):
    """Source entities and target objects from the entity's research row (`<entities> > <targets>; lookups: ...`).
    Targets with no API name ("custom object", "GoMeddo") stay unresolved, unless mapping/entities.md translates the
    only source entity (weekly-progress 3a.1) or the entity's object file names it."""
    r = research_row(ctx, summary)
    src = (r or {}).get("Source entity and lookups", "")
    if ">" not in src:
        return [], []
    left, right = src.split(">", 1)
    right = right.split("; lookups")[0]
    sources = re.findall(r"\bnew_[a-z_]+\b", left)
    targets = []
    for part in [p.strip() for p in right.split(",") if p.strip()]:
        name = re.sub(r"\s*\(.*?\)", "", part).strip()
        if re.fullmatch(r"[A-Za-z][A-Za-z0-9_]*", name) and (name.endswith("__c") or ctx.object_exists(name)):
            targets.append(name)
        else:
            n = next((v for k, v in NUMBER_WORDS.items() if name.lower().startswith(k)), 1)
            targets += [None] * n
    if len(sources) == 1 and targets.count(None) == 1:
        t = ctx.entities.get(sources[0]) or next((f["object"].split()[0] for f in ctx.research
                                                  if f.get("legacy_entity") == sources[0]), None)
        if t:
            targets[targets.index(None)] = t
    return sources, targets


# ---------- 3a research ----------

def research_score(f):
    st = f["status"].lower()
    if st.startswith("approved"):
        return 100
    # D-035 (22.10.2026): only Draft and Approved; a file still marked "In review" scores as a Draft (Lior, 28.10.2026)
    if f["legacy_total"]:
        return 60 * f["legacy_done"] // f["legacy_total"]
    return 40


def score_research(ctx, task):
    sources, targets = entity_targets(ctx, task["summary"])
    named = set(API.findall(task.get("description") or ""))
    resolved = {t for t in targets if t} | named
    units = max(len(sources), len(targets) + len(named - set(targets)), 1)
    by_file = {}
    for f in ctx.research:
        obj = f["object"].split()[0]
        if f.get("legacy_entity") in sources or obj in resolved:
            rank = lambda x: (research_score(x), x["last_commit"])
            if f["file"] not in by_file or rank(f) > rank(by_file[f["file"]]):
                by_file[f["file"]] = f
    if not by_file:
        what = ", ".join(sources) or "the entity"
        return 0, f"no object file for {what} on main or any pushed branch"
    scores, ev = [], []
    for f in by_file.values():
        sc = research_score(f)
        scores.append(sc)
        where = "main" if f["ref"] == "main" else (f"PR #{f['pr']}" if f["pr"] else f"{f['ref']}, pushed, no PR yet")
        legacy = f"{f['legacy_done']}/{f['legacy_total']} legacy fields" if f["legacy_total"] else "no legacy entity"
        bits = [f"{f['file']} {f['status'].split('(')[0].strip()} ({sc})", legacy, f"{f['rows']} rows",
                f"{f['open_questions']} open questions", f"last push {f['last_commit']} on {where}"]
        if sc < 100 and f.get("legacy_left") and f["legacy_total"]:
            bits.append("not yet gone through: " + ", ".join(map(str, f["legacy_left"][:5])))
        if sc < 100 and f["last_commit"] != "?" and stale(ctx.date, f["last_commit"]):
            bits.append(f"no push since {f['last_commit']}")
        ev.append(", ".join(bits))
    missing = units - len(scores)
    if missing > 0:
        ev.append(f"{missing} more object(s) of the entity with no object file (0)")
        scores += [0] * missing
    pct = sum(scores) / len(scores)
    return floor1(pct), "; ".join(ev)


def stale(today, last):
    """Older than 3 working days (Sunday to Thursday)."""
    a, b = dt.date.fromisoformat(last), dt.date.fromisoformat(today)
    return sum(1 for i in range(1, (b - a).days + 1) if (a + dt.timedelta(i)).weekday() in (6, 0, 1, 2, 3)) > 3


# ---------- 3c pages ----------

def score_page(ctx, obj, findings, expected=None):
    if obj is None:
        return 0, "object with no API name yet: not found, 0"
    if ctx.pages is None:
        return score_page_from_names(ctx, obj)
    if not ctx.object_exists(obj):
        return 0, f"{obj} not in yv-partial, page 0"
    info = ctx.page_info(obj) or {"record_pages": [], "layouts": []}
    when = lambda p: f"changed {p['modified'][8:10]}.{p['modified'][5:7]}" if p.get("modified") else ""
    assigned = [p for p in info["record_pages"] if p["orders_app"]]
    if assigned:
        p = assigned[0]
        ev = f"{obj} page 100: {p['name']} assigned in the Orders app, {when(p)}"
        if not p["d030_name"]:
            findings.append(f"record page `{p['name']}` of {obj} not named `Order-<Object Name>- Record Page`")
        if expected:
            placed = set(map(fnorm, p["fields"]))
            if p["record_detail"]:
                for l in info["layouts"]:
                    placed |= set(map(fnorm, l["fields"] or []))
            missing = [e for e in expected if fnorm(e) not in placed]
            ev += f", {len(expected) - len(missing)} of {len(expected)} approved fields placed"
            if missing:
                ev += " (not placed: " + ", ".join(missing[:8]) + ("..." if len(missing) > 8 else "") + ")"
        return 100, ev
    mine = sorted((p for p in info["record_pages"] if p["created_by"] in ctx.implementers or p["modified_by"] in ctx.implementers),
                  key=lambda p: p["created_by"] not in ctx.implementers)
    if mine:
        p = mine[0]
        findings.append(f"record page `{p['name']}` of {obj} not assigned in the Orders app"
                        + ("" if p["d030_name"] else ", not named `Order-<Object Name>- Record Page`"))
        return 50, f"{obj} page 50: {p['name']} not assigned in the Orders app, {when(p)}"
    edited = [l for l in info["layouts"] if l["edited"]]
    if edited and not info["record_pages"]:
        return 50, f"{obj} page 50: page layout only, no Lightning record page ({edited[0]['name']})"
    return 0, f"{obj} page 0: " + ("no record page by an orders implementer" if info["record_pages"]
                                   else "no record page, layout not edited since created")


def score_page_from_names(ctx, obj):
    """weekly-progress 3c.5: no page retrieve, so the Orders app assignment cannot be seen."""
    stem = fnorm(obj).replace("__c", "")
    hit = [x for x in ctx.inv.get("FlexiPage", []) if stem in x["fullName"].lower()
           and (x.get("createdByName") in ctx.implementers or x.get("lastModifiedByName") in ctx.implementers)]
    return (50, f"{obj} page 50: {hit[0]['fullName']} (no page retrieve, scored from names)") if hit else \
        (0, f"{obj} page 0 (no page retrieve, scored from names)")


def is_stub(ctx, obj):
    if not obj.endswith("__c") or not ctx.object_exists(obj) or ctx.custom_fields_of(obj):
        return False
    info = ctx.page_info(obj) or {"record_pages": [], "layouts": []}
    return not info["record_pages"] and not any(l["edited"] for l in info["layouts"])


# ---------- 3b build ----------

def approved_file(ctx, objects):
    """The approved object file of one of the objects (origin/main, or an open PR not yet merged)."""
    for f in ctx.research:
        if f["object"].split()[0] in objects and f["status"].lower().startswith("approved") and (f["ref"] == "main" or f["pr"]):
            return f
    return None


def expected_fields(ctx, f):
    path = f"tickets/Data Migration/{f['file']}"
    text = git(ctx.root, "show", f"origin/{f['ref']}:{path}")
    not_built, on = set(), False
    for line in text.splitlines():
        if re.match(r"^\*\*Not built", line) or re.match(r"^#+ .*not built", line, re.I):
            on = True
            continue
        if on and (line.startswith("#") or line.startswith("**")):
            on = False
        if on:
            not_built |= set(API.findall(line))
    out = []
    for r in md_rows(text, "Salesforce API Name"):
        name = (r.get("Salesforce API Name") or "").replace("`", "").split()
        if not name or not name[0].endswith("__c"):
            continue
        if name[0] in not_built or "not built" in " ".join(r.values()).lower():
            continue
        out.append(name[0])
    return list(dict.fromkeys(out))


def named_components(text):
    """API names a task names: Object.Field, `Object: Field, Field; ...`, Flow: X, LWC X, and bare custom objects."""
    comps = []
    for seg in re.split(r";", text or ""):
        m = re.match(r"\s*([A-Za-z][A-Za-z0-9_]*)\s*:\s*(.*)", seg)
        if m and m.group(1) in ("Flow",):
            comps += [("Flow", n) for n in re.findall(r"\b([A-Za-z][A-Za-z0-9_]+)\b(?:\s*\(|$|,)", m.group(2)) if n not in ("Active", "Draft")][:1]
            continue
        if m and m.group(1) == "LWC" or re.match(r"\s*LWC\b", seg):
            comps += [("LightningComponentBundle", n) for n in re.findall(r"\b(yv[A-Za-z0-9_]+)\b", seg)]
            continue
        if m and (m.group(1).endswith("__c") or m.group(1)[0].isupper()) and API.search(m.group(2)):
            comps += [("CustomField", f"{m.group(1)}.{f}") for f in API.findall(m.group(2)) if "." not in f]
            continue
        for o, f in OBJ_FIELD.findall(seg):
            comps.append(("CustomField", f"{o}.{f}"))
        rest = OBJ_FIELD.sub(" ", seg)
        comps += [("CustomObject", o) for o in API.findall(rest)]
    return list(dict.fromkeys(comps))


def exists(ctx, kind, name):
    if kind == "CustomField":
        o, f = name.split(".", 1)
        return (fnorm(o), fnorm(f)) in ctx.fields
    if kind == "CustomObject":
        return fnorm(name) in ctx.objects
    return any(x["fullName"] == name for x in ctx.inv.get(kind, []))


def score_build(ctx, task, findings):
    row = tab_row(ctx, task["summary"]) or {}
    text = " ; ".join([task["summary"], task.get("description") or "", row.get("Components found in sandbox", ""),
                       row.get("Note", "")])
    comps = named_components(text)
    sources, targets = entity_targets(ctx, task["summary"])
    builds_object = bool(BUILDS_OBJECT.search(task["summary"]))
    # Objects of the task: the entity's targets when the task builds the object, and the objects its components name.
    page_objs = list(targets) if builds_object else []
    for kind, name in comps:
        o = name.split(".")[0] if kind == "CustomField" else name if kind == "CustomObject" else None
        if o and o not in page_objs:
            page_objs.append(o)
    if not page_objs:
        page_objs = [t for t in targets if t] or [None]
    real = [o for o in page_objs if o]
    f = approved_file(ctx, real)
    if f:
        exp = expected_fields(ctx, f)
        obj = f["object"].split()[0]
        found = [e for e in exp if (fnorm(obj), fnorm(e)) in ctx.fields]
        new_ok = ctx.object_exists(obj)
        fp = 100 * len(found) / len(exp) if exp and new_ok else (100 if new_ok else 0)
        missing = [e for e in exp if e not in found]
        fev = f"fields {len(found)} of {len(exp)} approved custom rows found ({fp:.0f})" + \
              (f", missing: {', '.join(missing[:6])}" if missing else "") + ("" if new_ok else f", {obj} not in yv-partial")
        pages = [score_page(ctx, obj, findings, exp)]
        rule = "build"
    else:
        counted = list(comps)
        if builds_object:
            for t in targets:
                if t is None or ("CustomObject", t) not in counted and t.endswith("__c"):
                    counted.append(("CustomObject", t))
        ok, ev = [], []
        for kind, name in counted:
            if name is None:
                ev.append("object with no API name: not found")
                continue
            if kind == "CustomObject" and is_stub(ctx, name):
                ev.append(f"{name} is a stub (no fields, no page): no credit")
                continue
            if exists(ctx, kind, name):
                ok.append(name)
            else:
                ev.append(f"{name} missing")
        n = len(counted)
        fp = 100 * len(ok) / n if n else 0
        fev = (f"fields part {len(ok)} of {n} named components exist ({fp:.0f}): " + ", ".join(ok + ev)) if n else \
            "fields part 0: the task names no component"
        fev += "; no approved object file, fallback"
        pages = [score_page(ctx, o, findings) for o in page_objs]
        rule = "fallback"
    pp = sum(p[0] for p in pages) / len(pages)
    pct = (fp + pp) / 2
    ev = f"{fev}; layouts {pp:.0f}: " + "; ".join(p[1] for p in pages)
    return floor1(pct), ev, rule


# ---------- 3d judged ----------

def changed_since(ctx, components, since):
    out = []
    for c in components:
        kind, _, name = c.partition(":") if ":" in c else ("", "", c)
        items = ctx.inv.get(kind) if kind else [x for v in ctx.inv.values() for x in v]
        for x in items or []:
            if x.get("fullName") == name:
                for k in ("createdDate", "lastModifiedDate"):
                    t = x.get(k) or ""
                    if t and dt.datetime.strptime(t[:19], "%Y-%m-%dT%H:%M:%S").replace(tzinfo=dt.timezone.utc) > since:
                        out.append(f"{c} {k[:-4].replace('lastModified', 'modified')} {t[:10]}")
                        break
    return out


def prev_snapshot_time(ctx, prev_date):
    snaps = sorted(d for d in glob.glob(os.path.join(ctx.root, "sources", "local", "org-snapshot-*"))
                   if os.path.basename(d)[13:] < ctx.date)
    pick = [d for d in snaps if os.path.basename(d)[13:] == prev_date] or snaps[-1:]
    if not pick:
        return None
    files = glob.glob(os.path.join(pick[0], "_inventory", "*.json"))
    t = max(os.path.getmtime(f) for f in files) if files else os.path.getmtime(pick[0])
    return dt.datetime.fromtimestamp(t, dt.timezone.utc)


# ---------- main ----------

def find_prev(root, out_path, date):
    c = []
    for p in glob.glob(os.path.join(root, "sources", "local", "daily", "tasks-*.json")) + \
            glob.glob(os.path.join(root, "sources", "local", "weekly", "tasks-*.json")):
        if os.path.abspath(p) == os.path.abspath(out_path):
            continue
        d = re.search(r"tasks-(\d{4}-\d{2}-\d{2})\.json$", p)
        if d and d.group(1) <= date:
            c.append((d.group(1), os.path.getmtime(p), p))
    return max(c)[2] if c else None


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--dir", required=True)
    ap.add_argument("--date", default=dt.date.today().isoformat())
    ap.add_argument("--run", choices=["weekly", "daily"], help="default: from --dir")
    ap.add_argument("--root", default=os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..")))
    ap.add_argument("--report")
    ap.add_argument("--prev")
    ap.add_argument("--out", help="output folder (default --dir)")
    a = ap.parse_args()
    root, date = os.path.abspath(a.root), a.date
    d = a.dir if os.path.isabs(a.dir) else os.path.join(root, a.dir)
    out_dir = a.out or d
    run = a.run or ("daily" if "daily" in os.path.basename(os.path.normpath(d)) else "weekly")
    jira = load(os.path.join(d, f"jira-{date}.json"))
    if not jira or not jira.get("tasks"):
        sys.exit(f"no tasks in {d}/jira-{date}.json")
    judg = {j["key"]: j for j in load(os.path.join(d, f"judgments-{date}.json"), [])}
    ctx = Ctx(root, date)
    tasks_out = os.path.join(out_dir, f"tasks-{date}.json")
    prev_path = a.prev or find_prev(root, tasks_out, date)
    prev = {t["key"]: t for t in load(prev_path, [])}
    prev_date = re.search(r"(\d{4}-\d{2}-\d{2})", os.path.basename(prev_path or "")) if prev_path else None
    since = prev_snapshot_time(ctx, prev_date.group(1) if prev_date else "")
    by_jira = {r["jira"].lower(): r for r in ctx.team} | {r["name"].lower(): r for r in ctx.team} | {r["sf"].lower(): r for r in ctx.team}
    order = {r["name"]: i for i, r in enumerate(ctx.team)}
    findings, disputed, out = [], [], []
    for t in jira["tasks"]:
        person = by_jira.get((t.get("assignee") or "").lower())
        name = person["name"] if person else (t.get("assignee") or "unassigned")
        s = t["summary"]
        if RESEARCH.search(s):
            pct, ev = score_research(ctx, t)
            rule = "research"
        elif BUILD.search(s):
            pct, ev, rule = score_build(ctx, t, findings)
        elif t["key"] in judg:
            j = judg[t["key"]]
            pct, ev, rule = float(j["percent"]), j.get("evidence", ""), "judged"
            p = prev.get(t["key"])
            if p is not None and float(p["percent"]) != pct and since is not None:
                moved = changed_since(ctx, j.get("components") or [], since)
                if not moved:
                    disputed.append({"key": t["key"], "assignee": name, "summary": s,
                                     "kept": float(p["percent"]), "previous_evidence": p.get("evidence", ""),
                                     "previous_run": os.path.basename(prev_path),
                                     "judged": pct, "judged_evidence": ev, "components": j.get("components") or []})
                    ev = (f"previous {float(p['percent']):.0f} kept, judged {pct:.0f} today with no listed component "
                          f"created or modified since the previous snapshot (score disputed): {ev}")
                    pct, rule = float(p["percent"]), "kept"
        else:
            p = prev.get(t["key"])
            pct = float(p["percent"]) if p else 0.0
            ev, rule = f"not judged this run; {'previous score kept: ' + p.get('evidence', '') if p else 'no previous score'}", "unjudged"
        if re.search(r"blocked", t.get("jira_status") or "", re.I) and "blocked" not in ev.lower():
            ev = "blocked in Jira; " + ev
        est = t.get("original_estimate_h")
        out.append({"assignee": name, "key": t["key"], "summary": s, "jira_status": t.get("jira_status", ""),
                    "original_estimate_h": est, "percent": pct, "evidence": ev, "rule": RULE[rule]})
    keynum = lambda k: int(re.sub(r"\D", "", k) or 0)
    out.sort(key=lambda t: (order.get(t["assignee"], 99), keynum(t["key"])))
    start, end, source = sprint_dates(ctx, jira)
    today = dt.date.fromisoformat(date)
    days = working_days_left(today, start, end, run == "daily" and dt.datetime.now().hour >= 12 and date == dt.date.today().isoformat())
    status = person_status(out, days)
    os.makedirs(out_dir, exist_ok=True)
    with open(tasks_out, "w", encoding="utf-8") as f:
        json.dump(out, f, ensure_ascii=False, indent=1)
    scores = {"date": date, "run": run, "sprint": {"start": start.isoformat(), "end": end.isoformat(), "source": source},
              "working_days_left": days, "hours_per_day": HOURS_PER_DAY, "previous_run": prev_path and os.path.relpath(prev_path, root),
              "status": status, "disputed": disputed, "page_findings": list(dict.fromkeys(findings))}
    with open(os.path.join(out_dir, f"scores-{date}.json"), "w", encoding="utf-8") as f:
        json.dump(scores, f, ensure_ascii=False, indent=1)
    if a.report:
        write_report(os.path.join(root, a.report) if not os.path.isabs(a.report) else a.report, out, scores)
    P, L = totals(out)
    print(f"{os.path.relpath(tasks_out, root)}: {len(out)} tasks, {(P - L) / P if P else 0:.0%} done, {L:.1f} h left, "
          f"{days} working days left, {len(disputed)} disputed")


def totals(tasks):
    est = [t for t in tasks if t["original_estimate_h"] not in (None, "")]
    P = sum(float(t["original_estimate_h"]) for t in est)
    L = sum(float(t["original_estimate_h"]) * (1 - float(t["percent"]) / 100) for t in est)
    return P, L


def person_status(tasks, days):
    out = {}
    for p in dict.fromkeys(t["assignee"] for t in tasks):
        ts = [t for t in tasks if t["assignee"] == p]
        _, left = totals(ts)
        blocked = [t["key"] for t in ts if float(t["percent"]) < 100 and "blocked" in (t["evidence"] or "").lower()]
        if blocked:
            out[p] = ["blocked", "blocked: " + ", ".join(blocked)]
        elif left > days * HOURS_PER_DAY:
            out[p] = ["at risk", f"{left:.1f} h left > {days} working days x {HOURS_PER_DAY}"]
        else:
            out[p] = ["on track", f"{left:.1f} h left <= {days} working days x {HOURS_PER_DAY}"]
    return out


MARK = "<!-- Sections 1 to 8 go here (weekly-progress step 5). Replace this line with Edit; do not retype section 9. -->"


def write_report(path, tasks, scores):
    P, L = totals(tasks)
    o = [f"# Weekly progress {scores['date']}", "", MARK, "",
         "## 9. Sprint tab update", "",
         f"From `scripts/weekly/score-tasks.py` ({scores['run']} run). Sprint {scores['sprint']['start']} to {scores['sprint']['end']}, "
         f"{scores['working_days_left']} working days left. **{(P - L) / P if P else 0:.0%} done, {L:.1f} hours left** of {P:.0f} estimated.", "",
         "| Assignee | Jira | Summary | Jira status | % done | Hours left | Evidence | Rule |", "|---|---|---|---|---|---|---|---|"]
    cell = lambda s: str(s).replace("|", "/").replace("\n", " ")
    for t in tasks:
        est = t["original_estimate_h"]
        left = "no estimate" if est in (None, "") else f"{float(est) * (1 - float(t['percent']) / 100):.1f}"
        o.append(f"| {t['assignee']} | {t['key']} | {cell(t['summary'])} | {t['jira_status']} | {float(t['percent']):.0f} | {left} | "
                 f"{cell(t['evidence'])} | {cell(t['rule'])} |")
    o += ["", "**Status per person** (blocked if an open task's evidence is blocked; at risk if hours left > working days left x 6):", ""]
    o += [f"- {p}: **{s[0]}** ({s[1]})" for p, s in scores["status"].items()]
    o += ["", "**Score disputed** (judged score differs from the previous run and no listed component changed since; previous score kept, Lior decides):", ""]
    o += [f"- {d['key']} {d['assignee']}: kept {d['kept']:.0f} ({d['previous_run']}: {cell(d['previous_evidence'])}); judged {d['judged']:.0f} today "
          f"({cell(d['judged_evidence'])}); components checked: {', '.join(d['components']) or 'none listed'}" for d in scores["disputed"]] or ["- none"]
    o += ["", "**Page mismatches found by the scoring (3c)**:", ""]
    o += [f"- {x}" for x in scores["page_findings"]] or ["- none"]
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "w", encoding="utf-8") as f:
        f.write("\n".join(o) + "\n")


if __name__ == "__main__":
    main()
