import { LightningElement } from 'lwc';
import LANG from '@salesforce/i18n/lang';
import DIR from '@salesforce/i18n/dir';

/* ============================================================
   Yad Vashem – self-service activity order flow
   One self-contained component.
   Everything marked TODO-APEX is mock data / mock logic to be
   replaced with Apex calls.
   ============================================================ */

/* ============================================================
   LABELS
   Every user-facing string lives here. To translate, replace each
   value with a Custom Label import, for example:
     import orderNow from '@salesforce/label/c.YV_OrderNow';
     ... orderNow: orderNow,
   Placeholders use {0}, {1}, ... and are filled by fmt().
   ============================================================ */
const L = {
    // activity selection
    newOrder: 'New order',
    hello: 'Hello, {0}',
    helloAnon: 'Hello',
    ageLayer: 'Age group (loaded from your institution)',
    gefen: 'The activity is part of the Gefen program',
    gefenHelp: 'The Gefen flag is used for classification and reporting only and does not change the price',
    chooseActivity: 'Choose an activity',
    filterAll: 'All',
    filter3: 'Up to 3 hours',
    filter6: 'Up to 6 hours',
    priceCaption: 'Price per group',
    priceCaptionBilled: 'Price per group (billed against the budget framework)',
    orderNow: 'Order now',
    howToOrder: 'How would you like to order?',
    withMuseum: 'With museum entry',
    withMuseumNotePrivate: 'Includes entry to the Holocaust History Museum. Registration and handling fee by entry time.',
    withMuseumNoteEdu: 'Includes entry to the Holocaust History Museum. Educational groups are exempt from the registration and handling fee.',
    withoutMuseum: 'Without museum',
    withoutMuseumNote: 'The activity only, with no museum entry and no registration and handling fee.',
    continueToOrder: 'Continue to order',
    close: 'Close',
    hoursTag: '⏳ {0} hours',
    langsTag: '🌐 {0}',

    // wizard header + steps
    changeActivity: 'Change activity',
    museumIncluded: 'Includes museum entry',
    noMuseum: 'No museum',
    toggleMuseum: 'Change: with / without museum',
    stepWho: 'Who is coming',
    stepWhen: 'When',
    stepDetails: 'Details',
    stepPayment: 'Payment',
    stepConfirm: 'Confirmation',
    holdTitle: '🔒 Your spot is held',
    addFiveMinutes: 'Add 5 minutes',

    // step 1
    groupsTitle: 'Groups',
    groupsSub: 'Define one class per row. Up to {0} participants per group and up to {1} visitors per order.',
    groupN: 'Group {0}',
    duplicate: 'Duplicate',
    remove: 'Remove',
    className: 'Class name *',
    participants: 'Number of participants *',
    groupProfile: 'Group profile *',
    select: 'Select',
    accessibility: 'Some participants need accessibility accommodations',
    noteToGuide: 'Additional note for the guide (optional)',
    note: 'Note',
    addClass: '+ Add class',
    groupsSummary: 'Total: {0} visitors in {1} groups',
    participantsError: 'Up to {0} participants per group. For a larger group, add another class',
    classDefault: 'Class {0}',
    questionsTitle: 'A few questions about the activity',
    q1: 'Has the group visited Yad Vashem before?',
    q2: 'Did the students study the Holocaust at school this year?',
    yes: 'Yes',
    no: 'No',
    partially: 'Partially',

    // step 2
    languageTitle: 'Guide language',
    langHe: 'Hebrew',
    langEn: 'English',
    langAr: 'Arabic',
    langRu: 'Russian',
    langOther: 'Other',
    langNoteRequest: 'This language is submitted as a request pending Yad Vashem approval. The booking will not be instant.',
    langNoteEn: 'Guiding in English is approved immediately.',
    langNoteHe: 'Guiding in Hebrew is approved immediately, subject to guide availability.',
    langHint: 'The language applies to the guide only, not to the other activities included in the package.',
    dateTitle: 'Visit date',
    legendAvailable: 'Available',
    legendFew: 'Last spots',
    legendFull: 'Full for this group size',
    legendClosed: 'Closed / too soon',
    dayInfoDefault: 'Hover over or tap a day to see its status',
    prevMonth: 'Previous month',
    nextMonth: 'Next month',
    dayAria: '{0}: {1}',
    whyPast: 'This date has passed',
    whyWeekend: 'Closed on Fridays and Saturdays',
    whyClosed: 'Closed – official event / closure day',
    whySoon: 'Too soon: {0} business days notice required',
    whyHorizon: 'Beyond the booking horizon ({0} months)',
    whyNoGuideLang: 'No guide is available in this language on this day',
    whyFewGuides: 'Guides are available for only {0} groups on this day; you requested {1}',
    whyFew: 'Last spots',
    whyOk: 'Available',
    timeEntryTitle: 'Museum entry time',
    timeStartTitle: 'Activity start time',
    timeSubEntry: 'Choose the museum entry time. Your arrival time at Yad Vashem is shown right away.',
    timeSubAssign: 'Choose when the group arrives. The system will assign an available museum entry for you.',
    timeSubStart: 'Choose when the group arrives at Yad Vashem. The order has no museum entry, so there is no entry time to choose.',
    spotsAvailable: '{0} spots available',
    noRoom: 'No room for {0} visitors',
    slotAvailable: 'Available',
    noGuides: 'No guides for {0} groups',
    feePerPerson: 'Registration fee {0} per person',
    segStart: 'Activity start',
    segEntryAssigned: 'Museum entry (assigned)',
    segArrival: 'Arrival at Yad Vashem',
    segEntry: 'Museum entry',
    timeTextAssign: 'Activity start {0} · Museum entry {1}',
    timeTextEntry: 'Arrival {0} · Museum entry {1}',
    timeTextStart: 'Activity start {0} · No museum entry',
    holdButton: 'Hold my spot for 15 minutes',
    holdHint: 'Holding keeps your spot while you fill in the details. It is not binding.',
    heldOk: '✓ Your spot is held',

    // step 3
    detailsTitle: 'Contact details',
    editDetails: 'Edit details',
    firstName: 'First name *',
    lastName: 'Last name *',
    email: 'Email *',
    confirmEmail: 'Confirm email *',
    phone: 'Phone *',
    country: 'Country of origin *',
    orderName: 'Order name *',
    specialRequests: 'Notes and special requests',
    consentPre: 'I confirm the order details, and I have read the ',
    consentLink1: 'visitor guidelines',
    consentMid: ' and the ',
    consentLink2: 'privacy policy',
    consentPost: '.',
    consentDetailsSummary: 'What does this consent include?',
    consentP1: 'The information is requested to handle your inquiry, register you for an activity, and provide any other service you request from Yad Vashem. You are under no legal obligation to provide the information, and providing it depends on your consent. If you do not agree to provide it, Yad Vashem will not be able to provide the service.',
    consentP2: "The information will be stored in Yad Vashem's database and may be shared with third parties in accordance with the privacy policy. You have the right to access, correct and request deletion of your information. For privacy inquiries: privacy@yadvashem.org.il.",
    consentUpdates: 'I would like to receive updates about Yad Vashem events and activities.',

    // step 4
    payTitle: 'Payment',
    confirmTitle: 'Order confirmation',
    finalPay: 'Amount due: {0}. Clicking the button takes you to the secure payment page.',
    finalPayNonRefundable: ' The registration and handling fee is non-refundable on cancellation.',
    finalBilledNow: 'Your order will be recorded and billed against the budget framework. No payment is due now. A confirmation will be emailed as soon as the order is completed.',
    finalBilledAfterApproval: 'Your order will be recorded and billed against the budget framework. No payment is due now. A confirmation will be emailed after Yad Vashem approves.',
    policyTitle: 'Cancellation and changes',
    policy1: 'Changes or cancellations up to 3 business days before the visit (not including Fridays and holiday eves).',
    policy2: 'One date change at no additional cost.',
    policy3: 'The registration and handling fee is non-refundable on cancellation.',
    policy4: 'Later cancellations or changes are charged at 100%.',

    // summary
    summaryTitle: 'Order summary',
    summaryLang: 'Guide language: {0}',
    visitors: '👥 Visitors',
    groups: '🧑‍🏫 Groups',
    guideUpTo3: 'Guided activity up to 3 hours',
    guideUpTo6: 'Guided activity up to 6 hours',
    perGroupLine: '{0} × {1} groups',
    museumEntry: '🏛️ Museum entry',
    included: 'Included',
    notIncluded: 'Not included',
    regFee: 'Registration and handling fee',
    regFeeSmall: '{0} × {1} (includes 20% guide discount)',
    regFeeOnlyMuseum: 'Applies only to orders with museum entry',
    notApplicable: 'N/A',
    eduGroup: 'Educational group',
    exempt: 'Exempt',
    total: 'Total',
    totalNotePayNow: 'Payment in the final step',
    totalNoteBilled: 'No payment now. Billing is made against the budget framework',

    // validation + buttons
    missingPrefix: 'Missing: {0}',
    missingMore: '{0} and {1} more',
    missGroupName: 'name for group {0}',
    missGroupSize: 'a valid number of participants in group {0}',
    missGroupProfile: 'profile for group {0}',
    missQuestions: 'answers to the activity questions',
    missDate: 'date',
    missEntryTime: 'entry time',
    missStartTime: 'start time',
    missHold: 'holding your spot',
    missFirstName: 'first name',
    missLastName: 'last name',
    missEmail: 'email',
    missPhone: 'phone',
    missEmailMatch: 'matching email confirmation',
    missCountry: 'country of origin',
    missOrderName: 'order name',
    missConsent: 'consent confirmation',
    ctaDate: 'Continue to choose a date',
    ctaDetails: 'Continue to details',
    ctaConfirm: 'Continue to confirmation',
    ctaSendRequest: 'Send request for approval',
    ctaPay: 'Pay and confirm order',
    ctaComplete: 'Confirm and complete order',
    back: 'Back',
    backToActivities: 'Back to activities',

    // done
    doneRequest: 'Request sent',
    doneReceived: 'Order received',
    doneSubRequest: 'Yad Vashem will approve guiding in the selected language and you will get an update by email. Your spot is held until they reply.',
    doneSub: 'A confirmation was sent to your email address.',
    doneCounts: '{0} groups · {1} visitors · {2}',
    newOrderBtn: 'New order',
    manageOrder: 'Manage order',

    // modal + toasts
    modalTitle: 'Your hold has expired',
    modalText: 'The spot was released so that it is available to others. Everything you filled in was saved, and you can choose a time again.',
    modalButton: 'Back to choosing a time',
    toastHoldStarted: 'Your spot is held for 15 minutes',
    toastExtended: '5 minutes added. You can extend only once',
    toastSubmitFail: 'Failed to submit the order. Please try again',
    relVisitors: 'Your hold was released because the number of visitors changed',
    relGroups: 'Your hold was released because the number of groups changed',
    relLanguage: 'Your hold was released because the guide language changed',
    relDate: 'Your hold was released because the date changed',
    relTime: 'Your hold was released because a different time was chosen',
    relRoute: 'Your hold was released because the museum option changed',
    routeWith: 'Updated: with museum entry',
    routeWithout: 'Updated: without museum',
    orderNameVisit: 'Visit {0}',
    orderNameInst: '{0} – {1}',
    orderNameFallback: 'Order'
};

const fmt = (s, ...a) => s.replace(/\{(\d+)\}/g, (m, i) => (a[i] !== undefined ? a[i] : m));

/* ---------- configuration (consider moving to Custom Metadata) ---------- */
const LONG_H = 3; // above this duration (hours) the customer picks a start time and the system assigns museum entry
const HOLD_SEC = 900; // spot hold: 15 minutes
const HOLD_EXTEND_SEC = 300;
const MAX_GROUPS = 12;
const MAX_PER_GROUP = 35;
const MIN_BUSINESS_DAYS = 3; // TODO-APEX: per product / audience
const HORIZON_MONTHS = 12;
const CLOSED_DATES = []; // TODO-APEX: closure days and holidays, 'YYYY-MM-DD'
const CURRENCY = 'ILS';

const LANG_CODES = [
    { code: 'he', key: 'langHe' },
    { code: 'en', key: 'langEn' },
    { code: 'ar', key: 'langAr' },
    { code: 'ru', key: 'langRu' },
    { code: 'other', key: 'langOther' }
];
const LAYER_CODES = [
    { code: 'elementary', label: 'Elementary school' },
    { code: 'middle', label: 'Middle school' },
    { code: 'high', label: 'High school' }
]; // TODO-APEX: picklist values from the institution record
const PROFILE_CODES = [
    { code: 'regular', label: 'Regular class' },
    { code: 'special', label: 'Special education' },
    { code: 'program', label: 'Special track / program' },
    { code: 'mixed', label: 'Mixed group' }
]; // TODO-APEX: closed list from the spec
const QUESTIONS = [
    // TODO-APEX: dynamic questionnaire per product
    {
        id: 'q1',
        key: 'q1',
        options: [
            { value: 'yes', key: 'yes' },
            { value: 'no', key: 'no' }
        ]
    },
    {
        id: 'q2',
        key: 'q2',
        options: [
            { value: 'yes', key: 'yes' },
            { value: 'partial', key: 'partially' },
            { value: 'no', key: 'no' }
        ]
    }
];

// TODO-APEX: replace with the catalog (Product2 + Price Book) filtered by age group, sector and Gefen
const PRODUCTS = [
    {
        id: '1',
        title: 'Guided activity up to 3 hours - middle school',
        sub: 'Guided tour of the Yad Vashem campus',
        hours: 3,
        price: 663,
        langs: 'Hebrew, English, Arabic, Russian',
        imageUrl: '',
        includes: ['Certified Yad Vashem guide', 'Entry to the Holocaust History Museum (optional)', "Program tailored to the group's needs"]
    },
    {
        id: '2',
        title: 'Workshop up to 3 hours - middle school',
        sub: 'Experiential learning',
        hours: 3,
        price: 663,
        langs: 'Hebrew, English',
        imageUrl: '',
        includes: ['Certified Yad Vashem guide', 'Entry to the Holocaust History Museum (optional)', 'Workshop of your choice']
    },
    {
        id: '3',
        title: 'Guided activity up to 6 hours - middle school',
        sub: 'Tour and workshop',
        hours: 6,
        price: 1180,
        langs: 'Hebrew, English',
        imageUrl: '',
        includes: ['Certified Yad Vashem guide', 'Entry to the Holocaust History Museum (optional)', 'Tour + workshop + exhibition visit']
    }
];

/* ---------- helpers ---------- */
const pad = (n) => String(n).padStart(2, '0');
const dateKey = (d) => `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
const hhmm = (m) => `${pad(Math.floor(m / 60))}:${pad(m % 60)}`;
const toMin = (t) => {
    const [h, m] = t.split(':').map(Number);
    return h * 60 + m;
};
const dateLong = (d) => d.toLocaleDateString(LANG, { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' });
const dateShort = (d) => d.toLocaleDateString(LANG, { day: 'numeric', month: 'numeric' });
const dayMonth = (d) => d.toLocaleDateString(LANG, { day: 'numeric', month: 'long' });
const monthTitle = (y, m) => new Date(y, m, 1).toLocaleDateString(LANG, { month: 'long', year: 'numeric' });
const hebDate = (d) => {
    // the Hebrew date is shown only when the site language is Hebrew
    if (!LANG || !LANG.toLowerCase().startsWith('he')) return '';
    try {
        return new Intl.DateTimeFormat('he-u-ca-hebrew', { day: 'numeric', month: 'long', year: 'numeric' }).format(d);
    } catch (e) {
        return '';
    }
};
const money = (n) => {
    try {
        return new Intl.NumberFormat(LANG, { style: 'currency', currency: CURRENCY, maximumFractionDigits: 2, minimumFractionDigits: 0 }).format(n);
    } catch (e) {
        return `${n} ₪`;
    }
};
const addBusinessDays = (from, n) => {
    const d = new Date(from.getFullYear(), from.getMonth(), from.getDate());
    let left = n;
    while (left > 0) {
        d.setDate(d.getDate() + 1);
        if (d.getDay() < 5) left -= 1;
    }
    return d;
};

export default class YvOrderFlow extends LightningElement {
    /* ---------- context loaded from the server (the component takes no inputs) ---------- */
    audienceMode = 'school-later'; // school-later | school-now | private
    institutionName = '';
    contactName = '';
    contactEmail = '';
    contactPhone = '';
    defaultLayer = 'middle'; // elementary | middle | high

    /* ---------- state ---------- */
    view = 'act'; // act | wiz | done
    step = 1;
    filter = 'all';
    openCard = '';
    cardMus = true;
    layer = '';
    gefen = false;

    act = null;
    museum = true;
    groups = [];
    answers = {};
    lang = 'he';
    date = null;
    monthOffset = 0;
    pick = '';
    entry = '';
    arrival = '';
    start = '';

    held = false;
    left = HOLD_SEC;
    ext = false;
    modalOpen = false;

    editContact = false;
    fn = '';
    ln = '';
    em = '';
    em2 = '';
    ph = '';
    country = '';
    orderName = '';
    notes = '';
    c1 = false;
    c3 = false;

    hoverText = '';
    toastMsg = '';
    sumOpen = false;
    submitting = false;
    orderNumber = '';

    _timer;
    _toastTimer;
    _gid = 0;
    _today;
    _soon;
    _horizon;

    /* ============================================================
       lifecycle
       ============================================================ */
    connectedCallback() {
        const t = new Date();
        this._today = new Date(t.getFullYear(), t.getMonth(), t.getDate());
        this._soon = addBusinessDays(this._today, MIN_BUSINESS_DAYS);
        this._horizon = new Date(this._today.getFullYear(), this._today.getMonth() + HORIZON_MONTHS, this._today.getDate());
        this.layer = this.defaultLayer;
        this.groups = [this.newGroup()];
        // TODO-APEX: load the context (audience mode, institution, contact, default layer) from the server
    }

    disconnectedCallback() {
        this.stopTimer();
        clearTimeout(this._toastTimer);
    }

    /* ============================================================
       general
       ============================================================ */
    get label() {
        return L;
    }
    get dir() {
        return DIR || 'ltr';
    }
    get isRtl() {
        return this.dir === 'rtl';
    }
    get prevChar() {
        return this.isRtl ? '›' : '‹';
    }
    get nextChar() {
        return this.isRtl ? '‹' : '›';
    }
    get isViewAct() {
        return this.view === 'act';
    }
    get isViewWiz() {
        return this.view === 'wiz';
    }
    get isViewDone() {
        return this.view === 'done';
    }
    get isStep1() {
        return this.step === 1;
    }
    get isStep2() {
        return this.step === 2;
    }
    get isStep3() {
        return this.step === 3;
    }
    get isStep4() {
        return this.step === 4;
    }
    get isPrivate() {
        return this.audienceMode === 'private';
    }
    get payNow() {
        return this.audienceMode !== 'school-later';
    }
    get isRequest() {
        return this.lang !== 'he' && this.lang !== 'en';
    }
    get isLong() {
        return !!this.act && this.act.hours > LONG_H;
    }
    get timeMode() {
        if (!this.museum) return 'start';
        return this.isLong ? 'assign' : 'entry';
    }
    get totalVis() {
        return this.groups.reduce((s, g) => s + (Number(g.n) || 0), 0);
    }
    get welcomeText() {
        return this.contactName ? fmt(L.hello, this.contactName) : L.helloAnon;
    }
    get showLogged() {
        return !this.isPrivate && !this.editContact;
    }
    get showForm() {
        return this.isPrivate || this.editContact;
    }

    /* ============================================================
       activity selection
       ============================================================ */
    get layerOptions() {
        return LAYER_CODES.map((l) => ({ value: l.code, label: l.label, selected: l.code === this.layer }));
    }

    get filterChips() {
        return [
            { id: 'all', label: L.filterAll },
            { id: '3', label: L.filter3 },
            { id: '6', label: L.filter6 }
        ].map((f) => ({ ...f, cls: 'fchip' + (this.filter === f.id ? ' on' : '') }));
    }

    get cards() {
        // TODO-APEX: PRODUCTS should come from the server based on this.layer and this.gefen
        return PRODUCTS.filter((p) => this.filter === 'all' || (this.filter === '3' ? p.hours <= 3 : p.hours > 3)).map((p) => {
            const open = this.openCard === p.id;
            return {
                id: p.id,
                title: p.title,
                sub: p.sub,
                hoursLabel: fmt(L.hoursTag, p.hours),
                langsLabel: fmt(L.langsTag, p.langs),
                includes: p.includes.map((t, i) => ({ key: `${p.id}-i${i}`, text: t })),
                priceCaption: this.isPrivate ? L.priceCaption : L.priceCaptionBilled,
                priceText: money(p.price),
                isOpen: open,
                cls: 'card' + (open ? ' open' : ''),
                imgStyle: p.imageUrl ? `background-image:url(${p.imageUrl});background-size:cover;background-position:center` : '',
                radioName: `mus${p.id}`,
                withCls: 'opt' + (this.cardMus ? ' on' : ''),
                withoutCls: 'opt' + (this.cardMus ? '' : ' on'),
                withChecked: this.cardMus,
                withoutChecked: !this.cardMus,
                withNote: this.isPrivate ? L.withMuseumNotePrivate : L.withMuseumNoteEdu
            };
        });
    }

    handleLayer(e) {
        this.layer = e.target.value;
        // TODO-APEX: refresh the catalog for the selected age group
    }
    handleGefen(e) {
        this.gefen = e.target.checked;
    }
    handleFilter(e) {
        this.filter = e.currentTarget.dataset.f;
    }
    handleOpenCard(e) {
        this.openCard = e.currentTarget.dataset.id;
    }
    handleCloseCard() {
        this.openCard = '';
    }
    handleMuseumChoice(e) {
        this.cardMus = e.target.value === '1';
    }
    handleGo(e) {
        const p = PRODUCTS.find((x) => x.id === e.currentTarget.dataset.id);
        if (p) this.openAct(p, this.cardMus);
    }

    openAct(p, museum) {
        this.release('');
        this.act = p;
        this.museum = museum !== false;
        this.date = null;
        this.pick = '';
        this.entry = '';
        this.arrival = '';
        this.start = '';
        if (!this.groups.length) this.groups = [this.newGroup()];
        this.step = 1;
        this.view = 'wiz';
        this.scrollTop();
    }

    handleChangeAct() {
        this.release('');
        this.view = 'act';
        this.scrollTop();
    }

    /* ============================================================
       wizard header
       ============================================================ */
    get actTitle() {
        return this.act ? `${this.act.title} · ${this.act.sub}` : '';
    }
    get actHoursTag() {
        return this.act ? fmt(L.hoursTag, this.act.hours) : '';
    }
    get museumTagText() {
        return this.museum ? L.museumIncluded : L.noMuseum;
    }
    get museumTagCls() {
        return 'tag' + (this.museum ? ' ok' : '');
    }
    handleToggleMuseum() {
        const was = this.held;
        this.museum = !this.museum;
        this.pick = '';
        this.entry = '';
        this.arrival = '';
        this.start = '';
        this.release(was ? L.relRoute : '');
        if (!was) this.toast(this.museum ? L.routeWith : L.routeWithout);
    }

    /* ============================================================
       stepper
       ============================================================ */
    get stepperItems() {
        const labels = [L.stepWho, L.stepWhen, L.stepDetails, this.payNow ? L.stepPayment : L.stepConfirm];
        const items = [];
        labels.forEach((label, i) => {
            const n = i + 1;
            items.push({
                key: `s${n}`,
                isStep: true,
                n,
                label,
                icon: n < this.step ? '✓' : String(n),
                cls: 'st' + (n === this.step ? ' cur' : n < this.step ? ' done' : ''),
                disabled: n >= this.step,
                wrapCls: 'stitem'
            });
            if (n < labels.length) {
                items.push({ key: `l${n}`, isStep: false, cls: 'st-line' + (n < this.step ? ' done' : ''), wrapCls: 'stitem grow1' });
            }
        });
        return items;
    }
    handleStepClick(e) {
        const n = Number(e.currentTarget.dataset.n);
        if (n < this.step) this.goStep(n);
    }
    goStep(n) {
        this.step = n;
        if (n === 3 && !this.orderName && this.date) {
            const short = dateShort(this.date);
            this.orderName = this.isPrivate ? fmt(L.orderNameVisit, short) : fmt(L.orderNameInst, this.institutionName || L.orderNameFallback, short);
        }
        this.scrollTop();
    }

    /* ============================================================
       spot hold (timer)
       ============================================================ */
    get holdText() {
        const l = Math.max(0, this.left);
        return `${pad(Math.floor(l / 60))}:${pad(l % 60)}`;
    }
    get holdBarStyle() {
        return `width:${Math.max(0, (this.left / HOLD_SEC) * 100)}%`;
    }
    get holdWarn() {
        return this.left <= 120;
    }
    get holdClass() {
        return 'holdbar' + (this.holdWarn ? ' warn' : '');
    }
    get canExtend() {
        return this.holdWarn && !this.ext;
    }
    get showHoldbar() {
        return this.held && this.view === 'wiz';
    }

    handleHold() {
        // TODO-APEX: create a temporary hold (e.g. GoMeddo Reservation in Hold status) and get the expiry timestamp from the server
        this.held = true;
        this.left = HOLD_SEC;
        this.ext = false;
        this.startTimer();
        this.toast(L.toastHoldStarted);
    }
    handleExtend() {
        // TODO-APEX: extend the hold on the server
        this.left += HOLD_EXTEND_SEC;
        this.ext = true;
        this.toast(L.toastExtended);
    }
    startTimer() {
        this.stopTimer();
        // eslint-disable-next-line @lwc/lwc/no-async-operation
        this._timer = setInterval(() => {
            if (!this.held) return;
            this.left -= 1;
            if (this.left <= 0) this.expire();
        }, 1000);
    }
    stopTimer() {
        if (this._timer) {
            clearInterval(this._timer);
            this._timer = null;
        }
    }
    release(msg) {
        if (!this.held) return;
        // TODO-APEX: release the hold on the server
        this.held = false;
        this.stopTimer();
        this.pick = '';
        this.entry = '';
        this.arrival = '';
        this.start = '';
        if (msg) this.toast(msg);
    }
    expire() {
        this.release('');
        this.modalOpen = true;
    }
    handleModalOk() {
        this.modalOpen = false;
        this.goStep(2);
    }

    /* ============================================================
       step 1: groups and questionnaire
       ============================================================ */
    newGroup() {
        this._gid += 1;
        return { id: this._gid, name: fmt(L.classDefault, this.groups ? this.groups.length + 1 : 1), n: 30, profile: '', access: false, notes: '' };
    }
    get groupsSubText() {
        return fmt(L.groupsSub, MAX_PER_GROUP, MAX_PER_GROUP * MAX_GROUPS);
    }
    get groupRows() {
        return this.groups.map((g, i) => {
            const bad = !(g.n >= 1 && g.n <= MAX_PER_GROUP);
            return {
                id: g.id,
                title: fmt(L.groupN, i + 1),
                name: g.name,
                n: g.n,
                access: g.access,
                notes: g.notes,
                canRemove: this.groups.length > 1,
                nCls: 'field' + (bad ? ' bad' : ''),
                nErr: bad ? fmt(L.participantsError, MAX_PER_GROUP) : '',
                profileOptions: PROFILE_CODES.map((p) => ({ value: p.code, label: p.label, selected: g.profile === p.code }))
            };
        });
    }
    get groupsSummaryText() {
        return fmt(L.groupsSummary, this.totalVis, this.groups.length);
    }
    get canAddGroup() {
        return this.groups.length < MAX_GROUPS;
    }
    get questionRows() {
        return QUESTIONS.map((q) => ({
            id: q.id,
            text: L[q.key],
            options: q.options.map((o) => ({
                key: `${q.id}-${o.value}`,
                value: o.value,
                label: L[o.key],
                checked: this.answers[q.id] === o.value
            }))
        }));
    }

    handleGroupInput(e) {
        const id = Number(e.target.dataset.id);
        const k = e.target.dataset.k;
        let v = e.target.type === 'checkbox' ? e.target.checked : e.target.value;
        if (k === 'n') v = v === '' ? '' : Number(v);
        this.groups = this.groups.map((g) => (g.id === id ? { ...g, [k]: v } : g));
        if (k === 'n') this.release(L.relVisitors);
    }
    handleAddGroup() {
        if (!this.canAddGroup) return;
        this.release(L.relGroups);
        this.groups = [...this.groups, this.newGroup()];
    }
    handleDupGroup(e) {
        if (!this.canAddGroup) return;
        const id = Number(e.currentTarget.dataset.id);
        const idx = this.groups.findIndex((g) => g.id === id);
        if (idx < 0) return;
        this.release(L.relGroups);
        this._gid += 1;
        const copy = { ...this.groups[idx], id: this._gid, name: `${this.groups[idx].name} (2)` };
        const next = [...this.groups];
        next.splice(idx + 1, 0, copy);
        this.groups = next;
    }
    handleRemoveGroup(e) {
        const id = Number(e.currentTarget.dataset.id);
        this.release(L.relGroups);
        this.groups = this.groups.filter((g) => g.id !== id);
    }
    handleAnswer(e) {
        this.answers = { ...this.answers, [e.target.dataset.q]: e.target.dataset.v };
    }

    /* ============================================================
       step 2: language, date, time
       ============================================================ */
    get langButtons() {
        return LANG_CODES.map((l) => ({ code: l.code, label: L[l.key], cls: 'lang' + (this.lang === l.code ? ' sel' : '') }));
    }
    get langNoteText() {
        if (this.isRequest) return L.langNoteRequest;
        return this.lang === 'en' ? L.langNoteEn : L.langNoteHe;
    }
    get langNoteCls() {
        return 'langnote ' + (this.isRequest ? 'req' : 'ok');
    }
    get langName() {
        const f = LANG_CODES.find((l) => l.code === this.lang);
        return f ? L[f.key] : '';
    }
    handleLang(e) {
        const l = e.currentTarget.dataset.l;
        if (l === this.lang) return;
        this.release(L.relLanguage);
        this.lang = l;
        if (this.date && /full|closed|soon/.test(this.dayStatus(this.date).st)) {
            this.date = null;
            this.pick = '';
        }
    }

    /* ---- availability (mock data) ----
       TODO-APEX: replace with Apex that returns, for every day of the month,
       an availability status based on group count, language, regulation,
       closure days and GoMeddo quotas. */
    dayStatus(d) {
        const dow = d.getDay();
        const n = d.getDate();
        if (d <= this._today) return { st: 'soon', why: L.whyPast };
        if (dow >= 5) return { st: 'closed', why: L.whyWeekend };
        if (CLOSED_DATES.includes(dateKey(d))) return { st: 'closed', why: L.whyClosed };
        if (d <= this._soon) return { st: 'soon', why: fmt(L.whySoon, MIN_BUSINESS_DAYS) };
        if (d > this._horizon) return { st: 'soon', why: fmt(L.whyHorizon, HORIZON_MONTHS) };
        let cap;
        if (this.lang === 'he') cap = 2 + ((n * 7) % 9);
        else if (this.lang === 'en') cap = 12;
        else cap = dow === 2 || dow === 4 ? 3 : 0;
        const g = this.groups.length;
        if (g > cap) {
            return { st: 'full', why: cap === 0 ? L.whyNoGuideLang : fmt(L.whyFewGuides, cap, g) };
        }
        if (cap - g <= 1) return { st: 'few', why: L.whyFew };
        return { st: 'ok', why: L.whyOk };
    }
    spaces(d, mins) {
        // TODO-APEX: free spots in the museum entry window
        return Math.min(100, 30 + ((d.getDate() * 13 + (mins / 10) * 7) % 90));
    }
    slotCap(d, mins) {
        // TODO-APEX: number of free guides at this time
        return 1 + ((d.getDate() * 3 + (mins / 10) * 5) % 8);
    }

    get dowLabels() {
        // Sunday-first week (4 Jan 2026 is a Sunday)
        return [0, 1, 2, 3, 4, 5, 6].map((i) => ({ key: `d${i}`, label: new Date(2026, 0, 4 + i).toLocaleDateString(LANG, { weekday: 'short' }) }));
    }
    get months() {
        return [0, 1].map((i) => {
            const mo = this.monthOffset + i;
            const base = new Date(this._today.getFullYear(), this._today.getMonth() + mo, 1);
            const y = base.getFullYear();
            const m = base.getMonth();
            const first = base.getDay();
            const days = new Date(y, m + 1, 0).getDate();
            const cells = [];
            for (let b = 0; b < first; b++) cells.push({ key: `b${i}-${b}`, label: '', cls: 'day blank', disabled: true, aria: '', title: '', dateKey: '' });
            for (let d = 1; d <= days; d++) {
                const dt = new Date(y, m, d);
                const st = this.dayStatus(dt);
                const sel = this.date && dateKey(this.date) === dateKey(dt);
                cells.push({
                    key: dateKey(dt),
                    label: String(d),
                    cls: `day ${st.st}${sel ? ' sel' : ''}`,
                    disabled: false,
                    aria: fmt(L.dayAria, dayMonth(dt), st.why),
                    title: st.why,
                    dateKey: dateKey(dt)
                });
            }
            return {
                key: `m${i}`,
                title: monthTitle(y, m),
                showPrev: i === 0,
                showNext: i === 1,
                prevDisabled: this.monthOffset <= 0,
                nextDisabled: this.monthOffset >= HORIZON_MONTHS - 1,
                cells
            };
        });
    }
    parseKey(k) {
        const [y, m, d] = k.split('-').map(Number);
        return new Date(y, m - 1, d);
    }
    handleNav(e) {
        const n = Number(e.currentTarget.dataset.nav);
        this.monthOffset = Math.max(0, Math.min(HORIZON_MONTHS - 1, this.monthOffset + n));
    }
    handleDayHover(e) {
        const k = e.currentTarget.dataset.key;
        if (!k) return;
        const dt = this.parseKey(k);
        this.hoverText = `${dayMonth(dt)} – ${this.dayStatus(dt).why}`;
    }
    handleDayLeave() {
        this.hoverText = '';
    }
    handleDay(e) {
        const k = e.currentTarget.dataset.key;
        if (!k) return;
        const dt = this.parseKey(k);
        const st = this.dayStatus(dt);
        if (/full|closed|soon/.test(st.st)) {
            this.hoverText = `${dayMonth(dt)} – ${st.why}`;
            return;
        }
        this.release(L.relDate);
        this.hoverText = '';
        this.date = dt;
        this.pick = '';
        this.entry = '';
    }
    get dayInfoText() {
        if (this.hoverText) return this.hoverText;
        if (this.date) {
            const h = hebDate(this.date);
            return dateLong(this.date) + (h ? ` · ${h}` : '');
        }
        return L.dayInfoDefault;
    }
    get dayInfoCls() {
        return 'dayinfo' + (this.date && !this.hoverText ? ' sel' : '');
    }

    get showTime() {
        return !!this.date && !!this.act;
    }
    get timeHeading() {
        return this.timeMode === 'entry' ? L.timeEntryTitle : L.timeStartTitle;
    }
    get timeSub() {
        if (this.timeMode === 'entry') return L.timeSubEntry;
        if (this.timeMode === 'assign') return L.timeSubAssign;
        return L.timeSubStart;
    }
    get slots() {
        if (!this.showTime) return [];
        const mode = this.timeMode;
        const vis = this.totalVis;
        const g = this.groups.length;
        const to = mode === 'assign' ? toMin('11:00') : toMin('13:30');
        const out = [];
        for (let t = toMin('08:30'); t <= to; t += 30) {
            let ok;
            let label;
            let feeText = '';
            if (mode === 'start') {
                ok = g <= this.slotCap(this.date, t);
                label = ok ? L.slotAvailable : fmt(L.noGuides, g);
            } else {
                const entry = mode === 'assign' ? t + 30 : t;
                const sp = this.spaces(this.date, entry);
                ok = sp >= vis;
                label = ok ? fmt(L.spotsAvailable, sp) : fmt(L.noRoom, vis);
                if (ok && this.isPrivate) feeText = fmt(L.feePerPerson, money(entry >= 600 && entry < 840 ? 15 : 10));
            }
            const time = hhmm(t);
            out.push({ key: time, time, label, feeText, hasFee: !!feeText, disabled: !ok, cls: 'slot' + (this.pick === time ? ' sel' : '') });
        }
        return out;
    }
    handleSlot(e) {
        const t = e.currentTarget.dataset.t;
        this.release(L.relTime);
        this.pick = t;
        this.computeTimes();
    }
    computeTimes() {
        const t = toMin(this.pick);
        const mode = this.timeMode;
        if (mode === 'assign') {
            this.start = this.pick;
            this.entry = hhmm(t + 30);
            this.arrival = this.pick;
        } else if (mode === 'entry') {
            this.entry = this.pick;
            this.arrival = hhmm(t - 30);
            this.start = this.arrival;
        } else {
            this.start = this.pick;
            this.entry = '';
            this.arrival = this.pick;
        }
    }
    get pickSegments() {
        if (!this.pick) return [];
        const m = this.timeMode;
        if (m === 'assign') return [{ key: 'a', label: L.segStart, value: this.start }, { key: 'b', label: L.segEntryAssigned, value: this.entry }];
        if (m === 'entry') return [{ key: 'a', label: L.segArrival, value: this.arrival }, { key: 'b', label: L.segEntry, value: this.entry }];
        return [{ key: 'a', label: L.segStart, value: this.start }];
    }
    get hasPick() {
        return !!this.pick;
    }
    get holdBtnDisabled() {
        return !this.pick || this.held;
    }
    get showHoldBtn() {
        return !this.held;
    }
    get timeText() {
        if (!this.pick) return '';
        const m = this.timeMode;
        if (m === 'assign') return fmt(L.timeTextAssign, this.start, this.entry);
        if (m === 'entry') return fmt(L.timeTextEntry, this.arrival, this.entry);
        return fmt(L.timeTextStart, this.start);
    }

    /* ============================================================
       step 3: details
       ============================================================ */
    handleEditContact() {
        this.editContact = true;
        if (!this.fn) {
            const parts = (this.contactName || '').split(' ');
            this.fn = parts[0] || '';
            this.ln = parts.slice(1).join(' ');
        }
        if (!this.em) this.em = this.contactEmail || '';
        if (!this.ph) this.ph = this.contactPhone || '';
    }
    handleNoop(e) {
        // TODO: link to the visitor guidelines and privacy policy pages
        e.preventDefault();
    }
    handleField(e) {
        this[e.target.dataset.f] = e.target.value;
    }
    handleCheck(e) {
        this[e.target.dataset.f] = e.target.checked;
    }
    get contactDisplayLine() {
        return `${this.contactEmail} · ${this.contactPhone}`;
    }

    /* ============================================================
       pricing and summary
       ============================================================ */
    get feeInfo() {
        if (!this.isPrivate || !this.museum) return null;
        const em = this.entry ? toMin(this.entry) : 600;
        const rate = em >= 600 && em < 840 ? 15 : 10;
        const eff = Math.round(rate * 0.8 * 100) / 100; // 20% discount with a guide
        return { rate, eff, total: Math.round(eff * this.totalVis) };
    }
    get total() {
        const a = this.act ? this.act.price * this.groups.length : 0;
        return a + (this.feeInfo ? this.feeInfo.total : 0);
    }
    get totalText() {
        return money(this.total);
    }
    get totalNote() {
        return this.payNow ? L.totalNotePayNow : L.totalNoteBilled;
    }
    get summaryWhen() {
        const out = [];
        if (this.date) out.push({ key: 'd', text: dateLong(this.date), cls: '' });
        if (this.pick && this.timeText) out.push({ key: 't', text: this.timeText, cls: '' });
        out.push({ key: 'l', text: fmt(L.summaryLang, this.langName), cls: 'muted-line' });
        return out;
    }
    get summaryLines() {
        if (!this.act) return [];
        const g = this.groups.length;
        const f = this.feeInfo;
        const L2 = [
            { key: 'v', label: L.visitors, small: '', value: String(this.totalVis), cls: 'ln', vcls: '' },
            { key: 'g', label: L.groups, small: '', value: String(g), cls: 'ln', vcls: '' },
            {
                key: 'p',
                label: this.act.hours <= 3 ? L.guideUpTo3 : L.guideUpTo6,
                small: fmt(L.perGroupLine, money(this.act.price), g),
                value: money(this.act.price * g),
                cls: 'ln gap',
                vcls: ''
            },
            { key: 'm', label: L.museumEntry, small: '', value: this.museum ? L.included : L.notIncluded, cls: 'ln', vcls: '' }
        ];
        if (f) {
            L2.push({ key: 'f', label: L.regFee, small: fmt(L.regFeeSmall, money(f.eff), this.totalVis), value: money(f.total), cls: 'ln', vcls: '' });
        } else if (!this.museum) {
            L2.push({ key: 'f', label: L.regFee, small: L.regFeeOnlyMuseum, value: L.notApplicable, cls: 'ln', vcls: 'exempt' });
        } else {
            L2.push({ key: 'f', label: L.regFee, small: L.eduGroup, value: L.exempt, cls: 'ln', vcls: 'exempt' });
        }
        return L2;
    }
    get sumCls() {
        return 'sum' + (this.sumOpen ? ' open' : '');
    }
    handleSumToggle() {
        this.sumOpen = !this.sumOpen;
    }

    /* ============================================================
       validation and navigation
       ============================================================ */
    get missingList() {
        const m = [];
        if (this.step === 1) {
            this.groups.forEach((g, i) => {
                if (!String(g.name).trim()) m.push(fmt(L.missGroupName, i + 1));
                if (!(g.n >= 1 && g.n <= MAX_PER_GROUP)) m.push(fmt(L.missGroupSize, i + 1));
                if (!g.profile) m.push(fmt(L.missGroupProfile, i + 1));
            });
            if (QUESTIONS.some((q) => !this.answers[q.id])) m.push(L.missQuestions);
        } else if (this.step === 2) {
            if (!this.date) m.push(L.missDate);
            else if (!this.pick) m.push(this.timeMode === 'entry' ? L.missEntryTime : L.missStartTime);
            else if (!this.held) m.push(L.missHold);
        } else if (this.step === 3) {
            if (this.showForm) {
                if (!this.fn.trim()) m.push(L.missFirstName);
                if (!this.ln.trim()) m.push(L.missLastName);
                if (!this.em.trim()) m.push(L.missEmail);
                if (!this.ph.trim()) m.push(L.missPhone);
                if (this.isPrivate && this.em !== this.em2) m.push(L.missEmailMatch);
                if (this.isPrivate && !this.country.trim()) m.push(L.missCountry);
            }
            if (!this.orderName.trim()) m.push(L.missOrderName);
            if (!this.c1) m.push(L.missConsent);
        }
        return m;
    }
    get missingText() {
        const m = this.missingList;
        if (!m.length) return '';
        const shown = m.slice(0, 3).join(', ');
        return fmt(L.missingPrefix, m.length > 3 ? fmt(L.missingMore, shown, m.length - 3) : shown);
    }
    get nextDisabled() {
        return this.missingList.length > 0 || this.submitting;
    }
    get ctaLabel() {
        if (this.step === 1) return L.ctaDate;
        if (this.step === 2) return L.ctaDetails;
        if (this.step === 3) return L.ctaConfirm;
        if (this.isRequest) return L.ctaSendRequest;
        return this.payNow ? L.ctaPay : L.ctaComplete;
    }
    get prevLabel() {
        return this.step === 1 ? L.backToActivities : L.back;
    }
    handlePrev() {
        if (this.step === 1) {
            this.release('');
            this.view = 'act';
            this.scrollTop();
        } else {
            this.goStep(this.step - 1);
        }
    }
    async handleNext() {
        if (this.missingList.length) return;
        if (this.step < 4) {
            this.goStep(this.step + 1);
            return;
        }
        await this.submitOrder();
    }

    /* ============================================================
       step 4: confirmation / payment
       ============================================================ */
    get finalHeading() {
        return this.payNow ? L.payTitle : L.confirmTitle;
    }
    get finalText() {
        if (this.payNow) {
            return fmt(L.finalPay, this.totalText) + (this.isPrivate ? L.finalPayNonRefundable : '');
        }
        return this.isRequest ? L.finalBilledAfterApproval : L.finalBilledNow;
    }
    get contactFinal() {
        if (this.showForm) return { name: `${this.fn} ${this.ln}`.trim(), email: this.em, phone: this.ph };
        return { name: this.contactName, email: this.contactEmail, phone: this.contactPhone };
    }

    buildPayload() {
        return {
            audienceMode: this.audienceMode,
            productId: this.act.id,
            withMuseum: this.museum,
            layer: this.layer,
            gefen: this.gefen,
            groups: this.groups.map((g) => ({ name: g.name, participants: g.n, profile: g.profile, accessibility: g.access, notes: g.notes })),
            answers: { ...this.answers },
            language: this.lang,
            isLanguageRequest: this.isRequest,
            visitDate: dateKey(this.date),
            startTime: this.start,
            arrivalTime: this.arrival,
            museumEntryTime: this.entry,
            holdSecondsLeft: this.left,
            contact: this.contactFinal,
            country: this.isPrivate ? this.country : null,
            orderName: this.orderName,
            specialRequests: this.notes,
            consentTerms: this.c1,
            consentUpdates: this.c3,
            total: this.total
        };
    }

    async submitOrder() {
        this.submitting = true;
        const payload = this.buildPayload();
        this.dispatchEvent(new CustomEvent('ordersubmit', { detail: payload }));
        try {
            // TODO-APEX: create the Opportunity + line items + session reservation and return the order number and a payment URL (if needed):
            // const result = await createOrder({ payloadJson: JSON.stringify(payload) });
            const result = { orderNumber: `YV-${new Date().getFullYear() % 100}-${10000 + Math.floor(Math.random() * 89999)}`, paymentUrl: null };
            if (this.payNow && result.paymentUrl) {
                window.location.assign(result.paymentUrl);
                return;
            }
            this.orderNumber = result.orderNumber;
            this.stopTimer();
            this.held = false;
            this.view = 'done';
            this.scrollTop();
        } catch (err) {
            this.toast(L.toastSubmitFail);
        } finally {
            this.submitting = false;
        }
    }

    /* ============================================================
       done screen
       ============================================================ */
    get doneHeading() {
        return this.isRequest ? L.doneRequest : L.doneReceived;
    }
    get doneSub() {
        return this.isRequest ? L.doneSubRequest : L.doneSub;
    }
    get doneLines() {
        if (!this.act || !this.date) return [];
        return [
            { key: '1', text: this.act.title },
            { key: '2', text: dateLong(this.date) },
            { key: '3', text: this.timeText },
            { key: '4', text: fmt(L.doneCounts, this.groups.length, this.totalVis, this.totalText) }
        ];
    }
    handleNewOrder() {
        this.act = null;
        this.date = null;
        this.pick = '';
        this.entry = '';
        this.arrival = '';
        this.start = '';
        this.answers = {};
        this._gid = 0;
        this.groups = [];
        this.groups = [this.newGroup()];
        this.orderName = '';
        this.notes = '';
        this.c1 = false;
        this.c3 = false;
        this.step = 1;
        this.openCard = '';
        this.view = 'act';
        this.scrollTop();
    }
    handleManage() {
        // TODO: navigate to the personal area / order management page
        this.dispatchEvent(new CustomEvent('manageorder', { detail: { orderNumber: this.orderNumber } }));
    }

    /* ============================================================
       utilities
       ============================================================ */
    get hasToast() {
        return !!this.toastMsg;
    }
    toast(msg) {
        this.toastMsg = msg;
        clearTimeout(this._toastTimer);
        // eslint-disable-next-line @lwc/lwc/no-async-operation
        this._toastTimer = setTimeout(() => {
            this.toastMsg = '';
        }, 3500);
    }
    scrollTop() {
        const root = this.template.querySelector('.yv');
        if (root && root.scrollIntoView) root.scrollIntoView({ block: 'start' });
    }
}