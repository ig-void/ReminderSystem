import { Injectable, signal, computed, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap, catchError, throwError, of } from 'rxjs';
import {
  AuthResponse,
  LoginRequest,
  RefreshTokenRequest,
  RegisterRequest,
  RevokeTokenRequest,
  UserProfile
} from '../models/auth.models';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  private readonly baseUrl = 'http://localhost:5062/api/auth';
  private readonly ACCESS_TOKEN_KEY = 'reminder_access_token';
  private readonly REFRESH_TOKEN_KEY = 'reminder_refresh_token';
  private readonly USER_PROFILE_KEY = 'reminder_user_profile';

  // Signals for state
  readonly currentUser = signal<UserProfile | null>(this.getStoredUser());
  readonly isAuthenticated = computed(() => !!this.currentUser() && !!this.getAccessToken());

  signup(data: RegisterRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.baseUrl}/register`, data).pipe(
      tap((res) => {
        if (res.success && res.accessToken && res.refreshToken) {
          this.storeTokens(res);
        }
      })
    );
  }

  login(credentials: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.baseUrl}/login`, credentials).pipe(
      tap((res) => {
        if (res.success && res.accessToken && res.refreshToken) {
          this.storeTokens(res);
        }
      })
    );
  }

  refreshToken(): Observable<AuthResponse> {
    const accessToken = this.getAccessToken();
    const refreshToken = this.getRefreshToken();

    if (!accessToken || !refreshToken) {
      this.clearSession();
      return throwError(() => new Error('No refresh token available'));
    }

    const payload: RefreshTokenRequest = { accessToken, refreshToken };
    return this.http.post<AuthResponse>(`${this.baseUrl}/refresh-token`, payload).pipe(
      tap((res) => {
        if (res.success && res.accessToken && res.refreshToken) {
          this.storeTokens(res);
        } else {
          this.clearSession();
        }
      }),
      catchError((err) => {
        this.clearSession();
        return throwError(() => err);
      })
    );
  }

  revokeToken(): Observable<{ message: string }> {
    const refreshToken = this.getRefreshToken();
    if (!refreshToken) {
      return of({ message: 'No token' });
    }

    const payload: RevokeTokenRequest = { refreshToken };
    return this.http.post<{ message: string }>(`${this.baseUrl}/revoke-token`, payload);
  }

  getProfile(): Observable<UserProfile> {
    return this.http.get<UserProfile>(`${this.baseUrl}/me`).pipe(
      tap((user) => {
        this.currentUser.set(user);
        localStorage.setItem(this.USER_PROFILE_KEY, JSON.stringify(user));
      })
    );
  }

  logout(): void {
    this.revokeToken().subscribe({
      next: () => this.handleLogoutRedirect(),
      error: () => this.handleLogoutRedirect()
    });
  }

  private handleLogoutRedirect(): void {
    this.clearSession();
    this.router.navigate(['/login']);
  }

  getAccessToken(): string | null {
    return localStorage.getItem(this.ACCESS_TOKEN_KEY);
  }

  getRefreshToken(): string | null {
    return localStorage.getItem(this.REFRESH_TOKEN_KEY);
  }

  isLoggedIn(): boolean {
    return !!this.getAccessToken();
  }

  private storeTokens(res: AuthResponse): void {
    if (res.accessToken) {
      localStorage.setItem(this.ACCESS_TOKEN_KEY, res.accessToken);
    }
    if (res.refreshToken) {
      localStorage.setItem(this.REFRESH_TOKEN_KEY, res.refreshToken);
    }

    const profile: UserProfile = {
      id: res.userId ?? '',
      email: res.email ?? '',
      userName: res.email ?? '',
      firstName: res.firstName,
      lastName: res.lastName,
      createdAt: new Date().toISOString(),
      roles: []
    };

    this.currentUser.set(profile);
    localStorage.setItem(this.USER_PROFILE_KEY, JSON.stringify(profile));
  }

  private clearSession(): void {
    localStorage.removeItem(this.ACCESS_TOKEN_KEY);
    localStorage.removeItem(this.REFRESH_TOKEN_KEY);
    localStorage.removeItem(this.USER_PROFILE_KEY);
    this.currentUser.set(null);
  }

  private getStoredUser(): UserProfile | null {
    try {
      const stored = localStorage.getItem(this.USER_PROFILE_KEY);
      return stored ? JSON.parse(stored) : null;
    } catch {
      return null;
    }
  }
}
