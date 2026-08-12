import { CommonModule } from '@angular/common';
import {
  ChangeDetectorRef,
  Component,
  OnDestroy,
  OnInit,
} from '@angular/core';
import {
  ActivatedRoute,
  RouterModule,
} from '@angular/router';
import {
  Subscription,
  catchError,
  finalize,
  map,
  of,
  switchMap,
  timer,
} from 'rxjs';

import {
  PaymentService,
  PaymentStatus,
  PaymentStatusResponse,
} from '../../services/payment.service';
import {
  GuestBookingConfirmationResponse,
  GuestBookingsService,
} from '../../../profile/services/guest-bookings';
import {
  AiRecommendationService,
  NearbyRecommendation,
} from '../../../../Shared/services/ai-recommendation.service';

interface ConfirmationLoadResult {
  status: PaymentStatusResponse;
  confirmation:
    GuestBookingConfirmationResponse | null;
  confirmationWarning: string;
}

@Component({
  selector: 'app-booking-confirmation',
  imports: [
    CommonModule,
    RouterModule,
  ],
  templateUrl:
    './booking-confirmation.html',
  styleUrls: [
    './booking-confirmation.css',
  ],
})
export class BookingConfirmation
  implements OnInit, OnDestroy {
  paymentId = '';

  paymentStatus:
    PaymentStatusResponse | null =
      null;

  bookingConfirmation:
    GuestBookingConfirmationResponse | null =
      null;

  isLoading = true;
  isRefreshingStatus = false;

  errorMessage = '';
  warningMessage = '';

  isAutoChecking = false;
  autoCheckMessage = '';

  recommendations:
    NearbyRecommendation[] =
      [];

  isLoadingRecommendations =
    false;

  recommendationsError =
    '';

  selectedCategory:
    string | null =
      null;

  readonly categoryOptions = [
    'coffee',
    'restaurants',
  ];

  private statusRequestSubscription?:
    Subscription;

  private pollingSubscription?:
    Subscription;

  private recommendationsSubscription?:
    Subscription;

  private pollingAttempts = 0;

  private readonly pollingIntervalMs =
    2000;

  private readonly maxPollingAttempts =
    15;

  constructor(
    private readonly route:
      ActivatedRoute,

    private readonly paymentService:
      PaymentService,

    private readonly guestBookingsService:
      GuestBookingsService,

    private readonly aiRecommendationService:
      AiRecommendationService,

    private readonly cdr:
      ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    this.paymentId = (
      this.route.snapshot.paramMap.get(
        'paymentId',
      ) ?? ''
    ).trim();

    if (
      !this.isGuid(
        this.paymentId,
      )
    ) {
      this.errorMessage =
        'The payment identifier is missing or invalid.';

      this.isLoading =
        false;

      this.cdr.detectChanges();

      return;
    }

    this.loadConfirmationData(
      true,
    );
  }

  ngOnDestroy(): void {
    this.statusRequestSubscription
      ?.unsubscribe();

    this.statusRequestSubscription =
      undefined;

    this.recommendationsSubscription
      ?.unsubscribe();

    this.recommendationsSubscription =
      undefined;

    this.stopAutoChecking();
  }

  loadConfirmationData(
    showLoading = true,
  ): void {
    if (
      this.isRefreshingStatus ||
      !this.paymentId
    ) {
      return;
    }

    if (showLoading) {
      this.stopAutoChecking();

      this.isLoading =
        true;

      this.warningMessage =
        '';

      this.errorMessage =
        '';
    }

    this.isRefreshingStatus =
      true;

    this.cdr.detectChanges();

    this.statusRequestSubscription =
      this.paymentService
        .getPaymentStatus(
          this.paymentId,
        )
        .pipe(
          switchMap(
            (
              status,
            ) => {
              this.paymentStatus =
                status;

              if (
                !this
                  .shouldLoadBookingConfirmation(
                    status,
                  )
              ) {
                return of<ConfirmationLoadResult>({
                  status,
                  confirmation:
                    null,
                  confirmationWarning:
                    '',
                });
              }

              return this
                .guestBookingsService
                .getBookingConfirmation(
                  status.bookingId,
                )
                .pipe(
                  map(
                    (
                      confirmation,
                    ) => ({
                      status,
                      confirmation,
                      confirmationWarning:
                        '',
                    }),
                  ),

                  catchError(
                    (
                      error:
                        unknown,
                    ) =>
                      of<ConfirmationLoadResult>({
                        status,

                        confirmation:
                          null,

                        confirmationWarning:
                          this
                            .extractErrorMessage(
                              error,
                              'Payment succeeded, but booking confirmation details are still being prepared.',
                            ),
                      }),
                  ),
                );
            },
          ),

          finalize(
            () => {
              this.isLoading =
                false;

              this.isRefreshingStatus =
                false;

              this.statusRequestSubscription =
                undefined;

              this.cdr.detectChanges();
            },
          ),
        )
        .subscribe({
          next: (
            result,
          ) => {
            this.paymentStatus =
              result.status;

            this.errorMessage =
              '';

            if (
              result.confirmation
            ) {
              this.bookingConfirmation =
                result.confirmation;

              this.warningMessage =
                '';
            } else {
              this.bookingConfirmation =
                null;

              this.warningMessage =
                result.confirmationWarning ||
                this.getStatusWarning(
                  result.status,
                );
            }

            if (
              this
                .shouldContinuePolling(
                  result.status,
                  result.confirmation,
                  result
                    .confirmationWarning,
                )
            ) {
              this.startAutoChecking();
            } else {
              this.stopAutoChecking();
            }

            this.cdr.detectChanges();
          },

          error: (
            error:
              unknown,
          ) => {
            this.errorMessage =
              this.extractErrorMessage(
                error,
                'Failed to load the latest payment status.',
              );

            this.warningMessage =
              '';

            this.stopAutoChecking();

            this.cdr.detectChanges();
          },
        });
  }

  selectCategory(
    category:
      string,
  ): void {
    this.selectedCategory =
      category;

    this.recommendations =
      [];

    this.recommendationsError =
      '';

    this.fetchRecommendations(
      category,
    );
  }

  clearSelectedCategory():
    void {
    this.recommendationsSubscription
      ?.unsubscribe();

    this.recommendationsSubscription =
      undefined;

    this.selectedCategory =
      null;

    this.recommendations =
      [];

    this.recommendationsError =
      '';

    this.isLoadingRecommendations =
      false;
  }

  get hasConfirmationData():
    boolean {
    return (
      !!this.paymentStatus ||
      !!this.bookingConfirmation
    );
  }

  get displayPaymentStatus():
    string {
    return (
      this.bookingConfirmation
        ?.payment.status ||
      this.paymentStatus
        ?.status ||
      'Unknown'
    );
  }

  get displayBookingStatus():
    string {
    return (
      this.bookingConfirmation
        ?.status ||
      this.paymentStatus
        ?.bookingStatus ||
      'Unknown'
    );
  }

  get displayAmount():
    number {
    return Number(
      this.bookingConfirmation
        ?.pricing.totalAmount ??
        this.bookingConfirmation
          ?.payment.amount ??
        this.paymentStatus
          ?.amount ??
        0,
    );
  }

  get displayCurrency():
    string {
    return (
      this.bookingConfirmation
        ?.pricing.currency ||
      this.bookingConfirmation
        ?.payment.currency ||
      this.paymentStatus
        ?.currency ||
      'EGP'
    );
  }

  get displayRefundedAmount():
    number {
    return Number(
      this.bookingConfirmation
        ?.payment.refundedAmount ??
        this.paymentStatus
          ?.refundedAmount ??
        0,
    );
  }

  get paymentFailureMessage():
    string {
    return (
      this.paymentStatus
        ?.failureMessage
        ?.trim() ||
      this.paymentStatus
        ?.failureCode
        ?.trim() ||
      ''
    );
  }

  get isConfirmed():
    boolean {
    if (
      this.bookingConfirmation
    ) {
      return (
        this.bookingConfirmation
          .status ===
          'Confirmed' ||
        this.bookingConfirmation
          .status ===
          'Completed'
      );
    }

    const status =
      this.paymentStatus;

    if (!status) {
      return false;
    }

    return (
      this.isSuccessfulPaymentStatus(
        status.status,
      ) &&
      (
        status.bookingStatus ===
          'Confirmed' ||
        status.bookingStatus ===
          'Completed'
      )
    );
  }

  get isProcessing():
    boolean {
    const status =
      this.paymentStatus;

    if (!status) {
      return false;
    }

    return (
      status.status ===
        'Pending' ||
      (
        this.isSuccessfulPaymentStatus(
          status.status,
        ) &&
        status.bookingStatus ===
          'Pending'
      )
    );
  }

  get isPaymentAttemptFailed():
    boolean {
    const status =
      this.paymentStatus;

    return (
      !!status &&
      (
        status.status ===
          'Failed' ||
        status.status ===
          'Cancelled'
      ) &&
      status.bookingStatus ===
        'Pending'
    );
  }

  get isBookingExpired():
    boolean {
    return (
      this.paymentStatus
        ?.bookingStatus ===
      'Expired'
    );
  }

  get isBookingCancelled():
    boolean {
    return (
      this.paymentStatus
        ?.bookingStatus ===
      'Cancelled'
    );
  }

  get isFailed():
    boolean {
    return (
      this.isPaymentAttemptFailed ||
      this.isBookingExpired ||
      this.isBookingCancelled
    );
  }

  get isRefunded():
    boolean {
    return (
      this.paymentStatus
        ?.status ===
        'PartiallyRefunded' ||
      this.paymentStatus
        ?.status ===
        'Refunded' ||
      this.displayRefundedAmount >
        0
    );
  }

  get canRetryPayment():
    boolean {
    const status =
      this.paymentStatus;

    if (
      !status ||
      !this
        .isPaymentAttemptFailed
    ) {
      return false;
    }

    const expirationTimestamp =
      Date.parse(
        status.bookingExpiresAt ??
          '',
      );

    return (
      Number.isFinite(
        expirationTimestamp,
      ) &&
      expirationTimestamp >
        Date.now()
    );
  }

  private fetchRecommendations(
    category:
      string,
  ): void {
    const confirmation =
      this.bookingConfirmation;

    if (!confirmation) {
      this.recommendationsError =
        'Booking confirmation data is not available.';

      return;
    }

    const latitude =
      confirmation.property
        .latitude;

    const longitude =
      confirmation.property
        .longitude;

    if (
      latitude === null ||
      latitude === undefined
    ) {
      this.recommendationsError =
        'Nearby recommendations are unavailable because the property latitude is missing.';

      return;
    }

    if (
      longitude === null ||
      longitude === undefined
    ) {
      this.recommendationsError =
        'Nearby recommendations are unavailable because the property longitude is missing.';

      return;
    }

    this.recommendationsSubscription
      ?.unsubscribe();

    this.isLoadingRecommendations =
      true;

    this.recommendationsError =
      '';

    this.recommendationsSubscription =
      this.aiRecommendationService
        .getRecommendations({
          latitude,
          longitude,
          category,
        })
        .pipe(
          catchError(
            (
              error:
                unknown,
            ) => {
              this.recommendationsError =
                this.extractErrorMessage(
                  error,
                  'Failed to fetch nearby recommendations.',
                );

              return of([]);
            },
          ),

          finalize(
            () => {
              this.isLoadingRecommendations =
                false;

              this.recommendationsSubscription =
                undefined;

              this.cdr.detectChanges();
            },
          ),
        )
        .subscribe(
          (
            recommendations,
          ) => {
            this.recommendations =
              recommendations;

            this.cdr.detectChanges();
          },
        );
  }

  private startAutoChecking():
    void {
    if (
      this.pollingSubscription &&
      !this.pollingSubscription.closed
    ) {
      return;
    }

    this.isAutoChecking =
      true;

    this.pollingAttempts =
      0;

    this.autoCheckMessage =
      'Auto-checking payment status...';

    this.pollingSubscription =
      timer(
        this.pollingIntervalMs,
        this.pollingIntervalMs,
      ).subscribe(
        () => {
          if (
            this.isRefreshingStatus
          ) {
            return;
          }

          if (
            this.pollingAttempts >=
            this.maxPollingAttempts
          ) {
            this.stopAutoChecking();

            this.warningMessage =
              'Payment confirmation is taking longer than expected. Use Refresh status to check again.';

            this.cdr.detectChanges();

            return;
          }

          this.pollingAttempts +=
            1;

          this.autoCheckMessage =
            `Auto-checking payment status... ` +
            `(${this.pollingAttempts}/${this.maxPollingAttempts})`;

          this.loadConfirmationData(
            false,
          );

          this.cdr.detectChanges();
        },
      );
  }

  private stopAutoChecking():
    void {
    this.pollingSubscription
      ?.unsubscribe();

    this.pollingSubscription =
      undefined;

    this.isAutoChecking =
      false;

    this.autoCheckMessage =
      '';

    this.pollingAttempts =
      0;
  }

  private shouldLoadBookingConfirmation(
    status:
      PaymentStatusResponse,
  ): boolean {
    return (
      status.bookingStatus ===
        'Confirmed' ||
      status.bookingStatus ===
        'Completed'
    );
  }

  private shouldContinuePolling(
    status:
      PaymentStatusResponse,

    confirmation:
      GuestBookingConfirmationResponse | null,

    confirmationWarning:
      string,
  ): boolean {
    if (
      status.status ===
        'Pending' &&
      !status.isFinal
    ) {
      return true;
    }

    if (
      this.isSuccessfulPaymentStatus(
        status.status,
      ) &&
      status.bookingStatus ===
        'Pending'
    ) {
      return true;
    }

    return (
      !confirmation &&
      !!confirmationWarning &&
      (
        status.bookingStatus ===
          'Confirmed' ||
        status.bookingStatus ===
          'Completed'
      )
    );
  }

  private getStatusWarning(
    status:
      PaymentStatusResponse,
  ): string {
    if (
      status.status ===
      'Pending'
    ) {
      return 'Stripe is still processing this payment. SmartStay will update the booking after the webhook confirms the final result.';
    }

    if (
      this.isSuccessfulPaymentStatus(
        status.status,
      ) &&
      status.bookingStatus ===
        'Pending'
    ) {
      return 'The payment succeeded and SmartStay is finalizing the booking confirmation.';
    }

    return '';
  }

  private isSuccessfulPaymentStatus(
    status:
      PaymentStatus,
  ): boolean {
    return (
      status ===
        'Succeeded' ||
      status ===
        'PartiallyRefunded' ||
      status ===
        'Refunded'
    );
  }

  private extractErrorMessage(
    error:
      unknown,

    fallbackMessage:
      string,
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
          .replace(
            /^\uFEFF/,
            '',
          )
          .trim();

      if (
        !normalizedError
      ) {
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
    value:
      unknown,
  ): string {
    if (
      !value ||
      typeof value !==
        'object'
    ) {
      return '';
    }

    const problem =
      value as {
        detail?: string;
        message?: string;
        title?: string;
        errors?:
          Record<
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

  private isGuid(
    value:
      string,
  ): boolean {
    return /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i
      .test(
        value,
      );
  }
}