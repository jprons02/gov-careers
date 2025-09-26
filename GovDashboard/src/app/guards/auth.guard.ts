// src/app/guards/auth.guard.ts
import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

export const authGuard: CanActivateFn = (route, state) => {
  const router = inject(Router);

  // Check token in localStorage (or sessionStorage if you prefer)
  const token = localStorage.getItem('token');

  if (token) {
    return true; // ✅ Allow access
  }

  // ❌ No token, redirect to login
  router.navigate(['/login']);
  return false;
};
