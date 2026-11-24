import { LightningElement, api } from 'lwc';
import basePath from '@salesforce/community/basePath';
import LANG from '@salesforce/i18n/lang';
import login from '@salesforce/apex/yvEduLoginController.login';
import YV_EDU_LOGO from '@salesforce/resourceUrl/yvEduLogo';
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

const LABELS = {
    title,
    subtitle,
    institutionIdLabel,
    institutionIdPlaceholder,
    emailLabel,
    emailPlaceholder,
    passwordLabel,
    passwordPlaceholder,
    loginButton,
    forgotPassword,
    logoAlt,
    heroAlt
};

export default class YvEduLoginForm extends LightningElement {
    // Unused: direction now follows the site language. Kept because the property is saved on the published
    // Login page and Salesforce rejects removing a targetConfig property that is in use.
    @api language;

    labels = LABELS;
    logoUrl = YV_EDU_LOGO;
    heroImageUrl = YV_EDU_HERO;
    institutionId = '';
    email = '';
    password = '';
    isLoading = false;
    isPasswordVisible = false;
    errorMessage = '';

    get langDir() {
        return /^(iw|he)/i.test(LANG || '') ? 'rtl' : 'ltr';
    }

    get passwordInputType() {
        return this.isPasswordVisible ? 'text' : 'password';
    }

    get passwordToggleLabel() {
        return this.isPasswordVisible ? hidePassword : showPassword;
    }

    get isSubmitDisabled() {
        return this.isLoading || !this.institutionId || !this.email || !this.password;
    }

    handleInstitutionIdChange(event) {
        this.institutionId = event.target.value;
        this.errorMessage = '';
    }

    handleEmailChange(event) {
        this.email = event.target.value;
        this.errorMessage = '';
    }

    handlePasswordChange(event) {
        this.password = event.target.value;
        this.errorMessage = '';
    }

    handleTogglePassword() {
        this.isPasswordVisible = !this.isPasswordVisible;
    }

    handleSubmit(event) {
        event.preventDefault();
        if (!this.institutionId || !this.email || !this.password) {
            this.errorMessage = requiredFieldError;
            return;
        }
        this.isLoading = true;
        this.errorMessage = '';

        login({
            institutionId: this.institutionId,
            email: this.email,
            password: this.password,
            startUrl: `${basePath}/`
        })
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