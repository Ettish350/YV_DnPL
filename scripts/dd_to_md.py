# -*- coding: utf-8 -*-
"""Regenerate the Markdown edition of the binding DD (dd/) from the docx masters.

Usage (from the repo root, Lior or Anna only):
    python3 scripts/dd_to_md.py "<Doc 1 .docx>" "<Doc 2 .docx>" "<source label date, e.g. 6.10.2026>" \
        ["<Doc 3 .docx>" "<Doc 3 source label, e.g. גרסה מאושרת על ידי יד ושם, 4.11.2026>"]

Doc 3 is optional so the two-document call still works. Doc 1 and Doc 2 are labelled
"גרסה מאושרת, <date>"; Doc 3 takes its own full label, since it was approved on a different date (D-037).

Writes:
    dd/doc1-intro-architecture/*.md, dd/doc2-ordering-processes/**/*.md,
    dd/doc3-events-finance/**/*.md                                       one file per chapter
    dd/assets/d1/, dd/assets/d2/, dd/assets/d3/                          images from the docx
    dd/_meta/toc.md, dd/_meta/open-comments.md
    dd/INDEX.md                                                          only the generated tables
                                                                          (section 5 codes, section 10 file map,
                                                                          open-thread count); prose is hand-written
Format: see dd/INDEX.md. Body text as paragraphs, tables as Markdown tables (cell paragraphs
joined by <br>), comment anchors as ⟦💬 cN⟧, threads at the bottom of each chapter file.
"""
import sys, os, re, zipfile, shutil, datetime
from xml.etree import ElementTree as ET

W = "{http://schemas.openxmlformats.org/wordprocessingml/2006/main}"
W14 = "{http://schemas.microsoft.com/office/word/2010/wordml}"
W15 = "{http://schemas.microsoft.com/office/word/2012/wordml}"
R = "{http://schemas.openxmlformats.org/officeDocument/2006/relationships}"
A = "{http://schemas.openxmlformats.org/drawingml/2006/main}"
ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "dd")

DOC1_FILES = {
    None: "00-front-matter", "מטרות": "01-goals", "מצב קיים": "02-current-state",
    "ארכיטקטורת פתרון": "03-architecture", "ישויות": "04-entities",
    "קונפיגורציות מערכת": "05-configurations", "תרשימי ישויות וזרימה": "06-diagrams",
}
DOC2_FILES = {
    None: "00-front-matter", "מטרות": "01-goals", "מצב קיים": "02-current-state",
    "ארכיטקטורת פתרון": "03-architecture", "ישויות": "04-entities", "מסכים מרכזיים": "05-screens",
    "תהליכים עסקיים": "06-processes/00-overview",
    "וולידציות, אבטחת מידע ותבניות תקשורת במערכת": "07-validations-security-templates",
    "דוחות ודשבורדים": "08-reports-dashboards", "שדות": "09-fields", "טפסים": "10-forms",
    "נספח הדוחות": "11-appendix-reports", "נספח תבניות התקשורת": "12-appendix-communication-templates",
    "נספחי חומרי יד ושם": "13-appendix-yv-materials",
}
DOC3_FILES = {
    None: "00-front-matter", "מטרות": "01-goals", "ארכיטקטורת פתרון": "02-architecture",
    "ישויות": "03-entities", "מסכים מרכזיים": "04-screens", "תהליכים עסקיים": "05-processes/00-overview",
    "דוחות ודשבורדים": "06-reports-dashboards", "טפסים": "07-forms",
    "פרופילים והרשאות": "08-profiles-permissions", "ממשקים": "09-interfaces",
    "נספח דמי ההרשמה והטיפול": "10-appendix-registration-handling-fees",
    "נספח נקודות הקצה של ממשק המערכת הכספית": "11-appendix-finance-interface-endpoints",
    "נספחי חומרי יד ושם": "12-appendix-yv-materials",
    "נספח דוגמאות מסכי החשבונות במערכת הקיימת": "13-appendix-legacy-account-screens",
    "נספח מקרא ישויות": "14-appendix-entity-legend",
}
DOC3_PROCESS_FILES = [
    ("אירועים פתוחים לקהל, סיורים, הצגות וחיזיון", "01-open-public-events-tours-shows"),
    ("פתיחת אירוע בסמארטיקט וסנכרונו לסיילספורס", "02-smartticket-event-sync"),
    ("כנסי מורים, ניהול מפגשים ומסלולים אישיים", "03-teacher-conferences-sessions-tracks"),
    ("סמינרים", "04-seminars"),
    ("קורסים והכשרות", "05-courses-trainings"),
    ("השתלמויות", "06-professional-development"),
    ("ניהול נוכחות בפועל וסימון הגעה", "07-attendance-arrival"),
    ("כניסה והזמנה בעמדות קיוסק", "08-kiosk-entry-ordering"),
    ("הפקת חשבון וחיוב לקוח מול אלמוג", "09-almog-invoicing-billing"),
    ("ביטול ושינוי הזמנה של לקוח פרטי", "10-private-customer-cancel-change"),
    ("ביטול, זיכוי והחזר ללקוחות מוסדיים", "11-institutional-cancel-credit-refund"),
    ("תשלום מדריכים ונותני שירות וקובץ מלם", "12-guide-provider-payment-malam-file"),
]
PROCESS_FILES = [
    ("בניית מוצרים, חבילות ותבניות יום פעילות", "01-products-bundles-templates"),
    ("הזמנה מקוונת עצמאית, לקוח פרטי ומוסד", "02-online-self-service-order"),
    ("קליטת הזמנה ידנית", "03-manual-order-intake"),
    ("זיהוי, סיווג וניתוב מוסד לרכזת", "04-institution-identification-routing"),
    ("הזמנות צבא, כוחות הביטחון ומשלחות", "05-army-security-delegations"),
    ("ביקורים רשמיים ותיעוד ביקורי תורמים", "06-official-visits-donors"),
    ("ניידות חינוכיות", "07-educational-mobile-units"),
    ("הצעת מחיר, הנחות ואישור", "08-quote-discounts-approval"),
    ("שיבוץ מדריכים ומשאבים", "09-guide-resource-scheduling"),
    ("הדרכות חוץ, הדרכות כפולות וחישוב מרחק וזמן", "10-external-double-tours-distance"),
    ("שיבוץ משאבים פיזיים, מוזיאון ותערוכות", "11-physical-resources-museum"),
    ("ויסות ונעילה רב-רמתית", "12-regulation-locking"),
    ("זמינות מדריכים וניהול נותני שירות", "13-guide-availability-service-providers"),
    ("תזכורות ותקשורת יזומה", "14-reminders-proactive-communication"),
    ("יוזרים, הרשאות והסבת נתונים", "15-users-permissions-migration"),
]
HEB = re.compile(r"[֐-׿]")
MARK = re.compile(r"\s*⟦💬 c\d+⟧\s*")
CODE = re.compile(r"\b([AV]\d{2}[A-Z]?-\d{2})\b")


def clean(t):
    return re.sub(r"\s+", " ", MARK.sub(" ", t)).strip()


class Doc:
    def __init__(self, path, key, folder, name, label, files, procs=None):
        self.path, self.key, self.folder, self.name, self.label, self.files = path, key, folder, name, label, files
        self.procs = dict(procs or [])
        # folder of the per-process files, taken from the file map ("06-processes" in Doc 2, "05-processes" in Doc 3)
        self.proc_dir = doc_proc_dir(files)
        self.z = zipfile.ZipFile(path)
        self.rels = {}
        rx = ET.fromstring(self.z.read("word/_rels/document.xml.rels"))
        for r in rx:
            self.rels[r.get("Id")] = r.get("Target")
        self.load_comments()
        self.marked = set()

    def load_comments(self):
        self.comments, para2cid, self.done, self.parent = {}, {}, set(), {}
        try:
            cx = ET.fromstring(self.z.read("word/comments.xml"))
        except KeyError:
            return
        for c in cx.findall(W + "comment"):
            cid = c.get(W + "id")
            paras = []
            for p in c.findall(".//" + W + "p"):
                s = ""
                for el in p.iter():
                    if el.tag == W + "t":
                        s += el.text or ""
                    elif el.tag in (W + "br", W + "cr"):
                        s += "<br>"
                s = s.strip()
                if s:
                    paras.append(s)
                pid = p.get(W14 + "paraId")
            ps = c.findall(".//" + W + "p")
            if ps and ps[-1].get(W14 + "paraId"):
                para2cid[ps[-1].get(W14 + "paraId")] = cid
            self.comments[cid] = {"author": c.get(W + "author", ""), "date": (c.get(W + "date") or "")[:10],
                                  "text": " / ".join(paras)}
        try:
            cex = ET.fromstring(self.z.read("word/commentsExtended.xml"))
            for e in cex.findall(W15 + "commentEx"):
                cid = para2cid.get(e.get(W15 + "paraId"))
                if cid and e.get(W15 + "done") == "1":
                    self.done.add(cid)
                pp = e.get(W15 + "paraIdParent")
                if cid and pp and pp in para2cid:
                    self.parent[cid] = para2cid[pp]
        except KeyError:
            pass

    def root(self, cid):
        while cid in self.parent:
            cid = self.parent[cid]
        return cid

    def inline(self, el, anchors, images):
        """Text of a paragraph with comment markers and image links, in document order."""
        out = ""
        for x in el.iter():
            if x.tag == W + "t":
                out += x.text or ""
            elif x.tag == W + "tab":
                out += " "
            elif x.tag in (W + "br", W + "cr"):
                out += "<br>"
            elif x.tag in (W + "commentRangeStart", W + "commentReference"):
                cid = x.get(W + "id")
                if cid in self.comments and cid not in self.parent and cid not in self.marked:
                    self.marked.add(cid)
                    out += f" ⟦💬 c{cid}⟧"
                    anchors.append(cid)
            elif x.tag == A + "blip":
                rid = x.get(R + "embed")
                tgt = self.rels.get(rid, "")
                if tgt:
                    images.append(tgt)
                    out += f"\x00IMG:{os.path.basename(tgt)}\x00"
        return out


def doc_proc_dir(files):
    f = files.get("תהליכים עסקיים", "")
    return f.split("/")[0] if "/" in f else None


def is_proc_file(doc, fname):
    return bool(doc.proc_dir) and fname.startswith(doc.proc_dir + "/") and not fname.endswith("00-overview")


def style(p):
    ps = p.find(W + "pPr/" + W + "pStyle")
    return ps.get(W + "val") if ps is not None else ""


def parse(doc):
    """Return list of chapters: {title, file, blocks:[(kind, level|None, text)], anchors, images, headings}."""
    chapters = []
    cur = None

    def new(title, fname):
        nonlocal cur
        cur = {"title": title, "file": fname, "blocks": [], "anchors": [], "images": [], "headings": []}
        chapters.append(cur)

    new(None, doc.files[None])
    in_proc = False
    body = doc.z.read("word/document.xml")
    body = ET.fromstring(body).find(W + "body")
    for el in body:
        if el.tag == W + "p":
            s = style(el).lower().replace(" ", "")
            m = re.match(r"heading(\d)$", s)
            saved = set(doc.marked)
            raw = doc.inline(el, [], [])
            doc.marked = saved
            t = clean(raw.replace("\x00", ""))
            if m and t:
                lvl = int(m.group(1))
                if lvl == 1:
                    if t not in doc.files:
                        sys.exit(f"Unknown chapter title in {doc.name}: {t!r}. Add it to the file map in dd_to_md.py.")
                    in_proc = t == "תהליכים עסקיים"
                    new(t, doc.files[t])
                elif lvl == 2 and in_proc:
                    f = doc.procs.get(t)
                    if not f:
                        sys.exit(f"Unknown process title in {doc.name}: {t!r}. Add it to the process file map.")
                    new(t, doc.proc_dir + "/" + f)
            text = doc.inline(el, cur["anchors"], cur["images"]).strip()
            if m and text.replace("\x00", "").strip():
                lvl = int(m.group(1))
                if is_proc_file(doc, cur["file"]):
                    lvl -= 1
                cur["blocks"].append(("h", lvl, text))
                cur["headings"].append((lvl, clean(text)))
            elif text:
                cur["blocks"].append(("p", None, text))
        elif el.tag == W + "tbl":
            rows = []
            for tr in el.findall(W + "tr"):
                cells = []
                for tc in tr.findall(W + "tc"):
                    ps = [doc.inline(p, cur["anchors"], cur["images"]).strip() for p in tc.findall(".//" + W + "p")]
                    cells.append("<br>".join(x for x in ps if x).replace("|", "/"))
                rows.append(cells)
            if rows:
                cur["blocks"].append(("t", None, rows))
    return chapters


def rel(from_file, to_file):
    return os.path.relpath(to_file, os.path.dirname(from_file)).replace(os.sep, "/")


def nav_title(ch):
    return ch["title"] or "עמוד שער ותקציר מנהלים"


def crumb_title(ch):
    return ch["title"] or "עמוד שער"


def render(doc, chapters, all_targets, today):
    out_dir = os.path.join(ROOT, doc.folder)
    if os.path.isdir(out_dir):
        shutil.rmtree(out_dir)
    os.makedirs(os.path.join(out_dir, doc.proc_dir) if doc.proc_dir else out_dir, exist_ok=True)
    meta = []
    for i, ch in enumerate(chapters):
        path = f"{doc.folder}/{ch['file']}.md"
        lines = [f"<!-- generated from: {os.path.basename(doc.path)} | {today} | do not edit by hand; regenerate from the docx -->", ""]
        lines.append(f"> **מקור:** {doc.label}  ")
        crumb = [doc.name]
        if is_proc_file(doc, ch["file"]):
            crumb.append("תהליכים עסקיים")
        crumb.append(crumb_title(ch))
        lines.append(f"> **מיקום בניווט:** {' › '.join(crumb)}  ")
        codes = set()
        for kind, _, b in ch["blocks"]:
            if kind == "t":
                for row in b:
                    m = row and re.match(r"([AV]\d{2}[A-Z]?-\d{2})\b", re.sub(r"\s*⟦💬 c\d+⟧\s*", "", row[0]).strip())
                    if m:
                        codes.add(m.group(1))
        codes = sorted(codes, key=lambda c: (c[0] != "A", c))
        if codes:
            lines.append(f"> **קודי ולידציה ואוטומציה בפרק:** {', '.join(codes)}  ")
        nav = []
        if i > 0:
            p = chapters[i - 1]
            nav.append(f"**הקודם:** [{nav_title(p)}]({rel(path, doc.folder + '/' + p['file'] + '.md')})")
        if i < len(chapters) - 1:
            n = chapters[i + 1]
            nav.append(f"**הבא:** [{nav_title(n)}]({rel(path, doc.folder + '/' + n['file'] + '.md')})")
        nav.append(f"[מקרא]({rel(path, 'INDEX.md')})")
        lines.append("> " + " · ".join(nav))
        lines.append("")
        for kind, lvl, b in ch["blocks"]:
            if kind == "h":
                lines += ["#" * max(lvl, 1) + " " + fix(b, path, doc), ""]
            elif kind == "p":
                lines += [fix(link_refs(b, path, doc, all_targets), path, doc), ""]
            else:
                width = max(len(r) for r in b)
                rows = [r + [""] * (width - len(r)) for r in b]
                lines.append("| " + " | ".join(fix(c, path, doc) for c in rows[0]) + " |")
                lines.append("| " + " | ".join(["---"] * width) + " |")
                for r in rows[1:]:
                    lines.append("| " + " | ".join(fix(c, path, doc) for c in r) + " |")
                lines.append("")
        waiting = threads(doc, ch, lines)
        while lines and lines[-1] == "":
            lines.pop()
        with open(os.path.join(ROOT, path), "w", encoding="utf-8") as f:
            f.write("\n".join(lines) + "\n")
        meta.append({"path": path, "title": nav_title(ch), "codes": codes, "waiting": waiting, "chapter": ch})
    return meta


def fix(text, path, doc):
    def img(m):
        return f"![תרשים {m.group(1)}]({rel(path, f'assets/{doc.key}/{m.group(1)}')})"
    text = re.sub(r"\x00IMG:([^\x00]+)\x00", img, text)
    # an anchor that lands inside a word (e.g. "V01- ⟦💬 c148⟧02") moves to after that word, so codes stay greppable
    return re.sub(r"(?<=\S)((?: ⟦💬 c\d+⟧)+)([^\s⟦]+)", r"\2\1", text)


def link_refs(text, path, doc, targets):
    """Link 'פרק <chapter or section title>' to the file that holds it (own doc first)."""
    out, i = "", 0
    for m in re.finditer(r"פרק ", text):
        if m.start() < i:
            continue
        rest = text[m.end():]
        best = None
        for title, tgt, key in targets:
            if rest.startswith(title) and (best is None or len(title) > len(best[0]) or
                                           (len(title) == len(best[0]) and key == doc.key and best[2] != doc.key)):
                best = (title, tgt, key)
        if not best or best[1] == path:
            continue
        out += text[i:m.end()] + f"[{best[0]}]({rel(path, best[1])})"
        i = m.end() + len(best[0])
    return out + text[i:]


def is_malam(author):
    return not HEB.search(author)


def threads(doc, ch, lines):
    roots = []
    for cid in ch["anchors"]:
        if cid not in roots:
            roots.append(cid)
    if not roots:
        return []
    kids = {}
    for c, p in doc.parent.items():
        kids.setdefault(doc.root(c), []).append(c)
    lines += ["", "---", "", "## הערות סקירה בפרק זה", "",
              "> הערות הסקירה אינן חלק מהאפיון המחייב. הן מסמנות מקומות שבהם הטקסט עדיין בדיון. הסימון ⟦💬 cN⟧ בגוף הטקסט מפנה להערה כאן.", ""]
    waiting = []
    for r in roots:
        msgs = [r] + sorted(kids.get(r, []), key=lambda c: int(c))
        last = doc.comments[msgs[-1]]
        if r in doc.done:
            status = "סגורה"
        elif is_malam(last["author"]):
            status = "נענתה על ידי מלם"
        else:
            status = "ממתינה למענה מלם"
            waiting.append(r)
        lines.append(f"### ⟦💬 c{r}⟧ {status}")
        for j, cid in enumerate(msgs):
            c = doc.comments[cid]
            closed = " · סומנה כסגורה" if cid in doc.done else ""
            lines.append(f"{'  ' if j else ''}- **c{cid}** · {c['author']} · {c['date']}{closed}: {c['text']}")
        lines.append("")
    return waiting


def extract_images(doc):
    d = os.path.join(ROOT, "assets", doc.key)
    if os.path.isdir(d):
        shutil.rmtree(d)
    os.makedirs(d)
    used = set()
    for t in doc.rels.values():
        if t.startswith("media/"):
            used.add(t)
    for name in doc.z.namelist():
        if name.startswith("word/media/") and name[len("word/"):] in used:
            with open(os.path.join(d, os.path.basename(name)), "wb") as f:
                f.write(doc.z.read(name))


def main(d1_path, d2_path, date_label, d3_path=None, d3_label=None):
    today = datetime.date.today().isoformat()
    docs = [
        Doc(d1_path, "d1", "doc1-intro-architecture", "מסמך 1 - מבוא וארכיטקטורה",
            f"מסמך 1 - מבוא וארכיטקטורה, גרסה מאושרת, {date_label}", DOC1_FILES),
        Doc(d2_path, "d2", "doc2-ordering-processes", "מסמך 2 - תהליכי ההזמנות",
            f"מסמך 2 - תהליכי ההזמנות, גרסה מאושרת, {date_label}", DOC2_FILES, PROCESS_FILES),
    ]
    if d3_path:
        docs.append(Doc(d3_path, "d3", "doc3-events-finance", "מסמך 3 - אירועים וכספים",
                        f"מסמך 3 - אירועים וכספים, {d3_label}", DOC3_FILES, DOC3_PROCESS_FILES))
    parsed = [(d, parse(d)) for d in docs]
    targets = []
    for d, chs in parsed:
        for ch in chs:
            p = f"{d.folder}/{ch['file']}.md"
            if ch["title"]:
                targets.append((ch["title"], p, d.key))
            for lvl, h in ch["headings"]:
                if len(h) > 6:
                    targets.append((h, p, d.key))
    all_meta = []
    for d, chs in parsed:
        extract_images(d)
        all_meta.append((d, render(d, chs, targets, today)))

    # toc
    toc = ["# Table of contents (all docs)", ""]
    for d, meta in all_meta:
        for m in meta:
            ch = m["chapter"]
            toc.append(f"- [{crumb_title(ch)}](../{m['path']})")
            base = 1
            for lvl, h in ch["headings"]:
                if lvl <= base:
                    continue
                toc.append("  " * (lvl - base) + "- " + h)
    os.makedirs(os.path.join(ROOT, "_meta"), exist_ok=True)
    open(os.path.join(ROOT, "_meta", "toc.md"), "w", encoding="utf-8").write("\n".join(toc) + "\n")

    # open comments
    oc = ["# Review threads awaiting a Malam reply", "",
          f"Generated {datetime.date.today().strftime('%d.%m.%Y')}. The last message in each thread is from the client. Links go to the chapter file; the full thread is at its bottom.", ""]
    n_wait = 0
    for d, meta in all_meta:
        oc += [f"## {d.name}", ""]
        for m in meta:
            for r in m["waiting"]:
                n_wait += 1
                kids = [c for c in d.parent if d.root(c) == r]
                last = d.comments[sorted([r] + kids, key=int)[-1]]
                txt = last["text"]
                if len(txt) > 200:
                    txt = txt[:200] + "…"
                oc.append(f"- [c{r}](../{m['path']}) · {m['title']} · {last['author']} {last['date']}: {txt}")
        oc.append("")
    open(os.path.join(ROOT, "_meta", "open-comments.md"), "w", encoding="utf-8").write("\n".join(oc).rstrip() + "\n")

    # INDEX generated tables
    idx_path = os.path.join(ROOT, "INDEX.md")
    idx = open(idx_path, encoding="utf-8").read()
    fm = ["| File | Chapter | V/A codes | Threads awaiting Malam |", "|---|---|---|---|"]
    code_rows = {}
    for d, meta in all_meta:
        for m in meta:
            p = m["path"]
            fm.append(f"| [{p}]({p}) | {m['title']} | {len(m['codes'])} | {len(m['waiting']) or ''} |")
            for c in m["codes"]:
                code_rows.setdefault(c.split("-")[0], []).append((c, p))
    idx = re.sub(r"\| File \| Chapter \| V/A codes \| Threads awaiting Malam \|\n(\|.*\|\n?)+", "\n".join(fm) + "\n", idx)
    cr = ["| Prefix | Codes present | Defined in |", "|---|---|---|"]
    for pre in sorted(code_rows, key=lambda k: (int(re.sub(r"\D", "", k)), k[0] != "A", k)):
        rows = code_rows[pre]
        files = sorted(set(p for _, p in rows))
        cr.append(f"| {pre} | {', '.join(c for c, _ in rows)} | " +
                  "<br>".join(f"[{os.path.basename(f)}]({f})" for f in files) + " |")
    idx = re.sub(r"\| Prefix \| Codes present \| Defined in \|\n(\|.*\|\n?)+", "\n".join(cr) + "\n", idx)
    idx = re.sub(r"\[_meta/open-comments.md\]\(_meta/open-comments.md\) \(\d+ threads\)",
                 f"[_meta/open-comments.md](_meta/open-comments.md) ({n_wait} thread{"" if n_wait == 1 else "s"})", idx)
    open(idx_path, "w", encoding="utf-8").write(idx)
    for d, meta in all_meta:
        nc = len(d.comments)
        print(f"{d.name}: {len(meta)} files, {nc} comments ({len(d.done)} resolved)")
    print(f"threads awaiting Malam: {n_wait}")


if __name__ == "__main__":
    if len(sys.argv) not in (4, 6):
        sys.exit(__doc__)
    main(*sys.argv[1:])
