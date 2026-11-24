import { LightningElement } from 'lwc';
import LANG from '@salesforce/i18n/lang';
import submitOrder from '@salesforce/apex/yvEduNewReservationController.submitOrder';
import saving from '@salesforce/label/c.yvEdu_Res_CreatingOrder';
import submitError from '@salesforce/label/c.yvEdu_Res_SubmitError';
import doneTitle from '@salesforce/label/c.yvEdu_Res_DoneTitle';
import doneOrderId from '@salesforce/label/c.yvEdu_Res_DoneReference';
import doneDate from '@salesforce/label/c.yvEdu_Res_DoneDate';
import doneStart from '@salesforce/label/c.yvEdu_Res_DoneStartTime';
import doneActivities from '@salesforce/label/c.yvEdu_Res_DoneActivities';
import doneGroups from '@salesforce/label/c.yvEdu_Res_GroupsTitle';
import doneParticipants from '@salesforce/label/c.yvEdu_Res_DoneParticipants';
import doneMuseumEntry from '@salesforce/label/c.yvEdu_Res_DoneMuseumEntry';
import doneTotal from '@salesforce/label/c.yvEdu_Res_DoneTotal';
import doneArriveEarly from '@salesforce/label/c.yvEdu_Res_DoneArriveEarly';
import doneArrival from '@salesforce/label/c.yvEdu_Res_DoneArrival';
import maxPerGroup from '@salesforce/label/c.yvEdu_Res_MaxPerGroup';
import instructionLanguageTitle from '@salesforce/label/c.yvEdu_Res_InstructionLanguage';
import stepActivity from '@salesforce/label/c.yvEdu_Res_StepActivity';
import stepWho from '@salesforce/label/c.yvEdu_Res_StepWho';
import stepWhen from '@salesforce/label/c.yvEdu_Res_StepWhen';
import stepDone from '@salesforce/label/c.yvEdu_Res_StepDone';

const TEXT = {
    saving,
    submitError,
    doneTitle,
    doneOrderId,
    doneDate,
    doneStart,
    doneActivities,
    doneGroups,
    doneParticipants,
    doneMuseumEntry,
    doneTotal,
    doneArriveEarly,
    doneArrival,
    instructionLanguageTitle
};

// The order of the steps is defined here only. Each key needs a matching getter (is<Key>) and a child in the HTML.
const STEPS = [
    { key: 'activity', label: stepActivity },
    { key: 'who', label: stepWho },
    { key: 'when', label: stepWhen },
    { key: 'done', label: stepDone }
];

// TODO-CONFIG: same limit as the server (V02-05); 50 only in the Tagalit and Overseas activity frameworks
const MAX_PER_GROUP = Number(maxPerGroup);

const RTL_LANGUAGE_PATTERN = /^(iw|he)/i;
// Salesforce reports Hebrew as "iw"; Intl expects "he".
const INTL_LOCALE = (LANG || 'en-US').replace(/^iw/i, 'he');

/**
 * Parent of the new-reservation flow: owns the stepper and the shared state, and shows the child component
 * of the current step. Nothing is saved on the way: everything the customer chose is kept here and sent to
 * the server once, at the end (submitOrder), which creates the whole order or nothing.
 */
export default class YvEduNewReservation extends LightningElement {
    stepIndex = 0;
    audience = null;
    selection = [];
    subject = null;
    visitDate = null;
    visitTime = null;
    museumRequired = false;
    instructionLanguage = '';
    instructionLanguageLabel = '';
    museumBefore = 0;
    museumArrival = false;
    museumEntries = [];
    museumTimes = [];
    isSaving = false;
    saveError = '';
    orderId = null;
    text = TEXT;
    groups = [{ id: 'g0', name: '', n: '' }];

    get langDir() {
        return RTL_LANGUAGE_PATTERN.test(LANG || '') ? 'rtl' : 'ltr';
    }

    get currentKey() {
        return STEPS[this.stepIndex].key;
    }

    get isDone() {
        return Boolean(this.orderId);
    }

    get isInProgress() {
        return !this.isDone;
    }

    get doneOrderText() {
        return TEXT.doneOrderId.replace('{0}', this.orderId);
    }

    /** The visit date written out in the language of the page, for the summary of the finished order. */
    get doneDateText() {
        if (!this.visitDate) {
            return '';
        }
        const [year, month, day] = this.visitDate.split('-').map(Number);
        return new Intl.DateTimeFormat(INTL_LOCALE, { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' }).format(
            new Date(year, month - 1, day)
        );
    }

    /** The start of the visit: the first class's start (its museum entry minus the programme before the museum), or the chosen time. */
    get doneStartTime() {
        if (!this.museumRequired) {
            return this.visitTime;
        }
        // a long visit: the class arrives at the time it chose; a short one starts before its museum entry
        const starts = this.museumTimes
            .filter(Boolean)
            .map((time) => {
                const [hour, minute] = time.split(':').map(Number);
                return hour * 60 + minute - (this.museumArrival ? 0 : this.museumBefore);
            });
        if (!starts.length) {
            return '';
        }
        const first = Math.min(...starts);
        return `${String(Math.floor(first / 60)).padStart(2, '0')}:${String(first % 60).padStart(2, '0')}`;
    }

    /** One row per class with its participants and, when the order includes the museum, the museum entry time. */
    get doneGroups() {
        return this.groups.map((group, index) => {
            const picked = this.museumRequired ? this.museumTimes[index] : null;
            // a long visit: the class chose its arrival and the system assigned the entry
            const entry = this.museumArrival ? this.museumEntries[index] : picked;
            const arrival = this.museumArrival && picked ? `${TEXT.doneArrival.replace('{0}', picked)} \u00B7 ` : '';
            return {
                key: group.id || `g${index}`,
                name: group.name,
                participantsText: TEXT.doneParticipants.replace('{0}', Number(group.n) || 0),
                hasEntry: Boolean(entry),
                entryText: entry ? `${arrival}${TEXT.doneMuseumEntry.replace('{0}', entry)}` : ''
            };
        });
    }

    /** The reminder to come early is for a short visit; in a long one the customer chose the arrival. */
    get showArriveEarly() {
        return this.museumRequired && !this.museumArrival;
    }

    get doneTotalText() {
        return TEXT.doneTotal.replace('{0}', this.totalParticipants);
    }

    get isActivity() {
        return this.currentKey === 'activity';
    }

    /** Total participants of all groups, used by GoMeddo for the capacity check. */
    get totalParticipants() {
        return this.groups.reduce((sum, group) => sum + (Number(group.n) || 0), 0);
    }

    /** Every chosen item of every chosen bundle. */
    get allItemIds() {
        return this.selection.flatMap((entry) => entry.itemIds);
    }

    /** What the groups step shows: each chosen bundle with the items ticked in it. */
    get whoBundles() {
        return this.selection.map((entry) => {
            const chosen = entry.bundle.items.filter((item) => entry.itemIds.includes(item.id));
            return {
                id: entry.bundle.id,
                name: entry.bundle.name,
                hasHours: entry.bundle.hasHours,
                hoursLabel: entry.bundle.hoursLabel,
                hasItems: chosen.length > 0,
                includedItems: chosen.map((item) => ({ key: item.id, text: item.name }))
            };
        });
    }

    get isWho() {
        return this.currentKey === 'who';
    }

    get isWhen() {
        return this.currentKey === 'when';
    }

    /** True when the data of a step is complete; a step can be opened only when all steps before it are complete. */
    isStepComplete(key) {
        switch (key) {
            case 'activity':
                return Boolean(this.audience && this.selection.length > 0 && this.selection.every((entry) => entry.itemIds.length > 0));
            case 'who':
                return (
                    Boolean(this.instructionLanguage) &&
                    this.groups.every((group) => {
                        const n = Number(group.n);
                        return String(group.name).trim() && Number.isInteger(n) && n >= 1 && n <= MAX_PER_GROUP;
                    })
                );
            case 'when':
                // with the museum the entry of every class is the question, otherwise the start time
                return Boolean(
                    this.visitDate &&
                        (this.museumRequired ? this.groups.every((group, index) => Boolean(this.museumTimes[index])) : this.visitTime)
                );
            case 'done':
                return Boolean(this.orderId);
            default:
                return false;
        }
    }

    /** A step can be opened when all steps before it are complete; the last step only shows the result, and nothing can be reopened once the order exists. */
    isStepEnabled(index) {
        if (this.isDone || STEPS[index].key === 'done') {
            return false;
        }
        return STEPS.slice(0, index).every((step) => this.isStepComplete(step.key));
    }

    get stepperItems() {
        return STEPS.map((step, index) => {
            const complete = this.isStepComplete(step.key);
            const isCurrent = index === this.stepIndex && !(step.key === 'done' && complete);
            const isDone = !isCurrent && complete;
            const state = isCurrent ? ' yvedu-res-step_current' : isDone ? ' yvedu-res-step_done' : '';
            // the line leading to a step is green once the step before it is complete
            const linked = index > 0 && this.isStepComplete(STEPS[index - 1].key) ? ' yvedu-res-step_linked' : '';
            return {
                key: step.key,
                label: step.label,
                icon: isDone ? '\u2713' : String(index + 1),
                className: `yvedu-res-step${state}${linked}`,
                ariaCurrent: isCurrent ? 'step' : null,
                disabled: !this.isStepEnabled(index)
            };
        });
    }

    handleStepClick(event) {
        const index = STEPS.findIndex((step) => step.key === event.currentTarget.dataset.key);
        if (index >= 0 && this.isStepEnabled(index)) {
            this.goTo(STEPS[index].key);
        }
    }

    handleAudienceChange(event) {
        this.audience = event.detail.audience;
        this.selection = [];
        this.clearVisit();
    }

    /** The subject does not affect availability, so nothing else is cleared. */
    handleSubjectChange(event) {
        this.subject = event.detail.subject;
    }

    handleSelectionChange(event) {
        this.selection = event.detail.selection;
        this.clearVisit();
    }

    handleChangeActivity() {
        this.goTo('activity');
    }

    /** Availability depends on the chosen items, so a date and time chosen earlier no longer count. */
    clearVisit() {
        this.visitDate = null;
        this.visitTime = null;
        this.museumRequired = false;
        this.museumTimes = [];
    }

    handleDateSelected(event) {
        this.visitDate = event.detail.date;
        this.visitTime = null;
        this.museumRequired = false;
        this.museumTimes = [];
    }

    handleMuseumLoaded(event) {
        this.museumRequired = event.detail.required;
        this.museumBefore = event.detail.beforeMinutes || 0;
        this.museumArrival = Boolean(event.detail.arrivalMode);
    }

    handleMuseumSelected(event) {
        const times = [...this.museumTimes];
        times[event.detail.index] = event.detail.time;
        this.museumTimes = times;
    }

    handleTimeSelected(event) {
        this.visitTime = event.detail.time;
        this.museumTimes = []; // the range of museum entries starts at the visit start
    }

    handleLanguageChange(event) {
        this.instructionLanguage = event.detail.value;
        this.instructionLanguageLabel = event.detail.label;
    }

    handleGroupsChange(event) {
        this.groups = event.detail.groups;
        this.museumTimes = []; // entries belong to the groups as they were
    }

    handleNext() {
        if (this.stepIndex < STEPS.length - 1) {
            this.goTo(STEPS[this.stepIndex + 1].key);
        }
    }

    /** The one and only save: sends everything chosen so far and creates the order on the server. */
    async handleSubmit() {
        if (this.isSaving) {
            return;
        }
        this.isSaving = true;
        this.saveError = '';
        try {
            const result = await submitOrder({
                requestJson: JSON.stringify({
                    audienceCategory: this.audience,
                    activityDaySubjectId: this.subject,
                    bundles: this.selection.map((entry) => ({ bundleId: entry.bundle.id, bundleItemIds: entry.itemIds })),
                    groups: this.groups.map((group, index) => ({
                        name: group.name,
                        participants: Number(group.n),
                        museumTime: this.museumRequired && !this.museumArrival ? this.museumTimes[index] : null,
                        arrivalTime: this.museumRequired && this.museumArrival ? this.museumTimes[index] : null
                    })),
                    visitDate: this.visitDate,
                    instructionLanguage: this.instructionLanguage,
                    visitTime: this.museumRequired ? null : this.visitTime
                })
            });
            if (result.error) {
                this.saveError = `${TEXT.submitError} (${result.error})`;
                return;
            }
            this.museumEntries = result.museumEntries || [];
            this.orderId = result.opportunityId;
            this.goTo('done');
        } catch (error) {
            // eslint-disable-next-line no-console
            console.error('yvEduNewReservation submitOrder:', error?.body?.message || error);
            this.saveError = TEXT.submitError;
        } finally {
            this.isSaving = false;
        }
    }

    goTo(key) {
        this.stepIndex = STEPS.findIndex((step) => step.key === key);
        this.template.querySelector('.yvedu-res-flow')?.scrollIntoView({ block: 'start' });
    }
}