import { APP_INITIALIZER, ApplicationConfig, provideZoneChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { providePrimeNG } from 'primeng/config';
import Aura from '@primeng/themes/aura';
import { ConfirmationService, MessageService } from 'primeng/api';
import { API_BASE_URL } from '@im-cloud/api';

import { routes } from './app.routes';
import { environment } from '../environments/environment';
import { jwtInterceptor } from './core/auth/jwt.interceptor';
import { errorNotificationInterceptor } from './core/http/error-notification.interceptor';
import { AuthService } from './core/auth/auth.service';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    MessageService,
    ConfirmationService,
    provideHttpClient(withInterceptors([jwtInterceptor, errorNotificationInterceptor])),
    { provide: API_BASE_URL, useValue: environment.apiUrl },
    {
      provide: APP_INITIALIZER,
      multi: true,
      useFactory: (auth: AuthService) => () => auth.initialize(),
      deps: [AuthService],
    },
    provideAnimationsAsync(),
    providePrimeNG({
      theme: {
        preset: Aura,
      },
    }),
  ],
};
