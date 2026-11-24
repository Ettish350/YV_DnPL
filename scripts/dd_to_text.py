# -*- coding: utf-8 -*-
"""Turn a binding DD (.docx) into plain text, one file per chapter, with Word comments inline.

Usage (from the repo root):
    python3 scripts/dd_to_text.py "<path to the DD .docx>" sources/local/doc2

Output (never committed; sources/local/ is in .gitignore):
    sources/local/doc2/00-index.md        chapter list with file names
    sources/local/doc2/NN-<chapter>.md    body text, tables as rows, comments as
                                          [COMMENT #id author (resolved)]: text
Why: the 14.9 decisions and many YV/Malam answers live in the comments. Reading only
the body misses them. Every Claude reads the DD through this script, so everyone sees
the same text.
"""
import sys, os, re, zipfile
from xml.etree import ElementTree as ET

W = "{http://schemas.openxmlformats.org/wordprocessingml/2006/main}"
W15 = "{http://schemas.microsoft.com/office/word/2012/wordml}"
W14 = "{http://schemas.microsoft.com/office/word/2010/wordml}"


def load(z, name):
    try:
        return ET.fromstring(z.read(name))
    except KeyError:
        return None


def ptext(p):
    return "".join(t.text or "" for t in p.iter(W + "t"))


def main(src, out):
    z = zipfile.ZipFile(src)
    doc = load(z, "word/document.xml")
    cx = load(z, "word/comments.xml")
    cex = load(z, "word/commentsExtended.xml")
    comments, para2cid, done, parent = {}, {}, set(), {}
    if cx is not None:
        for c in cx.findall(W + "comment"):
            cid = c.get(W + "id")
            ps = c.findall(".//" + W + "p")
            comments[cid] = (c.get(W + "author", ""), " ".join(ptext(p) for p in ps).strip())
            if ps:
                pid = ps[-1].get(W14 + "paraId")
                if pid:
                    para2cid[pid] = cid
    if cex is not None:
        for e in cex.findall(W15 + "commentEx"):
            cid = para2cid.get(e.get(W15 + "paraId"))
            if cid and e.get(W15 + "done") == "1":
                done.add(cid)
            pp = e.get(W15 + "paraIdParent")
            if cid and pp and pp in para2cid:
                parent[cid] = para2cid[pp]

    def styl(p):
        ps = p.find(W + "pPr/" + W + "pStyle")
        return ps.get(W + "val") if ps is not None else ""

    chapters, cur = [], None
    def new(title):
        nonlocal cur
        cur = {"title": title, "lines": []}
        chapters.append(cur)
    new("front matter")

    def cids_in(el):
        return [r.get(W + "id") for r in el.iter(W + "commentReference")]

    def emit_comments(ids):
        for cid in ids:
            if cid in comments:
                a, t = comments[cid]
                tag = " (resolved)" if cid in done else ""
                rep = f" reply to #{parent[cid]}" if cid in parent else ""
                cur["lines"].append(f"    [COMMENT #{cid} {a}{tag}{rep}]: {t}")

    body = doc.find(W + "body")
    split_h2 = False  # inside the long business-processes chapter, one file per process
    for el in body:
        if el.tag == W + "p":
            s = styl(el).lower().replace(" ", "")
            t = ptext(el).strip()
            if re.match(r"(heading)?1$", s) and t:
                split_h2 = t.startswith("תהליכים עסקיים")
                new(t)
                cur["lines"].append(f"# {t}")
            elif re.match(r"(heading)?2$", s) and t and split_h2:
                new("תהליכים עסקיים - " + t)
                cur["lines"].append(f"## {t}")
            elif s.startswith("heading") and t:
                lvl = re.sub(r"\D", "", s) or "2"
                cur["lines"].append(f"{'#' * int(lvl)} {t}")
            elif t:
                cur["lines"].append(t)
            emit_comments(cids_in(el))
        elif el.tag == W + "tbl":
            for tr in el.iter(W + "tr"):
                cells = [" ".join(ptext(p) for p in tc.iter(W + "p")).strip() for tc in tr.findall(W + "tc")]
                cur["lines"].append("| " + " | ".join(c.replace("|", "/") for c in cells) + " |")
            emit_comments(cids_in(el))
            cur["lines"].append("")

    os.makedirs(out, exist_ok=True)
    idx = ["# Index", "", f"Source: {os.path.basename(src)}", f"Comments: {len(comments)} ({len(done)} resolved)", ""]
    for i, ch in enumerate(chapters):
        safe = re.sub(r'[\\/:*?"<>|]', "", ch["title"])[:60].strip() or "untitled"
        fn = f"{i:02d}-{safe}.md"
        open(os.path.join(out, fn), "w", encoding="utf-8").write("\n".join(ch["lines"]) + "\n")
        idx.append(f"- {fn} ({len(ch['lines'])} lines)")
    open(os.path.join(out, "00-index.md"), "w", encoding="utf-8").write("\n".join(idx) + "\n")
    print(f"{len(chapters)} chapters, {len(comments)} comments ({len(done)} resolved) -> {out}")


if __name__ == "__main__":
    if len(sys.argv) != 3:
        sys.exit(__doc__)
    main(sys.argv[1], sys.argv[2])
