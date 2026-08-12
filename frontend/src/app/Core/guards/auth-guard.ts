import { inject } from '@angular/core';
import {
  CanActivateFn,
  Router,
  UrlTree,
} from '@angular/router';

import { AuthState } from '../../Features/auth/services/auth-state';

export const authGuard: CanActivateFn = (
  _route,
  state,
): boolean | UrlTree => {
  const authState = inject(AuthState);
  const router = inject(Router);

  /*
   * AuthState only verifies that the stored access token is
   * present and that its JWT payload can be decoded safely.
   *
   * The guard deliberately does not reject the token solely
   * because its exp claim has passed. SmartStay stores the
   * refresh token in an HttpOnly cookie, and authInterceptor
   * can renew the access token after the first protected API
   * request returns 401.
   */
  if (authState.isLoggedIn()) {
    return true;
  }

  /*
   * Clear any malformed stored authentication data before
   * redirecting. A valid but expired access token is not
   * cleared here because AuthState still considers its JWT
   * payload readable and allows the refresh flow to run.
   */
  authState.logout();

  return createLoginUrlTree(
    router,
    state.url,
  );
};

function createLoginUrlTree(
  router: Router,
  returnUrl: string,
): UrlTree {
  return router.createUrlTree(
    ['/login'],
    {
      queryParams: {
        returnUrl,
      },
    },
  );
}