import { LightningElement, wire } from 'lwc';
import { getRecord, getFieldValue } from 'lightning/uiRecordApi';
import { NavigationMixin } from 'lightning/navigation';
import basePath from '@salesforce/community/basePath';
import LANG from '@salesforce/i18n/lang';
import isGuest from '@salesforce/user/isGuest';
import USER_ID from '@salesforce/user/Id';
import USER_NAME_FIELD from '@salesforce/schema/User.Name';
import getInstitutionName from '@salesforce/apex/yvEduNavBarController.getInstitutionName';
import updateLanguage from '@salesforce/apex/yvEduNavBarController.updateLanguage';

import home from '@salesforce/label/c.yvEdu_Nav_Home';
import newReservation from '@salesforce/label/c.yvEdu_Nav_NewReservation';
import myReservations from '@salesforce/label/c.yvEdu_Nav_MyReservations';
import logout from '@salesforce/label/c.yvEdu_Nav_Logout';
import login from '@salesforce/label/c.yvEdu_Login_LoginButton';
import languageHebrew from '@salesforce/label/c.yvEdu_Nav_LanguageHebrew';
import languageEnglish from '@salesforce/label/c.yvEdu_Nav_LanguageEnglish';
import menuAria from '@salesforce/label/c.yvEdu_Nav_MenuAriaLabel';
import institutionFallback from '@salesforce/label/c.yvEdu_Nav_InstitutionFallback';
import languageError from '@salesforce/label/c.yvEdu_Nav_LanguageError';
import logoAlt from '@salesforce/label/c.yvEdu_Login_LogoAlt';
import YV_EDU_LOGO from '@salesforce/resourceUrl/yvEduLogo';

const RTL_LANGUAGE_PATTERN = /^(iw|he)/i;

// This LWR site switches language through a URL path prefix (/Education/iw/...), not a ?language= param.
// English needs an explicit /en-US prefix too: without it the site redirects back to the visitor's last-used language.
const LANGUAGE_PATH_PREFIX = { iw: 'iw', en_US: 'en-US' };
const LANGUAGE_PATH_SEGMENT = /^\/(iw|en-US)(?=\/|$)/i;

const LABELS = {
    home,
    newReservation,
    myReservations,
    logout,
    login,
    languageHebrew,
    languageEnglish,
    menuAria,
    languageError,
    logoAlt
};

export default class YvEduNavBar extends NavigationMixin(LightningElement) {
    labels = LABELS;
    logoUrl = YV_EDU_LOGO;
    isGuestUser = isGuest;
    institutionName;
    userName;
    isMenuOpen = false;
    languageError = false;

    _handleDocumentClick = (event) => {
        if (this.isMenuOpen && !event.composedPath().includes(this.template.host)) {
            this.isMenuOpen = false;
        }
    };

    connectedCallback() {
        document.addEventListener('click', this._handleDocumentClick);
    }

    disconnectedCallback() {
        document.removeEventListener('click', this._handleDocumentClick);
    }

    /** The signed-in user's own name, shown next to the avatar (not requested for guests). */
    @wire(getRecord, { recordId: '$userRecordId', fields: [USER_NAME_FIELD] })
    wiredUser({ data }) {
        this.userName = data ? getFieldValue(data, USER_NAME_FIELD) : undefined;
    }

    get userRecordId() {
        return isGuest ? undefined : USER_ID;
    }

    @wire(getInstitutionName)
    wiredInstitutionName({ data }) {
        this.institutionName = data;
    }

    get langDir() {
        return RTL_LANGUAGE_PATTERN.test(LANG || '') ? 'rtl' : 'ltr';
    }

    get displayInstitutionName() {
        return this.institutionName || institutionFallback;
    }

    get menuClass() {
        return this.isMenuOpen ? 'yvedu-nav-menu yvedu-nav-menu_open' : 'yvedu-nav-menu';
    }

    handleToggleMenu(event) {
        event.stopPropagation();
        this.isMenuOpen = !this.isMenuOpen;
    }

    handleNavigate(event) {
        const pageName = event.currentTarget.dataset.page;
        this.isMenuOpen = false;
        this[NavigationMixin.Navigate]({
            type: 'comm__namedPage',
            attributes: {
                name: pageName
            }
        });
    }

    handleLanguageChange(event) {
        const languageLocaleKey = event.currentTarget.dataset.lang;
        this.languageError = false;
        updateLanguage({ languageLocaleKey })
            .then(() => {
                window.location.href = this.buildLanguageUrl(languageLocaleKey);
            })
            .catch(() => {
                this.languageError = true;
            });
    }

    handleGuestLanguageChange(event) {
        window.location.href = this.buildLanguageUrl(event.currentTarget.dataset.lang);
    }

    buildLanguageUrl(languageLocaleKey) {
        const url = new URL(window.location.href);
        const sitePrefix = basePath.replace(LANGUAGE_PATH_SEGMENT, '').replace(/\/(iw|en-US)$/i, '');
        let rest = url.pathname.startsWith(sitePrefix) ? url.pathname.slice(sitePrefix.length) : url.pathname;
        rest = rest.replace(LANGUAGE_PATH_SEGMENT, '');
        const languageSegment = LANGUAGE_PATH_PREFIX[languageLocaleKey] ? `/${LANGUAGE_PATH_PREFIX[languageLocaleKey]}` : '';
        url.pathname = `${sitePrefix}${languageSegment}${rest || '/'}`;
        url.searchParams.delete('language');
        return url.toString();
    }

    handleLogout() {
        const loginPath = `${basePath}/login`;
        window.location.href = `${basePath}/secur/logout.jsp?retUrl=${encodeURIComponent(loginPath)}`;
    }

    handleLogin() {
        this[NavigationMixin.Navigate]({
            type: 'comm__loginPage',
            attributes: {
                actionName: 'login'
            }
        });
    }
}