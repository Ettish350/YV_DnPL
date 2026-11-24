import { LightningElement, api, wire } from 'lwc';
import getInstructionLanguages from '@salesforce/apex/yvEduNewReservationController.getInstructionLanguages';
import instructionLanguageTitle from '@salesforce/label/c.yvEdu_Res_InstructionLanguage';
import LANG from '@salesforce/i18n/lang';
import changeActivity from '@salesforce/label/c.yvEdu_Res_ChangeSelection';
import groupsTitle from '@salesforce/label/c.yvEdu_Res_GroupsTitle';
import groupTitle from '@salesforce/label/c.yvEdu_Res_GroupTitle';
import className from '@salesforce/label/c.yvEdu_Res_ClassName';
import participants from '@salesforce/label/c.yvEdu_Res_Participants';
import addGroup from '@salesforce/label/c.yvEdu_Res_AddGroup';
import duplicate from '@salesforce/label/c.yvEdu_Res_Duplicate';
import remove from '@salesforce/label/c.yvEdu_Res_Remove';
import participantsError from '@salesforce/label/c.yvEdu_Res_ParticipantsError';
import summary from '@salesforce/label/c.yvEdu_Res_GroupsSummary';
import next from '@salesforce/label/c.yvEdu_Res_Next';
import maxPerGroup from '@salesforce/label/c.yvEdu_Res_MaxPerGroup';

const TEXT = { changeActivity, groupsTitle, groupTitle, className, participants, addGroup, duplicate, remove, participantsError, summary, next, instructionLanguageTitle };

// The most participants in a class is the Custom Label yvEdu_Res_MaxPerGroup (35, DD V02-05); 50 in some budget frameworks is not built
const MAX_PER_GROUP = Number(maxPerGroup);

const RTL_LANGUAGE_PATTERN = /^(iw|he)/i;

// The guiding language offered first: the language of the site, when the picklist has it
const SITE_LANGUAGE_VALUES = { iw: 'Hebrew', he: 'Hebrew', en: 'English', ru: 'Russian', fr: 'French', es: 'Spanish' };

/**
 * Step "who": the chosen bundles (each with the items ticked) and the groups of the order.
 * Front end only for now (nothing is saved). The parent owns the groups; this
 * component fires `groupschange` with the full new list. Also fires
 * `changeactivity` and `next`.
 */
export default class YvEduNewReservationWho extends LightningElement {
    text = TEXT;
    maxParticipants = MAX_PER_GROUP;
    nextId = 1;
    languages = [];

    /** The bundles chosen in the activity step: [{ id, name, hasHours, hoursLabel, hasItems, includedItems: [{ key, text }] }]. */
    @api bundles = [];

    /** [{ id, name, n }], owned by the parent. */
    @api groups = [];

    /** Chosen instruction language (a picklist value) or '', owned by the parent. */
    @api instructionLanguage = '';

    @wire(getInstructionLanguages)
    wiredLanguages({ data, error }) {
        if (data) {
            this.languages = data;
            if (!this.instructionLanguage) {
                // nothing chosen yet: offer the language of the site first
                const siteValue = SITE_LANGUAGE_VALUES[(LANG || '').slice(0, 2).toLowerCase()];
                const option = data.find((candidate) => candidate.value === siteValue);
                if (option) {
                    this.emitLanguage(option);
                }
            }
        } else if (error) {
            // eslint-disable-next-line no-console
            console.error('yvEduNewReservationWho getInstructionLanguages:', error?.body?.message || error);
        }
    }

    get languageOptions() {
        return this.languages.map((option) => ({
            value: option.value,
            label: option.label,
            ariaPressed: String(option.value === this.instructionLanguage),
            className: option.value === this.instructionLanguage ? 'yvedu-res-pick yvedu-res-pick_selected' : 'yvedu-res-pick'
        }));
    }

    emitLanguage(option) {
        this.dispatchEvent(new CustomEvent('languagechange', { detail: { value: option.value, label: option.label } }));
    }

    handleLanguageClick(event) {
        const option = this.languages.find((candidate) => candidate.value === event.currentTarget.dataset.value);
        if (option) {
            this.emitLanguage(option);
        }
    }

    get langDir() {
        return RTL_LANGUAGE_PATTERN.test(LANG || '') ? 'rtl' : 'ltr';
    }

    get rows() {
        return this.groups.map((group, index) => {
            const invalid = this.isInvalidCount(group.n);
            return {
                id: group.id,
                title: TEXT.groupTitle.replace('{0}', index + 1),
                name: group.name,
                n: group.n,
                canRemove: this.groups.length > 1,
                countClass: invalid ? 'yvedu-res-field yvedu-res-field_error' : 'yvedu-res-field',
                countError: invalid ? TEXT.participantsError.replace('{0}', MAX_PER_GROUP) : ''
            };
        });
    }

    get summaryText() {
        const total = this.groups.reduce((sum, group) => sum + (Number(group.n) || 0), 0);
        return TEXT.summary.replace('{0}', this.groups.length).replace('{1}', total);
    }

    get nextDisabled() {
        return !this.instructionLanguage || this.groups.some((group) => !String(group.name).trim() || this.isInvalidCount(group.n));
    }

    isInvalidCount(value) {
        const n = Number(value);
        return !Number.isInteger(n) || n < 1 || n > MAX_PER_GROUP;
    }

    emitGroups(groups) {
        this.dispatchEvent(new CustomEvent('groupschange', { detail: { groups } }));
    }

    newId() {
        return `g${Date.now()}-${this.nextId++}`;
    }

    handleInput(event) {
        const { id, field } = event.currentTarget.dataset;
        const value = event.currentTarget.value;
        this.emitGroups(this.groups.map((group) => (group.id === id ? { ...group, [field]: value } : group)));
    }

    handleAddGroup() {
        this.emitGroups([...this.groups, { id: this.newId(), name: '', n: '' }]);
    }

    handleDuplicate(event) {
        const source = this.groups.find((group) => group.id === event.currentTarget.dataset.id);
        if (source) {
            this.emitGroups([...this.groups, { ...source, id: this.newId() }]);
        }
    }

    handleRemove(event) {
        this.emitGroups(this.groups.filter((group) => group.id !== event.currentTarget.dataset.id));
    }

    handleChangeActivity() {
        this.dispatchEvent(new CustomEvent('changeactivity'));
    }

    handleNext() {
        if (!this.nextDisabled) {
            this.dispatchEvent(new CustomEvent('next'));
        }
    }
}