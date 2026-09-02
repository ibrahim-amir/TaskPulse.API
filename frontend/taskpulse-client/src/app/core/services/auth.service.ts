import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, catchError, map, of, switchMap, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CommonResponse } from '../models/common-response.model';
import { AuthTokens, LoginRequest, User } from '../models/user.model';

const ACCESS_TOKEN_KEY = 'taskpulse_access_token';
const REFRESH_TOKEN_KEY = 'taskpulse_refresh_token';
const USER_KEY = 'taskpulse_user';

// Talks to the Auth vertical slice (§8). Every response is CommonResponse<T>
// — unwrap .data here so nothing downstream has to know about the envelope.
@Injectable({ providedIn: 'root' })
export class AuthService {
  // Any component (e.g. the shell nav) can subscribe to know who's logged in
  // without re-fetching /api/auth/me itself.
  private currentUserSubject = new BehaviorSubject<User | null>(this.readStoredUser());
  currentUser$ = this.currentUserSubject.asObservable();

  constructor(private http: HttpClient) {}

  // POST /auth/login only returns tokens (AuthResponseDto has no user shape),
  // so a successful login chains straight into /auth/me for the profile.
  login(request: LoginRequest): Observable<User> {
    return this.http
      .post<CommonResponse<AuthTokens>>(`${environment.apiUrl}/auth/login`, request)
      .pipe(
        map((res) => res.data!),
        tap((tokens) => this.storeTokens(tokens)),
        switchMap(() => this.me())
      );
  }

  me(): Observable<User> {
    return this.http.get<CommonResponse<User>>(`${environment.apiUrl}/auth/me`).pipe(
      map((res) => res.data!),
      tap((user) => this.storeUser(user))
    );
  }

  // Used by AuthInterceptor to replay a request after a 401 — access tokens
  // are short-lived on purpose (Jwt:ExpiryMinutes), the refresh token is what
  // keeps the session alive without asking for a password again.
  refreshAccessToken(): Observable<AuthTokens> {
    const refreshToken = this.getRefreshToken();
    return this.http
      .post<CommonResponse<AuthTokens>>(`${environment.apiUrl}/auth/refresh`, { refreshToken })
      .pipe(
        map((res) => res.data!),
        tap((tokens) => this.storeTokens(tokens))
      );
  }

  // Best-effort: the local session clears either way, since a dead/expired
  // refresh token on the server shouldn't strand the user on a page they
  // have no way to leave.
  logout(): Observable<void> {
    return this.http.post(`${environment.apiUrl}/auth/logout`, {}).pipe(
      map(() => void 0),
      catchError(() => of(void 0)),
      tap(() => this.clearSession())
    );
  }

  getAccessToken(): string | null {
    return localStorage.getItem(ACCESS_TOKEN_KEY);
  }

  getRefreshToken(): string | null {
    return localStorage.getItem(REFRESH_TOKEN_KEY);
  }

  get currentUser(): User | null {
    return this.currentUserSubject.value;
  }

  isLoggedIn(): boolean {
    return !!this.getAccessToken();
  }

  hasRole(...roles: User['role'][]): boolean {
    const user = this.currentUser;
    return !!user && roles.includes(user.role);
  }

  private storeTokens(tokens: AuthTokens): void {
    localStorage.setItem(ACCESS_TOKEN_KEY, tokens.accessToken);
    localStorage.setItem(REFRESH_TOKEN_KEY, tokens.refreshToken);
  }

  private storeUser(user: User): void {
    localStorage.setItem(USER_KEY, JSON.stringify(user));
    this.currentUserSubject.next(user);
  }

  private clearSession(): void {
    localStorage.removeItem(ACCESS_TOKEN_KEY);
    localStorage.removeItem(REFRESH_TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    this.currentUserSubject.next(null);
  }

  private readStoredUser(): User | null {
    const raw = localStorage.getItem(USER_KEY);
    return raw ? (JSON.parse(raw) as User) : null;
  }
}
