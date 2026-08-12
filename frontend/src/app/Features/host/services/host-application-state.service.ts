import { Injectable, computed, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { AuthState } from '../../auth/services/auth-state';
import { Auth } from '../../auth/services/auth';
import { UserProfileService } from '../../profile/services/user-profile-service';
import {
  HostApplication,
  HostApplicationBasicInfo,
  HostApplicationStep,
  resolveStepFromApplication,
} from '../models/host-application.models';
import { HostApplicationService } from './host-application.service';

@Injectable({ providedIn: 'root' })
export class HostApplicationStateService {
  private readonly api = inject(HostApplicationService);
  private readonly authService = inject(Auth);
  private readonly authState = inject(AuthState);
  private readonly profileService = inject(UserProfileService);

  private hostSessionActivationPromise: Promise<boolean> | null = null;

  readonly application = signal<HostApplication | null>(null);

  readonly currentStep = signal<HostApplicationStep>(
    HostApplicationStep.Information,
  );

  readonly loading = signal(false);
  readonly activatingHostSession = signal(false);
  readonly error = signal<string | null>(null);

  readonly status = computed(() => this.application()?.status ?? null);
  readonly isPending = computed(() => this.status() === 'Pending');
  readonly isApproved = computed(() => this.status() === 'Approved');
  readonly isRejected = computed(() => this.status() === 'Rejected');
  readonly rejectionReason = computed(
    () => this.application()?.rejectionReason ?? null,
  );

  async init(): Promise<void> {
    this.loading.set(true);
    this.error.set(null);

    try {
      const application = await firstValueFrom(this.api.getCurrent());

      this.application.set(application);
      this.currentStep.set(resolveStepFromApplication(application));

      /*
       * Admin approval updates the Identity roles immediately, but an
       * already-issued access token still contains the previous role set.
       * Refresh the session explicitly when this page discovers an approved
       * application so the Host guard and Navbar receive the new Host role.
       */
      if (this.isApproved()) {
        await this.ensureApprovedHostSession();
      }
    } catch (error: unknown) {
      const status = this.getHttpStatus(error);

      if (status === 404) {
        this.application.set(null);
        this.currentStep.set(HostApplicationStep.Information);
      } else {
        this.error.set(
          this.extractErrorMessage(
            error,
            'Failed to load your host application.',
          ),
        );
      }
    } finally {
      this.loading.set(false);
    }
  }

  async saveBasicInfo(payload: HostApplicationBasicInfo): Promise<void> {
    this.loading.set(true);
    this.error.set(null);

    try {
      const existingApplication = this.application();

      const application = existingApplication
        ? await firstValueFrom(this.api.updateCurrent(payload))
        : await firstValueFrom(this.api.createDraft(payload));

      this.application.set(application);
      this.currentStep.set(HostApplicationStep.ProfileImage);
    } catch (error: unknown) {
      this.error.set(
        this.extractErrorMessage(
          error,
          'Failed to save your information.',
        ),
      );

      throw error;
    } finally {
      this.loading.set(false);
    }
  }

  async saveProfileImage(file: File): Promise<void> {
    this.loading.set(true);
    this.error.set(null);

    try {
      const application = await firstValueFrom(
        this.api.uploadProfileImage(file),
      );

      this.application.set(application);
      this.currentStep.set(HostApplicationStep.IdDocument);
    } catch (error: unknown) {
      this.error.set(
        this.extractErrorMessage(
          error,
          'Failed to upload the profile image.',
        ),
      );

      throw error;
    } finally {
      this.loading.set(false);
    }
  }

  async saveNationalId(frontFile: File, backFile: File): Promise<void> {
    this.loading.set(true);
    this.error.set(null);

    try {
      const application = await firstValueFrom(
        this.api.uploadNationalId(frontFile, backFile),
      );

      this.application.set(application);
      this.currentStep.set(HostApplicationStep.Review);
    } catch (error: unknown) {
      this.error.set(
        this.extractErrorMessage(
          error,
          'Failed to upload your national ID.',
        ),
      );

      throw error;
    } finally {
      this.loading.set(false);
    }
  }

  async submit(): Promise<void> {
    this.loading.set(true);
    this.error.set(null);

    try {
      const application = await firstValueFrom(this.api.submit());
      this.application.set(application);
    } catch (error: unknown) {
      this.error.set(
        this.extractErrorMessage(
          error,
          'Failed to submit your application.',
        ),
      );

      throw error;
    } finally {
      this.loading.set(false);
    }
  }

  ensureApprovedHostSession(): Promise<boolean> {
    if (!this.isApproved()) {
      this.error.set(
        'Your host application must be approved before opening the Host Dashboard.',
      );

      return Promise.resolve(false);
    }

    if (this.accessTokenHasHostRole()) {
      this.error.set(null);
      return Promise.resolve(true);
    }

    if (this.hostSessionActivationPromise) {
      return this.hostSessionActivationPromise;
    }

    this.hostSessionActivationPromise = this.refreshApprovedHostSession();

    return this.hostSessionActivationPromise;
  }

  goBack(): void {
    const step = this.currentStep();

    if (step > HostApplicationStep.Information) {
      this.currentStep.set(step - 1);
    }
  }

  restartFromRejection(): void {
    this.error.set(null);
    this.currentStep.set(HostApplicationStep.Information);
  }

  private async refreshApprovedHostSession(): Promise<boolean> {
    this.activatingHostSession.set(true);
    this.error.set(null);

    try {
      const authResponse = await firstValueFrom(this.authService.refresh());
      const accessToken = authResponse.accessToken?.trim();

      if (!accessToken) {
        throw new Error(
          'The refreshed authentication response did not contain an access token.',
        );
      }

      await firstValueFrom(
        this.profileService.setAccessToken(
          accessToken,
          authResponse.user,
        ),
      );

      if (!this.accessTokenHasHostRole()) {
        throw new Error(
          'The refreshed access token does not contain the Host role yet.',
        );
      }

      this.error.set(null);
      return true;
    } catch (error: unknown) {
      this.error.set(
        this.extractErrorMessage(
          error,
          'Your application is approved, but SmartStay could not refresh your Host access. Please try again.',
        ),
      );

      return false;
    } finally {
      this.activatingHostSession.set(false);
      this.hostSessionActivationPromise = null;
    }
  }

  private accessTokenHasHostRole(): boolean {
    return this.authState.getRoles().some(
      (role) => role.trim().toLowerCase() === 'host',
    );
  }

  private getHttpStatus(error: unknown): number | undefined {
    return (error as { status?: number })?.status;
  }

  private extractErrorMessage(error: unknown, fallback: string): string {
    const typedError = error as {
      error?: unknown;
      detail?: string;
      message?: string;
      title?: string;
    };

    if (typeof typedError.error === 'string') {
      const normalizedError = typedError.error
        .replace(/^\uFEFF/, '')
        .trim();

      if (normalizedError) {
        try {
          const parsedError = JSON.parse(normalizedError) as {
            detail?: string;
            message?: string;
            title?: string;
          };

          return (
            parsedError.detail ||
            parsedError.message ||
            parsedError.title ||
            fallback
          );
        } catch {
          return normalizedError;
        }
      }
    }

    if (typedError.error && typeof typedError.error === 'object') {
      const parsedError = typedError.error as {
        detail?: string;
        message?: string;
        title?: string;
      };

      return (
        parsedError.detail ||
        parsedError.message ||
        parsedError.title ||
        fallback
      );
    }

    return (
      typedError.detail ||
      typedError.message ||
      typedError.title ||
      fallback
    );
  }
}