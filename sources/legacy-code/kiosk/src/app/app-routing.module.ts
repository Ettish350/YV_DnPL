import { NgModule, } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AppLangResolver } from 'app-lang-resolver';
import { LayoutComponent } from './features/components/layout/layout.component'

export const routes: Routes = [
  {
    path: 'screen-saver', loadComponent: () => import('./features/pages/screen-saver/screen-saver.component').then(x=>x.ScreenSaverComponent),
  },
  {
    path: ':lang', component: LayoutComponent, resolve: {lang: AppLangResolver},
      children: [
        {
          path: 'home', loadComponent: () => import('./features/pages/yv-home/yv-home.component').then(x => x.HomeComponent)
        },
        {
          path: 'menu', loadComponent: () => import('./features/pages/menu/kiosk-menu/kiosk-menu.component').then(x=>x.KioskMenuComponent)
        },
         {
          path: 'information', loadComponent: () => import('./features/pages/information/information.component').then(x=>x.InformationComponent)
        },
        {
          path: 'new-reservation', loadChildren: () => import('./features/pages/new-reservation/new-reservation-routing.module').then(x=>x.NewReservationRoutes),
        },
        {
          path: 'existing-reservation', loadChildren: () => import('./features/pages/existing-reservation/existing-reservation-routing.module').then(x=>x.ExistingReservationRoutes),
        },

     ],
  },
  {
    path: '', pathMatch: 'full', resolve: {lang: AppLangResolver},redirectTo: "/home"
  }
];


@NgModule({
  imports: [ RouterModule.forRoot(routes, {
    initialNavigation: 'enabledNonBlocking',useHash: true,
})],
  exports: [RouterModule],
})
export class AppRoutingModule { }
