import { inject } from '@angular/core';
import { CanActivateFn, Router, UrlTree } from '@angular/router';
import { catchError, map, of } from 'rxjs';
import { AuthService } from './auth.service';

export const permissionGuard: CanActivateFn = (route) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  const permission = route.data['permission'] as string | undefined;
  if (!permission) {
    return true;
  }

  const deny = (): UrlTree => router.createUrlTree(['/']);

  if (auth.hasPermission(permission)) {
    return true;
  }

  if (!auth.isAuthenticated()) {
    return deny();
  }

  // Re-fetch /me when memory is stale (HMR) or permissions were not hydrated yet.
  return auth.loadMe().pipe(
    map(() => (auth.hasPermission(permission) ? true : deny())),
    catchError(() => of(deny())),
  );
};
