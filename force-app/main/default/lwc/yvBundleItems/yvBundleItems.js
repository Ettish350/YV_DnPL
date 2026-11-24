import { LightningElement, api } from 'lwc';
import { ShowToastEvent } from 'lightning/platformShowToastEvent';
import TITLE from '@salesforce/label/c.YvBundleTitle';
import QUANTITY from '@salesforce/label/c.YvBundleQuantity';
import PRODUCT from '@salesforce/label/c.YvBundleProduct';
import REMOVE from '@salesforce/label/c.YvBundleRemove';
import TOTAL from '@salesforce/label/c.YvBundleTotal';
import SAVE from '@salesforce/label/c.YvBundleSave';
import SAVED from '@salesforce/label/c.YvBundleSaved';
import NO_ITEMS from '@salesforce/label/c.YvBundleNoItems';
import HELP from '@salesforce/label/c.YvBundleHelp';
import NO_BOOKS from '@salesforce/label/c.YvPricingNoBooks';
import PRICE from '@salesforce/label/c.YvPricingPrice';
import LOADING from '@salesforce/label/c.YvPricingLoading';
import UNEXPECTED from '@salesforce/label/c.YvPricingUnexpected';
import getBundleData from '@salesforce/apex/YvBundlePricingController.getBundleData';
import saveBundle from '@salesforce/apex/YvBundlePricingController.saveBundle';

// PR1 | A01-02 pricing rule | Bundle items and the price of each in every price book (Bundle Item Entry).
export default class YvBundleItems extends LightningElement {
    @api recordId;
    label = {
        title: TITLE, productHeader: PRODUCT, quantityHeader: QUANTITY, remove: REMOVE,
        total: TOTAL, save: SAVE, noItems: NO_ITEMS, help: HELP, noBooks: NO_BOOKS, price: PRICE, loading: LOADING
    };
    isLoading = true;
    isSaving = false;
    error;
    books = [];
    rows = [];

    connectedCallback() {
        this.load();
    }

    get visibleRows() {
        return this.rows.filter((r) => !r.removed);
    }
    get hasRows() {
        return this.visibleRows.length > 0;
    }
    get hasBooks() {
        return this.books.length > 0;
    }
    get saveDisabled() {
        return this.isSaving || this.isLoading;
    }
    get totals() {
        return this.books.map((b, i) => {
            let sum = 0;
            for (const r of this.visibleRows) {
                const price = r.cells[i].price;
                if (price !== null && price !== undefined) {
                    sum += price * (r.quantity || 0);
                }
            }
            return { key: b.pricebookId, name: b.name, value: sum.toFixed(2) };
        });
    }

    async load() {
        this.isLoading = true;
        this.error = undefined;
        try {
            const d = await getBundleData({ bundleId: this.recordId });
            this.books = d.books.map((b) => ({
                ...b,
                key: b.pricebookId,
                meta: [b.pricelistType, b.validFrom && b.validTo ? `${b.validFrom} - ${b.validTo}` : '']
                    .filter(Boolean)
                    .join(' | ')
            }));
            this.rows = d.items.map((i) => ({
                key: i.itemId,
                itemId: i.itemId,
                productId: i.productId,
                productName: i.productName,
                quantity: i.quantity,
                defaultOrder: i.defaultOrder,
                removed: false,
                cells: i.cells.map((c) => this.makeCell(c.pricebookId, c.price === null || c.price === undefined ? c.listPrice : c.price, c.listPrice))
            }));
        } catch (e) {
            this.error = this.message(e);
        } finally {
            this.isLoading = false;
        }
    }

    makeCell(pricebookId, price, listPrice) {
        const book = this.books.find((b) => b.pricebookId === pricebookId);
        return {
            key: pricebookId,
            pricebookId,
            label: book ? book.name : '',
            price: price === undefined ? null : price,
            listPrice
        };
    }

    handlePrice(event) {
        const { row, book } = event.target.dataset;
        const raw = event.detail.value;
        const value = raw === '' || raw === null ? null : Number(raw);
        this.rows = this.rows.map((r) =>
            r.key === row ? { ...r, cells: r.cells.map((c) => (c.pricebookId === book ? { ...c, price: value } : c)) } : r
        );
    }

    handleRemove(event) {
        const key = event.target.dataset.row;
        // A row that was never saved just disappears. A saved one is marked inactive on save.
        this.rows = this.rows
            .map((r) => (r.key === key ? { ...r, removed: true } : r))
            .filter((r) => !(r.removed && !r.itemId));
    }

    async handleSave() {
        const inputs = [...this.template.querySelectorAll('lightning-input')];
        if (!inputs.every((i) => i.reportValidity())) {
            return;
        }
        this.isSaving = true;
        this.error = undefined;
        try {
            await saveBundle({
                bundleId: this.recordId,
                itemsJson: JSON.stringify(
                    this.rows.map((r) => ({
                        itemId: r.itemId,
                        productId: r.productId,
                        quantity: r.quantity,
                        defaultOrder: r.defaultOrder,
                        active: !r.removed,
                        prices: r.cells.map((c) => ({ pricebookId: c.pricebookId, price: c.price }))
                    }))
                )
            });
            this.dispatchEvent(new ShowToastEvent({ title: SAVED, variant: 'success' }));
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