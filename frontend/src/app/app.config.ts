import {
  ApplicationConfig,
  inject,
  provideAppInitializer,
  provideBrowserGlobalErrorListeners,
} from '@angular/core';

import {
  provideHttpClient,
  withInterceptors,
} from '@angular/common/http';

import {
  provideRouter,
  withInMemoryScrolling,
} from '@angular/router';

import { authInterceptor } from './Core/interceptors/authInterceptor';
import { UserProfileService } from './Features/profile/services/user-profile-service';
import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),

    provideRouter(
      routes,
      withInMemoryScrolling({
        anchorScrolling: 'enabled',
        scrollPositionRestoration: 'enabled',
      }),
    ),

    provideHttpClient(
      withInterceptors([
        authInterceptor,
      ]),
    ),

    provideAppInitializer(() =>
      inject(
        UserProfileService,
      ).initializeSession(),
    ),
  ],
};