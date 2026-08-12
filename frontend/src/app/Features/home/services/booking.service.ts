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

export interface BookingPeriodRequest {
  checkInDate: string;
  checkOutDate: string;
  guestsCount: number;
}

export interface BookingQuoteResponse {
  propertyId: string;
  propertyTitle: string;
  checkInDate: string;
  checkOutDate: string;
  guestsCount: number;
  nights: number;
  pricePerNight: number;
  subtotal: number;
  serviceFeePercentage: number;
  serviceFee: number;
  totalAmount: number;
  currency: string;
  cancellationPolicy: string;
}

export interface CreateBookingRequest {
  propertyId: string;
  checkInDate: string;
  checkOutDate: string;
  guestsCount: number;
  acceptedBookingTerms: boolean;
  acceptedCancellationPolicy: boolean;
  acceptedPropertyRules: boolean;
  acceptedComplaintPolicy: boolean;
}

export interface CreateBookingResponse {
  bookingId: string;
  propertyId: string;
  propertyTitle: string;
  guestUserId: string;
  checkInDate: string;
  checkOutDate: string;
  guestsCount: number;
  nights: number;
  pricePerNight: number;
  subtotal: number;
  serviceFee: number;
  totalAmount: number;
  currency: string;
  cancellationPolicy: string;
  status: BookingStatus;
  expiresAt: string;
  createdAt: string;
  message: string;
}

export interface GuestBookingsQuery {
  page?: number;
  pageSize?: number;
  status?: BookingStatus;
}

export interface GuestBookingsResponse {
  items: GuestBookingListItem[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  appliedStatusFilter?: string | null;
}

export interface GuestBookingListItem {
  bookingId: string;
  propertyId: string;
  property: GuestBookingProperty;
  checkInDate: string;
  checkOutDate: string;
  nights: number;
  guestsCount: number;
  totalAmount: number;
  currency: string;
  status: BookingStatus;
  canCancel: boolean;
  isPaymentWindowExpired: boolean;
  expiresAt?: string | null;
  createdAt: string;
  confirmedAt?: string | null;
  cancelledAt?: string | null;
  expiredAt?: string | null;
  completedAt?: string | null;
}

export interface GuestBookingProperty {
  id: string;
  title: string;
  country: string;
  city: string;
  coverImageUrl?: string | null;
}

@Injectable({
  providedIn: 'root',
})
export class BookingService {
  private readonly apiUrl = `${environment.baseApi}/api/bookings`;
  private readonly requestTimeoutMs = 30000;

  constructor(private readonly http: HttpClient) {}

  getBookingQuote(
    propertyId: string,
    request: BookingPeriodRequest,
  ): Observable<BookingQuoteResponse> {
    const params = new HttpParams()
      .set('checkInDate', request.checkInDate)
      .set('checkOutDate', request.checkOutDate)
      .set('guestsCount', String(request.guestsCount));

    return this.http
      .get(
        `${environment.baseApi}/api/properties/${propertyId}/booking-quote`,
        {
          params,
          responseType: 'text',
          withCredentials: true,
        },
      )
      .pipe(
        timeout(this.requestTimeoutMs),
        map((responseText) => {
          const parsedResponse = this.parseRequiredJson<any>(
            responseText,
            'Booking quote API returned an empty response.',
          );

          return {
            propertyId:
              parsedResponse.propertyId ?? parsedResponse.PropertyId ?? '',

            propertyTitle:
              parsedResponse.propertyTitle ??
              parsedResponse.PropertyTitle ??
              '',

            checkInDate: this.toDateOnly(
              parsedResponse.checkInDate ?? parsedResponse.CheckInDate,
            ),

            checkOutDate: this.toDateOnly(
              parsedResponse.checkOutDate ?? parsedResponse.CheckOutDate,
            ),

            guestsCount:
              parsedResponse.guestsCount ??
              parsedResponse.GuestsCount ??
              0,

            nights:
              parsedResponse.nights ??
              parsedResponse.Nights ??
              0,

            pricePerNight:
              parsedResponse.pricePerNight ??
              parsedResponse.PricePerNight ??
              0,

            subtotal:
              parsedResponse.subtotal ??
              parsedResponse.Subtotal ??
              0,

            serviceFeePercentage:
              parsedResponse.serviceFeePercentage ??
              parsedResponse.ServiceFeePercentage ??
              0,

            serviceFee:
              parsedResponse.serviceFee ??
              parsedResponse.ServiceFee ??
              0,

            totalAmount:
              parsedResponse.totalAmount ??
              parsedResponse.TotalAmount ??
              0,

            currency:
              parsedResponse.currency ??
              parsedResponse.Currency ??
              'EGP',

            cancellationPolicy:
              parsedResponse.cancellationPolicy ??
              parsedResponse.CancellationPolicy ??
              '',
          };
        }),
      );
  }

  createBooking(
    request: CreateBookingRequest,
  ): Observable<CreateBookingResponse> {
    return this.http
      .post(this.apiUrl, request, {
        responseType: 'text',
        withCredentials: true,
      })
      .pipe(
        timeout(this.requestTimeoutMs),
        map((responseText) => {
          const parsedResponse = this.parseRequiredJson<any>(
            responseText,
            'Booking API returned an empty response.',
          );

          const bookingId =
            parsedResponse.bookingId ??
            parsedResponse.BookingId ??
            '';

          if (!bookingId) {
            throw new Error(
              'Booking API response does not contain a booking identifier.',
            );
          }

          return {
            bookingId,

            propertyId:
              parsedResponse.propertyId ??
              parsedResponse.PropertyId ??
              '',

            propertyTitle:
              parsedResponse.propertyTitle ??
              parsedResponse.PropertyTitle ??
              '',

            guestUserId:
              parsedResponse.guestUserId ??
              parsedResponse.GuestUserId ??
              '',

            checkInDate: this.toDateOnly(
              parsedResponse.checkInDate ??
                parsedResponse.CheckInDate,
            ),

            checkOutDate: this.toDateOnly(
              parsedResponse.checkOutDate ??
                parsedResponse.CheckOutDate,
            ),

            guestsCount:
              parsedResponse.guestsCount ??
              parsedResponse.GuestsCount ??
              0,

            nights:
              parsedResponse.nights ??
              parsedResponse.Nights ??
              0,

            pricePerNight:
              parsedResponse.pricePerNight ??
              parsedResponse.PricePerNight ??
              0,

            subtotal:
              parsedResponse.subtotal ??
              parsedResponse.Subtotal ??
              0,

            serviceFee:
              parsedResponse.serviceFee ??
              parsedResponse.ServiceFee ??
              0,

            totalAmount:
              parsedResponse.totalAmount ??
              parsedResponse.TotalAmount ??
              0,

            currency:
              parsedResponse.currency ??
              parsedResponse.Currency ??
              'EGP',

            cancellationPolicy:
              parsedResponse.cancellationPolicy ??
              parsedResponse.CancellationPolicy ??
              '',

            status: this.normalizeBookingStatus(
              parsedResponse.status ??
                parsedResponse.Status,
            ),

            expiresAt:
              parsedResponse.expiresAt ??
              parsedResponse.ExpiresAt ??
              '',

            createdAt:
              parsedResponse.createdAt ??
              parsedResponse.CreatedAt ??
              new Date().toISOString(),

            message:
              parsedResponse.message ??
              parsedResponse.Message ??
              '',
          };
        }),
      );
  }

  getMyBookings(
    query: GuestBookingsQuery = {},
  ): Observable<GuestBookingsResponse> {
    const page = query.page ?? 1;
    const pageSize = query.pageSize ?? 50;

    let params = new HttpParams()
      .set('page', String(page))
      .set('pageSize', String(pageSize))
      .set('_ts', String(Date.now()));

    if (query.status) {
      params = params.set('status', query.status);
    }

    return this.http
      .get(`${this.apiUrl}/my-bookings`, {
        params,
        responseType: 'text',
        withCredentials: true,
      })
      .pipe(
        timeout(this.requestTimeoutMs),
        map((responseText) => {
          if (!this.normalizeResponseText(responseText)) {
            return this.createEmptyBookingsResponse(
              page,
              pageSize,
              query.status,
            );
          }

          const parsedResponse = this.parseRequiredJson<any>(
            responseText,
            'Bookings API returned an empty response.',
          );

          const rawItems =
            parsedResponse.items ??
            parsedResponse.Items ??
            [];

          return {
            items: Array.isArray(rawItems)
              ? rawItems.map((item: any) =>
                  this.mapGuestBookingItem(item),
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
              query.status ??
              null,
          };
        }),
      );
  }

  private mapGuestBookingItem(
    item: any,
  ): GuestBookingListItem {
    const rawProperty =
      item.property ??
      item.Property ??
      {};

    const propertyId =
      rawProperty.id ??
      rawProperty.Id ??
      item.propertyId ??
      item.PropertyId ??
      '';

    return {
      bookingId:
        item.bookingId ??
        item.BookingId ??
        item.id ??
        item.Id ??
        '',

      propertyId,

      property: {
        id: propertyId,

        title:
          rawProperty.title ??
          rawProperty.Title ??
          'Untitled property',

        country:
          rawProperty.country ??
          rawProperty.Country ??
          '',

        city:
          rawProperty.city ??
          rawProperty.City ??
          '',

        coverImageUrl:
          rawProperty.coverImageUrl ??
          rawProperty.CoverImageUrl ??
          null,
      },

      checkInDate: this.toDateOnly(
        item.checkInDate ??
          item.CheckInDate,
      ),

      checkOutDate: this.toDateOnly(
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

      status: this.normalizeBookingStatus(
        item.status ??
          item.Status,
      ),

      canCancel:
        item.canCancel ??
        item.CanCancel ??
        false,

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

  private createEmptyBookingsResponse(
    page: number,
    pageSize: number,
    status?: BookingStatus,
  ): GuestBookingsResponse {
    return {
      items: [],
      page,
      pageSize,
      totalCount: 0,
      totalPages: 1,
      appliedStatusFilter: status ?? null,
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

  private parseRequiredJson<T>(
    responseText: string,
    emptyMessage: string,
  ): T {
    const normalizedResponse =
      this.normalizeResponseText(
        responseText,
      );

    if (!normalizedResponse) {
      throw new Error(
        emptyMessage,
      );
    }

    try {
      return JSON.parse(
        normalizedResponse,
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