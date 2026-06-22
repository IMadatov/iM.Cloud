import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { MessageService } from 'primeng/api';
import { catchError, throwError } from 'rxjs';
import { parseApiErrors } from './api-error.parser';

export const SKIP_ERROR_TOAST_HEADER = 'X-Skip-Error-Toast';

export const errorNotificationInterceptor: HttpInterceptorFn = (req, next) => {
  const messages = inject(MessageService);

  return next(req).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse && shouldShowToast(req, error)) {
        showErrorToasts(messages, error);
      }

      return throwError(() => error);
    }),
  );
};

function shouldShowToast(req: Parameters<HttpInterceptorFn>[0], error: HttpErrorResponse): boolean {
  if (req.headers.has(SKIP_ERROR_TOAST_HEADER)) {
    return false;
  }

  if (error.status === 401) {
    return false;
  }

  return error.status >= 400;
}

function showErrorToasts(messages: MessageService, error: HttpErrorResponse): void {
  const parsed = parseApiErrors(error.error, error.status);

  for (const item of parsed) {
    messages.add({
      severity: 'error',
      summary: item.displayKey,
      detail: item.errorMessage && item.errorMessage !== item.displayKey
        ? item.errorMessage
        : undefined,
      life: 5000,
    });
  }
}
