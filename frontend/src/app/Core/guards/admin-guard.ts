import { inject } from '@angular/core';
import {
  CanActivateFn,
  Router,
  UrlTree,
} from '@angular/router';

import { AuthState } from '../../Features/auth/services/auth-state';

export const adminGuard: CanActivateFn = (
  _route,
  state,
): boolean | UrlTree => {
  const authState = inject(AuthState);
  const router = inject(Router);

  if (!authState.isLoggedIn()) {
    authState.logout();

    return createLoginUrlTree(
      router,
      state.url,
    );
  }

  if (authState.isAdmin()) {
    return true;
  }

  /*
   * A readable JWT without any role cannot authorize access to
   * a role-protected area. Clear that incomplete local session
   * and require Login again instead of treating it as a normal
   * User or Host account.
   */
  if (authState.getRoles().length === 0) {
    authState.logout();

    return createLoginUrlTree(
      router,
      state.url,
    );
  }

  /*
   * A valid authenticated User or Host does not have access
   * to the Admin area, so return them to the public Home page.
   */
  return router.createUrlTree([
    '/',
  ]);
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