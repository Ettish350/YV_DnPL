import { Component, inject } from '@angular/core';
import { ActivatedRoute, RouterOutlet } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { environment } from 'src/environments/environment';
import { UserActivityService } from './services/user-activity.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css'],
  standalone: true,
  imports: [RouterOutlet],
})
export class AppComponent {
  title = 'kiosk-app';
  constructor(userActivityService: UserActivityService) {
    userActivityService.init();
    this.preloadAllImages();
  }
  preloadImage(src: string) {
    var img = new Image();
    img.src = src;
  }
  preloadAllImages() {
    this.preloadImage('https://kiosk.yadvashem.org/assets/background-home.jpg');
    this.preloadImage('https://kiosk.yadvashem.org/assets/background.png');
    this.preloadImage('https://kiosk.yadvashem.org/assets/svg/kiosk_demo.svg');
    this.preloadImage('https://kiosk.yadvashem.org/assets/screenSaver.mp4');
}
}
