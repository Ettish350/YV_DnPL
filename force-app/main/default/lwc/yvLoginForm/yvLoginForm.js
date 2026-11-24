import { LightningElement } from 'lwc';
import basePath from '@salesforce/community/basePath';
import LANG from '@salesforce/i18n/lang';
import login from '@salesforce/apex/yvLoginController.login';
import YV_EDU_LOGO from'@salesforce/resourceUrl/yvEduLogo';
import YV_EDU_HERO from '@salesforce/resourceUrl/yvEduLoginHero';
import title from '@salesforce/label/c.yvEdu_Login_Title';
import subtitle from '@salesforce/label/c.yvEdu_Login_Subtitle';
import institutionIdLabel from '@salesforce/label/c.yvEdu_Login_InstitutionIdLabel';
import institutionIdPlaceholder from '@salesforce/label/c.yvEdu_Login_InstitutionIdPlaceholder';
import emailLabel from '@salesforce/label/c.yvEdu_Login_EmailLabel';
import emailPlaceholder from '@salesforce/label/c.yvEdu_Login_EmailPlaceholder';
import passwordLabel from '@salesforce/label/c.yvEdu_Login_PasswordLabel';
import passwordPlaceholder from '@salesforce/label/c.yvEdu_Login_PasswordPlaceholder';
import showPassword from '@salesforce/label/c.yvEdu_Login_ShowPassword';
import hidePassword from '@salesforce/label/c.yvEdu_Login_HidePassword';
import loginButton from '@salesforce/label/c.yvEdu_Login_LoginButton';
import forgotPassword from '@salesforce/label/c.yvEdu_Login_ForgotPassword';
import requiredFieldError from '@salesforce/label/c.yvEdu_Login_RequiredFieldError';
import invalidCredentialsError from '@salesforce/label/c.yvEdu_Login_InvalidCredentialsError';
import logoAlt from '@salesforce/label/c.yvEdu_Login_LogoAlt';
import heroAlt from '@salesforce/label/c.yvEdu_Login_HeroAlt';
import typeEdu from '@salesforce/label/c.yvLogin_Type_Edu';
import typeAgent from '@salesforce/label/c.yvLogin_Type_Agent';
import typeGroupAria from '@salesforce/label/c.yvLogin_TypeGroupAria';
import usernameLabel from '@salesforce/label/c.yvLogin_UsernameLabel';
import usernamePlaceholder from '@salesforce/label/c.yvLogin_UsernamePlaceholder';

const LABELS = { title, subtitle, loginButton, forgotPassword, logoAlt, heroAlt, typeGroupAria };

// One entry per login type. To add a type: add an entry here (and its labels). The toggle and the
// fields are drawn from this list. `login` signs the user in; a type without it cannot be submitted
// yet. Field `kind` is 'text', 'email' or 'password'.
const LOGIN_TYPES = [
    {
        key: 'edu',
        label: typeEdu,
        fields: [
            { key: 'institutionId', kind: 'text', label: institutionIdLabel, placeholder: institutionIdPlaceholder, autocomplete: 'off' },
            { key: 'email', kind: 'email', label: emailLabel, placeholder: emailPlaceholder, autocomplete: 'username' },
            { key: 'password', kind: 'password', label: passwordLabel, placeholder: passwordPlaceholder, autocomplete: 'current-password' }
        ],
        login: (v, startUrl) => login({ loginType: 'edu', credentials: { ...v }, startUrl })
    },
    {
        key: 'agent',
        label: typeAgent,
        fields: [
            { key: 'username', kind: 'text', label: usernameLabel, placeholder: usernamePlaceholder, autocomplete: 'username' },
            { key: 'password', kind: 'password', label: passwordLabel, placeholder: passwordPlaceholder, autocomplete: 'current-password' }
        ],
        login: (v, startUrl) => login({ loginType: 'agent', credentials: { ...v }, startUrl })
    }
];

export default class YvLoginForm extends LightningElement {
    labels = LABELS;
    logoUrl = YV_EDU_LOGO;
    heroImageUrl = YV_EDU_HERO;
    selectedKey = LOGIN_TYPES[0].key;
    values = {};
    isLoading = false;
    isPasswordVisible = false;
    errorMessage = '';

    get langDir() {
        return /^(iw|he)/i.test(LANG || '') ? 'rtl' : 'ltr';
    }

    get selectedType() {
        return LOGIN_TYPES.find((t) => t.key === this.selectedKey);
    }

    get typeOptions() {
        return LOGIN_TYPES.map((t) => {
            const selected = t.key === this.selectedKey;
            return {
                key: t.key,
                label: t.label,
                selected: String(selected),
                className: selected ? 'yv-type-option yv-type-option-active' : 'yv-type-option'
            };
        });
    }

    get fields() {
        return this.selectedType.fields.map((f) => {
            const isPassword = f.kind === 'password';
            return {
                ...f,
                id: `yv-login-${f.key}`,
                isPassword,
                inputType: isPassword && this.isPasswordVisible ? 'text' : f.kind,
                value: this.values[f.key] || ''
            };
        });
    }

    get passwordToggleLabel() {
        return this.isPasswordVisible ? hidePassword : showPassword;
    }

    get isSubmitDisabled() {
        const t = this.selectedType;
        return this.isLoading || !t.login || t.fields.some((f) => !this.values[f.key]);
    }

    handleTypeSelect(event) {
        const key = event.currentTarget.dataset.key;
        if (key === this.selectedKey) {
            return;
        }
        this.selectedKey = key;
        this.values = {};
        this.errorMessage = '';
        this.isPasswordVisible = false;
    }

    handleFieldChange(event) {
        this.values = { ...this.values, [event.target.dataset.key]: event.target.value };
        this.errorMessage = '';
    }

    handleTogglePassword() {
        this.isPasswordVisible = !this.isPasswordVisible;
    }

    handleSubmit(event) {
        event.preventDefault();
        const t = this.selectedType;
        if (!t.login) {
            return;
        }
        if (t.fields.some((f) => !this.values[f.key])) {
            this.errorMessage = requiredFieldError;
            return;
        }
        this.isLoading = true;
        this.errorMessage = '';

        t.login(this.values, `${basePath}/`)
            .then((result) => {
                if (result.success) {
                    window.location.href = result.redirectUrl;
                } else {
                    this.errorMessage = invalidCredentialsError;
                    this.isLoading = false;
                }
            })
            .catch(() => {
                this.errorMessage = invalidCredentialsError;
                this.isLoading = false;
            });
    }

    handleForgotPassword() {
        this.dispatchEvent(new CustomEvent('forgotpassword'));
    }
}