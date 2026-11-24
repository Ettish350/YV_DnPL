import { LightningElement, api } from 'lwc';
import { FlowAttributeChangeEvent, FlowNavigationNextEvent } from 'lightning/flowSupport';

export default class GsAssignOverview extends LightningElement {
    @api orderName = '';
    @api totalCount = 0;
    @api fullCount = 0;
    @api availableActions = [];

    _mode;

    // Output to the flow: AUTO (approve full matches, review the rest) or ONE (review every group)
    @api
    get mode() {
        return this._mode;
    }
    set mode(value) {
        this._mode = value;
    }

    get needCount() {
        return Math.max(Number(this.totalCount) - Number(this.fullCount), 0);
    }

    get hasFull() {
        return Number(this.fullCount) > 0;
    }

    get autoSub() {
        return this.fullCount + ' קבוצות ישובצו מיד. נשארות ' + this.needCount + ' לבדיקה.';
    }

    get autoCls() {
        return this.hasFull ? 'big primary' : 'big';
    }

    get oneCls() {
        return this.hasFull ? 'big' : 'big primary';
    }

    handleAuto() {
        this.choose('AUTO');
    }

    handleOne() {
        this.choose('ONE');
    }

    choose(value) {
        this._mode = value;
        this.dispatchEvent(new FlowAttributeChangeEvent('mode', value));
        if (this.availableActions.includes('NEXT')) {
            this.dispatchEvent(new FlowNavigationNextEvent());
        }
    }
}