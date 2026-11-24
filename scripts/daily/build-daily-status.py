#!/usr/bin/env python3
"""Builds the daily status workbook from the task list the daily check writes.

Usage: python3 scripts/daily/build-daily-status.py <tasks.json> <output.xlsx> <DD.MM.YYYY>

tasks.json is a list of {assignee, key, summary, jira_status, original_estimate_h, percent, evidence},
written by the headless check (see run-daily-status.sh). Hours done = planned x % done; hours left = planned - done.
Everything is a formula, so editing a blue % cell updates every total.
"""
import json, sys
from openpyxl import Workbook
from openpyxl.styles import Font, PatternFill, Alignment, Border, Side
from openpyxl.workbook.properties import CalcProperties

src, out, day = sys.argv[1:4]
tasks = json.load(open(src, encoding='utf-8'))
if not tasks:
    sys.exit('no tasks in ' + src)
people = []
for t in tasks:
    if t['assignee'] not in people:
        people.append(t['assignee'])

F = 'Arial'
B, BB = Font(name=F), Font(name=F, bold=True)
H = Font(name=F, bold=True, color='FFFFFF'); HF = PatternFill('solid', fgColor='1F3864')
PF = PatternFill('solid', fgColor='D9E1F2'); TF = PatternFill('solid', fgColor='FFF2CC')
BLUE = Font(name=F, color='0000FF'); LINK = Font(name=F, color='0563C1', underline='single')
NOTE = Font(name=F, italic=True, color='595959')
s = Side(style='thin', color='BFBFBF'); BOX = Border(left=s, right=s, top=s, bottom=s)
RTL = Alignment(horizontal='right', vertical='top', wrap_text=True, readingOrder=2)
WR = Alignment(vertical='top', wrap_text=True); C = Alignment(horizontal='center', vertical='top')

wb = Workbook(); ws = wb.active; ws.title = 'Daily ' + day[:5]
ws['A1'] = f'Sprint daily status, {day}'; ws['A1'].font = Font(name=F, bold=True, size=14)
ws['A2'] = ('Hours done = planned hours x % done. Hours left = planned - done. % done (blue) is the evidence score '
            'from the daily check (Jira, git and a read-only pull of yv-partial); edit it and all totals update. Jira was not changed.')
ws['A2'].font = NOTE
for i, w in enumerate([18, 11, 46, 22, 10, 9, 10, 10, 60], 1):
    ws.column_dimensions[chr(64 + i)].width = w

def fmt(r, bold=False, fill=None):
    for j in range(1, 9):
        c = ws.cell(r, j); c.border = BOX
        if bold: c.font = BB
        if fill: c.fill = fill
        if j >= 5: c.alignment = C
    ws.cell(r, 6).number_format = '0%'
    for j in (5, 7, 8): ws.cell(r, j).number_format = '0.0'

for j, h in enumerate(['Team member', '', '', '', 'Planned h', '% done', 'Hours done', 'Hours left'], 1):
    c = ws.cell(4, j, h or None); c.font = H; c.fill = HF; c.border = BOX
sum_row = {p: 5 + i for i, p in enumerate(people)}
total_row = 5 + len(people)
r = total_row + 3
sub = {}
for p in people:
    ws.cell(r, 1, p).font = BB
    for j in range(1, 10): ws.cell(r, j).fill = PF
    r += 1
    for j, h in enumerate(['', 'Jira', 'Task', 'Jira status', 'Planned h', '% done', 'Hours done', 'Hours left', 'Evidence'], 1):
        c = ws.cell(r, j, h or None); c.font = H; c.fill = HF; c.border = BOX
    r += 1; first = r
    for t in [t for t in tasks if t['assignee'] == p]:
        vals = [None, t['key'], t['summary'], t['jira_status'], float(t['original_estimate_h'] or 0),
                float(t['percent']) / 100, f'=E{r}*F{r}', f'=E{r}-G{r}', t.get('evidence', '')]
        for j, v in enumerate(vals, 1):
            c = ws.cell(r, j, v); c.font = B; c.border = BOX; c.alignment = WR
        ws.cell(r, 2).hyperlink = f"https://malamteam.atlassian.net/browse/{t['key']}"; ws.cell(r, 2).font = LINK
        ws.cell(r, 3).alignment = RTL; ws.cell(r, 6).font = BLUE; ws.cell(r, 6).number_format = '0%'
        for j in (5, 7, 8): ws.cell(r, j).number_format = '0.0'
        for j in (5, 6, 7, 8): ws.cell(r, j).alignment = C
        r += 1
    last = r - 1
    ws.cell(r, 3, 'Total ' + p.split()[0])
    for j, col in ((5, 'E'), (7, 'G'), (8, 'H')): ws.cell(r, j, f'=SUM({col}{first}:{col}{last})')
    ws.cell(r, 6, f'=IF(E{r}=0,0,G{r}/E{r})')
    fmt(r, True, TF); ws.cell(r, 1).border = Border(); ws.cell(r, 2).border = Border()
    sub[p] = r; r += 2
for p, sr in sum_row.items():
    ws.cell(sr, 1, p)
    for j, col in ((5, 'E'), (7, 'G'), (8, 'H')): ws.cell(sr, j, f'={col}{sub[p]}')
    ws.cell(sr, 6, f'=IF(E{sr}=0,0,G{sr}/E{sr})')
    fmt(sr); ws.cell(sr, 1).font = BB
ws.cell(total_row, 1, 'Team total')
for j, col in ((5, 'E'), (7, 'G'), (8, 'H')): ws.cell(total_row, j, f'=SUM({col}5:{col}{total_row - 1})')
ws.cell(total_row, 6, f'=IF(E{total_row}=0,0,G{total_row}/E{total_row})')
fmt(total_row, True, TF)
ws.freeze_panes = 'A5'; ws.sheet_view.showGridLines = False
wb.calculation = CalcProperties(fullCalcOnLoad=True)
wb.save(out)
P = sum(float(t['original_estimate_h'] or 0) for t in tasks)
D = sum(float(t['original_estimate_h'] or 0) * float(t['percent']) / 100 for t in tasks)
print(f'{out}: {len(tasks)} tasks, {P:.1f} h planned, {D:.1f} h done, {P - D:.1f} h left ({D / P:.0%})')
