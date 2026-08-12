import {
  CommonModule,
  DecimalPipe,
} from '@angular/common';
import {
  HttpErrorResponse,
} from '@angular/common/http';
import {
  ChangeDetectorRef,
  Component,
  Input,
  inject,
} from '@angular/core';
import {
  FormsModule,
} from '@angular/forms';
import {
  Router,
} from '@angular/router';
import {
  AuthState,
} from '../../../../auth/services/auth-state';
import {
  Observable,
  TimeoutError,
  catchError,
  finalize,
  map,
  of,
  switchMap,
  throwError,
} from 'rxjs';
import {
  BookingPeriodRequest,
  BookingQuoteResponse,
  BookingService,
  CreateBookingRequest,
  GuestBookingListItem,
  GuestBookingsResponse,
} from '../../../services/booking.service';
import {
  PropertyDetails,
} from '../../../services/propertydetailservice';

interface BookingWidgetData {
  checkInDate: string;
  checkOutDate: string;
  guestsCount: number;
}

class BookingFlowError extends Error {
  constructor(
    message: string,
  ) {
    super(message);

    this.name =
      'BookingFlowError';
  }
}

@Component({
  selector:
    'app-booking-widget',

  standalone:
    true,

  imports: [
    CommonModule,
    FormsModule,
    DecimalPipe,
  ],

  templateUrl:
    './booking-widget.html',

  styleUrls: [
    './booking-widget.css',
  ],
})
export class BookingWidget {
  private readonly bookingService =
    inject(
      BookingService,
    );

  private readonly authState =
    inject(
      AuthState,
    );

  private readonly router =
    inject(
      Router,
    );

  private readonly changeDetectorRef =
    inject(
      ChangeDetectorRef,
    );

  @Input({
    required: true,
  })
  property!:
    PropertyDetails;

  bookingData:
    BookingWidgetData = {
      checkInDate: '',
      checkOutDate: '',
      guestsCount: 1,
    };

  isSubmitting =
    false;

  isQuoteLoading =
    false;

  bookingQuote:
    BookingQuoteResponse |
    null =
      null;

  errorMessage =
    '';

  statusMessage =
    '';

  showBookingTermsModal =
    false;

  termsErrorMessage =
    '';

  acceptedAllBookingTerms =
    false;

  readonly today =
    this.formatLocalDate(
      new Date(),
    );

  get isBusy():
    boolean {
    return (
      this.isSubmitting ||
      this.isQuoteLoading
    );
  }

  get guestOptions():
    number[] {
    const propertyMaximum =
      Math.max(
        1,
        Number(
          this.property
            ?.maxGuests,
        ) || 1,
      );

    const supportedMaximum =
      Math.min(
        propertyMaximum,
        20,
      );

    return Array.from(
      {
        length:
          supportedMaximum,
      },

      (
        _,
        index,
      ) =>
        index + 1,
    );
  }

  get areAllTermsAccepted():
    boolean {
    return this
      .acceptedAllBookingTerms;
  }

  get cancellationPolicyTitle():
    string {
    return (
      this.bookingQuote
        ?.cancellationPolicy
        ?.trim() ||
      this.property
        ?.cancellationPolicy
        ?.trim() ||
      'Moderate'
    );
  }

  get cancellationPolicyDescription():
    string {
    const policy =
      this
        .cancellationPolicyTitle
        .toLowerCase();

    if (
      policy.includes(
        'flexible',
      )
    ) {
      return 'Flexible policy: early cancellations generally receive the highest available refund according to the cancellation snapshot stored with this booking.';
    }

    if (
      policy.includes(
        'strict',
      )
    ) {
      return 'Strict policy: late cancellations may receive a partial refund or no refund depending on the remaining time before check-in.';
    }

    return 'Moderate policy: early cancellations may be refundable, while late cancellations may receive a partial refund or no refund depending on the cancellation time.';
  }

  get propertyRulesList():
    string[] {
    const rules:
      string[] =
        [];

    const additionalRules =
      this.property
        ?.additionalHouseRules
        ?.trim();

    if (
      !this.property
        ?.allowsSmoking
    ) {
      rules.push(
        'Smoking is not allowed inside this property.',
      );
    }

    if (
      !this.property
        ?.allowsPets
    ) {
      rules.push(
        'Pets are not allowed unless the host explicitly approves them.',
      );
    }

    if (
      !this.property
        ?.allowsParties
    ) {
      rules.push(
        'Parties and disruptive events are not allowed.',
      );
    }

    if (
      !this.property
        ?.allowsChildren
    ) {
      rules.push(
        'This property is not listed as suitable for children.',
      );
    }

    if (
      additionalRules
    ) {
      additionalRules
        .split(
          /\n|;|,/,
        )
        .map(
          (
            rule,
          ) =>
            rule.trim(),
        )
        .filter(
          Boolean,
        )
        .forEach(
          (
            rule,
          ) =>
            rules.push(
              rule,
            ),
        );
    }

    if (
      rules.length ===
      0
    ) {
      rules.push(
        'Follow all property rules shown on the listing and any reasonable check-in instructions provided by the host.',
      );
    }

    return [
      ...new Set(
        rules,
      ),
    ];
  }

  get stayNights():
    number {
    if (
      this.bookingQuote
    ) {
      return this
        .bookingQuote
        .nights;
    }

    if (
      !this.bookingData
        .checkInDate ||
      !this.bookingData
        .checkOutDate
    ) {
      return 0;
    }

    const checkIn =
      this
        .toLocalDate(
          this.bookingData
            .checkInDate,
        )
        .getTime();

    const checkOut =
      this
        .toLocalDate(
          this.bookingData
            .checkOutDate,
        )
        .getTime();

    if (
      Number.isNaN(
        checkIn,
      ) ||
      Number.isNaN(
        checkOut,
      ) ||
      checkOut <=
        checkIn
    ) {
      return 0;
    }

    return Math.ceil(
      (
        checkOut -
        checkIn
      ) /
        86400000,
    );
  }

  get estimatedSubtotal():
    number {
    if (
      this.bookingQuote
    ) {
      return this
        .bookingQuote
        .subtotal;
    }

    return (
      (
        Number(
          this.property
            ?.pricePerNight,
        ) || 0
      ) *
      this.stayNights
    );
  }

  get estimatedServiceFee():
    number {
    return (
      this.bookingQuote
        ?.serviceFee ??
      0
    );
  }

  get estimatedTotal():
    number {
    return (
      this.bookingQuote
        ?.totalAmount ??
      this
        .estimatedSubtotal
    );
  }

  get quoteCurrency():
    string {
    return (
      this.bookingQuote
        ?.currency
        ?.trim() ||
      this.property
        ?.currency
        ?.trim() ||
      'EGP'
    );
  }

  onBookingDataChanged():
    void {
    this.bookingQuote =
      null;

    this.clearMessages();
  }

  onBookNow():
    void {
    if (
      this.isBusy
    ) {
      return;
    }

    this.clearMessages();

    this.bookingQuote =
      null;

    if (
      !this
        .validateBookingForm()
    ) {
      this
        .changeDetectorRef
        .detectChanges();

      return;
    }

    if (
      !this.authState
        .isLoggedIn()
    ) {
      this.navigateToLogin();

      return;
    }

    this
      .loadBookingQuoteAndOpenTerms();
  }

  private loadBookingQuoteAndOpenTerms():
    void {
    const request =
      this
        .buildBookingPeriodRequest();

    this.isQuoteLoading =
      true;

    this.setStatusMessage(
      'Checking availability and calculating the latest price...',
    );

    this.bookingService
      .getBookingQuote(
        this.property.id,
        request,
      )
      .pipe(
        finalize(
          () => {
            this.isQuoteLoading =
              false;

            this
              .changeDetectorRef
              .detectChanges();
          },
        ),
      )
      .subscribe({
        next: (
          quote,
        ) => {
          this.bookingQuote =
            quote;

          this.clearMessages();

          this
            .openBookingTermsModal();
        },

        error: (
          error:
            unknown,
        ) => {
          this.bookingQuote =
            null;

          this.setErrorMessage(
            this.getErrorMessage(
              error,
              'Unable to calculate the booking price. Please try again.',
            ),
          );
        },
      });
  }

  confirmBookingTermsAndContinue():
    void {
    if (
      this.isSubmitting
    ) {
      return;
    }

    this.termsErrorMessage =
      '';

    if (
      !this
        .areAllTermsAccepted
    ) {
      this.termsErrorMessage =
        'Please read and accept all booking terms before continuing.';

      this
        .changeDetectorRef
        .detectChanges();

      return;
    }

    this.startBookingProcess();
  }

  closeBookingTermsModal():
    void {
    if (
      this.isSubmitting
    ) {
      return;
    }

    this.showBookingTermsModal =
      false;

    this.termsErrorMessage =
      '';

    this
      .changeDetectorRef
      .detectChanges();
  }

  private openBookingTermsModal():
    void {
    this.termsErrorMessage =
      '';

    this.acceptedAllBookingTerms =
      false;

    this.showBookingTermsModal =
      true;

    this
      .changeDetectorRef
      .detectChanges();
  }

  private startBookingProcess():
    void {
    if (
      this.isSubmitting
    ) {
      return;
    }

    if (
      !this.bookingQuote
    ) {
      this.termsErrorMessage =
        'The booking quote is no longer available. Close this window and check the latest price again.';

      this
        .changeDetectorRef
        .detectChanges();

      return;
    }

    const request =
      this
        .buildCreateBookingRequest();

    this.isSubmitting =
      true;

    this.termsErrorMessage =
      '';

    this.setStatusMessage(
      'Checking your pending bookings...',
    );

    this
      .resolveBookingForCheckout(
        request,
      )
      .pipe(
        finalize(
          () => {
            this.isSubmitting =
              false;

            this
              .changeDetectorRef
              .detectChanges();
          },
        ),
      )
      .subscribe({
        next: (
          bookingId,
        ) => {
          this.showBookingTermsModal =
            false;

          this.clearMessages();

          this
            .changeDetectorRef
            .detectChanges();

          void this.router
            .navigate([
              '/checkout',
              bookingId,
            ]);
        },

        error: (
          error:
            unknown,
        ) => {
          this.showBookingTermsModal =
            false;

          this.setErrorMessage(
            this.getErrorMessage(
              error,
              'Failed to create booking. Please try again.',
            ),
          );
        },
      });
  }

  private resolveBookingForCheckout(
    request:
      CreateBookingRequest,
  ): Observable<string> {
    return this.bookingService
      .getMyBookings({
        page: 1,
        pageSize: 50,
        status: 'Pending',
      })
      .pipe(
        catchError(
          () =>
            of(
              this
                .createEmptyPendingBookingsResponse(),
            ),
        ),

        switchMap(
          (
            response,
          ) => {
            const exactPendingBooking =
              this
                .findLatestExactPendingBooking(
                  response.items,
                  request,
                );

            if (
              exactPendingBooking
            ) {
              this.setStatusMessage(
                'Opening your existing pending booking...',
              );

              return of(
                exactPendingBooking
                  .bookingId,
              );
            }

            const overlappingPendingBooking =
              this
                .findLatestOverlappingPendingBooking(
                  response.items,
                  request,
                );

            if (
              overlappingPendingBooking
            ) {
              return throwError(
                () =>
                  new BookingFlowError(
                    'You already have a pending booking for this property that overlaps these dates. Complete or cancel that booking before choosing different overlapping dates.',
                  ),
              );
            }

            this.setStatusMessage(
              'Creating booking...',
            );

            return this.bookingService
              .createBooking(
                request,
              )
              .pipe(
                map(
                  (
                    response,
                  ) =>
                    response
                      .bookingId,
                ),

                catchError(
                  (
                    error:
                      unknown,
                  ) => {
                    if (
                      !this
                        .isConflictError(
                          error,
                        )
                    ) {
                      return throwError(
                        () =>
                          error,
                      );
                    }

                    this.setStatusMessage(
                      'A matching booking already exists. Preparing checkout...',
                    );

                    return this
                      .bookingService
                      .getMyBookings({
                        page: 1,
                        pageSize: 50,
                        status:
                          'Pending',
                      })
                      .pipe(
                        map(
                          (
                            pendingResponse,
                          ) => {
                            const matchingBooking =
                              this
                                .findLatestExactPendingBooking(
                                  pendingResponse
                                    .items,
                                  request,
                                );

                            if (
                              !matchingBooking
                            ) {
                              throw new BookingFlowError(
                                'A booking conflict was detected, but the pending booking could not be opened automatically. Open My Bookings and continue the payment from there.',
                              );
                            }

                            return matchingBooking
                              .bookingId;
                          },
                        ),
                      );
                  },
                ),
              );
          },
        ),
      );
  }

  private createEmptyPendingBookingsResponse():
    GuestBookingsResponse {
    return {
      items: [],
      page: 1,
      pageSize: 50,
      totalCount: 0,
      totalPages: 1,
      appliedStatusFilter:
        'Pending',
    };
  }

  private findLatestExactPendingBooking(
    bookings:
      GuestBookingListItem[],

    request:
      CreateBookingRequest,
  ): GuestBookingListItem |
    null {
    return (
      bookings
        .filter(
          (
            booking,
          ) =>
            this
              .isExactPendingBookingMatch(
                booking,
                request,
              ),
        )
        .sort(
          (
            first,
            second,
          ) =>
            new Date(
              second
                .createdAt ||
                '',
            ).getTime() -
            new Date(
              first
                .createdAt ||
                '',
            ).getTime(),
        )[0] ??
      null
    );
  }

  private findLatestOverlappingPendingBooking(
    bookings:
      GuestBookingListItem[],

    request:
      CreateBookingRequest,
  ): GuestBookingListItem |
    null {
    return (
      bookings
        .filter(
          (
            booking,
          ) =>
            this
              .isOverlappingPendingBookingMatch(
                booking,
                request,
              ),
        )
        .sort(
          (
            first,
            second,
          ) =>
            new Date(
              second
                .createdAt ||
                '',
            ).getTime() -
            new Date(
              first
                .createdAt ||
                '',
            ).getTime(),
        )[0] ??
      null
    );
  }

  private isExactPendingBookingMatch(
    booking:
      GuestBookingListItem,

    request:
      CreateBookingRequest,
  ): boolean {
    const bookingPropertyId =
      booking.propertyId ||
      booking.property
        ?.id ||
      '';

    const sameProperty =
      this.normalizeId(
        bookingPropertyId,
      ) ===
      this.normalizeId(
        request.propertyId,
      );

    const isActivePending =
      booking.status ===
        'Pending' &&
      !booking
        .isPaymentWindowExpired;

    const sameDates =
      this.toDateOnly(
        booking.checkInDate,
      ) ===
        this.toDateOnly(
          request.checkInDate,
        ) &&
      this.toDateOnly(
        booking.checkOutDate,
      ) ===
        this.toDateOnly(
          request.checkOutDate,
        );

    return (
      sameProperty &&
      isActivePending &&
      sameDates
    );
  }

  private isOverlappingPendingBookingMatch(
    booking:
      GuestBookingListItem,

    request:
      CreateBookingRequest,
  ): boolean {
    const bookingPropertyId =
      booking.propertyId ||
      booking.property
        ?.id ||
      '';

    const sameProperty =
      this.normalizeId(
        bookingPropertyId,
      ) ===
      this.normalizeId(
        request.propertyId,
      );

    const isActivePending =
      booking.status ===
        'Pending' &&
      !booking
        .isPaymentWindowExpired;

    const bookingCheckIn =
      this.toDateOnly(
        booking.checkInDate,
      );

    const bookingCheckOut =
      this.toDateOnly(
        booking.checkOutDate,
      );

    const requestCheckIn =
      this.toDateOnly(
        request.checkInDate,
      );

    const requestCheckOut =
      this.toDateOnly(
        request.checkOutDate,
      );

    const datesOverlap =
      bookingCheckIn <
        requestCheckOut &&
      bookingCheckOut >
        requestCheckIn;

    const sameDates =
      bookingCheckIn ===
        requestCheckIn &&
      bookingCheckOut ===
        requestCheckOut;

    return (
      sameProperty &&
      isActivePending &&
      datesOverlap &&
      !sameDates
    );
  }

  private validateBookingForm():
    boolean {
    this.statusMessage =
      '';

    if (
      !this.property
        ?.id
    ) {
      this.errorMessage =
        'Property data is not loaded yet.';

      return false;
    }

    if (
      !this.bookingData
        .checkInDate
    ) {
      this.errorMessage =
        'Please select a check-in date.';

      return false;
    }

    if (
      !this.bookingData
        .checkOutDate
    ) {
      this.errorMessage =
        'Please select a check-out date.';

      return false;
    }

    if (
      this.bookingData
        .checkInDate <
      this.today
    ) {
      this.errorMessage =
        'Check-in date cannot be in the past.';

      return false;
    }

    if (
      this.bookingData
        .checkOutDate <=
      this.bookingData
        .checkInDate
    ) {
      this.errorMessage =
        'Check-out date must be after check-in date.';

      return false;
    }

    const guestsCount =
      Number(
        this.bookingData
          .guestsCount,
      );

    const maxGuests =
      Math.min(
        Math.max(
          1,
          Number(
            this.property
              .maxGuests,
          ) || 1,
        ),
        20,
      );

    if (
      !Number.isInteger(
        guestsCount,
      ) ||
      guestsCount < 1 ||
      guestsCount >
        maxGuests
    ) {
      this.errorMessage =
        `Guests count must be between 1 and ${maxGuests}.`;

      return false;
    }

    return true;
  }

  private buildBookingPeriodRequest():
    BookingPeriodRequest {
    return {
      checkInDate:
        this.toDateOnly(
          this.bookingData
            .checkInDate,
        ),

      checkOutDate:
        this.toDateOnly(
          this.bookingData
            .checkOutDate,
        ),

      guestsCount:
        Number(
          this.bookingData
            .guestsCount,
        ),
    };
  }

  private buildCreateBookingRequest():
    CreateBookingRequest {
    return {
      propertyId:
        this.property.id,

      checkInDate:
        this.toDateOnly(
          this.bookingData
            .checkInDate,
        ),

      checkOutDate:
        this.toDateOnly(
          this.bookingData
            .checkOutDate,
        ),

      guestsCount:
        Number(
          this.bookingData
            .guestsCount,
        ),

      acceptedBookingTerms:
        this
          .acceptedAllBookingTerms,

      acceptedCancellationPolicy:
        this
          .acceptedAllBookingTerms,

      acceptedPropertyRules:
        this
          .acceptedAllBookingTerms,

      acceptedComplaintPolicy:
        this
          .acceptedAllBookingTerms,
    };
  }

  private isConflictError(
    error:
      unknown,
  ): boolean {
    return error instanceof
      HttpErrorResponse
      ? error.status ===
          409
      : (
          error as {
            status?:
              number;
          }
        )
          ?.status ===
        409;
  }

  private isOwnPropertyBookingError(
    message:
      string,
  ): boolean {
    const normalizedMessage =
      message
        .toLowerCase();

    return (
      normalizedMessage
        .includes(
          'cannot book your own property',
        ) ||
      normalizedMessage
        .includes(
          'own property',
        )
    );
  }

  private getErrorMessage(
    error:
      unknown,

    fallbackMessage:
      string,
  ): string {
    if (
      error instanceof
        BookingFlowError
    ) {
      return error.message;
    }

    if (
      error instanceof
        TimeoutError
    ) {
      return 'Request took too long. Please try again.';
    }

    const typedError =
      error as {
        error?:
          unknown;

        message?:
          string;

        status?:
          number;

        name?:
          string;
      };

    if (
      typedError.name ===
      'TimeoutError'
    ) {
      return 'Request took too long. Please try again.';
    }

    const backendMessage =
      this
        .extractBackendMessage(
          error,
        );

    if (
      this
        .isOwnPropertyBookingError(
          backendMessage,
        )
    ) {
      return 'You cannot book your own property. Use a normal User account that does not own this listing.';
    }

    if (
      backendMessage
    ) {
      return backendMessage;
    }

    if (
      typedError.status ===
      0
    ) {
      return 'Cannot reach the server. Check your internet connection and try again.';
    }

    if (
      typedError.status ===
      401
    ) {
      return 'Please log in before booking this property.';
    }

    if (
      typedError.status ===
      403
    ) {
      return 'Your account is not allowed to create this booking.';
    }

    return (
      typedError.message ||
      fallbackMessage
    );
  }

  private extractBackendMessage(
    error:
      unknown,
  ): string {
    const typedError =
      error as {
        error?:
          unknown;

        message?:
          string;
      };

    const parsedError =
      this.parseErrorBody(
        typedError.error,
      );

    if (
      parsedError
    ) {
      const firstValidationError =
        parsedError.errors
          ? Object.values(
              parsedError
                .errors,
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
      typeof typedError
        .error ===
      'string'
    ) {
      return typedError
        .error
        .replace(
          /^\uFEFF/,
          '',
        )
        .trim();
    }

    return (
      typedError.message ||
      ''
    );
  }

  private parseErrorBody(
    errorBody:
      unknown,
  ):
    | {
        detail?:
          string;

        message?:
          string;

        title?:
          string;

        errors?:
          Record<
            string,
            string[]
          >;
      }
    | null {
    if (
      errorBody &&
      typeof errorBody ===
        'object'
    ) {
      return errorBody as {
        detail?:
          string;

        message?:
          string;

        title?:
          string;

        errors?:
          Record<
            string,
            string[]
          >;
      };
    }

    if (
      typeof errorBody !==
      'string'
    ) {
      return null;
    }

    const normalizedErrorBody =
      errorBody
        .replace(
          /^\uFEFF/,
          '',
        )
        .trim();

    if (
      !normalizedErrorBody
    ) {
      return null;
    }

    try {
      return JSON.parse(
        normalizedErrorBody,
      ) as {
        detail?:
          string;

        message?:
          string;

        title?:
          string;

        errors?:
          Record<
            string,
            string[]
          >;
      };
    } catch {
      return null;
    }
  }

  private navigateToLogin():
    void {
    void this.router.navigate(
      ['/login'],
      {
        queryParams: {
          returnUrl:
            this.router.url,
        },
      },
    );
  }

  private setStatusMessage(
    message:
      string,
  ): void {
    this.statusMessage =
      message;

    this.errorMessage =
      '';

    this
      .changeDetectorRef
      .detectChanges();
  }

  private setErrorMessage(
    message:
      string,
  ): void {
    this.errorMessage =
      message;

    this.statusMessage =
      '';

    this
      .changeDetectorRef
      .detectChanges();
  }

  private clearMessages():
    void {
    this.errorMessage =
      '';

    this.statusMessage =
      '';
  }

  private normalizeId(
    id?:
      string |
      null,
  ): string {
    return (
      id ?? ''
    )
      .trim()
      .toLowerCase();
  }

  private toDateOnly(
    value?:
      string |
      null,
  ): string {
    if (
      !value
    ) {
      return '';
    }

    return String(
      value,
    )
      .split(
        'T',
      )[0];
  }

  private toLocalDate(
    dateOnly:
      string,
  ): Date {
    return new Date(
      `${dateOnly}T00:00:00`,
    );
  }

  private formatLocalDate(
    date:
      Date,
  ): string {
    const year =
      date.getFullYear();

    const month =
      String(
        date.getMonth() +
          1,
      )
        .padStart(
          2,
          '0',
        );

    const day =
      String(
        date.getDate(),
      )
        .padStart(
          2,
          '0',
        );

    return `${year}-${month}-${day}`;
  }
}