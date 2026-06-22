import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from './auth.service';

export const jwtInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const token = auth.getAccessToken();
  const isAuthEndpoint =
    req.url.includes('/api/auth/login') || req.url.includes('/api/auth/refresh');

  const authedReq =
    token && !isAuthEndpoint
      ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
      : req;

  return next(authedReq).pipe(
    catchError((error: HttpErrorResponse) => {
      if (
        error.status !== 401 ||
        isAuthEndpoint ||
        req.headers.has('X-Retry-After-Refresh')
      ) {
        return throwError(() => error);
      }

      return auth.refreshToken().pipe(
        switchMap(() => {
          const newToken = auth.getAccessToken();
          const retryReq = newToken
            ? req.clone({
                setHeaders: {
                  Authorization: `Bearer ${newToken}`,
                  'X-Retry-After-Refresh': 'true',
                },
              })
            : req.clone({ setHeaders: { 'X-Retry-After-Refresh': 'true' } });

          return next(retryReq);
        }),
        catchError(() => {
          auth.logout();
          return throwError(() => error);
        }),
      );
    }),
  );
};
