# -*- coding: utf-8 -*-
"""מיפוי מוצע מדיינמיקס לסיילספורס, תכולת הפיילוט. הצעה לבדיקת ארכיטקט."""
import re, openpyxl
from openpyxl.styles import Font, PatternFill, Alignment, Border, Side
from openpyxl.utils import get_column_letter
from openpyxl.worksheet.datavalidation import DataValidation

import os
SRC = os.environ.get("YV_FIELDS_XLSX", "sources/local/yv-fields.xlsx")  # YV fields workbook, kept out of git
F, ACC = "Arial", "1F3864"
HDR = PatternFill("solid", fgColor="DDEBF7")
MINE = PatternFill("solid", fgColor="EDEDED")      # my proposal
REV = PatternFill("solid", fgColor="FFF2CC")       # architect fills
WARN = PatternFill("solid", fgColor="FCE4D6")      # needs a decision
thin = Side(style="thin", color="BFBFBF"); BOX = Border(thin, thin, thin, thin)

A1E = ['new_yv_product', 'new_tour_type', 'new_product_subject',
       'new_activity_day_location', 'new_day_and_time', 'new_tag', 'new_coupon']
B1E = ['new_service_provider', 'new_certification', 'new_certification_category',
       'new_service_provider_status', 'new_pickup_station', 'new_contract_days']

# ---- entity level proposal: (target object, kind, source in DD, note) ----
ENT = {
 'new_yv_product': ("Product2", "אובייקט סטנדרטי", "מפת הישויות: מוצר = Product",
                    "המוצר הנמכר ובעל מחירון. מק\"ט, משך, מינימום ומקסימום משתתפים."),
 'new_tour_type': ("Bundle", "אובייקט של GoMeddo", "מפת הישויות: תבנית יום פעילות = החבילה על פריטיה",
                   "88 שדות. חלק ניכר מהם שדות תצוגה לערוץ המקוון ולקיוסק, שמקומם באפוס ההזמנה המקוונת ולא כאן."),
 'new_product_subject': ("Product_Subject__c", "אובייקט מותאם", "מפת הישויות: לא מופיע בנפרד",
                         "שדה אחד בלבד. אפשר גם רשימת ערכים על המוצר במקום אובייקט. שאלה לארכיטקט."),
 'new_activity_day_location': ("YV_Location__c", "אובייקט מותאם", "מפת הישויות: מקום פעילות = YV Location",
                               "שם בעברית ובאנגלית בלבד."),
 'new_day_and_time': ("Activity_Window__c", "אובייקט מותאם", "לא מופיע במפת הישויות",
                      "חלון ימים ושעות שבו תבנית הפעילות זמינה. ייתכן שנכון למפות ל-GoMeddo Availability. שאלה לארכיטקט."),
 'new_tag': ("Tag__c", "אובייקט מותאם", "לא מופיע במפת הישויות",
             "שלושה שדות. אפשר גם Topic סטנדרטי. שאלה לארכיטקט."),
 'new_coupon': ("Coupon__c", "אובייקט מותאם", "מפת הישויות: קופון = Custom", "זיכוי או הנחה עם תוקף."),
 'new_service_provider': ("Contact", "אובייקט סטנדרטי, סוג רשומה מדריך",
                          "מפת הישויות: נותן שירות ומדריך = Contact, סוג רשומה מדריך",
                          "רוב שדות הזיהוי וההתקשרות ממוזגים לשדות סטנדרטיים של Contact ואינם נבנים מחדש. "
                          "ייצוג השיבוץ הוא GoMeddo Staff המקושר לאותו Contact."),
 'new_certification': ("Certification__c", "אובייקט מותאם", "מפת הישויות: מטריצת הסמכות = Custom",
                       "הקשר לנותן שירות הוא רבים לרבים ולכן נדרש אובייקט חיבור."),
 'new_certification_category': ("Certification_Category__c", "אובייקט מותאם",
                                "מפת הישויות: מטריצת הסמכות = Custom", "שני שדות בלבד."),
 'new_service_provider_status': ("Service_Provider_Status__c", "אובייקט מותאם",
                                 "מסמך 2, תהליך זמינות מדריכים, מפרט סטטוסים מתוארכים",
                                 "כאן מתרכזים אחד עשר שדות הסימון שהיום יושבים על נותן השירות. זה השינוי המבני המרכזי שאני מציע."),
 'new_pickup_station': ("Pickup_Station__c", "אובייקט מותאם", "לא מופיע במפת הישויות",
                        "61,496 שורות במקור. לבדוק כמה מהן באמת פעילות לפני טעינה."),
 'new_contract_days': ("Contract_Days__c", "אובייקט מותאם", "מפת הישויות: ימי חוזה = Custom",
                       "מכסת ימי העבודה החוזית של נותן השירות."),
}
OUT = {  # lookup targets outside the slice
 'new_almog_interface': ("Almog_Interface__c", "שלד", "נוגע בממשק החיוב מול אלמוג"),
 'new_event_reservations': ("דחיית הקשר", "קשר נדחה", "ישות של מסמך 3. לא לבנות עכשיו ולא ליצור את שדה החיפוש"),
 'new_budget_framework': ("Budget_Framework__c", "שלד", "מפת הישויות: מסגרת תקציבית = Custom. נבנה במלואו באפוס A3"),
 'new_rsv_department': ("Department__c", "שלד", "מפת הישויות: מחלקה = Custom. נבנה במלואו באפוס C2"),
 'new_resource': ("GoMeddo Resource", "אובייקט קיים", "מפת הישויות: משאב = GoMeddo Resource. מותקן כבר"),
 'new_activity_day': ("GoMeddo Reservation", "אובייקט קיים", "מפת הישויות: יום פעילות = שריון אב ב-GoMeddo"),
 'new_yv_time': ("רשימת ערכים", "יורד כאובייקט", "שעות. עדיף רשימת ערכים ולא אובייקט"),
 'new_contact_title': ("Salutation", "שדה סטנדרטי", "תואר. שדה סטנדרטי על Contact"),
 'new_country': ("MailingCountry", "שדה סטנדרטי", "ארץ. שדה כתובת סטנדרטי"),
 'SystemUser': ("User", "אובייקט סטנדרטי", "קישור נותן השירות למשתמש מערכת"),
 'קשר של רבים לרבים': ("אובייקט חיבור", "אובייקט חיבור", "אינו אובייקט במקור. מסומן כך בגיליון של יד ושם"),
}

# ---- field level overrides ----
STD_CONTACT = {
 'new_first_name_he': ('FirstName', 'Text(40) סטנדרטי', 'ממוזג'),
 'new_last_name_he': ('LastName', 'Text(80) סטנדרטי', 'ממוזג'),
 'new_email': ('Email', 'Email סטנדרטי', 'ממוזג'),
 'new_mobile': ('MobilePhone', 'Phone סטנדרטי', 'ממוזג'),
 'new_telephone': ('HomePhone', 'Phone סטנדרטי', 'ממוזג'),
 'new_work_telephone': ('Phone', 'Phone סטנדרטי', 'ממוזג'),
 'new_address_line1': ('MailingStreet', 'Address סטנדרטי', 'ממוזג'),
 'new_city': ('MailingCity', 'Address סטנדרטי', 'ממוזג'),
 'new_postalcode': ('MailingPostalCode', 'Address סטנדרטי', 'ממוזג'),
 'new_stateorprovince': ('MailingState', 'Address סטנדרטי', 'ממוזג'),
 'new_country': ('MailingCountry', 'Address סטנדרטי', 'ממוזג'),
 'new_contact_title': ('Salutation', 'Picklist סטנדרטי', 'ממוזג'),
 'new_name': ('Name', 'Name סטנדרטי, מחושב', 'יורד'),
 'new_comments': ('Description', 'Long Text Area סטנדרטי', 'ממוזג'),
}
# the eleven role booleans that move to the dated-status object
ROLE_BITS = ['new_rsv_status_is_actor', 'new_rsv_status_is_cantor', 'new_rsv_status_is_coordinator',
             'new_rsv_status_is_guide', 'new_rsv_status_is_lecturer', 'new_rsv_status_is_national_service',
             'new_rsv_status_is_photographer', 'new_rsv_status_is_poland_guide',
             'new_rsv_status_is_testimony', 'new_rsv_status_is_tour_guide', 'new_rsv_status_is_yv_employee']
WEEKDAY = re.compile(r'(relevant_day|contract_day)_(sunday|monday|tuesday|wednesday|thursday|friday)$')
BAREDAY = re.compile(r'^new_(sunday|monday|tuesday|wednesday|thursday|friday)$')

TYPE = {'nvarchar': 'Text(255)', 'ntext': 'Long Text Area(32000)', 'text': 'Text Area',
        'bit': 'Checkbox', 'boolean': 'Checkbox', 'picklist': 'Picklist', 'lookup': 'Lookup',
        'int': 'Number(18,0)', 'decimal': 'Number(18,2)', 'money': 'Currency(16,2)',
        'datetime': 'Date', 'attachment': 'Files'}


def api(sysname, obj):
    if not sysname:
        return ""
    n = re.sub(r'^new_', '', str(sysname))
    n = ''.join(w.capitalize() for w in n.split('_'))
    return n + "__c"


def propose(r):
    """return target, label, apiname, sftype, lkp, decision, confidence, note"""
    ent, label, sysname = r[0], (r[2] or '').strip(), (r[5] or '').strip()
    t = (str(r[4]).strip().lower() if r[4] else '')
    tgt = (r[6] or '').strip(); tgt = '' if tgt in ('NULL', '') else tgt
    obj = ENT[ent][0]
    sf = TYPE.get(t, 'Text(255)')
    dec, conf, note = 'מועבר כמו שהוא', 'גבוהה', ''

    # 1. role booleans on the service provider
    if sysname in ROLE_BITS:
        return ("Service_Provider_Status__c", label.replace(' (סטטוס)', ''), "Status_Type__c",
                "Picklist, ערך אחד מתוך אחד עשר", "", "ממוזג", "לבדיקה",
                "אחד עשר שדות סימון על נותן השירות מייצגים תפקיד אחד, ולמסמך 2 יש כבר אובייקט סטטוסים מתוארכים. "
                "אני מציע לקפל את כולם לערך רשימה אחד על אובייקט הסטטוס, במקום אחד עשר תיבות סימון.")
    # 2. weekday booleans
    if WEEKDAY.search(sysname or '') or BAREDAY.match(sysname or ''):
        base = 'Relevant_Days__c' if 'relevant' in (sysname or '') else (
               'Contract_Days_Of_Week__c' if 'contract' in (sysname or '') else 'Days_Of_Week__c')
        return (obj, "ימים רלוונטיים" if 'relevant' in sysname else "ימי השבוע", base,
                "Multi-Select Picklist", "", "ממוזג", "לבדיקה",
                "שישה שדות סימון ליום בשבוע. מוצע שדה רב ערכי אחד. מקל על דוחות ועל נוסחאות זמינות.")
    # 3. standard Contact merges
    if ent == 'new_service_provider' and sysname in STD_CONTACT:
        a, ty, d = STD_CONTACT[sysname]
        return ("Contact", label, a, ty, "", d, "גבוהה",
                "שדה סטנדרטי של Contact. אין להקים שדה מותאם.")
    # 4. lookups
    if t == 'lookup':
        if tgt in OUT:
            to, kind, why = OUT[tgt]
            if kind == 'קשר נדחה':
                return (obj, label, api(sysname, obj), "Lookup", to, "יורד", "שאלה",
                        "היעד הוא " + tgt + ". " + why)
            return (obj, label, api(sysname, obj), "Lookup", to, "מומר", "לבדיקה", why)
        if tgt in ENT:
            return (obj, label, api(sysname, obj), "Lookup", ENT[tgt][0], "מועבר כמו שהוא", "גבוהה", "")
        return (obj, label, api(sysname, obj), "Lookup", tgt, "מועבר כמו שהוא", "שאלה",
                "יעד הקשר אינו מזוהה בתכולה ואינו ברשימת הישויות. לזהות לפני הקמה.")
    # 5. dates
    if t == 'datetime':
        sf = 'Date/Time' if any(k in (sysname or '') for k in ('time', 'reminder')) else 'Date'
    # 6. text without a length
    if t == 'nvarchar':
        note = 'אורך 255 כברירת מחדל. המיישם מצמצם לפי הנתונים בפועל. אינו דורש הכרעת ארכיטקט.'
    if t == 'picklist':
        note = 'ערכי הרשימה מגיליון PICKLISTS, נבדקים מול הנתונים בעת ההקמה. אינו דורש הכרעת ארכיטקט.'
    if not sysname:
        conf, note = 'שאלה', 'אין שם מערכת במקור. כנראה שדה שנוסף בדיינמיקס או שדה סטנדרטי. לזהות לפני הקמה.'
    return (obj, label, api(sysname, obj) or "", sf, "", dec, conf, note)


src = openpyxl.load_workbook(SRC, read_only=True, data_only=True)
allrows = [r for r in list(src['שדות'].iter_rows(values_only=True))[1:] if r[0]]
wb = openpyxl.Workbook()
QUESTIONS = []


def mk(title, headers, widths, data, note, fillfn):
    ws = wb.create_sheet(title)
    ws.sheet_view.rightToLeft = True
    ws.cell(1, 1, note).font = Font(name=F, size=10, italic=True, color="555555")
    ws.merge_cells(start_row=1, start_column=1, end_row=1, end_column=len(headers))
    ws.row_dimensions[1].height = 28
    for i, h in enumerate(headers, 1):
        c = ws.cell(2, i, h); c.font = Font(name=F, size=10, bold=True, color=ACC)
        c.fill = HDR; c.border = BOX
        c.alignment = Alignment(wrap_text=True, vertical="center", horizontal="right")
    for j, row in enumerate(data):
        for i, v in enumerate(row, 1):
            c = ws.cell(3 + j, i, v); c.font = Font(name=F, size=10); c.border = BOX
            c.alignment = Alignment(wrap_text=True, vertical="top", horizontal="right")
            f = fillfn(j, i, row)
            if f: c.fill = f
    for i, w in enumerate(widths, 1):
        ws.column_dimensions[get_column_letter(i)].width = w
    ws.freeze_panes = ws.cell(3, 1)
    return ws


def fieldsheet(name, ents, epic, owner):
    data = []
    for e in ents:
        for r in [x for x in allrows if x[0] == e]:
            obj, lab, ap, sf, lk, dec, conf, note = propose(r)
            t = (str(r[4]).strip().lower() if r[4] else '')
            tg = (r[6] or '').strip(); tg = '' if tg in ('NULL', '') else tg
            row = (e, r[2] or '', r[5] or '', t, tg, obj, lab, ap, sf, lk, dec, conf, note, "", "")
            data.append(row)
            if conf != 'גבוהה':
                QUESTIONS.append((epic, e, r[2] or '', conf, obj, sf, note))
    ws = mk(name,
            ["ישות במקור", "שם השדה במקור", "שם מערכת במקור", "סוג במקור", "יעד חיפוש במקור",
             "אובייקט יעד מוצע", "תווית מוצעת", "שם API מוצע", "סוג מוצע", "יעד הקשר",
             "ההחלטה", "רמת ודאות", "נימוק", "אישור ארכיטקט", "הערת ארכיטקט"],
            [22, 28, 26, 11, 20, 24, 26, 24, 22, 20, 15, 11, 46, 14, 30], data,
            f"אפוס {epic}, {owner}. {len(data)} שדות. אפור = ההצעה שלי. צהוב = לאישור הארכיטקט. "
            f"כתום = שדה שבו אני מציע שינוי מבני ולא העברה, ושם צריך להסתכל קודם.",
            lambda j, i, row: (WARN if row[11] != 'גבוהה' and 6 <= i <= 13 else
                               (MINE if 6 <= i <= 13 else (REV if i >= 14 else None))))
    dv = DataValidation(type="list", formula1='"מאושר,לשנות,לדון"', allow_blank=True)
    ws.add_data_validation(dv); dv.add(f"N3:N{2+len(data)}")
    return len(data)


# ---- entity sheet ----
ED = []
for e in A1E + B1E:
    o, kind, srcdd, note = ENT[e]
    ED.append((e, "A1" if e in A1E else "B1", o, kind, srcdd, note, "", ""))
for e, (o, kind, why) in OUT.items():
    ED.append((e, "מחוץ לתכולה", o, kind, "", why, "", ""))
mk("מיפוי ישויות",
   ["ישות במקור", "אפוס", "אובייקט יעד מוצע", "סוג", "המקור במסמך 2", "נימוק", "אישור ארכיטקט", "הערה"],
   [30, 14, 28, 24, 44, 60, 14, 30], ED,
   "ההצעה נגזרת ממפת הישויות בפרק ישויות של מסמך 2. עמודת המקור מפנה לשורה שממנה נגזרה ההצעה.",
   lambda j, i, row: MINE if 3 <= i <= 6 else (REV if i >= 7 else None))

n1 = fieldsheet("מיפוי שדות - A1", A1E, "A1", "מוצרים, חבילות ותבניות יום פעילות")
n2 = fieldsheet("מיפוי שדות - B1", B1E, "B1", "כרטיס נותן השירות, הסמכות וסטטוסים")

# ---- questions sheet ----
mk("שאלות לארכיטקט",
   ["אפוס", "ישות", "שדה", "רמת ודאות", "אובייקט יעד מוצע", "סוג מוצע", "מה צריך להכריע"],
   [8, 24, 28, 11, 24, 24, 62], QUESTIONS,
   f"{len(QUESTIONS)} שורות מתוך {n1+n2} שצריכות מבט. השאר מכני ואפשר לאשר בבת אחת.",
   lambda j, i, row: WARN if i == 4 and row[3] == 'שאלה' else None)

# ---- instructions ----
ws = wb.create_sheet("הוראות", 0)
ws.sheet_view.rightToLeft = True
ws.column_dimensions['A'].width = 24; ws.column_dimensions['B'].width = 104
INS = [("מיפוי מוצע מדיינמיקס לסיילספורס", ""), ("", ""),
 ("מה זה", "הצעת מיפוי מלאה לתכולת הפיילוט, 13 ישויות ו-230 שדות. ההצעה כבר ממולאת. "
           "תפקיד הארכיטקט הוא לבדוק ולאשר, לא למפות מאפס."),
 ("", ""),
 ("על מה נשענת ההצעה", "מפת הישויות בפרק ישויות של מסמך 2, גרסת 15.09.2026. לכל ישות מצוינת השורה שממנה נגזרה ההצעה. "
                       "היכן שמסמך 2 אינו קובע, כתוב במפורש שזו שאלה."),
 ("", ""),
 ("איך לבדוק", "להתחיל מגיליון שאלות לארכיטקט. הוא מרכז רק את השורות שדורשות מבט. "
               "השאר הן המרה מכנית ואפשר לאשר אותן כקבוצה."),
 ("עמודות", "אפור = ההצעה שלי. צהוב = לאישור. כתום = הצעתי שינוי מבני ולא העברה ישירה."),
 ("", ""),
 ("שני שינויים מבניים שאני מציע ושווה להתעכב עליהם", ""),
 ("סטטוסים של נותן שירות", "אחד עשר שדות סימון על נותן השירות מייצגים תפקיד אחד: שחקן, חזן, רכז, מדריך, מרצה, "
                           "בת שירות, צלם, מדריך פולין, איש עדות, מורה דרך, עובד יד ושם. "
                           "מסמך 2 כבר מגדיר אובייקט סטטוסים מתוארכים ומדבר על השפעתם על השיבוץ. "
                           "מוצע לקפל את כל אחד עשר לערך רשימה אחד על אובייקט הסטטוס. "
                           "אחרת אי אפשר לענות מתי מישהו הפסיק להיות מדריך."),
 ("ימי השבוע", "שישה שדות סימון ליום בשבוע חוזרים בשלוש ישויות. מוצע שדה רב ערכי אחד בכל אחת."),
 ("", ""),
 ("מי בונה", "לא שכבת בסיס משותפת. כל בעל אפוס מקים את השדות של האפוס שלו. "
             "בעלים 1 מקים את שדות A1, בעלים 2 מקים את שדות B1. אחרי אישור המיפוי."),
 ("", ""),
 ("מה עדיין לא כאן", "אובייקטים שמחוץ לתכולה מופיעים בגיליון מיפוי ישויות עם הצעה, אבל לא נבנים בפיילוט. "
                     "הקשר לארועים מערכת אחודה מוצע לדחייה, כי הישות שייכת למסמך 3."),
 ("", ""),
 ("אין פריסה", "אין פריסה לארגון בלי אישור מפורש של ליאור."),
]
for i, (a, b) in enumerate(INS, 1):
    ws.cell(i, 1, a).font = Font(name=F, size=11, bold=(b == "" and a != ""), color=ACC if b == "" else "000000")
    c = ws.cell(i, 2, b); c.font = Font(name=F, size=10)
    c.alignment = Alignment(wrap_text=True, vertical="top", horizontal="right")
    ws.cell(i, 1).alignment = Alignment(vertical="top", horizontal="right")
ws.cell(1, 1).font = Font(name=F, size=14, bold=True, color=ACC)

del wb["Sheet"]
out = "/tmp/claude-0/-home-claude/cf26434e-77f7-57cf-af0c-35cc27748132/scratchpad/mapping.xlsx"
wb.save(out)
print("saved | A1:", n1, "| B1:", n2, "| total:", n1 + n2, "| needing review:", len(QUESTIONS))
