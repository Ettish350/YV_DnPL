import { LightningElement, api } from 'lwc';
import { FlowNavigationFinishEvent } from 'lightning/flowSupport';

export default class GsAssignDone extends LightningElement {
    @api results = [];
    @api orderName = '';
    @api availableActions = [];

    get rows() {
        return (this.results || []).map((r) => {
            const saved = r.status === 'SAVED';
            const failed = r.status === 'FAILED';
            const name = r.staffName || '';
            return {
                key: r.reservationId,
                group: r.groupLabel,
                reservationName: r.reservationName,
                staff: name,
                initial: name.charAt(0),
                hasStaff: !!name,
                url: '/lightning/r/B25__Reservation__c/' + r.reservationId + '/view',
                saved,
                failed,
                waiting: r.status === 'WAITING',
                message: r.message,
                cls: 'item ' + (saved ? 'saved' : failed ? 'failed' : 'waiting'),
                badge: saved ? '✓ שובץ' : failed ? 'לא נשמר' : 'ממתינה לשיבוץ',
                badgeCls: 'badge ' + (saved ? 'ok' : failed ? 'bad' : 'muted')
            };
        });
    }

    get savedCount() {
        return (this.results || []).filter((r) => r.status === 'SAVED').length;
    }

    get failedCount() {
        return (this.results || []).filter((r) => r.status === 'FAILED').length;
    }

    get waitingCount() {
        return (this.results || []).filter((r) => r.status === 'WAITING').length;
    }

    get hasFailed() {
        return this.failedCount > 0;
    }

    get hasWaiting() {
        return this.waitingCount > 0;
    }

    get hasRows() {
        return (this.results || []).length > 0;
    }

    get heroCls() {
        return 'hero ' + (this.hasFailed ? 'warn' : 'ok');
    }

    get heroIcon() {
        return this.hasFailed ? '!' : '✓';
    }

    get headline() {
        if (this.hasFailed) {
            return 'חלק מהשיבוצים לא נשמרו';
        }
        return this.savedCount ? 'השיבוצים נשמרו' : 'לא נשמרו שיבוצים';
    }

    get subline() {
        const parts = [];
        parts.push('שובצו ' + this.savedCount + ' קבוצות');
        if (this.hasFailed) {
            parts.push(this.failedCount + ' לא נשמרו');
        }
        if (this.hasWaiting) {
            parts.push(this.waitingCount + ' ממתינות לשיבוץ');
        }
        return parts.join(' · ');
    }

    handleFinish() {
        if (this.availableActions.includes('FINISH')) {
            this.dispatchEvent(new FlowNavigationFinishEvent());
        }
    }
}