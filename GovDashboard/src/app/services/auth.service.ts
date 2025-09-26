import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { appSettings } from '../app.config';
import { Observable, tap } from 'rxjs';
import { Router } from '@angular/router';

// Minimal interface for decoding JWT payload
interface JwtPayload {
  exp: number; // expiration time (in seconds)
}

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private apiUrl = appSettings.apiUrl;

  constructor(private http: HttpClient, private router: Router) {}

  // 🔹 Register a new user
  register(email: string, password: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/users/register`, { email, password });
  }

  // 🔹 Login, store token in localStorage
  login(email: string, password: string): Observable<{ token: string }> {
    return this.http
      .post<{ token: string }>(`${this.apiUrl}/users/login`, { email, password })
      .pipe(
        tap((response) => {
          localStorage.setItem('token', response.token);
        })
      );
  }

  // 🔹 Get currently logged-in user (requires valid token)
  getMe(): Observable<any> {
    return this.http.get(`${this.apiUrl}/users/me`);
  }

  // 🔹 Logout and redirect to login
  logout(): void {
    localStorage.removeItem('token');
    this.router.navigate(['/login']);
  }

  // 🔹 Check if user is logged in (and token is not expired)
  isLoggedIn(): boolean {
    const token = this.getToken();
    if (!token) return false;

    if (this.isTokenExpired(token)) {
      this.logout();
      return false;
    }
    return true;
  }

  // 🔹 Get stored token
  getToken(): string | null {
    return localStorage.getItem('token');
  }

  // 🔹 Decode JWT and check expiration
  private isTokenExpired(token: string): boolean {
    try {
      const payload: JwtPayload = JSON.parse(atob(token.split('.')[1]));
      const expiry = payload.exp * 1000; // convert to ms
      return Date.now() >= expiry;
    } catch (e) {
      console.error('Invalid token', e);
      return true; // treat invalid tokens as expired
    }
  }
}
