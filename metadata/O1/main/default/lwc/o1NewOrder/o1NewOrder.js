// YV20-226 | A03-01 | Opens the standard new-order form with the defaults: Stage New, Hebrew, 1 group, 1 participant.
import { LightningElement, wire } from 'lwc';
import { NavigationMixin } from 'lightning/navigation';
import { getObjectInfo } from 'lightning/uiObjectInfoApi';
import { encodeDefaultFieldValues } from 'lightning/pageReferenceUtils';
import OPPORTUNITY from '@salesforce/schema/Opportunity';
import HAS_ORDER_DEFAULTS from '@salesforce/customPermission/O1_Order_Defaults';

const ORDER_RECORD_TYPE_NAME = 'Order';
const DEFAULTS = {
    StageName: 'New',
    InstructionLanguage__c: 'Hebrew',
    NumberOfGroups__c: 1,
    NumberOfParticipants__c: 1
};

export default class O1NewOrder extends NavigationMixin(LightningElement) {
    opened = false;

    @wire(getObjectInfo, { objectApiName: OPPORTUNITY })
    objectInfo({ data }) {
        if (!data || this.opened) {
            return;
        }
        this.opened = true;
        const orderType = Object.values(data.recordTypeInfos).find((r) => r.name === ORDER_RECORD_TYPE_NAME);
        // Only a user with the custom permission gets the order defaults; everyone else gets the standard New.
        const state = { nooverride: '1' };
        if (HAS_ORDER_DEFAULTS) {
            state.defaultFieldValues = encodeDefaultFieldValues(DEFAULTS);
            if (orderType) {
                state.recordTypeId = orderType.recordTypeId;
            }
        }
        this[NavigationMixin.Navigate]({
            type: 'standard__objectPage',
            attributes: { objectApiName: 'Opportunity', actionName: 'new' },
            state
        });
    }
}
