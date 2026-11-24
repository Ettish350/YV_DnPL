import { LightningElement, api } from 'lwc';
import { FlowAttributeChangeEvent, FlowNavigationNextEvent, FlowNavigationFinishEvent } from 'lightning/flowSupport';

const SKIP = 'SKIP';
const TOP = 5; // full matches shown before "show more"
const SEARCH_LIMIT = 30; // rows shown for a search

export default class GsGuidePicker extends LightningElement {
    @api choices = [];
    @api headerText = '';
    @api current = 1;
    @api total = 1;
    @api suggestedValue = SKIP;
    @api availableActions = [];

    _selected;
    query = '';
    showAllFull = false;
    nearToggled = false; // set once the user opens or closes the near-match section
    nearOpen = false;
    only25 = false;

    // Output to the flow
    @api
    get selectedValue() {
        return this.selected;
    }
    set selectedValue(value) {
        this._selected = value;
    }

    connectedCallback() {
        // Start from the pre-selected guide (a full match), or from "later" when there is none
        this._selected = this.suggestedValue || SKIP;
        this.publishSelection();
    }

    // ---------- data ----------

    get selected() {
        return this._selected || SKIP;
    }

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
        return (this.choices || []).some((c) => c.tier === 'FULL');
    }

    get noFullMatch() {
        return !this.hasFullMatch;
    }

    get searching() {
        return this.query.trim().length > 0;
    }

    get has25() {
        return (this.choices || []).some((c) => c.tag);
    }

    // The rows of the list: section headers, cards and buttons, in order
    get rows() {
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
                open,
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
            return 'הקבוצה תישאר ממתינה לשיבוץ';
        }
        const c = this.cardsAll.find((x) => x.value === this.selected);
        return c ? 'נבחר: ' + c.name : '';
    }

    get skipChecked() {
        return String(this.selected === SKIP);
    }

    get skipCls() {
        return 'card skip' + (this.selected === SKIP ? ' selected' : '');
    }

    get only25Cls() {
        return 'toggle' + (this.only25 ? ' on' : '');
    }

    get only25Pressed() {
        return String(this.only25);
    }

    get hint() {
        return this.hasFullMatch ? 'המדריך המומלץ מסומן מראש' : 'אין התאמה מלאה, נדרשת בחירה ידנית';
    }

    get progress() {
        const total = Number(this.total) || 1;
        const current = Number(this.current) || 1;
        return Array.from({ length: total }, (_, i) => ({
            key: 'p' + i,
            cls: 'step ' + (i + 1 < current ? 'done' : i + 1 === current ? 'now' : '')
        }));
    }

    get counter() {
        return 'קבוצה ' + this.current + ' מתוך ' + this.total + ' לבדיקה';
    }

    get nextLabel() {
        return Number(this.current) < Number(this.total) ? 'אשר והמשך' : 'אשר וסיים';
    }

    // ---------- events ----------

    handleSelect(event) {
        if (event.currentTarget.disabled) {
            return;
        }
        this._selected = event.currentTarget.dataset.value;
        this.publishSelection();
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

    handleOnly25() {
        this.only25 = !this.only25;
    }

    publishSelection() {
        this.dispatchEvent(new FlowAttributeChangeEvent('selectedValue', this.selected));
    }

    handleNext() {
        this.publishSelection();
        if (this.availableActions.includes('NEXT')) {
            this.dispatchEvent(new FlowNavigationNextEvent());
        } else if (this.availableActions.includes('FINISH')) {
            this.dispatchEvent(new FlowNavigationFinishEvent());
        }
    }
}