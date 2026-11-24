#!/usr/bin/env python3
"""Record pages and page layouts per object, for rule 3c (layouts) of the weekly progress check.

The FlexiPage and Layout inventory (`sf org list metadata`) has names and dates but not the object a Lightning
page belongs to, nor what is on it. This script retrieves, read-only, every FlexiPage and Layout of the inventory
that no managed package owns (no namespace prefix), and the `Orders` app, into
sources/local/org-snapshot-<today>/_pages/, and writes per object:
- each Lightning record page: name, label, whether the label follows D-030 (`Order-<Object Name>- Record Page`),
  whether the Orders app assigns it, who created and last modified it and when, the fields placed on it
  (Dynamic Forms) and whether it shows the record detail (then its fields come from the page layout);
- each page layout: who created and last modified it and when, whether a person edited it after it was created
  (Salesforce creates a default layout with every custom object), and its fields.
Writes _pages.md and _pages.json next to _inventory/. Never deploys; the only org call is `sf project retrieve start`.
Usage: python3 scripts/weekly/record-pages.py   (after pull-inventory.sh)
"""
import datetime
import json
import os
import re
import subprocess
import sys
import xml.etree.ElementTree as ET
from collections import defaultdict

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
SNAP = os.path.join(ROOT, "sources", "local", f"org-snapshot-{datetime.date.today():%Y-%m-%d}")
INV, OUT = os.path.join(SNAP, "_inventory"), os.path.join(SNAP, "_pages")
NS = "{http://soap.sforce.com/2006/04/metadata}"
D030 = re.compile(r"^order\s*-.*-\s*record\s*page$", re.I)
SYSTEM_USERS = {"Automated Process", "Salesforce, Inc."}


def inventory(t):
    with open(os.path.join(INV, f"{t}.json")) as f:
        return json.load(f).get("result") or []


def member(item, folder, ext):
    """The manifest member name: the file name carries the encoding Salesforce expects (for example %28 for '(')."""
    return item["fileName"][len(folder) + 1:-len(ext) - 1]


def retrieve(pages, layouts):
    os.makedirs(OUT, exist_ok=True)
    types = [("CustomApplication", ["Orders"]), ("FlexiPage", pages), ("Layout", layouts)]
    xml = ['<?xml version="1.0" encoding="UTF-8"?>', '<Package xmlns="http://soap.sforce.com/2006/04/metadata">']
    for name, members in types:
        esc = [m.replace("&", "&amp;").replace("<", "&lt;") for m in sorted(members)]
        xml += ["  <types>"] + [f"    <members>{m}</members>" for m in esc] + [f"    <name>{name}</name>", "  </types>"]
    xml += ["  <version>62.0</version>", "</Package>"]
    manifest = os.path.join(OUT, "package.xml")
    with open(manifest, "w") as f:
        f.write("\n".join(xml) + "\n")
    r = subprocess.run(["sf", "project", "retrieve", "start", "--manifest", manifest, "--target-metadata-dir", OUT,
                        "--unzip", "--target-org", "yv-partial"], cwd=ROOT, capture_output=True, text=True)
    return r.returncode == 0, (r.stderr or r.stdout)[-2000:]


def texts(root, tag):
    return [e.text for e in root.iter(NS + tag) if e.text]


def edited(item):
    """A person changed it at least 10 minutes after it was created (a default layout is saved twice on creation)."""
    c, m = item.get("createdDate", ""), item.get("lastModifiedDate", "")
    if not c or not m or item.get("lastModifiedByName") in SYSTEM_USERS:
        return False
    fmt = "%Y-%m-%dT%H:%M:%S.%fZ"
    return (datetime.datetime.strptime(m, fmt) - datetime.datetime.strptime(c, fmt)).total_seconds() > 600


def who(item):
    return (f"created {item.get('createdByName', '?')} {item.get('createdDate', '')[:10]}, "
            f"modified {item.get('lastModifiedByName', '?')} {item.get('lastModifiedDate', '')[:10]}")


def main():
    if not os.path.isdir(INV) or os.path.exists(os.path.join(INV, "_ERROR.txt")):
        print("pages: no inventory for today, nothing retrieved")
        return 0
    flexi = {x["fullName"]: x for x in inventory("FlexiPage") if not x.get("namespacePrefix")}
    lays = {x["fullName"]: x for x in inventory("Layout") if not x.get("namespacePrefix")}
    ok, log = retrieve([member(x, "flexipages", "flexipage") for x in flexi.values()],
                       [member(x, "layouts", "layout") for x in lays.values()])
    base = os.path.join(OUT, "unpackaged", "unpackaged")
    if not ok or not os.path.isdir(base):
        with open(os.path.join(OUT, "_ERROR.txt"), "w") as f:
            f.write("retrieve failed\n" + log)
        print("pages: retrieve failed, see _pages/_ERROR.txt")
        return 0
    # Pages the Orders app assigns, per object (profile and form factor).
    assigned = defaultdict(list)
    app = os.path.join(base, "applications", "Orders.app")
    if os.path.exists(app):
        for o in ET.parse(app).getroot():
            if o.tag in (NS + "profileActionOverrides", NS + "actionOverrides") and (o.findtext(NS + "type") or "").lower() == "flexipage":
                assigned[o.findtext(NS + "content")].append(
                    f"{o.findtext(NS + 'pageOrSobjectType')} ({o.findtext(NS + 'profile') or 'org default'}, {o.findtext(NS + 'formFactor') or 'all'})")
    objects = defaultdict(lambda: {"record_pages": [], "layouts": []})
    for name, item in flexi.items():
        path = os.path.join(base, "flexipages", member(item, "flexipages", "flexipage") + ".flexipage")
        if not os.path.exists(path):
            continue
        root = ET.parse(path).getroot()
        if root.findtext(NS + "type") != "RecordPage" or not root.findtext(NS + "sobjectType"):
            continue
        label = root.findtext(NS + "masterLabel") or ""
        objects[root.findtext(NS + "sobjectType")]["record_pages"].append({
            "name": name, "label": label, "d030_name": bool(D030.match(label.strip())),
            "orders_app": assigned.get(name, []), "who": who(item),
            "created_by": item.get("createdByName"), "modified_by": item.get("lastModifiedByName"),
            "modified": item.get("lastModifiedDate", "")[:10],
            "fields": sorted({f.split(".", 1)[1] for f in texts(root, "fieldItem") if f.startswith("Record.")}),
            "record_detail": any(c in ("force:detailPanel", "force:recordDetailPanelMobile")
                                 for c in texts(root, "componentName")),
        })
    for name, item in lays.items():
        path = os.path.join(base, "layouts", member(item, "layouts", "layout") + ".layout")
        obj = name.split("-", 1)[0]
        root = ET.parse(path).getroot() if os.path.exists(path) else None
        objects[obj]["layouts"].append({
            "name": name, "who": item and who(item), "edited": edited(item),
            "modified": item.get("lastModifiedDate", "")[:10],
            "fields": sorted(set(texts(root, "field"))) if root is not None else None,
        })
    with open(os.path.join(SNAP, "_pages.json"), "w") as f:
        json.dump(objects, f, ensure_ascii=False, indent=1)
    lines = [f"# Record pages and page layouts {datetime.date.today():%Y-%m-%d}", "",
             "Retrieved read-only from yv-partial: every FlexiPage and Layout with no namespace prefix (managed package "
             "pages such as `B25__*` are left out), and the `Orders` app. One section per object. "
             "D-030 name = the label matches `Order-<Object Name>- Record Page`. Edited = a person changed the layout "
             "10 minutes or more after it was created (Salesforce creates a default layout with every custom object).", ""]
    for obj in sorted(objects):
        o = objects[obj]
        lines.append(f"## {obj}")
        for p in sorted(o["record_pages"], key=lambda p: p["name"]):
            flags = [x for x in ("D-030 name" if p["d030_name"] else "", "Orders app: " + "; ".join(p["orders_app"]) if p["orders_app"] else "") if x]
            shown = (f"{len(p['fields'])} fields placed" if p["fields"] else "no fields placed") + \
                    (", shows the record detail (fields from the page layout)" if p["record_detail"] else "")
            lines.append(f"- Record page `{p['name']}`, label \"{p['label']}\"{', ' + ', '.join(flags) if flags else ''}; {p['who']}; {shown}"
                         + (f": {', '.join(p['fields'])}" if p["fields"] else ""))
        if not o["record_pages"]:
            lines.append("- No Lightning record page.")
        for l in sorted(o["layouts"], key=lambda l: l["name"]):
            fl = "not retrieved" if l["fields"] is None else f"{len(l['fields'])} fields: {', '.join(l['fields'])}"
            lines.append(f"- Page layout `{l['name']}`; {l['who']}; {'edited' if l['edited'] else 'not edited since created'}; {fl}")
        lines.append("")
    with open(os.path.join(SNAP, "_pages.md"), "w") as f:
        f.write("\n".join(lines))
    pages = sum(len(o["record_pages"]) for o in objects.values())
    print(f"pages: {os.path.relpath(SNAP, ROOT)}/_pages.md ({pages} record pages, {len(lays)} layouts, {len(objects)} objects)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
