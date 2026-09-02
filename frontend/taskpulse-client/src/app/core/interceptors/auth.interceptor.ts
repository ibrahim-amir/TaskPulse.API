import { Injectable } from '@angular/core';
import {
  HttpErrorResponse,
  HttpEvent,
  HttpHandler,
  HttpInterceptor,
  HttpRequest,
} from '@angular/common/http';
import { Router } from '@angular/router';
import { BehaviorSubject, Observable, catchError, filter, switchMap, take, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';

// Attaches the JWT to every outgoing request. Access tokens are short-lived
// by design (Jwt:ExpiryMinutes) — rather than bouncing the user to /login on
// every expiry, a 401 triggers one silent refresh and replays the original
// request. Registered in app.module.ts with
// { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true }.
@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  private isRefreshing = false;
  private refreshedToken$ = new BehaviorSubject<string | null>(null);

  constructor(private auth: AuthService, private router: Router) {}

  intercept(req: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    const authReq = this.withToken(req, this.auth.getAccessToken());

    return next.handle(authReq).pipe(
      catchError((error: unknown) => {
        const isAuthEndpoint = req.url.includes('/auth/login') || req.url.includes('/auth/refresh');
        if (error instanceof HttpErrorResponse && error.status === 401 && !isAuthEndpoint) {
          return this.handle401(req, next);
        }
        return throwError(() => error);
      })
    );
  }

  private withToken(req: HttpRequest<unknown>, token: string | null): HttpRequest<unknown> {
    return token ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : req;
  }

  private handle401(req: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    if (!this.auth.getRefreshToken()) {
      this.router.navigate(['/login']);
      return throwError(() => new Error('Session expired.'));
    }

    if (!this.isRefreshing) {
      this.isRefreshing = true;
      this.refreshedToken$.next(null);

      return this.auth.refreshAccessToken().pipe(
        switchMap((tokens) => {
          this.isRefreshing = false;
          this.refreshedToken$.next(tokens.accessToken);
          return next.handle(this.withToken(req, tokens.accessToken));
        }),
        catchError((err) => {
          this.isRefreshing = false;
          this.router.navigate(['/login']);
          return throwError(() => err);
        })
      );
    }

    // A refresh is already in flight for another request — wait for it
    // instead of firing a second /auth/refresh call, then replay with
    // whatever token it produced.
    return this.refreshedToken$.pipe(
      filter((token): token is string => token !== null),
      take(1),
      switchMap((token) => next.handle(this.withToken(req, token)))
    );
  }
}
