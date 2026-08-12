import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, map, timeout } from 'rxjs';
import { environment } from '../../../../environments/environment';

export type BookingStatus =
  | 'Pending'
  | 'Confirmed'
  | 'Cancelled'
  | 'Completed'
  | 'Expired';

export type BookingReviewStatus =
  | 'Pending'
  | 'Posted'
  | 'Rejected';

export interface GuestBookingProperty {
  id: string;
  title: string;
  country: string;
  city: string;
  coverImageUrl?: string | null;
}

export interface GuestBookingListItem {
  bookingId: string;
  property: GuestBookingProperty;
  checkInDate: string;
  checkOutDate: string;
  nights: number;
  guestsCount: number;
  totalAmount: number;
  currency: string;
  status: BookingStatus;
  canCancel: boolean;
  canReview: boolean;
  hasReview: boolean;
  reviewId?: string | null;
  reviewStatus?: BookingReviewStatus | null;
  isPaymentWindowExpired: boolean;
  expiresAt?: string | null;
  createdAt: string;
  confirmedAt?: string | null;
  cancelledAt?: string | null;
  expiredAt?: string | null;
  completedAt?: string | null;
}

export interface GuestBookingsResponse {
  items: GuestBookingListItem[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  appliedStatusFilter?: string | null;
}

export interface CancelBookingRequest {
  reason?: string | null;
}

export interface CancelBookingResponse {
  bookingId: string;
  status: BookingStatus;
  cancellationPolicy: string;
  estimatedRefundPercentage: number;
  estimatedRefundAmount: number;
  currency: string;
  cancellationReason?: string | null;
  cancelledAt: string;
  isRefundRequired: boolean;
  refundId?: string | null;
  providerRefundId?: string | null;
  refundStatus?: string | null;
  refundAmount: number;
  refundMessage?: string | null;
  message: string;
}

export interface GuestBookingConfirmationProperty {
  id: string;
  title: string;
  propertyType: string;
  coverImageUrl?: string | null;
  country: string;
  city: string;
  streetAddress?: string | null;
  buildingNumber?: string | null;
  floor?: string | null;
  apartmentNumber?: string | null;
  postalCode?: string | null;
  fullAddress: string;
  latitude?: number | null;
  longitude?: number | null;
}

export interface GuestBookingConfirmationStay {
  checkInDate: string;
  checkOutDate: string;
  guestsCount: number;
  nights: number;
}

export interface GuestBookingConfirmationPricing {
  pricePerNight: number;
  subtotal: number;
  serviceFee: number;
  totalAmount: number;
  currency: string;
}

export interface GuestBookingConfirmationPayment {
  paymentId: string;
  status: string;
  provider: string;
  amount: number;
  refundedAmount: number;
  currency: string;
  succeededAt?: string | null;
}

export interface GuestBookingConfirmationResponse {
  bookingId: string;
  status: BookingStatus;
  confirmedAt?: string | null;
  guestEmail: string;
  property: GuestBookingConfirmationProperty;
  stay: GuestBookingConfirmationStay;
  pricing: GuestBookingConfirmationPricing;
  payment: GuestBookingConfirmationPayment;
}

@Injectable({
  providedIn: 'root',
})
export class GuestBookingsService {
  private readonly bookingsApiUrl =
    `${environment.baseApi}/api/bookings`;

  private readonly requestTimeoutMs = 30000;

  constructor(
    private readonly http: HttpClient,
  ) {}

  getMyBookings(
    page = 1,
    pageSize = 10,
    status?: BookingStatus,
  ): Observable<GuestBookingsResponse> {
    let params = new HttpParams()
      .set('page', String(page))
      .set('pageSize', String(pageSize))
      .set('_ts', String(Date.now()));

    if (status) {
      params = params.set(
        'status',
        status,
      );
    }

    return this.http
      .get(
        `${this.bookingsApiUrl}/my-bookings`,
        {
          params,
          responseType: 'text',
          withCredentials: true,
        },
      )
      .pipe(
        timeout(this.requestTimeoutMs),
        map((responseText) => {
          const normalizedResponse =
            this.normalizeResponseText(
              responseText,
            );

          if (!normalizedResponse) {
            return {
              items: [],
              page,
              pageSize,
              totalCount: 0,
              totalPages: 1,
              appliedStatusFilter:
                status ?? null,
            };
          }

          const parsedResponse =
            this.parseJson<any>(
              normalizedResponse,
            );

          const rawItems =
            parsedResponse.items ??
            parsedResponse.Items ??
            [];

          return {
            items: Array.isArray(rawItems)
              ? rawItems.map(
                  (item: any) =>
                    this.mapBookingItem(
                      item,
                    ),
                )
              : [],

            page:
              parsedResponse.page ??
              parsedResponse.Page ??
              page,

            pageSize:
              parsedResponse.pageSize ??
              parsedResponse.PageSize ??
              pageSize,

            totalCount:
              parsedResponse.totalCount ??
              parsedResponse.TotalCount ??
              0,

            totalPages: Math.max(
              1,
              parsedResponse.totalPages ??
                parsedResponse.TotalPages ??
                1,
            ),

            appliedStatusFilter:
              parsedResponse.appliedStatusFilter ??
              parsedResponse.AppliedStatusFilter ??
              status ??
              null,
          };
        }),
      );
  }

  cancelBooking(
    bookingId: string,
    reason?: string,
  ): Observable<CancelBookingResponse> {
    const body: CancelBookingRequest = {
      reason:
        reason?.trim() || null,
    };

    return this.http
      .post(
        `${this.bookingsApiUrl}/${bookingId}/cancel`,
        body,
        {
          responseType: 'text',
          withCredentials: true,
        },
      )
      .pipe(
        timeout(this.requestTimeoutMs),
        map((responseText) => {
          const response =
            this.parseRequiredJson<any>(
              responseText,
            );

          return {
            bookingId:
              response.bookingId ??
              response.BookingId ??
              bookingId,

            status:
              this.normalizeBookingStatus(
                response.status ??
                  response.Status,
              ),

            cancellationPolicy:
              response.cancellationPolicy ??
              response.CancellationPolicy ??
              '',

            estimatedRefundPercentage:
              response.estimatedRefundPercentage ??
              response.EstimatedRefundPercentage ??
              0,

            estimatedRefundAmount:
              response.estimatedRefundAmount ??
              response.EstimatedRefundAmount ??
              0,

            currency:
              response.currency ??
              response.Currency ??
              'EGP',

            cancellationReason:
              response.cancellationReason ??
              response.CancellationReason ??
              null,

            cancelledAt:
              response.cancelledAt ??
              response.CancelledAt ??
              new Date().toISOString(),

            isRefundRequired:
              response.isRefundRequired ??
              response.IsRefundRequired ??
              false,

            refundId:
              response.refundId ??
              response.RefundId ??
              null,

            providerRefundId:
              response.providerRefundId ??
              response.ProviderRefundId ??
              null,

            refundStatus:
              response.refundStatus ??
              response.RefundStatus ??
              null,

            refundAmount:
              response.refundAmount ??
              response.RefundAmount ??
              0,

            refundMessage:
              response.refundMessage ??
              response.RefundMessage ??
              null,

            message:
              response.message ??
              response.Message ??
              'Booking cancelled successfully.',
          };
        }),
      );
  }

  getBookingConfirmation(
    bookingId: string,
  ): Observable<GuestBookingConfirmationResponse> {
    return this.http
      .get(
        `${this.bookingsApiUrl}/${bookingId}/confirmation`,
        {
          responseType: 'text',
          withCredentials: true,
        },
      )
      .pipe(
        timeout(this.requestTimeoutMs),
        map((responseText) =>
          this.parseRequiredJson<GuestBookingConfirmationResponse>(
            responseText,
          ),
        ),
      );
  }

  private mapBookingItem(
    item: any,
  ): GuestBookingListItem {
    const property =
      item.property ??
      item.Property ??
      {};

    return {
      bookingId:
        item.bookingId ??
        item.BookingId ??
        '',

      property: {
        id:
          property.id ??
          property.Id ??
          '',

        title:
          property.title ??
          property.Title ??
          'Untitled property',

        country:
          property.country ??
          property.Country ??
          '',

        city:
          property.city ??
          property.City ??
          '',

        coverImageUrl:
          property.coverImageUrl ??
          property.CoverImageUrl ??
          null,
      },

      checkInDate:
        this.toDateOnly(
          item.checkInDate ??
            item.CheckInDate,
        ),

      checkOutDate:
        this.toDateOnly(
          item.checkOutDate ??
            item.CheckOutDate,
        ),

      nights:
        item.nights ??
        item.Nights ??
        0,

      guestsCount:
        item.guestsCount ??
        item.GuestsCount ??
        0,

      totalAmount:
        item.totalAmount ??
        item.TotalAmount ??
        0,

      currency:
        item.currency ??
        item.Currency ??
        'EGP',

      status:
        this.normalizeBookingStatus(
          item.status ??
            item.Status,
        ),

      canCancel:
        item.canCancel ??
        item.CanCancel ??
        false,

      canReview:
        item.canReview ??
        item.CanReview ??
        false,

      hasReview:
        item.hasReview ??
        item.HasReview ??
        false,

      reviewId:
        item.reviewId ??
        item.ReviewId ??
        null,

      reviewStatus:
        this.normalizeReviewStatus(
          item.reviewStatus ??
            item.ReviewStatus,
        ),

      isPaymentWindowExpired:
        item.isPaymentWindowExpired ??
        item.IsPaymentWindowExpired ??
        false,

      expiresAt:
        item.expiresAt ??
        item.ExpiresAt ??
        null,

      createdAt:
        item.createdAt ??
        item.CreatedAt ??
        new Date().toISOString(),

      confirmedAt:
        item.confirmedAt ??
        item.ConfirmedAt ??
        null,

      cancelledAt:
        item.cancelledAt ??
        item.CancelledAt ??
        null,

      expiredAt:
        item.expiredAt ??
        item.ExpiredAt ??
        null,

      completedAt:
        item.completedAt ??
        item.CompletedAt ??
        null,
    };
  }

  private normalizeBookingStatus(
    value: unknown,
  ): BookingStatus {
    const normalizedValue =
      String(value ?? '')
        .trim()
        .toLowerCase();

    switch (normalizedValue) {
      case '1':
      case 'pending':
        return 'Pending';

      case '2':
      case 'confirmed':
        return 'Confirmed';

      case '3':
      case 'cancelled':
      case 'canceled':
        return 'Cancelled';

      case '4':
      case 'completed':
        return 'Completed';

      case '5':
      case 'expired':
        return 'Expired';

      default:
        return 'Pending';
    }
  }

  private normalizeReviewStatus(
    value: unknown,
  ): BookingReviewStatus | null {
    const normalizedValue =
      String(value ?? '')
        .trim()
        .toLowerCase();

    switch (normalizedValue) {
      case '1':
      case 'pending':
        return 'Pending';

      case '2':
      case 'posted':
        return 'Posted';

      case '3':
      case 'rejected':
        return 'Rejected';

      default:
        return null;
    }
  }

  private parseRequiredJson<T>(
    responseText: string,
  ): T {
    const normalizedResponse =
      this.normalizeResponseText(
        responseText,
      );

    if (!normalizedResponse) {
      throw new Error(
        'The server returned an empty response.',
      );
    }

    return this.parseJson<T>(
      normalizedResponse,
    );
  }

  private parseJson<T>(
    responseText: string,
  ): T {
    try {
      return JSON.parse(
        responseText,
      ) as T;
    } catch {
      throw new Error(
        'The server returned an invalid JSON response.',
      );
    }
  }

  private normalizeResponseText(
    responseText: string,
  ): string {
    return (responseText ?? '')
      .replace(/^\uFEFF/, '')
      .trim();
  }

  private toDateOnly(
    value?: string | null,
  ): string {
    if (!value) {
      return '';
    }

    return String(value)
      .split('T')[0];
  }
}