// src/app/services/auth.interceptor.ts
import { HttpInterceptorFn } from '@angular/common/http';
import { appSettings } from '../app.config';

// Angular 16+ functional interceptor
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const token = localStorage.getItem('token');

  if (token) {
    // Clone request and add Authorization header
    const cloned = req.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`,
      },
    });
    return next(cloned);
  }

  // No token → forward original request
  return next(req);
};
