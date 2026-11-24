import { LightningElement, api } from 'lwc';
import { FlowAttributeChangeEvent, FlowNavigationNextEvent, FlowNavigationFinishEvent } from 'lightning/flowSupport';
import getCandidates from '@salesforce/apex/GS_AssignController.getCandidates';

const SKIP = 'SKIP';
const TOP = 5; // full matches shown before "show more"
const SEARCH_LIMIT = 30; // rows shown for a search

export default class GsGuideWizard extends LightningElement {
    @api orderId;
    @api rows = [];
    @api mode = 'ONE';
    @api pairs = []; // in and out: "reservationId|staffId|name"
    @api skips = []; // in and out: reservation Ids chosen as "later"
    @api pairCount = 0;
    @api skipCount = 0;
    @api availableActions = [];

    // decisions: reservationId -> { sid: staffId or SKIP, name }. A group with no entry is not decided yet.
    dec = {};
    idx = 0;
    choices = [];
    loading = true;
    error = '';
    query = '';
    showAllFull = false;
    nearToggled = false;
    nearOpen = false;
    only25 = false;
    token = 0;

    connectedCallback() {
        const rows = this.rows || [];
        const dec = {};
        const storedPairs = this.pairs || [];
        const storedSkips = this.skips || [];
        if (storedPairs.length || storedSkips.length) {
            // Coming back from the confirm screen: restore the earlier choices
            storedPairs.forEach((p) => {
                const parts = p.split('|');
                dec[parts[0]] = { sid: parts[1], name: parts.slice(2).join('|') };
            });
            storedSkips.forEach((id) => {
                dec[id] = { sid: SKIP };
            });
        } else if (this.mode === 'AUTO') {
            // Full matches are approved up front; the coordinator reviews the rest
            rows.forEach((r) => {
                if (r.matchType === 'FULL') {
                    dec[r.reservationId] = { sid: r.suggestedStaffId, name: r.suggestedName };
                }
            });
        }
        this.dec = dec;
        const firstOpen = rows.findIndex((r) => !dec[r.reservationId]);
        this.idx = firstOpen >= 0 ? firstOpen : 0;
        this.publish();
        this.load();
    }

    // ---------- loading candidates for the current group ----------

    async load() {
        const row = this.currentRow;
        if (!row) {
            this.loading = false;
            return;
        }
        const token = ++this.token;
        this.loading = true;
        this.error = '';
        this.query = '';
        this.showAllFull = false;
        this.nearToggled = false;
        try {
            const result = await getCandidates({
                orderId: this.orderId,
                reservationId: row.reservationId,
                picks: this.pickList
            });
            if (token === this.token) {
                this.choices = result || [];
            }
        } catch (e) {
            if (token === this.token) {
                this.choices = [];
                this.error = (e && e.body && e.body.message) || 'אירעה שגיאה בטעינת המדריכים';
            }
        } finally {
            if (token === this.token) {
                this.loading = false;
            }
        }
    }

    // ---------- state ----------

    get currentRow() {
        return (this.rows || [])[this.idx];
    }

    get total() {
        return (this.rows || []).length;
    }

    get pickList() {
        return (this.rows || [])
            .filter((r) => this.dec[r.reservationId] && this.dec[r.reservationId].sid !== SKIP)
            .map((r) => r.reservationId + '|' + this.dec[r.reservationId].sid);
    }

    get bestFull() {
        return (this.choices || []).find((c) => c.tier === 'FULL');
    }

    get decision() {
        const row = this.currentRow;
        return row ? this.dec[row.reservationId] : undefined;
    }

    get selected() {
        if (this.decision) {
            return this.decision.sid;
        }
        return this.bestFull ? this.bestFull.value : SKIP;
    }

    get isLast() {
        return this.idx >= this.total - 1;
    }

    get headerText() {
        return this.currentRow ? this.currentRow.label : '';
    }

    get counter() {
        return 'קבוצה ' + (this.idx + 1) + ' מתוך ' + this.total;
    }

    get assignedCount() {
        return (this.rows || []).filter((r) => this.dec[r.reservationId] && this.dec[r.reservationId].sid !== SKIP).length;
    }

    get skippedCount() {
        return (this.rows || []).filter((r) => this.dec[r.reservationId] && this.dec[r.reservationId].sid === SKIP).length;
    }

    get undecidedCount() {
        return this.total - this.assignedCount - this.skippedCount;
    }

    get summary() {
        return 'משובצות ' + this.assignedCount + ' · לא משובצות ' + this.skippedCount + ' · טרם נבחר ' + this.undecidedCount;
    }

    // The strip of groups across the top: one chip per group with its state
    get chips() {
        return (this.rows || []).map((r, i) => {
            const d = this.dec[r.reservationId];
            let state = 'pending';
            let line = 'טרם נבחר';
            let icon = '○';
            if (d && d.sid === SKIP) {
                state = 'skipped';
                line = 'לא משובצת';
                icon = '↷';
            } else if (d) {
                state = 'assigned';
                line = d.name || 'משובצת';
                icon = '✓';
            }
            const parts = (r.label || '').split(' | ');
            return {
                key: 'chip-' + r.reservationId,
                index: i,
                name: parts[0],
                time: parts[1] || '',
                line,
                icon,
                pressed: String(i === this.idx),
                cls: 'gchip ' + state + (i === this.idx ? ' current' : '')
            };
        });
    }

    // ---------- cards ----------

    get cardsAll() {
        return (this.choices || []).map((c) => {
            const blocked = c.tier === 'BLOCKED';
            const full = c.tier === 'FULL';
            const isSelected = this.selected === c.value;
            const name = c.staffName || c.label || '';
            return {
                key: 'c-' + c.value,
                isCard: true,
                value: c.value,
                tier: c.tier,
                name,
                initial: name.charAt(0),
                tag: c.tag,
                full,
                blocked,
                gaps: (c.gapTexts || []).map((text, i) => ({
                    key: c.value + '-g' + i,
                    text,
                    cls: 'chip ' + ((c.gapKinds || [])[i] === 'bad' ? 'bad' : 'warn')
                })),
                checked: String(isSelected),
                disabled: blocked,
                cls: 'card' + (full ? ' full' : '') + (isSelected ? ' selected' : '') + (blocked ? ' blocked' : '')
            };
        });
    }

    get hasFullMatch() {
        return !!this.bestFull;
    }

    get noFullMatch() {
        return !this.loading && !this.error && !this.bestFull;
    }

    get searching() {
        return this.query.trim().length > 0;
    }

    get n25() {
        return (this.choices || []).filter((c) => c.tag && c.tier !== 'BLOCKED' && c.tier !== 'FAR').length;
    }

    get has25() {
        return (this.choices || []).some((c) => c.tag);
    }

    get listRows() {
        const all = this.cardsAll;
        const rows = [];

        if (this.searching) {
            const q = this.query.trim().toLowerCase();
            const hits = all.filter((c) => c.name.toLowerCase().includes(q));
            if (!hits.length) {
                rows.push({ key: 'empty', isNote: true, text: 'לא נמצא מדריך בשם הזה' });
                return rows;
            }
            hits.slice(0, SEARCH_LIMIT).forEach((c) => rows.push(c));
            if (hits.length > SEARCH_LIMIT) {
                rows.push({ key: 'lim', isNote: true, text: 'מוצגים ' + SEARCH_LIMIT + ' מתוך ' + hits.length + '. צמצם את החיפוש.' });
            }
            return rows;
        }

        const keep = (c) => !this.only25 || c.tag;
        const full = all.filter((c) => c.tier === 'FULL' && keep(c));
        const near = all.filter((c) => c.tier === 'NEAR' && keep(c));
        const hidden = all.filter((c) => c.tier === 'FAR' || c.tier === 'BLOCKED').length;

        if (full.length) {
            rows.push({ key: 'h-full', isSection: true, title: 'מומלצים', count: full.length, toggle: false });
            (this.showAllFull ? full : full.slice(0, TOP)).forEach((c) => rows.push(c));
            if (full.length > TOP) {
                rows.push({
                    key: 'more',
                    isMore: true,
                    label: this.showAllFull ? 'הצג פחות' : 'הצג עוד (' + (full.length - TOP) + ')'
                });
            }
        }
        if (near.length) {
            const open = this.nearToggled ? this.nearOpen : !full.length;
            rows.push({
                key: 'h-near',
                isSection: true,
                title: 'כמעט מתאימים',
                sub: 'חסרה הסמכה או שפה',
                count: near.length,
                toggle: true,
                expanded: String(open),
                arrow: open ? '▾' : '◂'
            });
            if (open) {
                near.forEach((c) => rows.push(c));
            }
        }
        if (hidden) {
            rows.push({
                key: 'hidden',
                isNote: true,
                text: hidden + ' מדריכים נוספים אינם מוצגים (לא זמינים, תפוסים או חסרות להם כמה דרישות). חפש בשם כדי למצוא מדריך מסוים.'
            });
        }
        if (!rows.length) {
            rows.push({ key: 'none', isNote: true, text: 'אין מדריכים מתאימים. אפשר לחפש בשם או לדלג.' });
        }
        return rows;
    }

    get selectedSummary() {
        if (this.selected === SKIP) {
            return this.decision ? 'הקבוצה לא תשובץ כעת' : 'טרם נבחר מדריך. ללא בחירה הקבוצה תישאר ממתינה.';
        }
        const c = this.cardsAll.find((x) => x.value === this.selected);
        const name = c ? c.name : this.decision && this.decision.name;
        return name ? (this.decision ? 'נבחר: ' + name : 'מומלץ: ' + name + ' (לחץ אשר כדי לשבץ)') : '';
    }

    get skipChecked() {
        return String(this.selected === SKIP);
    }

    get skipCls() {
        return 'card skip' + (this.selected === SKIP ? ' selected' : '');
    }

    get only25Pressed() {
        return String(this.only25);
    }

    get filterNote() {
        return this.only25 && !this.searching ? 'מוצגים רק מדריכי 25. בטל את הסינון כדי לראות את כל המדריכים.' : '';
    }

    get prevDisabled() {
        return this.idx <= 0;
    }

    get nextLabel() {
        return this.isLast ? 'אשר וסיים' : 'אשר והמשך';
    }

    get showSpinner() {
        return this.loading;
    }

    get showCards() {
        return !this.loading && !this.error;
    }

    get hasError() {
        return !!this.error;
    }

    // ---------- events ----------

    handleSelect(event) {
        if (event.currentTarget.disabled) {
            return;
        }
        const sid = event.currentTarget.dataset.value;
        const name = event.currentTarget.dataset.name;
        this.commit(sid, name);
    }

    commit(sid, name) {
        const row = this.currentRow;
        if (!row) {
            return;
        }
        this.dec = { ...this.dec, [row.reservationId]: sid === SKIP ? { sid: SKIP } : { sid, name } };
        this.publish();
    }

    handleNext() {
        if (!this.decision) {
            // "Approve and continue" confirms what is pre-selected
            const best = this.bestFull;
            if (this.selected === SKIP) {
                this.commit(SKIP);
            } else {
                this.commit(this.selected, best ? best.staffName : '');
            }
        }
        if (this.isLast) {
            this.finish();
        } else {
            this.idx += 1;
            this.load();
        }
    }

    handlePrev() {
        if (this.idx > 0) {
            this.idx -= 1;
            this.load();
        }
    }

    handleJump(event) {
        const i = Number(event.currentTarget.dataset.index);
        if (i !== this.idx) {
            this.idx = i;
            this.load();
        }
    }

    handleSummary() {
        this.finish();
    }

    handleSearch(event) {
        this.query = event.target.value || '';
    }

    handleClear() {
        this.query = '';
        const input = this.template.querySelector('.search input');
        if (input) {
            input.value = '';
            input.focus();
        }
    }

    handleMore() {
        this.showAllFull = !this.showAllFull;
    }

    handleNearToggle() {
        const wasOpen = this.nearToggled ? this.nearOpen : !this.hasFullMatch;
        this.nearToggled = true;
        this.nearOpen = !wasOpen;
    }

    handleOnly25(event) {
        this.only25 = event.target.checked;
    }

    // ---------- output to the flow ----------

    publish() {
        const pairs = [];
        const skips = [];
        (this.rows || []).forEach((r) => {
            const d = this.dec[r.reservationId];
            if (!d) {
                return;
            }
            if (d.sid === SKIP) {
                skips.push(r.reservationId);
            } else {
                pairs.push(r.reservationId + '|' + d.sid + '|' + (d.name || ''));
            }
        });
        this.dispatchEvent(new FlowAttributeChangeEvent('pairs', pairs));
        this.dispatchEvent(new FlowAttributeChangeEvent('skips', skips));
        this.dispatchEvent(new FlowAttributeChangeEvent('pairCount', pairs.length));
        this.dispatchEvent(new FlowAttributeChangeEvent('skipCount', this.total - pairs.length));
    }

    finish() {
        this.publish();
        if (this.availableActions.includes('NEXT')) {
            this.dispatchEvent(new FlowNavigationNextEvent());
        } else if (this.availableActions.includes('FINISH')) {
            this.dispatchEvent(new FlowNavigationFinishEvent());
        }
    }
}