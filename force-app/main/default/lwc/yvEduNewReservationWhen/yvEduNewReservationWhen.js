import { LightningElement, api } from 'lwc';
import LANG from '@salesforce/i18n/lang';
import getAvailableDates from '@salesforce/apex/yvEduNewReservationController.getAvailableDates';
import getAvailableTimes from '@salesforce/apex/yvEduNewReservationController.getAvailableTimes';
import getMuseumSlots from '@salesforce/apex/yvEduNewReservationController.getMuseumSlots';
import title from '@salesforce/label/c.yvEdu_Res_DateTitle';
import loading from '@salesforce/label/c.yvEdu_Res_CheckingAvailability';
import loadError from '@salesforce/label/c.yvEdu_Res_AvailabilityError';
import prevMonth from '@salesforce/label/c.yvEdu_Res_PrevMonth';
import nextMonth from '@salesforce/label/c.yvEdu_Res_NextMonth';
import timeTitle from '@salesforce/label/c.yvEdu_Res_TimeTitle';
import timePlaceholder from '@salesforce/label/c.yvEdu_Res_TimePlaceholder';
import timesLoading from '@salesforce/label/c.yvEdu_Res_CheckingTimes';
import timesError from '@salesforce/label/c.yvEdu_Res_TimesError';
import noTimes from '@salesforce/label/c.yvEdu_Res_NoTimes';
import submit from '@salesforce/label/c.yvEdu_Res_CreateOrder';
import museumTitle from '@salesforce/label/c.yvEdu_Res_MuseumTitle';
import museumClass from '@salesforce/label/c.yvEdu_Res_MuseumClass';
import museumNoRoom from '@salesforce/label/c.yvEdu_Res_MuseumNoRoom';
import museumError from '@salesforce/label/c.yvEdu_Res_MuseumError';
import museumChoose from '@salesforce/label/c.yvEdu_Res_MuseumChoose';
import museumArrivalTitle from '@salesforce/label/c.yvEdu_Res_MuseumArrivalTitle';
import museumEntry from '@salesforce/label/c.yvEdu_Res_DoneMuseumEntry';

const TEXT = { title, loading, loadError, prevMonth, nextMonth, timeTitle, timePlaceholder, timesLoading, timesError, noTimes, submit, museumTitle, museumClass, museumNoRoom, museumError, museumChoose, museumArrivalTitle, museumEntry };

// DD 12.2: orders can be placed up to twelve months ahead
const HORIZON_MONTHS = 12;

/** The pieces ('HH:mm', one step each) an entry at the given time holds; step and hold come from the museum resource. */
function pieceTimes(entryTime, step, hold) {
    const [hour, minute] = entryTime.split(':').map(Number);
    const result = [];
    for (let offset = 0; offset < hold; offset += step) {
        const total = hour * 60 + minute + offset;
        result.push(`${String(Math.floor(total / 60)).padStart(2, '0')}:${String(total % 60).padStart(2, '0')}`);
    }
    return result;
}

function toMinutes(time) {
    const [hour, minute] = time.split(':').map(Number);
    return hour * 60 + minute;
}

function fromMinutes(total) {
    return `${String(Math.floor(total / 60)).padStart(2, '0')}:${String(total % 60).padStart(2, '0')}`;
}

const RTL_LANGUAGE_PATTERN = /^(iw|he)/i;
// Salesforce reports Hebrew as "iw"; Intl expects "he".
const INTL_LOCALE = (LANG || 'en-US').replace(/^iw/i, 'he');

/**
 * Step "when": the customer picks a date. Availability comes from GoMeddo through the server (free/busy per
 * day only). Once a date is chosen: when the chosen items include the museum, the only question is when every class enters
 * the museum (the rest of the programme is laid out around that entry on the server); otherwise the free start times of that
 * day are offered. Fires `dateselected` ({ date: 'YYYY-MM-DD' }), `timeselected` ({ time: 'HH:mm' }), `museumloaded`
 * ({ required, beforeMinutes }) once the museum of the date is known, `museumselected` ({ index, time }) and `ordersubmit`
 * (the button that ends the flow).
 */
export default class YvEduNewReservationWhen extends LightningElement {
    text = TEXT;
    isLoading = false;
    errorMessage = '';
    days = [];
    times = [];
    timesDate = null;
    isLoadingTimes = false;
    timesErrorMessage = '';
    museumRequired = false;
    museumSlots = [];
    arrivalMode = false;
    museumGap = 0;
    museumBefore = 0;
    museumStep = 10;
    museumHold = 30;
    museumFirst = '';
    museumLast = '';
    isLoadingMuseum = false;
    museumErrorMessage = '';

    today = new Date();
    viewYear = this.today.getFullYear();
    viewMonth = this.today.getMonth();

    /** Ids of the chosen Bundle Items, owned by the parent. */
    @api bundleItemIds = [];

    /** Total number of participants, used by GoMeddo for the capacity check. */
    @api participants;

    /** Chosen date ('YYYY-MM-DD') or null, owned by the parent. */
    @api selectedDate;

    /** Chosen start time ('HH:mm') or null, owned by the parent. */
    @api selectedTime;

    /** The groups of the order ({ name, n }), each picks a museum entry time when the order includes the museum. */
    @api groups = [];

    /** Museum entry time ('HH:mm') per group, same order as groups; owned by the parent. */
    @api museumTimes = [];

    connectedCallback() {
        if (this.selectedDate) {
            const [year, month] = this.selectedDate.split('-').map(Number);
            this.viewYear = year;
            this.viewMonth = month - 1;
        }
        this.loadMonth();
        if (this.selectedDate) {
            this.loadDay(this.selectedDate);
        }
    }

    get langDir() {
        return RTL_LANGUAGE_PATTERN.test(LANG || '') ? 'rtl' : 'ltr';
    }

    /** Previous and next point the way the text is read: left and right in English, right and left in Hebrew. */
    get prevChar() {
        return this.langDir === 'rtl' ? '\u203A' : '\u2039';
    }

    get nextChar() {
        return this.langDir === 'rtl' ? '\u2039' : '\u203A';
    }

    get monthTitle() {
        return new Intl.DateTimeFormat(INTL_LOCALE, { month: 'long', year: 'numeric' }).format(
            new Date(this.viewYear, this.viewMonth, 1)
        );
    }

    get weekdayLabels() {
        // 2023-01-01 is a Sunday; the week starts on Sunday
        const format = new Intl.DateTimeFormat(INTL_LOCALE, { weekday: 'short' });
        return Array.from({ length: 7 }, (_, index) => ({
            key: `d${index}`,
            label: format.format(new Date(2023, 0, 1 + index))
        }));
    }

    get cells() {
        const blanks = new Date(this.viewYear, this.viewMonth, 1).getDay();
        const cells = Array.from({ length: blanks }, (_, index) => ({ key: `b${index}`, isBlank: true }));
        this.days.forEach((info) => {
            const day = Number(info.day.slice(8, 10));
            const isSelected = info.day === this.selectedDate;
            cells.push({
                key: info.day,
                isBlank: false,
                date: info.day,
                label: String(day),
                disabled: !info.available,
                ariaPressed: String(isSelected),
                className: isSelected ? 'yvedu-res-day yvedu-res-day_selected' : 'yvedu-res-day'
            });
        });
        return cells;
    }

    get isPrevDisabled() {
        return this.viewYear === this.today.getFullYear() && this.viewMonth <= this.today.getMonth();
    }

    get isNextDisabled() {
        const last = new Date(this.today.getFullYear(), this.today.getMonth() + HORIZON_MONTHS, 1);
        return this.viewYear > last.getFullYear() || (this.viewYear === last.getFullYear() && this.viewMonth >= last.getMonth());
    }

    get hasSelection() {
        return Boolean(this.selectedDate);
    }

    get timeOptions() {
        return this.times.map((time) => ({ time, selected: time === this.selectedTime }));
    }

    get hasNoTimes() {
        return this.hasSelection && !this.isLoadingTimes && !this.timesErrorMessage && this.times.length === 0;
    }

    /** The start times are asked only when the order has no museum: with the museum the entries are the question. */
    get showTimes() {
        return this.hasSelection && !this.museumRequired && !this.isLoadingMuseum;
    }

    get submitDisabled() {
        if (!this.selectedDate || this.isLoadingMuseum) {
            return true;
        }
        return this.museumRequired ? !this.museumComplete : !this.selectedTime;
    }

    get museumComplete() {
        const assigned = this.assignedEntries;
        return (
            !this.museumRequired ||
            (this.groups || []).every(
                (group, index) => Boolean((this.museumTimes || [])[index]) && (!this.arrivalMode || Boolean(assigned[index]))
            )
        );
    }

    get museumHeading() {
        return this.arrivalMode ? TEXT.museumArrivalTitle : TEXT.museumTitle;
    }

    /**
     * Long visit: the museum entry of every class from the arrival it chose, class after class as the server does it: the first
     * entry with room for the class that is at least the gap after the arrival (and at least the programme before the museum),
     * after the seats of the classes before it are taken. Null for a class that has no arrival yet or no entry.
     */
    get assignedEntries() {
        const groups = this.groups || [];
        const picked = this.museumTimes || [];
        const earliestOffset = Math.max(this.museumGap, this.museumBefore);
        const taken = new Map();
        return groups.map((group, index) => {
            if (!this.arrivalMode || !picked[index]) {
                return null;
            }
            const entry = this.firstEntry(toMinutes(picked[index]) + earliestOffset, Number(group.n) || 0, taken);
            if (entry) {
                taken.set(entry, (taken.get(entry) || 0) + (Number(group.n) || 0));
            }
            return entry;
        });
    }

    /** The first entry from the given minute with room for size seats once the seats in taken are counted, or null. */
    firstEntry(fromMinute, size, taken) {
        const slot = this.museumSlots.find(
            (candidate) => toMinutes(candidate.startTime) >= fromMinute && candidate.free - (taken.get(candidate.startTime) || 0) >= size
        );
        return slot ? slot.startTime : null;
    }

    get showMuseum() {
        return this.hasSelection && this.museumRequired && !this.isLoadingMuseum;
    }

    get hasNoMuseumRoom() {
        return this.showMuseum && !this.museumErrorMessage && this.museumSlots.length === 0;
    }

    /**
     * One card per class. Short visit: the times are the museum entries (one step each, only those with room for the class after
     * the other classes that enter in the same piece are counted). Long visit: the times are arrivals, offered when the class
     * would get an entry for them; the card shows the entry it would get.
     */
    get museumRows() {
        const groups = this.groups || [];
        const picked = this.museumTimes || [];
        const assigned = this.assignedEntries;
        const pieces = (entryTime) => pieceTimes(entryTime, this.museumStep, this.museumStep); // seats are counted only in the entry piece
        const freeByPiece = new Map(this.museumSlots.map((slot) => [slot.startTime, slot.free]));
        const earliestOffset = Math.max(this.museumGap, this.museumBefore);
        return groups.map((group, index) => {
            const size = Number(group.n) || 0;
            let times;
            if (this.arrivalMode) {
                // the seats the classes before this one have taken with the arrivals they chose
                const taken = new Map();
                assigned.slice(0, index).forEach((entry, earlier) => {
                    if (entry) {
                        taken.set(entry, (taken.get(entry) || 0) + (Number(groups[earlier].n) || 0));
                    }
                });
                times = this.museumSlots
                    .map((slot) => toMinutes(slot.startTime) - earliestOffset)
                    .filter((arrival) => arrival >= 0)
                    .map((arrival) => fromMinutes(arrival))
                    .filter((arrival) => arrival === picked[index] || this.firstEntry(toMinutes(arrival) + earliestOffset, size, taken));
            } else {
                times = this.museumSlots
                    .filter(
                        (slot) =>
                            slot.startTime >= this.museumFirst &&
                            slot.startTime <= this.museumLast &&
                            pieces(slot.startTime).every((piece) => freeByPiece.has(piece))
                    )
                    .filter((slot) => {
                        const isSelected = picked[index] === slot.startTime;
                        const fitsHere = pieces(slot.startTime).every((piece) => {
                            const others = groups.reduce(
                                (sum, other, otherIndex) =>
                                    otherIndex !== index && picked[otherIndex] && pieces(picked[otherIndex]).includes(piece)
                                        ? sum + (Number(other.n) || 0)
                                        : sum,
                                0
                            );
                            return freeByPiece.get(piece) - others >= size;
                        });
                        return isSelected || fitsHere; // times without room for this class are left out, not shown greyed
                    })
                    .map((slot) => slot.startTime);
            }
            return {
                key: group.id || `g${index}`,
                index,
                number: index + 1,
                title: TEXT.museumClass.replace('{0}', group.name || index + 1).replace('{1}', size),
                entryText: this.arrivalMode && assigned[index] ? TEXT.museumEntry.replace('{0}', assigned[index]) : '',
                cardClass: `yvedu-res-museum-card${picked[index] ? ' yvedu-res-museum-card_done' : ''}`,
                options: times.map((time) => ({ time, selected: picked[index] === time }))
            };
        });
    }

    async loadTimes(date) {
        this.timesDate = date;
        this.isLoadingTimes = true;
        this.timesErrorMessage = '';
        this.times = [];
        try {
            const result = await getAvailableTimes({
                bundleItemIds: this.bundleItemIds,
                visitDate: date,
                participants: this.participants
            });
            if (date !== this.timesDate) {
                return; // the customer picked another date meanwhile
            }
            if (result.error) {
                this.timesErrorMessage = `${TEXT.timesError} (${result.error})`;
                return;
            }
            this.times = result.times;
        } catch (error) {
            // eslint-disable-next-line no-console
            console.error('yvEduNewReservationWhen getAvailableTimes:', error?.body?.message || error);
            this.timesErrorMessage = TEXT.timesError;
        } finally {
            this.isLoadingTimes = false;
        }
    }

    /** A day was chosen: the museum of the day first (it decides which question follows), then the start times when there is no museum. */
    async loadDay(date) {
        this.timesDate = date;
        await this.loadMuseum(date);
        if (date === this.timesDate && !this.museumRequired && !this.museumErrorMessage) {
            this.loadTimes(date);
        }
    }

    async loadMuseum(date) {
        this.isLoadingMuseum = true;
        this.museumRequired = false;
        this.museumErrorMessage = '';
        this.museumSlots = [];
        let beforeMinutes = 0;
        let arrivalMode = false;
        try {
            const result = await getMuseumSlots({ bundleItemIds: this.bundleItemIds, visitDate: date });
            if (date !== this.timesDate) {
                return; // the customer picked another date meanwhile
            }
            if (result.error) {
                this.museumErrorMessage = `${TEXT.museumError} (${result.error})`;
                return;
            }
            this.museumRequired = result.required;
            this.arrivalMode = Boolean(result.arrivalMode);
            this.museumGap = result.gapMinutes || 0;
            this.museumBefore = result.beforeMinutes || 0;
            this.museumStep = result.stepMinutes || 10;
            this.museumHold = result.holdMinutes || 30;
            this.museumFirst = result.firstEntry || '';
            this.museumLast = result.lastEntry || '';
            this.museumSlots = result.slots;
            beforeMinutes = result.beforeMinutes || 0;
            arrivalMode = Boolean(result.arrivalMode);
        } catch (error) {
            // eslint-disable-next-line no-console
            console.error('yvEduNewReservationWhen getMuseumSlots:', error?.body?.message || error);
            this.museumErrorMessage = TEXT.museumError;
        } finally {
            this.isLoadingMuseum = false;
            this.dispatchEvent(new CustomEvent('museumloaded', { detail: { required: this.museumRequired, beforeMinutes, arrivalMode } }));
        }
    }

    async loadMonth() {
        this.isLoading = true;
        this.errorMessage = '';
        const requestedYear = this.viewYear;
        const requestedMonth = this.viewMonth;
        try {
            const result = await getAvailableDates({
                bundleItemIds: this.bundleItemIds,
                year: requestedYear,
                month: requestedMonth + 1,
                participants: this.participants
            });
            if (requestedYear !== this.viewYear || requestedMonth !== this.viewMonth) {
                return; // the customer moved on to another month meanwhile
            }
            if (result.error) {
                this.errorMessage = `${TEXT.loadError} (${result.error})`;
                this.days = [];
                return;
            }
            this.days = result.days.map((info) => ({ day: info.day, available: info.available }));
        } catch (error) {
            // eslint-disable-next-line no-console
            console.error('yvEduNewReservationWhen getAvailableDates:', error?.body?.message || error);
            this.errorMessage = TEXT.loadError;
            this.days = [];
        } finally {
            this.isLoading = false;
        }
    }

    handlePrev() {
        this.moveMonth(-1);
    }

    handleNext() {
        this.moveMonth(1);
    }

    moveMonth(step) {
        const moved = new Date(this.viewYear, this.viewMonth + step, 1);
        this.viewYear = moved.getFullYear();
        this.viewMonth = moved.getMonth();
        this.loadMonth();
    }

    handleDayClick(event) {
        const date = event.currentTarget.dataset.date;
        this.dispatchEvent(new CustomEvent('dateselected', { detail: { date } }));
        this.loadDay(date);
    }

    /** Opens the entry times of a class, or closes them when they are already open. */
    handleMuseumChange(event) {
        const index = Number(event.target.dataset.index);
        const time = event.target.value;
        if (time) {
            this.dispatchEvent(new CustomEvent('museumselected', { detail: { index, time } }));
        }
    }

    handleTimeChange(event) {
        const time = event.target.value;
        if (time) {
            this.dispatchEvent(new CustomEvent('timeselected', { detail: { time } }));
        }
    }

    handleSubmit() {
        if (!this.submitDisabled) {
            this.dispatchEvent(new CustomEvent('ordersubmit'));
        }
    }
}