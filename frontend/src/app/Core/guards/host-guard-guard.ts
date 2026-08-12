import { inject } from '@angular/core';
import {
  CanActivateFn,
  Router,
  UrlTree,
} from '@angular/router';

import { AuthState } from '../../Features/auth/services/auth-state';

export const hostGuard: CanActivateFn = (
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

  if (authState.isHost()) {
    return true;
  }

  /*
   * The backend includes every Identity role in the access-token
   * role claims. A readable JWT without any role is therefore an
   * incomplete local session and cannot authorize the Host area.
   */
  if (authState.getRoles().length === 0) {
    authState.logout();

    return createLoginUrlTree(
      router,
      state.url,
    );
  }

  /*
   * A logged-in User or Admin without the Host role must not enter
   * the Host area. Return them to Home instead of redirecting a
   * normal signed-in user to the Become Host application page.
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