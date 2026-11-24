import { LightningElement, api, wire } from 'lwc';
import { getRecord, getFieldValue } from 'lightning/uiRecordApi';
import ACCOUNT_PERSON_CONTACT_ID from '@salesforce/schema/Account.PersonContactId';

const FIELDS = [ACCOUNT_PERSON_CONTACT_ID];

export default class ActiveTrailPersonAccountWrapper extends LightningElement {
    @api recordId;

    personContactId;
    hasError = false;

    @wire(getRecord, { recordId: '$recordId', fields: FIELDS })
    wiredAccount({ data, error }) {
        if (data) {
            this.personContactId = getFieldValue(data, ACCOUNT_PERSON_CONTACT_ID);
            this.hasError = false;
        } else if (error) {
            this.personContactId = undefined;
            this.hasError = true;
            // eslint-disable-next-line no-console
            console.error('ActiveTrailPersonAccountWrapper: failed to retrieve PersonContactId', error);
        }
    }

    get hasContactId() {
        return !!this.personContactId;
    }

    renderedCallback() {
        if (this.hasContactId && !this.resizeDispatched) {
            this.resizeDispatched = true;
            // eslint-disable-next-line @lwc/lwc/no-async-operation
            requestAnimationFrame(() => {
                window.dispatchEvent(new Event('resize'));
            });
        }
    }
}