import { HttpErrorResponse } from '@angular/common/http';
import {
  AfterViewInit,
  ChangeDetectorRef,
  Component,
  ElementRef,
  OnDestroy,
  OnInit,
  ViewChild,
  inject,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { Subscription, TimeoutError, interval } from 'rxjs';

import { environment } from '../../../../../environments/environment';
import {
  PaymentService,
  StartPaymentResponse,
} from '../../services/payment.service';

interface StripePaymentError {
  message?: string;
}

interface StripePaymentElement {
  mount(selectorOrElement: string | HTMLElement): void;
  unmount(): void;
  destroy?(): void;
}

interface StripeElementsInstance {
  create(
    type: 'payment',
    options?: Record<string, unknown>,
  ): StripePaymentElement;

  submit?(): Promise<{
    error?: StripePaymentError;
  }>;
}

interface StripeInstance {
  elements(options: {
    clientSecret: string;
    appearance?: Record<string, unknown>;
  }): StripeElementsInstance;

  confirmPayment(options: {
    elements: StripeElementsInstance;

    confirmParams: {
      return_url: string;
    };

    redirect?: 'always' | 'if_required';
  }): Promise<{
    error?: StripePaymentError;
  }>;
}

declare global {
  interface Window {
    Stripe?: (
      publishableKey: string,
    ) => StripeInstance | null;
  }
}

@Component({
  selector: 'app-checkout',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './checkout.html',
  styleUrl: './checkout.css',
})
export class Checkout
  implements OnInit, AfterViewInit, OnDestroy
{
  @ViewChild('paymentElementContainer')
  paymentElementContainer?: ElementRef<HTMLDivElement>;

  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly paymentService = inject(PaymentService);
  private readonly cdr = inject(ChangeDetectorRef);

  bookingId = '';
  paymentId = '';

  paymentResponse: StartPaymentResponse | null = null;

  isPreparingPayment = false;
  isPaymentReady = false;
  isPaying = false;

  loadingMessage = 'Preparing secure payment form...';
  errorMessage = '';

  remainingPaymentSeconds = 0;
  isPaymentExpired = false;
  isPaymentDeadlineUnavailable = false;

  private stripe: StripeInstance | null = null;
  private elements: StripeElementsInstance | null = null;
  private paymentElement: StripePaymentElement | null = null;

  private paymentPreparationSubscription?: Subscription;
  private paymentCountdownSubscription?: Subscription;

  private isDestroyed = false;

  ngOnInit(): void {
    this.bookingId =
      this.route.snapshot.paramMap.get('bookingId') ?? '';

    if (!this.bookingId) {
      this.errorMessage =
        'Booking id is missing from checkout URL.';
    }
  }

  ngAfterViewInit(): void {
    if (!this.bookingId) {
      this.cdr.detectChanges();
      return;
    }

    queueMicrotask(() => {
      this.preparePayment();
    });
  }

  ngOnDestroy(): void {
    this.isDestroyed = true;

    this.paymentPreparationSubscription?.unsubscribe();
    this.paymentPreparationSubscription = undefined;

    this.stopPaymentCountdown();
    this.destroyPaymentElement();
  }

  payNow(): void {
    this.updatePaymentCountdown();

    if (
      this.isPaymentExpired ||
      this.isPaymentDeadlineUnavailable
    ) {
      this.errorMessage = this.isPaymentExpired
        ? 'The booking payment window has expired. Return to My Bookings to review its current status.'
        : 'SmartStay could not verify the booking payment deadline. Return to My Bookings and try again.';

      this.cdr.detectChanges();
      return;
    }

    if (
      !this.stripe ||
      !this.elements ||
      !this.paymentId
    ) {
      this.errorMessage =
        'Payment form is not ready yet. Please wait a moment and try again.';

      this.cdr.detectChanges();
      return;
    }

    this.isPaying = true;
    this.errorMessage = '';

    this.cdr.detectChanges();

    this.confirmStripePayment()
      .then(() => {
        this.clearStoredIdempotencyKey(this.bookingId);

        void this.router.navigate([
          '/booking-confirmation',
          this.paymentId,
        ]);
      })
      .catch((error) => {
        this.errorMessage = this.getClientErrorMessage(
          error,
          'Payment failed. Please check your card details and try again.',
        );
      })
      .finally(() => {
        this.isPaying = false;

        if (!this.isDestroyed) {
          this.cdr.detectChanges();
        }
      });
  }

  retryPreparePayment(): void {
    if (this.isPaymentExpired) {
      this.goToMyBookings();
      return;
    }

    this.preparePayment();
  }

  goToMyBookings(): void {
    void this.router.navigate([
      '/profile/bookings/all',
    ]);
  }

  get paymentCountdownLabel(): string {
    const totalSeconds = Math.max(
      0,
      this.remainingPaymentSeconds,
    );

    const minutes = Math.floor(totalSeconds / 60);
    const seconds = totalSeconds % 60;

    return `${String(minutes).padStart(
      2,
      '0',
    )}:${String(seconds).padStart(2, '0')}`;
  }

  get isPaymentWindowUrgent(): boolean {
    return (
      !this.isPaymentExpired &&
      !this.isPaymentDeadlineUnavailable &&
      this.remainingPaymentSeconds > 0 &&
      this.remainingPaymentSeconds <= 300
    );
  }

  get canPayNow(): boolean {
    return (
      this.isPaymentReady &&
      !this.isPreparingPayment &&
      !this.isPaying &&
      !this.isPaymentExpired &&
      !this.isPaymentDeadlineUnavailable
    );
  }

  private preparePayment(): void {
    if (this.isPreparingPayment) {
      return;
    }

    this.paymentPreparationSubscription?.unsubscribe();
    this.paymentPreparationSubscription = undefined;

    this.stopPaymentCountdown();
    this.destroyPaymentElement();

    this.paymentResponse = null;
    this.paymentId = '';

    this.remainingPaymentSeconds = 0;
    this.isPaymentExpired = false;
    this.isPaymentDeadlineUnavailable = false;

    this.isPreparingPayment = true;
    this.isPaymentReady = false;
    this.errorMessage = '';

    this.loadingMessage =
      'Preparing secure payment form...';

    this.cdr.detectChanges();

    const idempotencyKey =
      this.getOrCreateIdempotencyKey(this.bookingId);

    this.paymentPreparationSubscription =
      this.paymentService
        .startPayment(
          this.bookingId,
          idempotencyKey,
        )
        .subscribe({
          next: async (response) => {
            if (this.isDestroyed) {
              return;
            }

            try {
              this.paymentResponse = response;
              this.paymentId = response.paymentId;

              if (!response.clientSecret) {
                throw new Error(
                  'Payment API did not return a Stripe client secret.',
                );
              }

              if (
                !this.startPaymentCountdown(
                  response.bookingExpiresAt,
                )
              ) {
                return;
              }

              await this.loadStripeJs();

              if (
                this.isDestroyed ||
                this.isPaymentExpired
              ) {
                return;
              }

              this.initializeStripe(
                response.clientSecret,
              );

              this.isPaymentReady = true;
              this.loadingMessage =
                'Payment form is ready.';

              if (!this.isDestroyed) {
                this.cdr.detectChanges();
              }
            } catch (error) {
              this.errorMessage =
                this.getClientErrorMessage(
                  error,
                  'Failed to prepare Stripe payment form.',
                );
            } finally {
              this.isPreparingPayment = false;

              if (!this.isDestroyed) {
                this.cdr.detectChanges();
              }
            }
          },

          error: (error) => {
            if (this.isDestroyed) {
              return;
            }

            this.isPreparingPayment = false;

            this.errorMessage =
              this.getApiErrorMessage(
                error,
                'Failed to start payment for this booking.',
              );

            if (
              this.isBookingPaymentWindowExpiredError(
                error,
              )
            ) {
              this.markPaymentWindowExpired();
            }

            if (!this.isDestroyed) {
              this.cdr.detectChanges();
            }
          },
        });
  }

  private initializeStripe(
    clientSecret: string,
  ): void {
    const publishableKey =
      environment.stripePublishableKey;

    if (
      !publishableKey ||
      publishableKey.includes(
        'PUT_YOUR_STRIPE_PUBLISHABLE_KEY_HERE',
      ) ||
      !publishableKey.startsWith('pk_')
    ) {
      throw new Error(
        'Stripe publishable key is missing. Add a valid pk_test_ key in environment.ts.',
      );
    }

    const stripeInstance =
      window.Stripe?.(publishableKey);

    if (!stripeInstance) {
      throw new Error(
        'Stripe.js failed to initialize.',
      );
    }

    const container =
      this.paymentElementContainer?.nativeElement;

    if (!container) {
      throw new Error(
        'Payment element container was not found.',
      );
    }

    this.stripe = stripeInstance;

    this.elements = stripeInstance.elements({
      clientSecret,

      appearance: {
        theme: 'stripe',
      },
    });

    this.paymentElement =
      this.elements.create('payment', {
        layout: 'tabs',
      });

    try {
      this.paymentElement.mount(container);
    } catch (mountError) {
      console.error(
        'Stripe payment element mount failed:',
        mountError,
      );

      throw new Error(
        'Stripe payment element failed to load. Please check your Stripe configuration and client secret.',
      );
    }
  }

  private async confirmStripePayment(): Promise<void> {
    if (
      !this.stripe ||
      !this.elements ||
      !this.paymentId
    ) {
      throw new Error(
        'Stripe payment form is not ready.',
      );
    }

    if (this.elements.submit) {
      const submitResult =
        await this.elements.submit();

      if (submitResult.error) {
        throw new Error(
          submitResult.error.message ||
            'Please check your payment details.',
        );
      }
    }

    const result =
      await this.stripe.confirmPayment({
        elements: this.elements,

        confirmParams: {
          return_url:
            `${window.location.origin}` +
            `/booking-confirmation/${this.paymentId}`,
        },

        redirect: 'if_required',
      });

    if (result.error) {
      throw new Error(
        result.error.message ||
          'Stripe could not confirm this payment.',
      );
    }
  }

  private loadStripeJs(): Promise<void> {
    return new Promise((resolve, reject) => {
      if (window.Stripe) {
        resolve();
        return;
      }

      const existingScript =
        document.querySelector<HTMLScriptElement>(
          'script[src="https://js.stripe.com/v3/"]',
        );

      if (existingScript) {
        existingScript.addEventListener(
          'load',
          () => {
            if (window.Stripe) {
              resolve();
            } else {
              reject(
                new Error(
                  'Stripe.js loaded but window.Stripe is not available.',
                ),
              );
            }
          },
        );

        existingScript.addEventListener(
          'error',
          () =>
            reject(
              new Error(
                'Failed to load Stripe.js script.',
              ),
            ),
        );

        return;
      }

      const script =
        document.createElement('script');

      script.src =
        'https://js.stripe.com/v3/';

      script.async = true;

      script.onload = () => {
        if (window.Stripe) {
          resolve();
        } else {
          reject(
            new Error(
              'Stripe.js loaded but window.Stripe is not available.',
            ),
          );
        }
      };

      script.onerror = () =>
        reject(
          new Error(
            'Failed to load Stripe.js script.',
          ),
        );

      document.body.appendChild(script);
    });
  }

  private getOrCreateIdempotencyKey(
    bookingId: string,
  ): string {
    const storageKey =
      `smartstay-payment-idempotency-${bookingId}`;

    try {
      const existingKey =
        localStorage.getItem(storageKey);

      if (existingKey) {
        return existingKey;
      }

      const randomPart =
        typeof crypto !== 'undefined' &&
        'randomUUID' in crypto
          ? crypto.randomUUID()
          : String(Date.now());

      const newKey =
        `booking-${bookingId}-${randomPart}`.slice(
          0,
          100,
        );

      localStorage.setItem(
        storageKey,
        newKey,
      );

      return newKey;
    } catch {
      return `booking-${bookingId}`.slice(
        0,
        100,
      );
    }
  }

  private destroyPaymentElement(): void {
    if (this.paymentElement) {
      try {
        this.paymentElement.unmount();

        if (this.paymentElement.destroy) {
          this.paymentElement.destroy();
        }
      } catch {
        // Ignore cleanup errors.
      }
    }

    this.paymentElement = null;
    this.elements = null;
    this.stripe = null;
    this.isPaymentReady = false;
  }

  private startPaymentCountdown(
    expiresAt?: string | null,
  ): boolean {
    this.stopPaymentCountdown();

    const expirationTimestamp =
      Date.parse(expiresAt ?? '');

    if (!Number.isFinite(expirationTimestamp)) {
      this.isPaymentDeadlineUnavailable = true;
      this.isPaymentExpired = false;
      this.remainingPaymentSeconds = 0;

      this.errorMessage =
        'SmartStay could not verify the booking payment deadline. Return to My Bookings and try again.';

      this.destroyPaymentElement();
      this.cdr.detectChanges();

      return false;
    }

    this.isPaymentDeadlineUnavailable = false;

    this.updatePaymentCountdown(
      expirationTimestamp,
    );

    if (this.isPaymentExpired) {
      return false;
    }

    this.paymentCountdownSubscription =
      interval(1000).subscribe(() => {
        this.updatePaymentCountdown(
          expirationTimestamp,
        );

        if (!this.isDestroyed) {
          this.cdr.detectChanges();
        }
      });

    return true;
  }

  private updatePaymentCountdown(
    expirationTimestamp?: number,
  ): void {
    const resolvedExpirationTimestamp =
      expirationTimestamp ??
      Date.parse(
        this.paymentResponse?.bookingExpiresAt ??
          '',
      );

    if (
      !Number.isFinite(
        resolvedExpirationTimestamp,
      )
    ) {
      return;
    }

    const remainingMilliseconds =
      resolvedExpirationTimestamp -
      Date.now();

    this.remainingPaymentSeconds =
      Math.max(
        0,
        Math.ceil(
          remainingMilliseconds / 1000,
        ),
      );

    if (
      this.remainingPaymentSeconds <= 0
    ) {
      this.markPaymentWindowExpired();
    }
  }

  private markPaymentWindowExpired(): void {
    if (this.isPaymentExpired) {
      return;
    }

    this.isPaymentExpired = true;
    this.isPaymentDeadlineUnavailable = false;
    this.remainingPaymentSeconds = 0;

    this.stopPaymentCountdown();

    this.clearStoredIdempotencyKey(
      this.bookingId,
    );

    if (!this.isPaying) {
      this.destroyPaymentElement();
    }

    this.errorMessage =
      'The booking payment window has expired. Return to My Bookings to review its current status.';
  }

  private stopPaymentCountdown(): void {
    this.paymentCountdownSubscription?.unsubscribe();
    this.paymentCountdownSubscription =
      undefined;
  }

  private isBookingPaymentWindowExpiredError(
    error: unknown,
  ): boolean {
    return this.extractBackendMessage(error)
      .toLowerCase()
      .includes(
        'payment window has expired',
      );
  }

  private clearStoredIdempotencyKey(
    bookingId: string,
  ): void {
    if (!bookingId) {
      return;
    }

    try {
      localStorage.removeItem(
        `smartstay-payment-idempotency-${bookingId}`,
      );
    } catch {
      // Ignore storage cleanup failures.
    }
  }

  private getApiErrorMessage(
    error: unknown,
    fallbackMessage: string,
  ): string {
    if (error instanceof TimeoutError) {
      return 'Payment request took too long. Please try again.';
    }

    const possibleError = error as {
      error?: unknown;
      message?: string;
      status?: number;
      name?: string;
    };

    if (possibleError.status === 401) {
      return 'Please login again before completing payment.';
    }

    if (possibleError.status === 403) {
      return 'This account is not allowed to pay for this booking.';
    }

    if (possibleError.status === 404) {
      return 'Booking was not found or does not belong to your account.';
    }

    if (possibleError.status === 409) {
      return (
        this.extractBackendMessage(error) ||
        'This booking cannot be paid right now. It may be expired, cancelled, or already processed.'
      );
    }

    const backendMessage =
      this.extractBackendMessage(error);

    if (backendMessage) {
      return backendMessage;
    }

    return (
      possibleError.message ||
      fallbackMessage
    );
  }

  private getClientErrorMessage(
    error: unknown,
    fallbackMessage: string,
  ): string {
    if (
      error instanceof HttpErrorResponse
    ) {
      const backendMessage =
        this.extractBackendMessage(error);

      if (backendMessage) {
        return backendMessage;
      }

      if (error.status === 0) {
        return 'Unable to reach the payment server. Please check your network and try again.';
      }
    }

    if (
      error instanceof Error &&
      error.message
    ) {
      return error.message;
    }

    return fallbackMessage;
  }

  private extractBackendMessage(
    error: unknown,
  ): string {
    const possibleError = error as {
      error?: unknown;
      message?: string;
    };

    if (
      typeof possibleError.error ===
      'string'
    ) {
      const normalizedError =
        possibleError.error
          .replace(/^\uFEFF/, '')
          .trim();

      if (!normalizedError) {
        return possibleError.message || '';
      }

      try {
        const parsedError =
          JSON.parse(normalizedError) as {
            message?: string;
            title?: string;
            detail?: string;
            errors?: Record<
              string,
              string[]
            >;
          };

        if (parsedError.message) {
          return parsedError.message;
        }

        if (parsedError.detail) {
          return parsedError.detail;
        }

        if (parsedError.title) {
          return parsedError.title;
        }

        if (parsedError.errors) {
          const firstError =
            Object.values(
              parsedError.errors,
            )[0]?.[0];

          if (firstError) {
            return firstError;
          }
        }
      } catch {
        return normalizedError;
      }
    }

    if (
      possibleError.error &&
      typeof possibleError.error ===
        'object'
    ) {
      const errorObject =
        possibleError.error as {
          message?: string;
          title?: string;
          detail?: string;
          errors?: Record<
            string,
            string[]
          >;
        };

      if (errorObject.message) {
        return errorObject.message;
      }

      if (errorObject.detail) {
        return errorObject.detail;
      }

      if (errorObject.title) {
        return errorObject.title;
      }

      if (errorObject.errors) {
        const firstError =
          Object.values(
            errorObject.errors,
          )[0]?.[0];

        if (firstError) {
          return firstError;
        }
      }
    }

    return possibleError.message || '';
  }
}