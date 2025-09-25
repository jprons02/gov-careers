import { ApplicationConfig, provideZoneChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';

import { routes } from './app.routes';
import { authInterceptor } from './services/auth.interceptor';

// ✅ App constants (use this in services)
export const appSettings = {
  apiUrl: window.location.hostname.includes('localhost')
    ? 'http://localhost:5200/api'
    : 'https://gov-careers-api-hydtfyfshbgde2fm.centralus-01.azurewebsites.net/api',
};

// ✅ Angular DI config (use this in main.ts)
export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideHttpClient(withInterceptors([authInterceptor])),
  ],
};
