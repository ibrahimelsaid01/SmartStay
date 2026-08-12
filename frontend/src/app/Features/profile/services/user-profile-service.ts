import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, computed, signal } from '@angular/core';
import { toObservable } from '@angular/core/rxjs-interop';
import { Observable, firstValueFrom, of, throwError } from 'rxjs';
import {
  catchError,
  finalize,
  map,
  switchMap,
  tap,
} from 'rxjs/operators';

import { environment } from '../../../../environments/environment';
import {
  Auth,
  AuthenticatedUserResponse,
} from '../../auth/services/auth';

export interface UserProfile {
  id?: string;
  firstName: string;
  lastName: string;
  email?: string;
  phoneNumber: string;
  gender: string;
  birthday: string;
  country: string;
  address: string;
  zipCode: string;
  profileImageUrl?: string | null;
  isProfileCompleted?: boolean;
  roles?: string[];
  createdAt?: string;
  updatedAt?: string | null;
}

interface UpdateProfilePayload {
  firstName: string;
  lastName: string;
  phoneNumber: string | null;
  gender: number | null;
  birthday: string | null;
  country: string | null;
  address: string | null;
  zipCode: string | null;
}

@Injectable({
  providedIn: 'root',
})
export class UserProfileService {
  private readonly profileApiUrl =
    `${environment.baseApi}/api/profile`;

  private readonly tokenKey =
    'token';

  private readonly userNameKey =
    'userName';

  private readonly profileKey =
    'current-user-profile';

  private readonly initializedState =
    signal(false);

  private sessionInitializationPromise:
    Promise<void> | null =
      null;

  private readonly loadingState =
    signal(false);

  private readonly accessTokenState =
    signal<string | null>(
      localStorage.getItem(
        this.tokenKey,
      ),
    );

  private readonly currentUserState =
    signal<UserProfile | null>(
      this.readCachedProfile(),
    );

  readonly currentUser =
    computed(() =>
      this.currentUserState(),
    );

  readonly currentUser$ =
    toObservable(
      this.currentUser,
    );

  readonly isAuthenticated =
    computed(() =>
      this.accessTokenState() !==
      null,
    );

  readonly isLoading =
    computed(() =>
      this.loadingState(),
    );

  readonly isInitialized =
    computed(() =>
      this.initializedState(),
    );

  constructor(
    private readonly http:
      HttpClient,

    private readonly authService:
      Auth,
  ) {}

  initializeSession():
    Promise<void> {
    if (
      this.initializedState()
    ) {
      return Promise.resolve();
    }

    if (
      this.sessionInitializationPromise
    ) {
      return this
        .sessionInitializationPromise;
    }

    this.sessionInitializationPromise =
      this.runSessionInitialization()
        .finally(() => {
          this.loadingState.set(
            false,
          );

          this.initializedState.set(
            true,
          );

          this.sessionInitializationPromise =
            null;
        });

    return this
      .sessionInitializationPromise;
  }

  private async runSessionInitialization():
    Promise<void> {
    this.loadingState.set(
      true,
    );

    const storedToken =
      this.readToken()
        ?.trim();

    if (storedToken) {
      try {
        await firstValueFrom(
          this.fetchProfile({
            silent: true,
          }),
        );
      } catch {
        /*
         * fetchProfile and authInterceptor already handle an
         * unauthorized session by trying Refresh and clearing
         * invalid authentication state when Refresh fails.
         *
         * A temporary network/server error must not prevent the
         * Angular application from starting. The cached profile
         * and stored token remain available for a later retry.
         */
      }

      return;
    }

    await this
      .restoreSessionFromRefreshCookie();
  }

  private async restoreSessionFromRefreshCookie():
    Promise<void> {
    try {
      const response =
        await firstValueFrom(
          this.authService.refresh(),
        );

      const accessToken =
        (response.accessToken ?? '')
          .trim();

      if (!accessToken) {
        throw new Error(
          'Refresh response does not contain an access token.',
        );
      }

      await firstValueFrom(
        this.setAccessToken(
          accessToken,
          response.user,
        ),
      );
    } catch {
      /*
       * A missing, expired, or revoked HttpOnly refresh cookie
       * represents an anonymous browser session. Clear any stale
       * cached profile and allow application startup to continue.
       */
      this.clearAuth();
    }
  }

  setAccessToken(
    token: string,
    user?:
      | AuthenticatedUserResponse
      | UserProfile
      | null,
  ): Observable<UserProfile | null> {
    const normalizedToken =
      token.trim();

    if (!normalizedToken) {
      this.clearAuth();

      return of(null);
    }

    localStorage.setItem(
      this.tokenKey,
      normalizedToken,
    );

    this.accessTokenState.set(
      normalizedToken,
    );

    this.initializedState.set(
      true,
    );

    /*
     * The refresh interceptor supplies only a new
     * access token.
     *
     * Starting /api/profile from inside the active
     * refresh stream could create a circular
     * dependency if that profile request also
     * returns 401 and waits for the same refresh
     * operation.
     */
    if (!user) {
      return of(
        this.currentUserState(),
      );
    }

    const normalizedUser =
      this.normalizeProfile(
        user,
      );

    this.setCurrentUser(
      normalizedUser,
    );

    return this.fetchProfile({
      silent: true,
    }).pipe(
      catchError(() =>
        of(normalizedUser),
      ),
    );
  }

  setCurrentUser(
    profile:
      | UserProfile
      | AuthenticatedUserResponse
      | null,
  ): void {
    const normalizedProfile =
      profile
        ? this.normalizeProfile(
            profile,
          )
        : null;

    this.currentUserState.set(
      normalizedProfile,
    );

    this.writeCachedProfile(
      normalizedProfile,
    );
  }

  refreshCurrentUser():
    Observable<UserProfile> {
    return this.fetchProfile();
  }

  getUserProfile():
    Observable<UserProfile> {
    return this.fetchProfile();
  }

  updateProfile(
    profileData: UserProfile,
    imageFile?: File,
  ): Observable<UserProfile> {
    const payload:
      UpdateProfilePayload = {
        firstName:
          profileData.firstName,

        lastName:
          profileData.lastName,

        phoneNumber:
          this.toNullableString(
            profileData.phoneNumber,
          ),

        gender:
          this.mapGenderToApi(
            profileData.gender,
          ),

        birthday:
          this.toNullableString(
            profileData.birthday,
          ),

        country:
          this.toNullableString(
            profileData.country,
          ),

        address:
          this.toNullableString(
            profileData.address,
          ),

        zipCode:
          this.toNullableString(
            profileData.zipCode,
          ),
      };

    this.loadingState.set(
      true,
    );

    return this.http
      .put<UserProfile>(
        this.profileApiUrl,
        payload,
      )
      .pipe(
        switchMap(
          (
            updatedProfile,
          ) => {
            if (!imageFile) {
              return of(
                updatedProfile,
              );
            }

            const formData =
              new FormData();

            formData.append(
              'file',
              imageFile,
              imageFile.name,
            );

            return this.http
              .post<UserProfile>(
                `${this.profileApiUrl}/image`,
                formData,
              );
          },
        ),

        map((profile) =>
          this.normalizeProfile(
            profile,
          ),
        ),

        tap((profile) => {
          this.setCurrentUser(
            profile,
          );
        }),

        finalize(() => {
          this.loadingState.set(
            false,
          );
        }),
      );
  }

  deleteProfileImage():
    Observable<UserProfile> {
    this.loadingState.set(
      true,
    );

    return this.http
      .delete<UserProfile>(
        `${this.profileApiUrl}/image`,
      )
      .pipe(
        map((profile) =>
          this.normalizeProfile(
            profile,
          ),
        ),

        tap((profile) => {
          this.setCurrentUser(
            profile,
          );
        }),

        finalize(() => {
          this.loadingState.set(
            false,
          );
        }),
      );
  }

  logoutCurrentDevice():
    Observable<void> {
    /*
     * The backend revokes the refresh token
     * represented by the HttpOnly cookie and
     * deletes that cookie from this browser.
     *
     * Local authentication state is cleared
     * whether the request succeeds or fails,
     * so the UI never remains authenticated
     * after the user explicitly chooses Logout.
     */
    return this.authService
      .logout()
      .pipe(
        finalize(() => {
          this.clearAuth();
        }),
      );
  }

  clearAuth(): void {
    localStorage.removeItem(
      this.tokenKey,
    );

    localStorage.removeItem(
      this.userNameKey,
    );

    localStorage.removeItem(
      this.profileKey,
    );

    this.accessTokenState.set(
      null,
    );

    this.currentUserState.set(
      null,
    );

    this.loadingState.set(
      false,
    );

    this.initializedState.set(
      true,
    );
  }

  getUserEmail():
    Observable<string> {
    return this.currentUser$
      .pipe(
        map((profile) =>
          profile?.email || '',
        ),
      );
  }

  private fetchProfile(
    options: {
      silent?: boolean;
    } = {},
  ): Observable<UserProfile> {
    if (!this.readToken()) {
      return throwError(
        () =>
          new Error(
            'No access token found.',
          ),
      );
    }

    if (!options.silent) {
      this.loadingState.set(
        true,
      );
    }

    return this.http
      .get<UserProfile>(
        this.profileApiUrl,
      )
      .pipe(
        map((profile) =>
          this.normalizeProfile(
            profile,
          ),
        ),

        tap((profile) => {
          this.setCurrentUser(
            profile,
          );
        }),

        catchError(
          (error: unknown) => {
            /*
             * The interceptor has already attempted
             * Refresh before an unauthorized response
             * reaches this point.
             */
            if (
              this.isUnauthorized(
                error,
              )
            ) {
              this.clearAuth();
            }

            return throwError(
              () => error,
            );
          },
        ),

        finalize(() => {
          if (!options.silent) {
            this.loadingState.set(
              false,
            );
          }
        }),
      );
  }

  private normalizeProfile(
    profile:
      | UserProfile
      | AuthenticatedUserResponse,
  ): UserProfile {
    return {
      id:
        'id' in profile
          ? profile.id
          : undefined,

      firstName:
        profile.firstName ?? '',

      lastName:
        profile.lastName ?? '',

      email:
        profile.email ?? '',

      phoneNumber:
        'phoneNumber' in profile
          ? profile.phoneNumber ??
            ''
          : '',

      gender:
        'gender' in profile
          ? this
              .normalizeGenderFromApi(
                profile.gender,
              )
          : '',

      birthday:
        'birthday' in profile
          ? this.normalizeDate(
              profile.birthday,
            )
          : '',

      country:
        'country' in profile
          ? profile.country ?? ''
          : '',

      address:
        'address' in profile
          ? profile.address ?? ''
          : '',

      zipCode:
        'zipCode' in profile
          ? profile.zipCode ?? ''
          : '',

      profileImageUrl:
        profile.profileImageUrl ??
        null,

      isProfileCompleted:
        profile.isProfileCompleted ??
        false,

      roles:
        profile.roles ?? [],

      createdAt:
        'createdAt' in profile
          ? profile.createdAt
          : undefined,

      updatedAt:
        'updatedAt' in profile
          ? profile.updatedAt
          : undefined,
    };
  }

  private readCachedProfile():
    UserProfile | null {
    const cachedValue =
      localStorage.getItem(
        this.profileKey,
      );

    if (!cachedValue) {
      return null;
    }

    try {
      return this.normalizeProfile(
        JSON.parse(
          cachedValue,
        ) as UserProfile,
      );
    } catch {
      localStorage.removeItem(
        this.profileKey,
      );

      return null;
    }
  }

  private writeCachedProfile(
    profile: UserProfile | null,
  ): void {
    if (!profile) {
      localStorage.removeItem(
        this.profileKey,
      );

      return;
    }

    localStorage.setItem(
      this.profileKey,
      JSON.stringify(
        profile,
      ),
    );
  }

  private isUnauthorized(
    error: unknown,
  ): boolean {
    return (
      error instanceof
        HttpErrorResponse &&
      error.status === 401
    );
  }

  private readToken():
    string | null {
    return localStorage.getItem(
      this.tokenKey,
    );
  }

  private toNullableString(
    value:
      | string
      | null
      | undefined,
  ): string | null {
    const normalizedValue =
      value?.trim();

    return normalizedValue
      ? normalizedValue
      : null;
  }

  private mapGenderToApi(
    gender:
      | string
      | null
      | undefined,
  ): number | null {
    const normalizedGender =
      gender
        ?.trim()
        .toLowerCase();

    if (
      normalizedGender ===
      'male'
    ) {
      return 1;
    }

    if (
      normalizedGender ===
      'female'
    ) {
      return 2;
    }

    return null;
  }

  private normalizeGenderFromApi(
    value: unknown,
  ): string {
    if (
      value === 1 ||
      value === '1'
    ) {
      return 'male';
    }

    if (
      value === 2 ||
      value === '2'
    ) {
      return 'female';
    }

    if (
      typeof value ===
      'string'
    ) {
      return value;
    }

    return '';
  }

  private normalizeDate(
    value:
      | string
      | null
      | undefined,
  ): string {
    if (!value) {
      return '';
    }

    return value.includes('T')
      ? value.split('T')[0]
      : value;
  }
}