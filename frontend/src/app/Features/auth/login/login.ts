import { CommonModule } from '@angular/common';
import {
  ChangeDetectorRef,
  Component,
  OnInit,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  ActivatedRoute,
  Router,
  RouterModule,
} from '@angular/router';
import { finalize } from 'rxjs/operators';

import { UserProfileService } from '../../profile/services/user-profile-service';
import { AuthState } from '../services/auth-state';
import {
  Auth,
  AuthNextStep,
  AuthResponse,
  ExternalAuthProvider,
} from '../services/auth';

interface GoogleCredentialResponse {
  credential?: string;
}

interface FacebookLoginResponse {
  authResponse?: {
    accessToken?: string;
  };
}

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterModule,
  ],
  templateUrl: './login.html',
  styleUrl: './login.css',
})
export class Login implements OnInit {
  step = 1;

  email = '';
  code = '';
  firstName = '';
  lastName = '';

  codeError = false;
  emailError = '';
  otpError = '';
  resendMessage = '';
  authenticationError = '';
  completeProfileError = '';

  loading = false;
  resendLoading = false;

  private returnUrl: string | null = null;

  readonly googleClientId =
    '706250915965-eeh1h434ie2lh2jgs4fpce0n339irvpe.apps.googleusercontent.com';

  readonly facebookAppId =
    '977673998422284';

  constructor(
    private readonly authService: Auth,
    private readonly authState: AuthState,
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly userProfileService: UserProfileService,
    private readonly cdr: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    this.returnUrl = this.normalizeReturnUrl(
      this.route.snapshot.queryParamMap.get(
        'returnUrl',
      ),
    );

    /*
     * Session initialization runs before the Router starts.
     * Do not show Login again when the access token or refresh
     * cookie has already restored a valid authenticated session.
     */
    if (this.authState.isLoggedIn()) {
      const currentUser =
        this.userProfileService.currentUser();

      /*
       * A restored authenticated session may still require the
       * mandatory first-name and last-name completion step.
       */
      if (
        currentUser?.isProfileCompleted ===
        false
      ) {
        this.firstName =
          currentUser.firstName?.trim() ??
          '';

        this.lastName =
          currentUser.lastName?.trim() ??
          '';

        this.step = 4;
        this.cdr.detectChanges();

        return;
      }

      this.navigateAfterAuthentication(
        undefined,
        this.authState.getRoles(),
      );

      return;
    }

    this.initializeFacebookLogin();
    this.initializeGoogleLogin();
  }

  onContinueEmail(
    emailValue: string,
  ): void {
    if (this.loading) {
      return;
    }

    const normalizedEmail =
      emailValue.trim();

    if (!normalizedEmail) {
      this.emailError =
        'Email is required.';

      return;
    }

    if (
      normalizedEmail.length > 256 ||
      !this.isValidEmail(
        normalizedEmail,
      )
    ) {
      this.emailError =
        'Enter a valid email address.';

      return;
    }

    this.email = normalizedEmail;

    this.emailError = '';
    this.authenticationError = '';
    this.resendMessage = '';

    this.loading = true;

    this.authService
      .sendOtp(this.email)
      .pipe(
        finalize(() => {
          this.loading = false;
          this.cdr.detectChanges();
        }),
      )
      .subscribe({
        next: () => {
          this.code = '';
          this.codeError = false;
          this.otpError = '';
          this.step = 2;
        },

        error: (error: unknown) => {
          this.emailError =
            this.extractErrorMessage(
              error,
              'Unable to send the verification code. Please try again.',
            );
        },
      });
  }

  onVerifyCode(): void {
    if (this.loading) {
      return;
    }

    const normalizedCode =
      this.code
        .replace(/\D/g, '')
        .slice(0, 6);

    this.code = normalizedCode;

    if (
      !/^\d{6}$/.test(
        normalizedCode,
      )
    ) {
      this.codeError = true;

      this.otpError =
        'Enter the complete 6-digit verification code.';

      this.step = 3;
      this.cdr.detectChanges();

      return;
    }

    this.codeError = false;
    this.otpError = '';
    this.authenticationError = '';
    this.loading = true;

    this.authService
      .verifyOtp(
        this.email,
        normalizedCode,
      )
      .subscribe({
        next: (
          response: AuthResponse,
        ) => {
          this.handleAuthResponse(
            response,
          );
        },

        error: (error: unknown) => {
          this.loading = false;
          this.codeError = true;

          this.otpError =
            this.extractErrorMessage(
              error,
              'The verification code is invalid or expired.',
            );

          this.step = 3;
          this.cdr.detectChanges();
        },
      });
  }

  onRetryCode(): void {
    if (this.loading) {
      return;
    }

    this.code = '';
    this.codeError = false;
    this.otpError = '';
    this.authenticationError = '';
    this.step = 2;

    this.cdr.detectChanges();
  }

  onResendOtp(): void {
    if (
      !this.email ||
      this.loading ||
      this.resendLoading
    ) {
      return;
    }

    this.resendMessage = '';
    this.resendLoading = true;

    this.authService
      .resendOtp(this.email)
      .pipe(
        finalize(() => {
          this.resendLoading = false;
          this.cdr.detectChanges();
        }),
      )
      .subscribe({
        next: () => {
          this.code = '';
          this.codeError = false;
          this.otpError = '';

          this.resendMessage =
            'Code sent successfully!';
        },

        error: (error: unknown) => {
          this.resendMessage =
            this.extractErrorMessage(
              error,
              'Unable to resend the verification code. Please try again.',
            );
        },
      });
  }

  onFinish(): void {
    if (this.loading) {
      return;
    }

    const normalizedFirstName =
      this.firstName.trim();

    const normalizedLastName =
      this.lastName.trim();

    if (
      normalizedFirstName.length < 2 ||
      normalizedFirstName.length > 100
    ) {
      this.completeProfileError =
        'First name must contain between 2 and 100 characters.';

      return;
    }

    if (
      normalizedLastName.length < 2 ||
      normalizedLastName.length > 100
    ) {
      this.completeProfileError =
        'Last name must contain between 2 and 100 characters.';

      return;
    }

    this.firstName =
      normalizedFirstName;

    this.lastName =
      normalizedLastName;

    this.completeProfileError = '';
    this.authenticationError = '';
    this.loading = true;

    this.authService
      .completeProfile(
        normalizedFirstName,
        normalizedLastName,
      )
      .pipe(
        finalize(() => {
          this.loading = false;
          this.cdr.detectChanges();
        }),
      )
      .subscribe({
        next: (user) => {
          this.userProfileService
            .setCurrentUser(user);

          this.navigateAfterAuthentication(
            undefined,
            user.roles ?? [],
          );
        },

        error: (error: unknown) => {
          this.completeProfileError =
            this.extractErrorMessage(
              error,
              'Unable to complete your profile. Please try again.',
            );
        },
      });
  }

  onGoogleLogin(): void {
    if (this.loading) {
      return;
    }

    this.authenticationError = '';

    const googleAccounts =
      (window as any)
        .google
        ?.accounts
        ?.id;

    if (!googleAccounts) {
      this.authenticationError =
        'Google sign-in is currently unavailable. Refresh the page and try again.';

      this.cdr.detectChanges();
      return;
    }

    googleAccounts.prompt();
  }

  onFacebookLogin(): void {
    if (this.loading) {
      return;
    }

    this.authenticationError = '';

    const facebookApi =
      (window as any).FB;

    if (!facebookApi) {
      this.authenticationError =
        'Facebook sign-in is currently unavailable. Refresh the page and try again.';

      this.cdr.detectChanges();
      return;
    }

    this.loading = true;

    facebookApi.login(
      (
        response:
          FacebookLoginResponse,
      ) => {
        const token =
          response.authResponse
            ?.accessToken
            ?.trim();

        if (!token) {
          this.loading = false;

          this.authenticationError =
            'Facebook sign-in was cancelled or could not be authorized.';

          this.cdr.detectChanges();
          return;
        }

        this.startExternalLogin(
          'Facebook',
          token,
        );
      },
      {
        scope: 'email',
      },
    );
  }

  private handleGoogleLogin(
    response:
      GoogleCredentialResponse,
  ): void {
    const idToken =
      response.credential?.trim();

    if (!idToken) {
      this.authenticationError =
        'Google sign-in did not return a valid credential.';

      this.cdr.detectChanges();
      return;
    }

    this.startExternalLogin(
      'Google',
      idToken,
    );
  }

  private startExternalLogin(
    provider:
      ExternalAuthProvider,

    token: string,
  ): void {
    this.loading = true;
    this.authenticationError = '';

    this.authService
      .externalLogin(
        provider,
        token,
      )
      .subscribe({
        next: (
          response: AuthResponse,
        ) => {
          this.handleAuthResponse(
            response,
          );
        },

        error: (error: unknown) => {
          this.loading = false;

          this.authenticationError =
            this.extractErrorMessage(
              error,
              `${provider} sign-in failed. Please try again.`,
            );

          this.cdr.detectChanges();
        },
      });
  }

  private handleAuthResponse(
    response: AuthResponse,
  ): void {
    const accessToken =
      response.accessToken?.trim();

    if (
      !accessToken ||
      !response.user
    ) {
      this.loading = false;

      this.authenticationError =
        'The authentication server returned an incomplete response.';

      this.userProfileService.clearAuth();
      this.cdr.detectChanges();

      return;
    }

    this.userProfileService
      .setAccessToken(
        accessToken,
        response.user,
      )
      .pipe(
        finalize(() => {
          this.loading = false;
          this.cdr.detectChanges();
        }),
      )
      .subscribe({
        next: (profile) => {
          const normalizedNextStep =
            response.nextStep
              ?.trim()
              .toLowerCase();

          const isProfileCompleted =
            profile?.isProfileCompleted ??
            response.user
              .isProfileCompleted;

          /*
           * Do not use isNewUser by itself.
           *
           * External providers may create a new account and provide
           * both names immediately. In that case the Backend can return:
           *
           * isNewUser = true
           * isProfileCompleted = true
           * nextStep = properties
           */
          const shouldCompleteProfile =
            normalizedNextStep ===
              'complete_profile' ||
            !isProfileCompleted;

          if (shouldCompleteProfile) {
            this.firstName =
              profile?.firstName?.trim() ||
              response.user.firstName?.trim() ||
              '';

            this.lastName =
              profile?.lastName?.trim() ||
              response.user.lastName?.trim() ||
              '';

            this.completeProfileError = '';
            this.step = 4;

            return;
          }

          this.navigateAfterAuthentication(
            response.nextStep,
            profile?.roles ??
              response.user.roles ??
              [],
          );
        },

        error: (error: unknown) => {
          this.userProfileService.clearAuth();

          this.authenticationError =
            this.extractErrorMessage(
              error,
              'Unable to initialize your authenticated session.',
            );
        },
      });
  }

  private navigateAfterAuthentication(
    nextStep?: AuthNextStep,
    roles: string[] = [],
  ): void {
    const destination =
      this.returnUrl ??
      this.resolveDefaultDestination(
        nextStep,
        roles,
      );

    void this.router.navigateByUrl(
      destination,
      {
        replaceUrl: true,
      },
    );
  }

  private resolveDefaultDestination(
    nextStep?: AuthNextStep,
    roles: string[] = [],
  ): string {
    switch (nextStep) {
      case 'admin_dashboard':
        return '/admin/dashboard';

      case 'host_dashboard':
        return '/host/dashboard';

      case 'properties':
        return '/';

      case 'complete_profile':
      default:
        return this.resolveDestinationFromRoles(
          roles,
        );
    }
  }

  private resolveDestinationFromRoles(
    roles: string[],
  ): string {
    const normalizedRoles =
      roles.map((role) =>
        role
          .trim()
          .toLowerCase(),
      );

    if (
      normalizedRoles.includes(
        'admin',
      )
    ) {
      return '/admin/dashboard';
    }

    if (
      normalizedRoles.includes(
        'host',
      )
    ) {
      return '/host/dashboard';
    }

    /*
     * A normal authenticated User returns to Home.
     * Becoming a Host remains an explicit user action.
     */
    return '/';
  }

  private normalizeReturnUrl(
    returnUrl: string | null,
  ): string | null {
    const normalizedReturnUrl =
      (returnUrl ?? '').trim();

    if (
      !normalizedReturnUrl ||
      !normalizedReturnUrl.startsWith(
        '/',
      ) ||
      normalizedReturnUrl.startsWith(
        '//',
      )
    ) {
      return null;
    }

    try {
      const urlTree =
        this.router.parseUrl(
          normalizedReturnUrl,
        );

      const primarySegments =
        urlTree.root
          .children['primary']
          ?.segments ?? [];

      const primaryPath =
        primarySegments
          .map(
            (segment) =>
              segment.path,
          )
          .join('/')
          .toLowerCase();

      /*
       * Prevent redirecting back to Login and creating
       * a successful-authentication navigation loop.
       */
      if (
        primaryPath === 'login' ||
        primaryPath.startsWith(
          'login/',
        )
      ) {
        return null;
      }

      return this.router.serializeUrl(
        urlTree,
      );
    } catch {
      return null;
    }
  }

  private isValidEmail(
    email: string,
  ): boolean {
    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(
      email,
    );
  }

  private extractErrorMessage(
    error: unknown,
    fallbackMessage: string,
  ): string {
    const typedError =
      error as {
        error?: unknown;
        message?: string;
      };

    if (
      typeof typedError.error ===
      'string'
    ) {
      const normalizedError =
        typedError.error
          .replace(/^\uFEFF/, '')
          .trim();

      if (!normalizedError) {
        return (
          typedError.message ||
          fallbackMessage
        );
      }

      try {
        return (
          this.extractProblemDetailsMessage(
            JSON.parse(
              normalizedError,
            ) as unknown,
          ) ||
          normalizedError
        );
      } catch {
        return normalizedError;
      }
    }

    if (
      typedError.error &&
      typeof typedError.error ===
        'object'
    ) {
      return (
        this.extractProblemDetailsMessage(
          typedError.error,
        ) ||
        typedError.message ||
        fallbackMessage
      );
    }

    return (
      typedError.message ||
      fallbackMessage
    );
  }

  private extractProblemDetailsMessage(
    value: unknown,
  ): string {
    if (
      !value ||
      typeof value !== 'object'
    ) {
      return '';
    }

    const problem =
      value as {
        detail?: string;
        message?: string;
        title?: string;
        errors?: Record<
          string,
          string[]
        >;
      };

    const firstValidationError =
      problem.errors
        ? Object.values(
            problem.errors,
          )[0]?.[0]
        : undefined;

    return (
      problem.detail ||
      problem.message ||
      firstValidationError ||
      problem.title ||
      ''
    );
  }

  private initializeGoogleLogin(): void {
    setTimeout(() => {
      const button =
        document.getElementById(
          'google-btn',
        );

      const googleAccounts =
        (window as any)
          .google
          ?.accounts
          ?.id;

      if (!googleAccounts) {
        return;
      }

      googleAccounts.initialize({
        client_id:
          this.googleClientId,

        callback: (
          response:
            GoogleCredentialResponse,
        ) =>
          this.handleGoogleLogin(
            response,
          ),
      });

      if (button) {
        googleAccounts.renderButton(
          button,
          {
            theme: 'outline',
            size: 'large',
            width: '100%',
            locale: 'en',
          },
        );
      }
    }, 500);
  }

  private initializeFacebookLogin(): void {
    const facebookApi =
      (window as any).FB;

    if (!facebookApi) {
      return;
    }

    facebookApi.init({
      appId: this.facebookAppId,
      cookie: true,
      xfbml: true,
      version: 'v21.0',
    });
  }
}