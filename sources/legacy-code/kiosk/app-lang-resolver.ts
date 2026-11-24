import {
  ActivatedRouteSnapshot,
  Resolve,
  Route,
  Router,
  RouterStateSnapshot,
} from '@angular/router';
import { Injectable, inject } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { environment } from 'src/environments/environment';

@Injectable({ providedIn: 'root' })
export class AppLangResolver implements Resolve<string> {
  isBrowser: boolean = false;

  #translate = inject(TranslateService);
  #router = inject(Router);
  async resolve(
    route: ActivatedRouteSnapshot,
    state: RouterStateSnapshot
  ): Promise<any> {
    const kiosk = route.queryParamMap.get('kiosk');
    if (kiosk && kiosk.length > 0) {
      localStorage.setItem('kiosk', kiosk);
    } else if (!localStorage.getItem('kiosk')) {
      alert('error kiosk is not set!!!');
    }

    let lang = route.params['lang'];
    if (lang) {
      if (environment.optionsLangs.includes(lang)) {
        this.#translate.use(lang);
        if (this.#translate.currentLang != lang) {
          await this.sleep(200);
        }
      } else {
        lang = environment.defaultLang;
        this.#router.navigateByUrl(lang + state.url);
      }
    }
  }

  sleep(ms: number) {
    return new Promise((resolve) => {
      setTimeout(resolve, ms);
    });
  }
}
