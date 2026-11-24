import { LightningElement, wire, api } from 'lwc';
import LANG from '@salesforce/i18n/lang';
import getAudienceCategories from '@salesforce/apex/yvEduNewReservationController.getAudienceCategories';
import loadError from '@salesforce/label/c.yvEdu_Res_LoadError';
import loading from '@salesforce/label/c.yvEdu_Res_Loading';

// TODO-LABEL: move to Custom Labels (c.yvEdu_Res_*) once the new labels are created
const TITLE = 'Who is the activity for?';

const RTL_LANGUAGE_PATTERN = /^(iw|he)/i;

/**
 * Step "audience": the customer picks the Audience Category (the picklist shared by
 * Opportunity, Bundle and Bundle Item). Fires `audienceselected` ({ audience }).
 */
export default class YvEduNewReservationAudience extends LightningElement {
    title = TITLE;
    loadingText = loading;
    loadErrorText = loadError;
    audiences = [];
    isLoading = true;
    hasError = false;

    /** Chosen Audience Category value, owned by the parent. */
    @api audience;

    @wire(getAudienceCategories)
    wiredAudiences({ data, error }) {
        if (data) {
            this.audiences = data;
            this.hasError = false;
            this.isLoading = false;
        } else if (error) {
            this.hasError = true;
            this.isLoading = false;
        }
    }

    get langDir() {
        return RTL_LANGUAGE_PATTERN.test(LANG || '') ? 'rtl' : 'ltr';
    }

    get options() {
        return this.audiences.map((option) => {
            const isActive = option.value === this.audience;
            return {
                value: option.value,
                label: option.label,
                ariaPressed: String(isActive),
                className: isActive ? 'yvedu-res-filter yvedu-res-filter_active' : 'yvedu-res-filter'
            };
        });
    }

    handleClick(event) {
        this.dispatchEvent(new CustomEvent('audienceselected', { detail: { audience: event.currentTarget.dataset.value } }));
    }
}