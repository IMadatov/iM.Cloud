import { inject, Injectable } from '@angular/core';
import { Router } from '@angular/router';
import {
  BehaviorSubject,
  catchError,
  firstValueFrom,
  map,
  Observable,
  of,
  switchMap,
  tap,
  throwError,
} from 'rxjs';
import { AuthApiService } from './auth-api.service';
import { AUTH_STORAGE_KEYS } from './auth.types';
import { LoginResponse, MeResponse, UserDto } from '@im-cloud/api';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly authApi = inject(AuthApiService);
  private readonly router = inject(Router);

  private readonly currentUserSubject = new BehaviorSubject<UserDto | null>(null);
  private readonly meSubject = new BehaviorSubject<MeResponse | null>(null);

  readonly currentUser$ = this.currentUserSubject.asObservable();
  readonly me$ = this.meSubject.asObservable();

  initialize(): Promise<void> {
    if (!this.getAccessToken()) {
      return Promise.resolve();
    }

    return firstValueFrom(
      this.loadMe().pipe(
        catchError(() => {
          this.clearSession();
          return of(null);
        }),
      ),
    ).then(() => undefined);
  }

  login(email: string, password: string): Observable<void> {
    return this.authApi.login(email, password).pipe(
      tap((response) => this.storeSession(response)),
      switchMap(() => this.loadMe()),
      map(() => undefined),
    );
  }

  refreshToken(): Observable<void> {
    const refreshToken = this.getRefreshToken();
    if (!refreshToken) {
      this.clearSession();
      return throwError(() => new Error('No refresh token'));
    }

    return this.authApi.refresh(refreshToken).pipe(
      tap((response) => this.storeSession(response)),
      map(() => undefined),
      catchError((error) => {
        this.clearSession();
        return throwError(() => error);
      }),
    );
  }

  loadMe(): Observable<MeResponse> {
    return this.authApi.me().pipe(
      tap((me) => {
        this.meSubject.next(me);
        this.currentUserSubject.next(me.user ?? null);
      }),
    );
  }

  logout(): void {
    this.clearSession();
    void this.router.navigate(['/login']);
  }

  isAuthenticated(): boolean {
    return !!this.getAccessToken();
  }

  getAccessToken(): string | null {
    return localStorage.getItem(AUTH_STORAGE_KEYS.accessToken);
  }

  getRefreshToken(): string | null {
    return localStorage.getItem(AUTH_STORAGE_KEYS.refreshToken);
  }

  private storeSession(response: LoginResponse): void {
    if (response.accessToken) {
      localStorage.setItem(AUTH_STORAGE_KEYS.accessToken, response.accessToken);
    }
    if (response.refreshToken) {
      localStorage.setItem(AUTH_STORAGE_KEYS.refreshToken, response.refreshToken);
    }
    if (response.user) {
      this.currentUserSubject.next(response.user);
    }
  }

  private clearSession(): void {
    localStorage.removeItem(AUTH_STORAGE_KEYS.accessToken);
    localStorage.removeItem(AUTH_STORAGE_KEYS.refreshToken);
    this.currentUserSubject.next(null);
    this.meSubject.next(null);
  }
}
