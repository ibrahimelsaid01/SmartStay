import { CommonModule } from '@angular/common';
import {
  ChangeDetectorRef,
  Component,
  OnInit,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  Observable,
  finalize,
} from 'rxjs';
import {
  AdminBookingDetails,
  AdminBookingListItem,
  AdminBookingStatus,
  AdminBookingsQuery,
  AdminBookingsResponse,
  AdminBookingsService,
  AdminBookingsSummary,
} from '../../services/admin-bookings';
import {
  AdminBookingPayoutResponse,
  AdminBookingPayoutsService,
} from '../../services/admin-booking-payouts';

type PayoutAction =
  | 'hold'
  | 'release'
  | 'block'
  | 'refunded';

@Component({
  selector: 'app-bookings-management',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './bookings-management.html',
  styleUrl: './bookings-management.css',
})
export class BookingsManagement
  implements OnInit {
  bookings: AdminBookingListItem[] = [];

  selectedBooking:
    AdminBookingDetails | null = null;

  selectedPayout:
    AdminBookingPayoutResponse | null = null;

  summary: AdminBookingsSummary = {
    totalBookings: 0,
    pendingBookings: 0,
    confirmedBookings: 0,
    cancelledBookings: 0,
    completedBookings: 0,
    expiredBookings: 0,
    upcomingBookings: 0,
    currentStays: 0,
    amountsByCurrency: [],
  };

  summaryLoading = false;
  bookingsLoading = false;
  detailsLoading = false;
  payoutLoading = false;

  activePayoutAction:
    PayoutAction | null = null;

  selectedDetailsBookingId:
    string | null = null;

  errorMessage = '';
  successMessage = '';
  payoutErrorMessage = '';
  payoutSuccessMessage = '';

  status:
    AdminBookingStatus | '' = '';

  propertyId = '';
  guestUserId = '';
  hostUserId = '';
  checkInFrom = '';
  checkInTo = '';

  payoutHoldReason = '';
  payoutReleaseNote = '';
  payoutBlockReason = '';
  payoutRefundedNote = '';

  page = 1;
  pageSize = 10;
  totalPages = 1;
  totalCount = 0;

  readonly statusOptions:
    Array<{
      value: AdminBookingStatus | '';
      label: string;
    }> = [
      {
        value: '',
        label: 'All statuses',
      },
      {
        value: 'Pending',
        label: 'Pending',
      },
      {
        value: 'Confirmed',
        label: 'Confirmed',
      },
      {
        value: 'Cancelled',
        label: 'Cancelled',
      },
      {
        value: 'Completed',
        label: 'Completed',
      },
      {
        value: 'Expired',
        label: 'Expired',
      },
    ];

  constructor(
    private readonly adminBookingsService:
      AdminBookingsService,
    private readonly payoutService:
      AdminBookingPayoutsService,
    private readonly changeDetectorRef:
      ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    this.loadPageData();
  }

  get payoutActionLoading(): boolean {
    return this.activePayoutAction !== null;
  }

  get isAnyLoading(): boolean {
    return (
      this.summaryLoading ||
      this.bookingsLoading ||
      this.detailsLoading ||
      this.payoutLoading ||
      this.payoutActionLoading
    );
  }

  loadPageData(): void {
    this.loadSummary();
    this.loadBookings(this.page);
  }

  loadSummary(): void {
    if (this.summaryLoading) {
      return;
    }

    this.summaryLoading = true;
    this.errorMessage = '';

    this.changeDetectorRef.detectChanges();

    this.adminBookingsService
      .getSummary()
      .pipe(
        finalize(() => {
          this.summaryLoading = false;
          this.changeDetectorRef.detectChanges();
        }),
      )
      .subscribe({
        next: (summary) => {
          this.summary = summary;
        },
        error: (error: unknown) => {
          this.errorMessage =
            this.extractErrorMessage(error) ||
            'Failed to load bookings summary.';
        },
      });
  }

  loadBookings(
    page = this.page,
  ): void {
    if (this.bookingsLoading) {
      return;
    }

    this.page = page;
    this.bookingsLoading = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.changeDetectorRef.detectChanges();

    const query:
      AdminBookingsQuery = {
        page: this.page,
        pageSize: this.pageSize,
        status: this.status || null,
        propertyId: this.propertyId,
        guestUserId: this.guestUserId,
        hostUserId: this.hostUserId,
        checkInFrom: this.checkInFrom,
        checkInTo: this.checkInTo,
      };

    this.adminBookingsService
      .getBookings(query)
      .pipe(
        finalize(() => {
          this.bookingsLoading = false;
          this.changeDetectorRef.detectChanges();
        }),
      )
      .subscribe({
        next: (
          response:
            AdminBookingsResponse,
        ) => {
          this.bookings =
            response.items ?? [];

          this.totalCount =
            response.totalCount ?? 0;

          this.totalPages =
            Math.max(
              1,
              response.totalPages ?? 1,
            );

          this.page =
            response.page || this.page;
        },
        error: (error: unknown) => {
          this.bookings = [];
          this.totalCount = 0;
          this.totalPages = 1;

          this.errorMessage =
            this.extractErrorMessage(error) ||
            'Failed to load bookings.';
        },
      });
  }

  applyFilters(): void {
    if (this.bookingsLoading) {
      return;
    }

    this.loadBookings(1);
  }

  resetFilters(): void {
    if (this.bookingsLoading) {
      return;
    }

    this.status = '';
    this.propertyId = '';
    this.guestUserId = '';
    this.hostUserId = '';
    this.checkInFrom = '';
    this.checkInTo = '';

    this.loadBookings(1);
  }

  refreshAll(): void {
    if (this.isAnyLoading) {
      return;
    }

    const selectedBookingId =
      this.selectedBooking?.bookingId ??
      null;

    this.loadPageData();

    if (selectedBookingId) {
      this.viewDetails(
        selectedBookingId,
      );
    }
  }

  viewDetails(
    bookingId: string,
  ): void {
    if (
      this.detailsLoading ||
      this.payoutActionLoading
    ) {
      return;
    }

    this.detailsLoading = true;
    this.selectedDetailsBookingId =
      bookingId;

    this.errorMessage = '';
    this.successMessage = '';
    this.payoutErrorMessage = '';
    this.payoutSuccessMessage = '';

    this.selectedBooking = null;
    this.selectedPayout = null;

    this.resetPayoutForms();
    this.changeDetectorRef.detectChanges();

    this.adminBookingsService
      .getBookingDetails(bookingId)
      .pipe(
        finalize(() => {
          this.detailsLoading = false;
          this.selectedDetailsBookingId =
            null;

          this.changeDetectorRef.detectChanges();
        }),
      )
      .subscribe({
        next: (booking) => {
          this.selectedBooking = booking;

          this.loadPayoutForBooking(
            booking.bookingId,
            false,
          );
        },
        error: (error: unknown) => {
          this.errorMessage =
            this.extractErrorMessage(error) ||
            'Failed to load booking details.';
        },
      });
  }

  closeDetails(): void {
    if (this.payoutActionLoading) {
      return;
    }

    this.selectedBooking = null;
    this.selectedPayout = null;
    this.payoutErrorMessage = '';
    this.payoutSuccessMessage = '';

    this.resetPayoutForms();
    this.changeDetectorRef.detectChanges();
  }

  loadPayoutForBooking(
    bookingId: string,
    showSuccess = true,
  ): void {
    if (
      !bookingId ||
      this.payoutLoading ||
      this.payoutActionLoading
    ) {
      return;
    }

    this.payoutLoading = true;
    this.payoutErrorMessage = '';
    this.payoutSuccessMessage = '';

    this.changeDetectorRef.detectChanges();

    this.payoutService
      .getBookingPayoutByBookingId(
        bookingId,
      )
      .pipe(
        finalize(() => {
          this.payoutLoading = false;
          this.changeDetectorRef.detectChanges();
        }),
      )
      .subscribe({
        next: (payout) => {
          this.selectedPayout = payout;

          if (showSuccess) {
            this.payoutSuccessMessage =
              'Payout data refreshed successfully.';
          }
        },
        error: (error: unknown) => {
          this.selectedPayout = null;

          this.payoutErrorMessage =
            this.extractErrorMessage(error) ||
            'No payout record was found. A payout is created after successful payment confirmation.';
        },
      });
  }

  holdPayout(): void {
    const reason =
      this.payoutHoldReason.trim();

    if (
      !this.selectedBooking ||
      reason.length < 5
    ) {
      this.payoutErrorMessage =
        'Hold reason must contain at least 5 characters.';

      this.changeDetectorRef.detectChanges();
      return;
    }

    this.runPayoutAction(
      'hold',
      this.payoutService
        .holdBookingPayout(
          this.selectedBooking.bookingId,
          reason,
        ),
    );
  }

  releasePayout(): void {
    if (!this.selectedBooking) {
      return;
    }

    this.runPayoutAction(
      'release',
      this.payoutService
        .releaseBookingPayout(
          this.selectedBooking.bookingId,
          this.payoutReleaseNote,
        ),
    );
  }

  blockPayout(): void {
    const reason =
      this.payoutBlockReason.trim();

    if (
      !this.selectedBooking ||
      reason.length < 5
    ) {
      this.payoutErrorMessage =
        'Block reason must contain at least 5 characters.';

      this.changeDetectorRef.detectChanges();
      return;
    }

    this.runPayoutAction(
      'block',
      this.payoutService
        .blockBookingPayout(
          this.selectedBooking.bookingId,
          reason,
        ),
    );
  }

  markPayoutRefunded(): void {
    if (!this.selectedBooking) {
      return;
    }

    this.runPayoutAction(
      'refunded',
      this.payoutService
        .markBookingPayoutRefunded(
          this.selectedBooking.bookingId,
          this.payoutRefundedNote,
        ),
    );
  }

  goToPreviousPage(): void {
    if (
      this.page <= 1 ||
      this.bookingsLoading
    ) {
      return;
    }

    this.loadBookings(this.page - 1);
  }

  goToNextPage(): void {
    if (
      this.page >= this.totalPages ||
      this.bookingsLoading
    ) {
      return;
    }

    this.loadBookings(this.page + 1);
  }

  trackBookingById(
    _index: number,
    booking: AdminBookingListItem,
  ): string {
    return booking.bookingId;
  }

  getBookingStatusClass(
    status: AdminBookingStatus,
  ): string {
    switch (status) {
      case 'Pending':
        return 'status-pending';

      case 'Confirmed':
        return 'status-confirmed';

      case 'Cancelled':
        return 'status-cancelled';

      case 'Completed':
        return 'status-completed';

      case 'Expired':
        return 'status-expired';

      default:
        return 'status-default';
    }
  }

  getStayStateLabel(
    booking: AdminBookingListItem,
  ): string {
    if (
      booking.isPaymentWindowExpired
    ) {
      return 'Payment window expired';
    }

    if (
      booking.isCurrentlyStaying
    ) {
      return 'Currently staying';
    }

    if (booking.isUpcoming) {
      return 'Upcoming';
    }

    return 'Standard';
  }

  getPayoutStatusClass(
    status: string,
  ): string {
    switch (status) {
      case 'Held':
        return 'payout-held';

      case 'Available':
      case 'Paid':
        return 'payout-available';

      case 'Blocked':
      case 'Refunded':
        return 'payout-blocked';

      case 'Pending':
      default:
        return 'payout-pending';
    }
  }

  formatMoney(
    amount:
      number | null | undefined,
    currency:
      string | null | undefined,
  ): string {
    return `${(amount ?? 0).toLocaleString(
      undefined,
      {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2,
      },
    )} ${currency || 'EGP'}`;
  }

  private runPayoutAction(
    action: PayoutAction,
    request$:
      Observable<AdminBookingPayoutResponse>,
  ): void {
    if (this.payoutActionLoading) {
      return;
    }

    this.activePayoutAction = action;
    this.payoutErrorMessage = '';
    this.payoutSuccessMessage = '';

    this.changeDetectorRef.detectChanges();

    request$
      .pipe(
        finalize(() => {
          this.activePayoutAction = null;
          this.changeDetectorRef.detectChanges();
        }),
      )
      .subscribe({
        next: (payout) => {
          this.selectedPayout = payout;

          this.payoutSuccessMessage =
            this.getPayoutActionSuccessMessage(
              action,
            );

          this.resetPayoutForms();
        },
        error: (error: unknown) => {
          this.payoutErrorMessage =
            this.extractErrorMessage(error) ||
            'Failed to update payout status.';
        },
      });
  }

  private getPayoutActionSuccessMessage(
    action: PayoutAction,
  ): string {
    switch (action) {
      case 'hold':
        return 'Payout was placed on hold successfully.';

      case 'release':
        return 'Payout was released successfully.';

      case 'block':
        return 'Payout was blocked successfully.';

      case 'refunded':
        return 'Payout was marked as refunded successfully.';
    }
  }

  private resetPayoutForms(): void {
    this.payoutHoldReason = '';
    this.payoutReleaseNote = '';
    this.payoutBlockReason = '';
    this.payoutRefundedNote = '';
  }

  private extractErrorMessage(
    error: unknown,
  ): string {
    const typedError = error as {
      error?: unknown;
      message?: string;
      status?: number;
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

    if (typedError.status === 404) {
      return 'No payout record was found for this booking yet.';
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