# -*- coding: utf-8 -*-
"""Build the English field-mapping markdown files (mapping/*.md) for the pilot epics.

Usage (from the repo root):
    YV_FIELDS_XLSX=/path/to/yv-fields.xlsx python3 scripts/build_mapping_md.py

The YV fields workbook is client data and is NOT kept in git. Get it from SharePoint
(see sources/SOURCES.md) and point YV_FIELDS_XLSX at the local copy.
Lior's or Anna's Claude regenerates the A1 and B1 files. An implementer's Claude may edit the sections of its own entities through a PR (D-030).
"""
import os, sys, openpyxl

HERE = os.path.dirname(os.path.abspath(__file__))
code = open(os.path.join(HERE, "mapping.py"), encoding="utf-8").read().split("src = openpyxl")[0]
exec(code)  # brings in SRC, A1E, B1E, ENT, OUT, propose()

# 23 kiosk / website display fields of new_tour_type, proposed to move from A1 to C3 (pending Lior)
DISPLAY = {'new_description_en', 'new_description_he', 'new_short_desc_en', 'new_short_desc_he',
           'new_kiosk_sort_weight', 'new_link_to_more_details_en', 'new_link_to_more_details_he', 'new_ltr_rtl',
           'new_name_kiosk_en', 'new_name_kiosk_he', 'new_note_watcher_en', 'new_note_watcher_he',
           'new_price_desc_en', 'new_price_desc_he', 'new_price_desc_kiosk_en', 'new_price_desc_kiosk_he',
           'new_show_comments_on_website', 'new_sort_weight_en', 'new_sort_weight_he', 'new_tab_to_display',
           'new_is_to_display_age_and_country', 'new_is_recommended', 'new_product_display_tickets_cards'}

EN = {
    # decisions
    'מועבר כמו שהוא': 'carry over', 'ממוזג': 'merge', 'יורד': 'drop', 'מומר': 'redirect',
    # confidence
    'גבוהה': 'high', 'לבדיקה': 'review', 'שאלה': 'question',
    # types
    'Address סטנדרטי': 'Address (standard)', 'Email סטנדרטי': 'Email (standard)',
    'Long Text Area סטנדרטי': 'Long Text Area (standard)', 'Name סטנדרטי, מחושב': 'Name (standard, computed)',
    'Phone סטנדרטי': 'Phone (standard)', 'Picklist סטנדרטי': 'Picklist (standard)',
    'Picklist, ערך אחד מתוך אחד עשר': 'Picklist, one of 11 values',
    'Text(40) סטנדרטי': 'Text(40) (standard)', 'Text(80) סטנדרטי': 'Text(80) (standard)',
    # lookup targets
    'אובייקט חיבור': 'junction object', 'דחיית הקשר': 'deferred relationship', 'רשימת ערכים': 'picklist',
    # notes
    'אורך 255 כברירת מחדל. המיישם מצמצם לפי הנתונים בפועל. אינו דורש הכרעת ארכיטקט.': '',
    'ערכי הרשימה מגיליון PICKLISTS, נבדקים מול הנתונים בעת ההקמה. אינו דורש הכרעת ארכיטקט.': '',
    'אחד עשר שדות סימון על נותן השירות מייצגים תפקיד אחד, ולמסמך 2 יש כבר אובייקט סטטוסים מתוארכים. אני מציע לקפל את כולם לערך רשימה אחד על אובייקט הסטטוס, במקום אחד עשר תיבות סימון.':
        'One of the 11 role checkboxes. Proposed: fold all 11 into one picklist value on the dated status object (brief, section 6.1).',
    'אין שם מערכת במקור. כנראה שדה שנוסף בדיינמיקס או שדה סטנדרטי. לזהות לפני הקמה.':
        'No system name in the source. Probably a Dynamics standard or added field. Identify before building.',
    'אינו אובייקט במקור. מסומן כך בגיליון של יד ושם': 'Not an object in the source. Marked this way in the YV workbook.',
    'היעד הוא new_event_reservations. ישות של מסמך 3. לא לבנות עכשיו ולא ליצור את שדה החיפוש':
        'Target is new_event_reservations, a Doc 3 entity. Do not build it and do not create this lookup.',
    'מפת הישויות: יום פעילות = שריון אב ב-GoMeddo': 'Entity map: activity day = GoMeddo parent reservation.',
    'מפת הישויות: מחלקה = Custom. נבנה במלואו באפוס C2': 'Entity map: department = custom. Stub now, fully built in C2.',
    'מפת הישויות: מסגרת תקציבית = Custom. נבנה במלואו באפוס A3': 'Entity map: budget framework = custom. Stub now, fully built in A3.',
    'מפת הישויות: משאב = GoMeddo Resource. מותקן כבר': 'Entity map: resource = GoMeddo Resource. Already installed.',
    'נוגע בממשק החיוב מול אלמוג': 'Touches the Almog billing interface. Stub now.',
    'קישור נותן השירות למשתמש מערכת': 'Links the service provider to a system user.',
    'שדה סטנדרטי של Contact. אין להקים שדה מותאם.': 'Standard Contact field. Do not create a custom field.',
    'שישה שדות סימון ליום בשבוע. מוצע שדה רב ערכי אחד. מקל על דוחות ועל נוסחאות זמינות.':
        'One of six weekday checkboxes. Proposed: one multi-select picklist (brief, section 6.2).',
    'ישות של מסמך 3. לא לבנות עכשיו ולא ליצור את שדה החיפוש': 'A Doc 3 entity. Do not build it now and do not create the lookup.',
    'ארץ. שדה כתובת סטנדרטי': 'Country. Standard address field.',
    'שעות. עדיף רשימת ערכים ולא אובייקט': 'Hours. A picklist is better than an object.',
    'תואר. שדה סטנדרטי על Contact': 'Title. Standard Contact field.',
}
KIND = {'אובייקט סטנדרטי': 'standard object', 'אובייקט של GoMeddo': 'GoMeddo object', 'אובייקט מותאם': 'custom object',
        'אובייקט סטנדרטי, סוג רשומה מדריך': 'standard object, Guide record type'}
ENT_EN = {  # entity-level rationale, English
    'new_yv_product': ("Entity map: product = Product", "The sellable product with a price book. SKU, duration, min and max participants."),
    'new_tour_type': ("Entity map: activity day template = Bundle with its items", "88 fields. Many are online/kiosk display fields that belong to C3, not A1."),
    'new_product_subject': ("Not listed separately in the entity map", "One field only. Could be a picklist on the product instead. Question for the architect."),
    'new_activity_day_location': ("Entity map: activity location = YV Location", "Hebrew and English name only."),
    'new_day_and_time': ("Not in the entity map", "Day/time window in which the template is available. Might map to GoMeddo Availability. Question for the architect."),
    'new_tag': ("Not in the entity map", "Three fields. Standard Topic is an alternative. Question for the architect."),
    'new_coupon': ("Entity map: coupon = custom", "Credit or discount with validity dates."),
    'new_service_provider': ("Entity map: service provider / guide = Contact, Guide record type",
                             "Most identity and contact fields map to standard Contact fields. Scheduling uses GoMeddo Staff linked to the same Contact."),
    'new_certification': ("Entity map: certification matrix = custom", "Many-to-many with the service provider, so a junction object is needed."),
    'new_certification_category': ("Entity map: certification matrix = custom", "Two fields only."),
    'new_service_provider_status': ("Doc 2, guide availability process, dated statuses spec", "Receives the 11 role checkboxes from the service provider. The main structural change."),
    'new_pickup_station': ("Not in the entity map", "61,496 rows in the source. Check how many are active before loading."),
    'new_contract_days': ("Entity map: contract days = custom", "The provider's contractual workday quota."),
}


def t(s):
    s = s or ''
    return EN.get(s, s)


def esc(s):
    return str(s or '').replace('|', '/').replace('\n', ' ').strip()


def main():
    src = openpyxl.load_workbook(SRC, read_only=True, data_only=True)
    allrows = [r for r in list(src['שדות'].iter_rows(values_only=True))[1:] if r[0]]
    os.makedirs("mapping", exist_ok=True)

    # entities
    L = ["# Proposed entity mapping (pilot scope)", "",
         "Derived from the entity map in Doc 2, chapter 'Entities'. Architect reviews, does not re-map.", "",
         "| Source entity | Epic | Proposed target | Kind | Source in Doc 2 | Rationale |", "|---|---|---|---|---|---|"]
    for e in A1E + B1E:
        o, kind, _, _ = ENT[e]
        s, n = ENT_EN[e]
        L.append(f"| {e} | {'A1' if e in A1E else 'B1'} | {o} | {KIND.get(kind, kind)} | {s} | {n} |")
    L += ["", "## Lookup targets outside the pilot", "",
          "| Source entity | Proposed target | Handling | Note |", "|---|---|---|---|"]
    HAND = {'שלד': 'stub', 'קשר נדחה': 'deferred', 'אובייקט קיים': 'exists', 'יורד כאובייקט': 'not an object',
            'שדה סטנדרטי': 'standard field', 'אובייקט סטנדרטי': 'standard object', 'אובייקט חיבור': 'junction object'}
    for e, (o, kind, why) in OUT.items():
        L.append(f"| {e if e != 'קשר של רבים לרבים' else 'many-to-many (marked in YV workbook)'} | {t(o)} | {HAND.get(kind, kind)} | {t(why)} |")
    open("mapping/entities.md", "w", encoding="utf-8").write("\n".join(L) + "\n")

    stats = {}
    for epic, ents in (("A1", A1E), ("B1", B1E)):
        out = [f"# {epic}: proposed field mapping", "",
               "Generated by `scripts/build_mapping_md.py`. Edit only the sections of your own entities, through a PR (D-030).", "",
               "Decisions: **carry over** = build as mapped; **merge** = maps to a standard or consolidated field, do not create a custom field; "
               "**drop** = not built; **redirect** = lookup points to a stub; **move to C4** = display field taken out of the pilot.", "",
               "Confidence: **high** = mechanical; **review** = needs architect approval; **question** = the DD does not decide. "
               "Do not build review/question rows before they are approved.", "",
               "Source labels are Hebrew because they are YV data, and they are also the proposed Salesforce labels.", ""]
        n = q = 0
        for e in ents:
            rows = [x for x in allrows if x[0] == e]
            out += ["", f"## {e} -> {ENT[e][0]} ({len(rows)} fields)", "",
                    "| # | Source label | Source system name | Source type | Target object | Proposed label | Proposed API | Proposed type | Lookup target | Decision | Confidence | Note |",
                    "|---|---|---|---|---|---|---|---|---|---|---|---|"]
            for i, r in enumerate(rows, 1):
                o, l, a, sf, lk, d, c, nt = propose(r)
                if e == 'new_tour_type' and (r[5] or '') in DISPLAY:
                    d, c, nt = 'move to C3', 'review', 'Kiosk or website display field. Not built in A1 (pending Lior).'
                d, c, nt = t(d), t(c), t(nt)
                n += 1
                q += c != 'high'
                out.append(f"| {i} | {esc(r[2])} | {esc(r[5])} | {esc(r[4])} | {esc(o)} | {esc(l)} | {esc(a)} | {esc(t(sf))} | {esc(t(lk))} | {d} | {c} | {esc(nt)} |")
        out.insert(2, f"{n} source fields, {q} rows need a look (review or question).\n")
        open(f"mapping/{epic}-field-mapping.md", "w", encoding="utf-8").write("\n".join(out) + "\n")
        stats[epic] = (n, q)
    print(stats)


if __name__ == "__main__":
    main()
