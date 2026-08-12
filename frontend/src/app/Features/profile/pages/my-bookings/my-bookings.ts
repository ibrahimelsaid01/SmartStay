import { CommonModule } from '@angular/common';
import {
  ChangeDetectorRef,
  Component,
  DestroyRef,
  OnInit,
  inject,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import {
  ActivatedRoute,
  Router,
  RouterModule,
} from '@angular/router';
import { finalize } from 'rxjs';
import {
  BookingReviewStatus,
  BookingStatus,
  CancelBookingResponse,
  GuestBookingListItem,
  GuestBookingsService,
} from '../../services/guest-bookings';

type BookingFilter =
  | 'all'
  | 'pending'
  | 'active'
  | 'completed'
  | 'canceled';

@Component({
  selector: 'app-my-bookings',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './my-bookings.html',
  styleUrl: './my-bookings.css',
})
export class MyBookings implements OnInit {
  bookings: GuestBookingListItem[] = [];

  currentFilter: BookingFilter = 'all';

  page = 1;
  pageSize = 10;
  totalPages = 1;
  totalCount = 0;

  isLoading = false;
  errorMessage = '';
  successMessage = '';

  cancellingBookingId: string | null = null;
  cancelRequestBookingId: string | null = null;
  cancelReason = '';

  reviewNavigationBookingId: string | null = null;

  private readonly destroyRef = inject(DestroyRef);
  private loadRequestId = 0;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly guestBookingsService:
      GuestBookingsService,
    private readonly changeDetectorRef:
      ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    this.route.paramMap
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((params) => {
        this.currentFilter =
          this.normalizeFilter(
            params.get('filter'),
          );

        this.page = 1;
        this.closeCancelBox();
        this.loadBookings(1);
      });
  }

  loadBookings(
    page = this.page,
    preserveSuccessMessage = false,
  ): void {
    const requestId = ++this.loadRequestId;

    const status =
      this.mapFilterToStatus(
        this.currentFilter,
      );

    this.page = page;
    this.isLoading = true;
    this.errorMessage = '';

    if (!preserveSuccessMessage) {
      this.successMessage = '';
    }

    this.changeDetectorRef.detectChanges();

    this.guestBookingsService
      .getMyBookings(
        this.page,
        this.pageSize,
        status,
      )
      .pipe(
        finalize(() => {
          if (
            requestId === this.loadRequestId
          ) {
            this.isLoading = false;
            this.changeDetectorRef.detectChanges();
          }
        }),
      )
      .subscribe({
        next: (response) => {
          if (
            requestId !== this.loadRequestId
          ) {
            return;
          }

          this.bookings =
            response.items ?? [];

          this.totalPages =
            Math.max(
              1,
              response.totalPages ?? 1,
            );

          this.totalCount =
            response.totalCount ?? 0;

          this.page =
            response.page || this.page;

          this.changeDetectorRef.detectChanges();
        },
        error: (error: unknown) => {
          if (
            requestId !== this.loadRequestId
          ) {
            return;
          }

          this.bookings = [];
          this.totalPages = 1;
          this.totalCount = 0;

          this.errorMessage =
            this.extractErrorMessage(error) ||
            'Failed to load your bookings.';

          this.changeDetectorRef.detectChanges();
        },
      });
  }

  refreshBookings(): void {
    if (
      this.isLoading ||
      this.cancelRequestBookingId ||
      this.reviewNavigationBookingId
    ) {
      return;
    }

    this.loadBookings(this.page);
  }

  openCancelBox(
    bookingId: string,
  ): void {
    if (
      this.cancelRequestBookingId ||
      this.reviewNavigationBookingId
    ) {
      return;
    }

    this.cancellingBookingId = bookingId;
    this.cancelReason = '';
    this.errorMessage = '';
    this.successMessage = '';

    this.changeDetectorRef.detectChanges();
  }

  closeCancelBox(): void {
    if (this.cancelRequestBookingId) {
      return;
    }

    this.cancellingBookingId = null;
    this.cancelReason = '';

    this.changeDetectorRef.detectChanges();
  }

  confirmCancelBooking(
    bookingId: string,
  ): void {
    if (
      this.cancelRequestBookingId ||
      this.isLoading ||
      this.reviewNavigationBookingId
    ) {
      return;
    }

    const normalizedReason =
      this.cancelReason.trim();

    if (normalizedReason.length > 500) {
      this.errorMessage =
        'Cancellation reason cannot exceed 500 characters.';

      this.changeDetectorRef.detectChanges();
      return;
    }

    this.cancelRequestBookingId = bookingId;
    this.errorMessage = '';
    this.successMessage = '';

    this.changeDetectorRef.detectChanges();

    this.guestBookingsService
      .cancelBooking(
        bookingId,
        normalizedReason,
      )
      .pipe(
        finalize(() => {
          this.cancelRequestBookingId = null;
          this.changeDetectorRef.detectChanges();
        }),
      )
      .subscribe({
        next: (response) => {
          this.applyCancellationResponse(
            response,
          );

          this.successMessage =
            this.buildCancellationSuccessMessage(
              response,
            );

          this.cancellingBookingId = null;
          this.cancelReason = '';

          this.changeDetectorRef.detectChanges();
        },
        error: (error: unknown) => {
          this.errorMessage =
            this.extractErrorMessage(error) ||
            'Failed to cancel booking.';

          this.changeDetectorRef.detectChanges();
        },
      });
  }

  continuePayment(
    bookingId: string,
  ): void {
    if (
      this.isLoading ||
      this.cancelRequestBookingId ||
      this.reviewNavigationBookingId
    ) {
      return;
    }

    void this.router.navigate([
      '/checkout',
      bookingId,
    ]);
  }

  writeReview(
    booking: GuestBookingListItem,
  ): void {
    if (
      this.isLoading ||
      this.cancelRequestBookingId ||
      this.reviewNavigationBookingId
    ) {
      return;
    }

    if (
      booking.status !== 'Completed' ||
      !booking.canReview ||
      booking.hasReview
    ) {
      this.errorMessage =
        'A new review is not available for this booking.';

      this.changeDetectorRef.detectChanges();
      return;
    }

    this.navigateToReviews(
      booking.bookingId,
      {
        bookingId: booking.bookingId,
        propertyTitle:
          booking.property.title,
      },
    );
  }

  viewReview(
    booking: GuestBookingListItem,
  ): void {
    if (
      this.isLoading ||
      this.cancelRequestBookingId ||
      this.reviewNavigationBookingId
    ) {
      return;
    }

    if (
      !booking.hasReview ||
      !booking.reviewId
    ) {
      this.errorMessage =
        'The review for this booking could not be found.';

      this.changeDetectorRef.detectChanges();
      return;
    }

    this.navigateToReviews(
      booking.bookingId,
      {
        reviewId: booking.reviewId,
        status: booking.reviewStatus ?? null,
      },
    );
  }

  isOpeningReview(
    bookingId: string,
  ): boolean {
    return (
      this.reviewNavigationBookingId ===
      bookingId
    );
  }

  getReviewStatusClass(
    status?: BookingReviewStatus | null,
  ): string {
    switch (status) {
      case 'Pending':
        return 'review-status-pending';

      case 'Posted':
        return 'review-status-posted';

      case 'Rejected':
        return 'review-status-rejected';

      default:
        return 'review-status-unknown';
    }
  }

  getReviewStatusDescription(
    status?: BookingReviewStatus | null,
  ): string {
    switch (status) {
      case 'Pending':
        return 'Your review is waiting for admin moderation.';

      case 'Posted':
        return 'Your review is published on the property page.';

      case 'Rejected':
        return 'Your review needs changes before it can be published.';

      default:
        return 'Review details are available in My Reviews.';
    }
  }

  goToPreviousPage(): void {
    if (
      this.page <= 1 ||
      this.isLoading ||
      this.cancelRequestBookingId ||
      this.reviewNavigationBookingId
    ) {
      return;
    }

    this.loadBookings(this.page - 1);
  }

  goToNextPage(): void {
    if (
      this.page >= this.totalPages ||
      this.isLoading ||
      this.cancelRequestBookingId ||
      this.reviewNavigationBookingId
    ) {
      return;
    }

    this.loadBookings(this.page + 1);
  }

  isCancelling(
    bookingId: string,
  ): boolean {
    return (
      this.cancelRequestBookingId ===
      bookingId
    );
  }

  canContinuePayment(
    booking: GuestBookingListItem,
  ): boolean {
    return (
      booking.status === 'Pending' &&
      !booking.isPaymentWindowExpired &&
      !this.hasExpirationPassed(
        booking.expiresAt,
      )
    );
  }

  isPaymentExpired(
    booking: GuestBookingListItem,
  ): boolean {
    return (
      booking.status === 'Pending' &&
      (
        booking.isPaymentWindowExpired ||
        this.hasExpirationPassed(
          booking.expiresAt,
        )
      )
    );
  }

  trackBookingById(
    _index: number,
    booking: GuestBookingListItem,
  ): string {
    return booking.bookingId;
  }

  getStatusClass(
    status: BookingStatus,
  ): string {
    switch (status) {
      case 'Pending':
        return 'status-pending';

      case 'Confirmed':
        return 'status-confirmed';

      case 'Completed':
        return 'status-completed';

      case 'Cancelled':
        return 'status-cancelled';

      case 'Expired':
        return 'status-expired';

      default:
        return '';
    }
  }

  private navigateToReviews(
    bookingId: string,
    queryParams: {
      bookingId?: string;
      propertyTitle?: string;
      reviewId?: string;
      status?: BookingReviewStatus | null;
    },
  ): void {
    this.reviewNavigationBookingId =
      bookingId;

    this.errorMessage = '';
    this.successMessage = '';

    this.closeCancelBox();

    this.changeDetectorRef.detectChanges();

    void this.router
      .navigate(
        ['/profile/my-reviews'],
        {
          queryParams,
        },
      )
      .catch(() => {
        this.errorMessage =
          'Failed to open My Reviews.';
      })
      .finally(() => {
        this.reviewNavigationBookingId =
          null;

        this.changeDetectorRef.detectChanges();
      });
  }

  private applyCancellationResponse(
    response: CancelBookingResponse,
  ): void {
    const bookingIndex =
      this.bookings.findIndex(
        (booking) =>
          booking.bookingId ===
          response.bookingId,
      );

    if (bookingIndex < 0) {
      return;
    }

    if (
      this.currentFilter === 'pending' ||
      this.currentFilter === 'active'
    ) {
      this.bookings =
        this.bookings.filter(
          (booking) =>
            booking.bookingId !==
            response.bookingId,
        );

      this.totalCount =
        Math.max(
          0,
          this.totalCount - 1,
        );

      if (
        this.bookings.length === 0 &&
        this.page > 1
      ) {
        this.loadBookings(
          this.page - 1,
          true,
        );
      }

      return;
    }

    const currentBooking =
      this.bookings[bookingIndex];

    const updatedBooking:
      GuestBookingListItem = {
        ...currentBooking,
        status: response.status,
        canCancel: false,
        cancelledAt:
          response.cancelledAt,
      };

    this.bookings =
      this.bookings.map((booking) =>
        booking.bookingId ===
        response.bookingId
          ? updatedBooking
          : booking,
      );
  }

  private buildCancellationSuccessMessage(
    response: CancelBookingResponse,
  ): string {
    const messages = [
      response.message ||
        'Booking cancelled successfully.',
    ];

    if (response.isRefundRequired) {
      messages.push(
        `Refund: ${response.refundAmount.toLocaleString()} ${response.currency}`,
      );

      if (response.refundStatus) {
        messages.push(
          `Status: ${response.refundStatus}`,
        );
      }
    } else if (
      response.estimatedRefundAmount > 0
    ) {
      messages.push(
        `Estimated refund: ${response.estimatedRefundAmount.toLocaleString()} ${response.currency}`,
      );
    }

    if (response.refundMessage) {
      messages.push(response.refundMessage);
    }

    return messages.join(' ');
  }

  private normalizeFilter(
    value: string | null,
  ): BookingFilter {
    switch (
      (value ?? '').toLowerCase()
    ) {
      case 'pending':
        return 'pending';

      case 'active':
      case 'confirmed':
        return 'active';

      case 'completed':
        return 'completed';

      case 'canceled':
      case 'cancelled':
        return 'canceled';

      case 'all':
      default:
        return 'all';
    }
  }

  private mapFilterToStatus(
    filter: BookingFilter,
  ): BookingStatus | undefined {
    switch (filter) {
      case 'pending':
        return 'Pending';

      case 'active':
        return 'Confirmed';

      case 'completed':
        return 'Completed';

      case 'canceled':
        return 'Cancelled';

      case 'all':
      default:
        return undefined;
    }
  }

  private hasExpirationPassed(
    expiresAt?: string | null,
  ): boolean {
    if (!expiresAt) {
      return false;
    }

    const expirationTime =
      new Date(expiresAt).getTime();

    return (
      !Number.isNaN(expirationTime) &&
      expirationTime <= Date.now()
    );
  }

  private extractErrorMessage(
    error: unknown,
  ): string {
    const typedError = error as {
      error?: unknown;
      message?: string;
    };

    const parsedError =
      this.parseErrorBody(
        typedError.error,
      );

    if (parsedError) {
      const firstValidationError =
        parsedError.errors
          ? Object.values(
              parsedError.errors,
            )[0]?.[0]
          : undefined;

      return (
        parsedError.detail ||
        parsedError.message ||
        firstValidationError ||
        parsedError.title ||
        typedError.message ||
        ''
      );
    }

    if (
      typeof typedError.error === 'string' &&
      typedError.error.trim()
    ) {
      return typedError.error.trim();
    }

    return typedError.message || '';
  }

  private parseErrorBody(
    errorBody: unknown,
  ):
    | {
        detail?: string;
        message?: string;
        title?: string;
        errors?: Record<
          string,
          string[]
        >;
      }
    | null {
    if (
      errorBody &&
      typeof errorBody === 'object'
    ) {
      return errorBody as {
        detail?: string;
        message?: string;
        title?: string;
        errors?: Record<
          string,
          string[]
        >;
      };
    }

    if (
      typeof errorBody !== 'string' ||
      !errorBody.trim()
    ) {
      return null;
    }

    try {
      return JSON.parse(errorBody) as {
        detail?: string;
        message?: string;
        title?: string;
        errors?: Record<
          string,
          string[]
        >;
      };
    } catch {
      return null;
    }
  }
}