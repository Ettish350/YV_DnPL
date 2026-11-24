import { LightningElement, api, wire } from 'lwc';
import { getRecord, notifyRecordUpdateAvailable } from 'lightning/uiRecordApi';
import { ShowToastEvent } from 'lightning/platformShowToastEvent';
import IS_FREE from '@salesforce/schema/Product2.Is_Free__c';
import TITLE from '@salesforce/label/c.YvPricingTitle';
import STANDARD_BOOK from '@salesforce/label/c.YvPricingStandardBook';
import PRICE from '@salesforce/label/c.YvPricingPrice';
import FREE_PRODUCT from '@salesforce/label/c.YvPricingFreeProduct';
import NO_BOOKS from '@salesforce/label/c.YvPricingNoBooks';
import SAVE from '@salesforce/label/c.YvPricingSave';
import SAVED from '@salesforce/label/c.YvPricingSaved';
import LOADING from '@salesforce/label/c.YvPricingLoading';
import UNEXPECTED from '@salesforce/label/c.YvPricingUnexpected';
import getPrices from '@salesforce/apex/YvProductPricingController.getPrices';
import savePrices from '@salesforce/apex/YvProductPricingController.savePrices';

// PR1 | V01-03 | The product price in each price book (standard price book included), on the Product record page.
export default class YvProductPrices extends LightningElement {
    @api recordId;
    isLoading = true;
    isSaving = false;
    error;
    isFree = false;
    prices = [];
    loadedFree;
    label = { freeProduct: FREE_PRODUCT, title: TITLE, price: PRICE, noBooks: NO_BOOKS, save: SAVE, loading: LOADING };

    // Reloads when "Free product" changes on the record, so the rows lock or unlock.
    @wire(getRecord, { recordId: '$recordId', fields: [IS_FREE] })
    wiredRecord({ data }) {
        if (data) {
            const free = data.fields.Is_Free__c.value === true;
            if (this.loadedFree !== free) {
                this.loadedFree = free;
                this.load();
            }
        }
    }

    get saveDisabled() {
        return this.isSaving || this.isLoading || this.prices.length === 0;
    }
    get hasPrices() {
        return this.prices.length > 0;
    }

    async load() {
        this.isLoading = true;
        this.error = undefined;
        try {
            const d = await getPrices({ productId: this.recordId });
            this.isFree = d.isFree;
            this.prices = d.prices.map((p) => ({
                ...p,
                key: p.pricebookId,
                label: p.isStandard ? STANDARD_BOOK : p.pricebookName,
                meta: [p.pricelistType, p.validFrom && p.validTo ? `${p.validFrom} - ${p.validTo}` : '']
                    .filter(Boolean)
                    .join(' | '),
                price: d.isFree ? 0 : p.price
            }));
        } catch (e) {
            this.error = this.message(e);
        } finally {
            this.isLoading = false;
        }
    }

    handleFree(event) {
        this.isFree = event.target.checked;
        if (this.isFree) {
            this.prices = this.prices.map((p) => ({ ...p, price: 0 }));
        }
    }

    handlePrice(event) {
        const id = event.target.dataset.id;
        const raw = event.detail.value;
        const value = raw === '' || raw === null ? null : Number(raw);
        this.prices = this.prices.map((p) => (p.pricebookId === id ? { ...p, price: value } : p));
    }

    async handleSave() {
        const inputs = [...this.template.querySelectorAll('lightning-input')];
        if (!inputs.every((i) => i.reportValidity())) {
            return;
        }
        this.isSaving = true;
        this.error = undefined;
        try {
            await savePrices({
                productId: this.recordId,
                makeFree: this.isFree,
                pricesJson: JSON.stringify(this.prices.map((p) => ({ pricebookId: p.pricebookId, price: p.price })))
            });
            this.dispatchEvent(new ShowToastEvent({ title: SAVED, variant: 'success' }));
            await notifyRecordUpdateAvailable([{ recordId: this.recordId }]);
            await this.load();
        } catch (e) {
            this.error = this.message(e);
        } finally {
            this.isSaving = false;
        }
    }

    message(e) {
        return (e && e.body && e.body.message) || (e && e.message) || UNEXPECTED;
    }
}