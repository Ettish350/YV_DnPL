#!/usr/bin/env python3
"""Builds the daily status email, or the weekly progress email (--weekly), as HTML body and subject, from the task list
that scripts/weekly/score-tasks.py writes. One template for both, so the two emails always look and count the same.

Usage: python3 scripts/daily/build-daily-email.py <tasks.json> <out.html> [<previous tasks.json>]
       python3 scripts/daily/build-daily-email.py <tasks.json> <out.html> --weekly [--issue <n>] [--extras <extras.json>]
(weekly: the previous list is the one score-tasks.py compared with, from scores-<date>.json; run from the repo root)
Writes <out.html> and <out.html>.subject.txt. Reads scores-<date>.json next to <tasks.json> (working days left, status per
person, Score disputed). extras-<date>.json (weekly only, written by the weekly check in step 7): reviews Lior owes,
Jira changes made by the run, findings worth his decision.
Outlook strips <style>, <span> and <img>, so the look is plain tables, bold, emoji and text progress bars only.
"""
import argparse, datetime as dt, html, json, os, re

ap = argparse.ArgumentParser()
ap.add_argument('tasks'); ap.add_argument('out'); ap.add_argument('prev', nargs='?')
ap.add_argument('--weekly', action='store_true'); ap.add_argument('--issue'); ap.add_argument('--extras')
a = ap.parse_args()
src, out = a.tasks, a.out
tasks = json.load(open(src, encoding='utf-8'))
day = re.search(r'(\d{4}-\d{2}-\d{2})', os.path.basename(src)).group(1)
today = dt.date.fromisoformat(day)
scores = json.load(open(os.path.join(os.path.dirname(src), f'scores-{day}.json'), encoding='utf-8'))
# Weekly: "since last check" is the run score-tasks.py compared with (the newest earlier task list, daily or weekly).
prev_path = a.prev or (scores.get('previous_run') if a.weekly else None)
prev_path = prev_path if prev_path and os.path.exists(prev_path) else None
prev = {t['key']: float(t['percent']) for t in json.load(open(prev_path, encoding='utf-8'))} if prev_path else {}
extras_path = a.extras or os.path.join(os.path.dirname(src), f'extras-{day}.json')
extras = json.load(open(extras_path, encoding='utf-8')) if a.weekly and os.path.exists(extras_path) else None
REPO = 'https://github.com/liorpridorrrrrrr/YV2026'
JIRA = 'https://malamteam.atlassian.net/browse/'

start, end = (dt.date.fromisoformat(scores['sprint'][k]) for k in ('start', 'end'))
days_left = scores['working_days_left']
cap = days_left * scores['hours_per_day']
ICON = {'on track': '🟢', 'at risk': '🔴', 'blocked': '⛔'}

def bar(p):
    n = int(round(p / 10))
    return '█' * n + '░' * (10 - n)

def short(s):
    # the research task names repeat a long phrase; keep the entity and the kind of task
    return s.replace(' שהתקבלו מיד ושם וחיבורם לארכיטקטורה החדשה', '')

def h(x):
    return f'{x:.0f}' if abs(x - round(x)) < 0.05 else f'{x:.1f}'

def est(t):
    return float(t['original_estimate_h']) if t['original_estimate_h'] not in (None, '') else 0.0

def jl(key):
    return f'<a href="{JIRA}{key}">{key}</a>' if key.startswith('YV') else key

people = []
for t in tasks:
    if t['assignee'] not in people: people.append(t['assignee'])
stats = {}
for p in people:
    ts = [t for t in tasks if t['assignee'] == p]
    pl = sum(est(t) for t in ts)
    dn = sum(est(t) * float(t['percent']) / 100 for t in ts)
    stats[p] = (ts, pl, dn, pl - dn)
P = sum(s[1] for s in stats.values()); D = sum(s[2] for s in stats.values()); L = P - D
pct = 100 * D / P if P else 0
moved = [(t, prev[t['key']], float(t['percent'])) for t in tasks if t['key'] in prev and float(t['percent']) != prev[t['key']]]

E = html.escape
o = []
if a.weekly:
    o.append(f'<p>📋 Full report: <a href="{REPO}/issues/{a.issue}"><b>Weekly progress {day}, Issue #{a.issue}</b></a></p>'
             if a.issue else f'<p>📋 Issue not posted. Report: reports/weekly/progress-{day}.md</p>')
o.append(f'<h2>Sprint {start:%d.%m} to {end:%d.%m}: {pct:.0f}% done</h2>')
o.append(f'<p><b>{bar(pct)}</b> &nbsp; {h(D)} h done, <b>{h(L)} h left</b> of {h(P)} h &nbsp;|&nbsp; '
         f'{days_left} working day{"s" if days_left != 1 else ""} left ({h(cap)} h per person)</p>')
o.append('<table border="1" cellpadding="6" cellspacing="0">')
o.append('<tr><th>Team member</th><th>Progress</th><th>% done</th><th>Hours done</th><th>Hours left</th><th>Status</th></tr>')
for p in people:
    ts, pl, dn, lf = stats[p]
    pc = 100 * dn / pl if pl else 0
    st = scores['status'].get(p, ['', ''])[0]
    o.append(f'<tr><td><b>{E(p)}</b></td><td>{bar(pc)}</td><td>{pc:.0f}%</td><td>{h(dn)}</td><td><b>{h(lf)}</b></td>'
             f'<td>{ICON.get(st, "")} {st.capitalize()}</td></tr>')
o.append(f'<tr><td><b>Team</b></td><td>{bar(pct)}</td><td><b>{pct:.0f}%</b></td><td><b>{h(D)}</b></td><td><b>{h(L)}</b></td><td></td></tr>')
o.append('</table>')

if a.weekly:
    m = re.search(r'/(daily|weekly)/tasks-(\d{4})-(\d{2})-(\d{2})\.json$', prev_path or '')
    o.append(f'<h3>Since last check{f" ({m.group(1)} {m.group(4)}.{m.group(3)})" if m else ""}</h3>')
    none_txt = ('No earlier task list to compare with.', 'No task moved since the last check.')
else:
    o.append('<h3>Since yesterday</h3>')
    none_txt = ('No earlier daily list to compare with.', 'No task moved since yesterday.')
if not prev_path:
    o.append(f'<p>{none_txt[0]}</p>')
elif not moved:
    o.append(f'<p>{none_txt[1]}</p>')
else:
    o.append('<ul>')
    for t, x, y in moved:
        arrow = '▲' if y > x else '▼'
        o.append(f'<li>{arrow} {jl(t["key"])} '
                 f'{E(t["assignee"].split()[0])}: {x:.0f}% → <b>{y:.0f}%</b> &nbsp;{E(short(t["summary"])[:60])}</li>')
    o.append('</ul>')

for p in people:
    ts, pl, dn, lf = stats[p]
    pc = 100 * dn / pl if pl else 0
    st = scores['status'].get(p, ['', ''])[0]
    o.append(f'<h3>{ICON.get(st, "")} {E(p)}: {pc:.0f}% done, {h(lf)} h left</h3>')
    open_ts = [t for t in ts if float(t['percent']) < 100]
    done_ts = [t for t in ts if float(t['percent']) >= 100]
    if open_ts:
        o.append('<table border="1" cellpadding="4" cellspacing="0">')
        o.append('<tr><th>Progress</th><th>%</th><th>Jira</th><th>Task</th><th>h left</th><th>Evidence</th></tr>')
        for t in open_ts:
            pc_t = float(t['percent']); left = est(t) * (1 - pc_t / 100)
            ch = ''
            if t['key'] in prev and prev[t['key']] != pc_t:
                ch = ' ▲' if pc_t > prev[t['key']] else ' ▼'
            o.append(f'<tr><td>{bar(pc_t)}</td><td>{pc_t:.0f}%{ch}</td>'
                     f'<td>{jl(t["key"])}</td>'
                     f'<td dir="rtl">{E(short(t["summary"])[:60])}</td><td>{h(left)}</td><td>{E(t.get("evidence", "")[:90])}</td></tr>')
        o.append('</table>')
    if done_ts:
        links = ', '.join(jl(t['key']) for t in done_ts)
        o.append(f'<p>✅ Done: {links}</p>')

def table(head, rows):
    o.append('<table border="1" cellpadding="4" cellspacing="0">')
    o.append('<tr>' + ''.join(f'<th>{x}</th>' for x in head) + '</tr>')
    for r in rows:
        o.append('<tr>' + ''.join(f'<td>{x}</td>' for x in r) + '</tr>')
    o.append('</table>')

dis = scores.get('disputed') or []
if dis or a.weekly:
    o.append('<h3>⚖️ Score disputed</h3>')
    if not dis:
        o.append('<p>None.</p>')
    else:
        o.append('<p>The judged score differs from the last check, and no component it lists was created or changed since. '
                 'The last score is kept until you decide.</p>')
        table(['Jira', 'Who', 'Task', 'Kept', 'Judged today', 'Why'],
              [[jl(d['key']), E(d['assignee'].split()[0]), E(short(d["summary"])[:50]),
                f'<b>{d["kept"]:.0f}%</b>', f'{d["judged"]:.0f}%',
                E(f'last: {d["previous_evidence"][:70]}; today: {d["judged_evidence"][:70]}')] for d in dis])

if a.weekly:
    o.append('<h3>👀 Reviews you owe</h3>')
    rv = (extras or {}).get('reviews_owed') or []
    if not rv:
        o.append('<p>None.</p>' if extras else '<p>Not written by this run.</p>')
    else:
        table(['PR', 'Author', 'Title', 'Waiting since', 'Note'],
              [[f'<a href="{r.get("url") or REPO + "/pull/" + str(r["pr"])}">#{r["pr"]}</a>', E(r.get('author', '')),
                E(r.get('title', '')), E(r.get('requested', '')), E(r.get('note', ''))] for r in rv])
    o.append('<h3>✏️ Jira changes by this run</h3>')
    jc = (extras or {}).get('jira_changes') or []
    if not extras:
        o.append('<p>Not written by this run.</p>')
    elif not jc:
        o.append('<p>None.</p>')
    else:
        comments = sum(1 for c in jc if c.get('comment'))
        moves = [c for c in jc if c.get('to')]
        o.append(f'<p>{comments} comment{"s" if comments != 1 else ""} added, {len(moves)} status move{"s" if len(moves) != 1 else ""}.</p>')
        if moves:
            table(['Jira', 'From', 'To'], [[jl(c['key']), E(c.get('from', '')), f'<b>{E(c["to"])}</b>'] for c in moves])
    o.append('<h3>🧭 Findings worth your decision</h3>')
    fd = (extras or {}).get('findings') or []
    if not fd:
        o.append('<p>None.</p>' if extras else '<p>Not written by this run.</p>')
    else:
        table(['#', 'Finding'], [[str(i), E(f['text']) + (f' <a href="{f["link"]}">link</a>' if f.get('link') else '')]
                                 for i, f in enumerate(fd, 1)])
    o.append('<p><i>Scores come from scripts/weekly/score-tasks.py on Jira, git and a read-only pull of yv-partial, '
             'the same as the daily email. The full report, mismatches and open questions are in the Issue.</i></p>')
else:
    o.append(f'<p><i>Scores come from the daily check: Jira, git and a read-only pull of yv-partial. Nothing was written to Jira. '
             f'Full sheet: ~/yv-weekly/daily/Daily status {today:%Y-%m-%d}.xlsx</i></p>')
open(out, 'w', encoding='utf-8').write('\n'.join(o))
kind = 'weekly' if a.weekly else 'daily'
subject = f'YV sprint {kind} {today:%d.%m}: {pct:.0f}% done, {h(L)} h left, {days_left} days left'
open(out + '.subject.txt', 'w', encoding='utf-8').write(subject)
print(subject)
