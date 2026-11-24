import { LightningElement } from 'lwc';
import getStats from '@salesforce/apex/A13_AvailabilityMonthBuilder.getStats';
import build from '@salesforce/apex/A13_AvailabilityMonthBuilder.build';

const MONTHS = ['ינואר', 'פברואר', 'מרץ', 'אפריל', 'מאי', 'יוני', 'יולי', 'אוגוסט', 'ספטמבר', 'אוקטובר', 'נובמבר', 'דצמבר'];

export default class A13AvailabilityMonthBuilder extends LightningElement {
    year;
    month;
    stats;
    result;
    error;
    loading = false;

    connectedCallback() {
        const next = new Date();
        next.setDate(1);
        next.setMonth(next.getMonth() + 1);
        this.year = String(next.getFullYear());
        this.month = String(next.getMonth() + 1);
        this.loadStats();
    }

    get yearOptions() {
        const now = new Date().getFullYear();
        return [now, now + 1].map((y) => ({ label: String(y), value: String(y) }));
    }

    get monthOptions() {
        return MONTHS.map((name, i) => ({ label: `${i + 1} - ${name}`, value: String(i + 1) }));
    }

    get runDisabled() {
        return this.loading || !this.stats || this.stats.missing === 0;
    }

    get resultClass() {
        return this.result && this.result.success ? 'slds-text-color_success' : 'slds-text-color_error';
    }

    handleYear(event) {
        this.year = event.detail.value;
        this.result = undefined;
        this.loadStats();
    }

    handleMonth(event) {
        this.month = event.detail.value;
        this.result = undefined;
        this.loadStats();
    }

    async loadStats() {
        this.loading = true;
        this.error = undefined;
        try {
            this.stats = await getStats({ year: Number(this.year), month: Number(this.month) });
        } catch (e) {
            this.stats = undefined;
            this.error = this.message(e);
        } finally {
            this.loading = false;
        }
    }

    async handleRun() {
        this.loading = true;
        this.error = undefined;
        try {
            this.result = await build({ year: Number(this.year), month: Number(this.month) });
        } catch (e) {
            this.error = this.message(e);
        } finally {
            this.loading = false;
        }
        await this.loadStats();
    }

    message(e) {
        return (e && e.body && e.body.message) || (e && e.message) || 'שגיאה';
    }
}
