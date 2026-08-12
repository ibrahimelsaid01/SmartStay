import {
  HttpClient,
  HttpErrorResponse,
  HttpInterceptorFn,
  HttpRequest,
} from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import {
  Observable,
  catchError,
  finalize,
  map,
  shareReplay,
  switchMap,
  throwError,
} from 'rxjs';

import { environment } from '../../../environments/environment';
import { AuthResponse } from '../../Features/auth/services/auth';
import { UserProfileService } from '../../Features/profile/services/user-profile-service';

const accessTokenKey = 'token';
const userNameKey = 'userName';

let refreshRequest$: Observable<string> | null = null;

export const authInterceptor: HttpInterceptorFn = (
  req,
  next,
) => {
  const router = inject(Router);
  const http = inject(HttpClient);
  const profileService = inject(UserProfileService);

  /*
   * Keep the exact token attached to this request.
   *
   * A different request may finish Refresh and replace the stored
   * token before this request receives its original 401 response.
   */
  const requestToken = readStoredAccessToken();

  const authenticatedRequest = addAuthenticationData(
    req,
    requestToken,
  );

  return next(authenticatedRequest).pipe(
    catchError((error: unknown) => {
      if (
        !(error instanceof HttpErrorResponse) ||
        error.status !== 401 ||
        !isApiRequest(req) ||
        isRefreshRequest(req) ||
        isAnonymousAuthenticationRequest(req)
      ) {
        return throwError(() => error);
      }

      /*
       * A public or anonymous request that started without an access
       * token must not trigger an automatic Refresh operation.
       *
       * Restoring a session from the HttpOnly refresh cookie is handled
       * explicitly by UserProfileService.initializeSession().
       */
      if (!requestToken) {
        return throwError(() => error);
      }

      const latestToken = readStoredAccessToken();

      /*
       * Another protected request may already have completed Refresh
       * while this request was waiting for its old 401 response.
       *
       * Retry with the newer token rather than rotating the refresh
       * token again.
       */
      if (
        latestToken &&
        latestToken !== requestToken
      ) {
        return next(
          addAuthenticationData(
            req,
            latestToken,
          ),
        );
      }

      /*
       * All requests that failed with the same stale access token share
       * one Refresh request. This prevents simultaneous refresh-token
       * rotations.
       */
      return getOrCreateRefreshRequest(
        http,
        profileService,
        router,
      ).pipe(
        switchMap((newToken) =>
          next(
            addAuthenticationData(
              req,
              newToken,
            ),
          ),
        ),
      );
    }),
  );
};

function getOrCreateRefreshRequest(
  http: HttpClient,
  profileService: UserProfileService,
  router: Router,
): Observable<string> {
  if (refreshRequest$) {
    return refreshRequest$;
  }

  refreshRequest$ = http
    .post<AuthResponse>(
      `${environment.baseApi}/api/auth/refresh`,
      {},
      {
        withCredentials: true,
      },
    )
    .pipe(
      map((response) => {
        const newToken =
          (response.accessToken ?? '').trim();

        if (!newToken) {
          throw new Error(
            'Refresh response does not contain an access token.',
          );
        }

        /*
         * Store the new access token synchronously.
         *
         * Do not start another /api/profile request from inside the
         * active Refresh stream, because that request could create a
         * circular Refresh dependency if it also returned 401.
         */
        profileService.setAccessToken(
          newToken,
        );

        /*
         * The Refresh endpoint returns the current authenticated user
         * and their latest Identity roles. Update the cached profile so
         * Navbar and role-dependent UI stay aligned with the renewed JWT.
         */
        profileService.setCurrentUser(
          response.user,
        );

        return newToken;
      }),

      catchError((refreshError: unknown) => {
        clearAuthentication(
          profileService,
        );

        redirectToLogin(
          router,
        );

        return throwError(
          () => refreshError,
        );
      }),

      finalize(() => {
        refreshRequest$ = null;
      }),

      shareReplay({
        bufferSize: 1,
        refCount: false,
      }),
    );

  return refreshRequest$;
}

function clearAuthentication(
  profileService: UserProfileService,
): void {
  profileService.clearAuth();

  localStorage.removeItem(
    userNameKey,
  );
}

function redirectToLogin(
  router: Router,
): void {
  const returnUrl =
    getSafeReturnUrl(router);

  void router.navigate(
    ['/login'],
    {
      queryParams: {
        returnUrl,
      },
      replaceUrl: true,
    },
  );
}

function getSafeReturnUrl(
  router: Router,
): string {
  const currentUrl =
    (router.url || '/').trim();

  if (
    !currentUrl.startsWith('/') ||
    currentUrl.startsWith('//')
  ) {
    return '/';
  }

  try {
    const urlTree =
      router.parseUrl(currentUrl);

    const primarySegments =
      urlTree.root.children['primary']
        ?.segments ?? [];

    const primaryPath =
      primarySegments
        .map((segment) =>
          segment.path,
        )
        .join('/')
        .toLowerCase();

    if (
      primaryPath === 'login' ||
      primaryPath.startsWith('login/')
    ) {
      return '/';
    }

    return router.serializeUrl(
      urlTree,
    );
  } catch {
    return '/';
  }
}

function addAuthenticationData(
  req: HttpRequest<unknown>,
  token: string | null,
): HttpRequest<unknown> {
  const shouldSendCredentials =
    isApiRequest(req);

  const shouldAttachToken =
    !!token &&
    !isAnonymousAuthenticationRequest(req);

  if (shouldAttachToken) {
    return req.clone({
      withCredentials:
        shouldSendCredentials
          ? true
          : req.withCredentials,

      setHeaders: {
        Authorization:
          `Bearer ${token}`,
      },
    });
  }

  if (shouldSendCredentials) {
    return req.clone({
      withCredentials: true,
    });
  }

  return req;
}

function readStoredAccessToken(): string | null {
  const token = localStorage
    .getItem(accessTokenKey)
    ?.trim();

  return token || null;
}

function isApiRequest(
  req: HttpRequest<unknown>,
): boolean {
  try {
    const applicationOrigin =
      getApplicationOrigin();

    const backendOrigin =
      new URL(
        environment.baseApi,
        applicationOrigin,
      ).origin;

    const requestUrl =
      new URL(
        req.url,
        applicationOrigin,
      );

    const requestPath =
      requestUrl.pathname
        .toLowerCase();

    const isKnownApiPath =
      requestPath === '/api' ||
      requestPath.startsWith('/api/') ||
      requestPath === '/ai-api' ||
      requestPath.startsWith('/ai-api/');

    if (!isKnownApiPath) {
      return false;
    }

    return (
      requestUrl.origin ===
        backendOrigin ||
      requestUrl.origin ===
        applicationOrigin
    );
  } catch {
    return false;
  }
}

function isRefreshRequest(
  req: HttpRequest<unknown>,
): boolean {
  return (
    normalizeRequestPath(req) ===
    '/api/auth/refresh'
  );
}

function isAnonymousAuthenticationRequest(
  req: HttpRequest<unknown>,
): boolean {
  const requestPath =
    normalizeRequestPath(req);

  return [
    '/api/auth/otp/send',
    '/api/auth/otp/verify',
    '/api/auth/external-login',
    '/api/auth/refresh',
    '/api/auth/logout',
  ].includes(requestPath);
}

function normalizeRequestPath(
  req: HttpRequest<unknown>,
): string {
  try {
    const requestUrl =
      new URL(
        req.url,
        getApplicationOrigin(),
      );

    const normalizedPath =
      requestUrl.pathname
        .replace(/\/+$/, '')
        .toLowerCase();

    return normalizedPath || '/';
  } catch {
    return req.url
      .split('?')[0]
      .replace(/\/+$/, '')
      .toLowerCase();
  }
}

function getApplicationOrigin(): string {
  if (typeof window !== 'undefined') {
    return window.location.origin;
  }

  return new URL(
    environment.baseApi,
  ).origin;
}