import { LightningElement, wire, api } from 'lwc';
import LANG from '@salesforce/i18n/lang';
import getAudienceCategories from '@salesforce/apex/yvEduNewReservationController.getAudienceCategories';
import getActivities from '@salesforce/apex/yvEduNewReservationController.getActivities';
import getActivityDaySubjects from '@salesforce/apex/yvEduNewReservationController.getActivityDaySubjects';
import subjectTitle from '@salesforce/label/c.yvEdu_Res_SubjectTitle';
import subjectPlaceholder from '@salesforce/label/c.yvEdu_Res_SubjectPlaceholder';
import subjectEmpty from '@salesforce/label/c.yvEdu_Res_SubjectEmpty';
import title from '@salesforce/label/c.yvEdu_Res_Title';
import filterAll from '@salesforce/label/c.yvEdu_Res_FilterAll';
import filterUpTo3 from '@salesforce/label/c.yvEdu_Res_FilterUpTo3';
import filterUpTo6 from '@salesforce/label/c.yvEdu_Res_FilterUpTo6';
import hours from '@salesforce/label/c.yvEdu_Res_Hours';
import empty from '@salesforce/label/c.yvEdu_Res_Empty';
import loadError from '@salesforce/label/c.yvEdu_Res_LoadError';
import loading from '@salesforce/label/c.yvEdu_Res_Loading';
import audienceTitle from '@salesforce/label/c.yvEdu_Res_AudienceTitle';
import selectBundle from '@salesforce/label/c.yvEdu_Res_SelectBundle';
import unselectBundle from '@salesforce/label/c.yvEdu_Res_Remove';
import itemsTitle from '@salesforce/label/c.yvEdu_Res_ItemsTitle';
import needItems from '@salesforce/label/c.yvEdu_Res_NeedItems';
import next from '@salesforce/label/c.yvEdu_Res_Next';
import selectedCount from '@salesforce/label/c.yvEdu_Res_SelectedCount';

const TEXT = { subjectTitle, subjectPlaceholder, subjectEmpty, audienceTitle, selectBundle, unselectBundle, itemsTitle, needItems, next, summary: selectedCount };

const LABELS = { title, empty, loadError, loading, ...TEXT };

const FILTERS = [
    { key: 'all', label: filterAll, maxHours: null },
    { key: 'upTo3', label: filterUpTo3, maxHours: 3 },
    { key: 'upTo6', label: filterUpTo6, maxHours: 6 }
];

const RTL_LANGUAGE_PATTERN = /^(iw|he)/i;
// Salesforce reports Hebrew as "iw"; Intl expects "he".
const INTL_LOCALE = (LANG || 'en-US').replace(/^iw/i, 'he');

/**
 * Step "activity": pick an Audience Category first (only the ones set on the customer's account; when there is just one it
 * is chosen automatically and the question is skipped); only then are the bundles of that audience shown. Several
 * different bundles can be selected (each only once) and, in the same card, the items of each selected bundle are
 * ticked. The parent owns the selection ([{ bundle, itemIds }]); this component fires `audiencechange`
 * ({ audience }), `selectionchange` ({ selection }) and `next`.
 */
export default class YvEduNewReservationActivity extends LightningElement {
    labels = LABELS;
    activeFilter = 'all';
    activities = [];
    audiences = [];
    audiencesLoaded = false;
    isLoading = false;
    hasError = false;

    /** Chosen Audience Category value, owned by the parent. */
    @api audience;

    /** [{ bundle: card, itemIds: [Id] }], owned by the parent. */
    @api selection = [];

    /** Id of the chosen Activity Day Subject (optional), owned by the parent. */
    @api subject;

    subjects = [];
    subjectsLoaded = false;
    subjectsFailed = false;

    @wire(getActivityDaySubjects)
    wiredSubjects({ data, error }) {
        if (data) {
            this.subjects = data;
            this.subjectsLoaded = true;
            this.subjectsFailed = false;
        } else if (error) {
            // The subject is optional: the step still works without the list
            this.logError('getActivityDaySubjects', error);
            this.subjects = [];
            this.subjectsLoaded = true;
            this.subjectsFailed = true;
        }
    }

    @wire(getAudienceCategories)
    wiredAudiences({ data, error }) {
        if (data) {
            this.audiences = data;
            this.audiencesLoaded = true;
            if (data.length === 1 && this.audience !== data[0].value) {
                // A single audience needs no question: choose it for the customer
                this.isLoading = true;
                this.dispatchEvent(new CustomEvent('audiencechange', { detail: { audience: data[0].value } }));
            }
        } else if (error) {
            this.logError('getAudienceCategories', error);
            this.hasError = true;
        }
    }

    @wire(getActivities, { audienceCategory: '$audience' })
    wiredActivities({ data, error }) {
        if (data) {
            this.activities = data;
            this.hasError = false;
            this.isLoading = false;
        } else if (error) {
            this.logError('getActivities', error);
            this.activities = [];
            this.hasError = true;
            this.isLoading = false;
        }
    }

    logError(source, error) {
        // eslint-disable-next-line no-console
        console.error(`yvEduNewReservationActivity ${source}:`, error?.body?.message || error);
    }

    get langDir() {
        return RTL_LANGUAGE_PATTERN.test(LANG || '') ? 'rtl' : 'ltr';
    }

    get audienceOptions() {
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

    /** The question is asked only when the customer has more than one audience to choose from. */
    get showAudienceChoice() {
        return this.audiences.length > 1;
    }

    get subjectOptions() {
        return this.subjects.map((option) => ({ value: option.value, label: option.label, selected: option.value === this.subject }));
    }

    get showSubject() {
        return this.showCards && this.subjects.length > 0;
    }

    /** Shown instead of the list when it is empty or failed to load, so a missing permission is visible and not silent. */
    get showSubjectEmpty() {
        return this.showCards && this.subjectsLoaded && this.subjects.length === 0;
    }

    get subjectEmptyText() {
        return this.subjectsFailed ? LABELS.loadError : TEXT.subjectEmpty;
    }

    handleSubjectChange(event) {
        this.dispatchEvent(new CustomEvent('subjectchange', { detail: { subject: event.target.value || null } }));
    }

    get showCards() {
        return Boolean(this.audience);
    }

    get filterOptions() {
        return FILTERS.map((filter) => {
            const isActive = filter.key === this.activeFilter;
            return {
                key: filter.key,
                label: filter.label,
                ariaPressed: String(isActive),
                className: isActive ? 'yvedu-res-filter yvedu-res-filter_active' : 'yvedu-res-filter'
            };
        });
    }

    get cards() {
        const hoursFormat = new Intl.NumberFormat(INTL_LOCALE, { maximumFractionDigits: 2 });
        const hoursText = (value) => (value != null ? hours.replace('{0}', hoursFormat.format(value)) : '');

        return this.activities.map((activity) => {
            const chosen = this.selection.find((entry) => entry.bundle.id === activity.id);
            const chosenIds = new Set(chosen ? chosen.itemIds : []);
            const items = activity.items.map((item) => ({
                id: item.id,
                name: item.name,
                hasHours: item.hours != null,
                hoursLabel: hoursText(item.hours),
                checked: chosenIds.has(item.id),
                rowClass: chosenIds.has(item.id) ? 'yvedu-res-item yvedu-res-item_checked' : 'yvedu-res-item'
            }));
            return {
                id: activity.id,
                name: activity.name,
                totalHours: activity.totalHours,
                hasHours: activity.totalHours != null,
                hoursLabel: hoursText(activity.totalHours),
                items,
                hasItems: items.length > 0,
                includedItems: items.map((item) => ({ key: item.id, text: item.name })),
                isSelected: Boolean(chosen),
                needsItems: Boolean(chosen) && chosen.itemIds.length === 0,
                buttonLabel: chosen ? TEXT.unselectBundle : TEXT.selectBundle,
                buttonClass: chosen ? 'yvedu-res-secondary' : 'yvedu-res-order',
                cardClass: chosen ? 'yvedu-res-card yvedu-res-card_selected' : 'yvedu-res-card'
            };
        });
    }

    get visibleCards() {
        const maxHours = FILTERS.find((filter) => filter.key === this.activeFilter).maxHours;
        return this.cards.filter(
            (card) => maxHours === null || (card.totalHours != null && card.totalHours <= maxHours)
        );
    }

    /** A spinner until the audiences are known, and while the bundles load when no card list is on the screen yet. */
    get showSpinner() {
        return (!this.audiencesLoaded && !this.hasError) || (this.isLoading && !this.showCards);
    }

    get isEmpty() {
        return this.showCards && !this.isLoading && !this.hasError && this.visibleCards.length === 0;
    }

    get summaryText() {
        return TEXT.summary.replace('{0}', this.selection.length);
    }

    get hasSelection() {
        return this.selection.length > 0;
    }

    get isComplete() {
        return this.selection.length > 0 && this.selection.every((entry) => entry.itemIds.length > 0);
    }

    get nextDisabled() {
        return !this.isComplete;
    }

    emitSelection(selection) {
        this.dispatchEvent(new CustomEvent('selectionchange', { detail: { selection } }));
    }

    handleAudienceClick(event) {
        const audience = event.currentTarget.dataset.value;
        if (audience !== this.audience) {
            this.isLoading = true;
            this.hasError = false;
            this.dispatchEvent(new CustomEvent('audiencechange', { detail: { audience } }));
        }
    }

    handleFilterClick(event) {
        this.activeFilter = event.currentTarget.dataset.filter;
    }

    handleToggleBundle(event) {
        const id = event.currentTarget.dataset.id;
        if (this.selection.some((entry) => entry.bundle.id === id)) {
            this.emitSelection(this.selection.filter((entry) => entry.bundle.id !== id));
            return;
        }
        const card = this.cards.find((c) => c.id === id);
        this.emitSelection([...this.selection, { bundle: card, itemIds: [] }]);
    }

    handleToggleItem(event) {
        const { bundle, id } = event.currentTarget.dataset;
        const checked = event.currentTarget.checked;
        this.emitSelection(
            this.selection.map((entry) =>
                entry.bundle.id === bundle
                    ? { ...entry, itemIds: checked ? [...entry.itemIds, id] : entry.itemIds.filter((itemId) => itemId !== id) }
                    : entry
            )
        );
    }

    handleNext() {
        if (this.isComplete) {
            this.dispatchEvent(new CustomEvent('next'));
        }
    }
}