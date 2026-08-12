import {
  HttpClient,
  HttpParams,
} from '@angular/common/http';
import { Injectable } from '@angular/core';
import {
  Observable,
  map,
  timeout,
} from 'rxjs';
import { environment } from '../../../../environments/environment';

export type AdminBookingStatus =
  | 'Pending'
  | 'Confirmed'
  | 'Cancelled'
  | 'Completed'
  | 'Expired';

export interface AdminBookingsQuery {
  page?: number;
  pageSize?: number;
  status?: AdminBookingStatus | null;
  propertyId?: string;
  guestUserId?: string;
  hostUserId?: string;
  checkInFrom?: string;
  checkInTo?: string;
}

export interface AdminBookingPropertySummary {
  id: string;
  title: string;
  country: string;
  city: string;
  coverImageUrl?: string | null;
}

export interface AdminBookingUserSummary {
  userId: string;
  firstName: string;
  lastName: string;
  fullName: string;
  email: string;
  phoneNumber?: string | null;
  isActive: boolean;
}

export interface AdminBookingListItem {
  bookingId: string;
  property: AdminBookingPropertySummary;
  guest: AdminBookingUserSummary;
  host: AdminBookingUserSummary;
  checkInDate: string;
  checkOutDate: string;
  nights: number;
  guestsCount: number;
  subtotal: number;
  serviceFee: number;
  totalAmount: number;
  currency: string;
  cancellationPolicy: string;
  status: AdminBookingStatus;
  isUpcoming: boolean;
  isCurrentlyStaying: boolean;
  isPaymentWindowExpired: boolean;
  expiresAt?: string | null;
  createdAt: string;
  confirmedAt?: string | null;
  cancelledAt?: string | null;
  expiredAt?: string | null;
  completedAt?: string | null;
}

export interface AdminBookingDetails
  extends AdminBookingListItem {
  pricePerNight: number;
  cancellationReason?: string | null;
  updatedAt?: string | null;
}

export interface AdminBookingsResponse {
  items: AdminBookingListItem[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  appliedStatusFilter?: string | null;
  appliedPropertyIdFilter?: string | null;
  appliedGuestUserIdFilter?: string | null;
  appliedHostUserIdFilter?: string | null;
  appliedCheckInFromFilter?: string | null;
  appliedCheckInToFilter?: string | null;
}

export interface AdminBookingsCurrencyAmount {
  currency: string;
  confirmedGrossAmount: number;
  confirmedServiceFees: number;
  completedGrossAmount: number;
  completedServiceFees: number;
}

export interface AdminBookingsSummary {
  totalBookings: number;
  pendingBookings: number;
  confirmedBookings: number;
  cancelledBookings: number;
  completedBookings: number;
  expiredBookings: number;
  upcomingBookings: number;
  currentStays: number;
  amountsByCurrency:
    AdminBookingsCurrencyAmount[];
}

@Injectable({
  providedIn: 'root',
})
export class AdminBookingsService {
  private readonly apiUrl =
    `${environment.baseApi}/api/admin/bookings`;

  private readonly requestTimeoutMs = 30000;

  constructor(
    private readonly http: HttpClient,
  ) {}

  getSummary():
    Observable<AdminBookingsSummary> {
    return this.http
      .get(
        `${this.apiUrl}/summary`,
        {
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
            return this.createEmptySummary();
          }

          const response =
            this.parseJson<any>(
              normalizedResponse,
            );

          const rawAmounts =
            response.amountsByCurrency ??
            response.AmountsByCurrency ??
            [];

          return {
            totalBookings:
              response.totalBookings ??
              response.TotalBookings ??
              0,
            pendingBookings:
              response.pendingBookings ??
              response.PendingBookings ??
              0,
            confirmedBookings:
              response.confirmedBookings ??
              response.ConfirmedBookings ??
              0,
            cancelledBookings:
              response.cancelledBookings ??
              response.CancelledBookings ??
              0,
            completedBookings:
              response.completedBookings ??
              response.CompletedBookings ??
              0,
            expiredBookings:
              response.expiredBookings ??
              response.ExpiredBookings ??
              0,
            upcomingBookings:
              response.upcomingBookings ??
              response.UpcomingBookings ??
              0,
            currentStays:
              response.currentStays ??
              response.CurrentStays ??
              0,
            amountsByCurrency:
              Array.isArray(rawAmounts)
                ? rawAmounts.map(
                    (item: any) => ({
                      currency:
                        item.currency ??
                        item.Currency ??
                        'EGP',
                      confirmedGrossAmount:
                        item.confirmedGrossAmount ??
                        item.ConfirmedGrossAmount ??
                        0,
                      confirmedServiceFees:
                        item.confirmedServiceFees ??
                        item.ConfirmedServiceFees ??
                        0,
                      completedGrossAmount:
                        item.completedGrossAmount ??
                        item.CompletedGrossAmount ??
                        0,
                      completedServiceFees:
                        item.completedServiceFees ??
                        item.CompletedServiceFees ??
                        0,
                    }),
                  )
                : [],
          };
        }),
      );
  }

  getBookings(
    query: AdminBookingsQuery = {},
  ): Observable<AdminBookingsResponse> {
    const page = query.page ?? 1;
    const pageSize = query.pageSize ?? 10;

    let params = new HttpParams()
      .set('page', String(page))
      .set('pageSize', String(pageSize))
      .set('_ts', String(Date.now()));

    if (query.status) {
      params = params.set(
        'status',
        query.status,
      );
    }

    if (query.propertyId?.trim()) {
      params = params.set(
        'propertyId',
        query.propertyId.trim(),
      );
    }

    if (query.guestUserId?.trim()) {
      params = params.set(
        'guestUserId',
        query.guestUserId.trim(),
      );
    }

    if (query.hostUserId?.trim()) {
      params = params.set(
        'hostUserId',
        query.hostUserId.trim(),
      );
    }

    if (query.checkInFrom) {
      params = params.set(
        'checkInFrom',
        query.checkInFrom,
      );
    }

    if (query.checkInTo) {
      params = params.set(
        'checkInTo',
        query.checkInTo,
      );
    }

    return this.http
      .get(
        this.apiUrl,
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
            return this.createEmptyBookingsResponse(
              page,
              pageSize,
              query,
            );
          }

          const response =
            this.parseJson<any>(
              normalizedResponse,
            );

          const rawItems =
            response.items ??
            response.Items ??
            [];

          return {
            items: Array.isArray(rawItems)
              ? rawItems.map(
                  (item: any) =>
                    this.mapBookingListItem(
                      item,
                    ),
                )
              : [],
            page:
              response.page ??
              response.Page ??
              page,
            pageSize:
              response.pageSize ??
              response.PageSize ??
              pageSize,
            totalCount:
              response.totalCount ??
              response.TotalCount ??
              0,
            totalPages: Math.max(
              1,
              response.totalPages ??
                response.TotalPages ??
                1,
            ),
            appliedStatusFilter:
              response.appliedStatusFilter ??
              response.AppliedStatusFilter ??
              query.status ??
              null,
            appliedPropertyIdFilter:
              response.appliedPropertyIdFilter ??
              response.AppliedPropertyIdFilter ??
              query.propertyId ??
              null,
            appliedGuestUserIdFilter:
              response.appliedGuestUserIdFilter ??
              response.AppliedGuestUserIdFilter ??
              query.guestUserId ??
              null,
            appliedHostUserIdFilter:
              response.appliedHostUserIdFilter ??
              response.AppliedHostUserIdFilter ??
              query.hostUserId ??
              null,
            appliedCheckInFromFilter:
              this.toDateOnly(
                response.appliedCheckInFromFilter ??
                  response.AppliedCheckInFromFilter ??
                  query.checkInFrom,
              ),
            appliedCheckInToFilter:
              this.toDateOnly(
                response.appliedCheckInToFilter ??
                  response.AppliedCheckInToFilter ??
                  query.checkInTo,
              ),
          };
        }),
      );
  }

  getBookingDetails(
    bookingId: string,
  ): Observable<AdminBookingDetails> {
    return this.http
      .get(
        `${this.apiUrl}/${bookingId}`,
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

          const listItem =
            this.mapBookingListItem(
              response,
            );

          return {
            ...listItem,
            pricePerNight:
              response.pricePerNight ??
              response.PricePerNight ??
              0,
            cancellationReason:
              response.cancellationReason ??
              response.CancellationReason ??
              null,
            updatedAt:
              response.updatedAt ??
              response.UpdatedAt ??
              null,
          };
        }),
      );
  }

  private mapBookingListItem(
    item: any,
  ): AdminBookingListItem {
    return {
      bookingId:
        item.bookingId ??
        item.BookingId ??
        '',
      property: this.mapProperty(
        item.property ??
        item.Property ??
        {},
      ),
      guest: this.mapUser(
        item.guest ??
        item.Guest ??
        {},
      ),
      host: this.mapUser(
        item.host ??
        item.Host ??
        {},
      ),
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
      subtotal:
        item.subtotal ??
        item.Subtotal ??
        0,
      serviceFee:
        item.serviceFee ??
        item.ServiceFee ??
        0,
      totalAmount:
        item.totalAmount ??
        item.TotalAmount ??
        0,
      currency:
        item.currency ??
        item.Currency ??
        'EGP',
      cancellationPolicy:
        item.cancellationPolicy ??
        item.CancellationPolicy ??
        '',
      status:
        this.normalizeBookingStatus(
          item.status ??
          item.Status,
        ),
      isUpcoming:
        item.isUpcoming ??
        item.IsUpcoming ??
        false,
      isCurrentlyStaying:
        item.isCurrentlyStaying ??
        item.IsCurrentlyStaying ??
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

  private mapProperty(
    value: any,
  ): AdminBookingPropertySummary {
    return {
      id:
        value.id ??
        value.Id ??
        '',
      title:
        value.title ??
        value.Title ??
        'Untitled property',
      country:
        value.country ??
        value.Country ??
        '',
      city:
        value.city ??
        value.City ??
        '',
      coverImageUrl:
        value.coverImageUrl ??
        value.CoverImageUrl ??
        null,
    };
  }

  private mapUser(
    value: any,
  ): AdminBookingUserSummary {
    const firstName =
      value.firstName ??
      value.FirstName ??
      '';

    const lastName =
      value.lastName ??
      value.LastName ??
      '';

    return {
      userId:
        value.userId ??
        value.UserId ??
        '',
      firstName,
      lastName,
      fullName:
        value.fullName ??
        value.FullName ??
        `${firstName} ${lastName}`.trim(),
      email:
        value.email ??
        value.Email ??
        '',
      phoneNumber:
        value.phoneNumber ??
        value.PhoneNumber ??
        null,
      isActive:
        value.isActive ??
        value.IsActive ??
        false,
    };
  }

  private normalizeBookingStatus(
    value: unknown,
  ): AdminBookingStatus {
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

  private createEmptySummary():
    AdminBookingsSummary {
    return {
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
  }

  private createEmptyBookingsResponse(
    page: number,
    pageSize: number,
    query: AdminBookingsQuery,
  ): AdminBookingsResponse {
    return {
      items: [],
      page,
      pageSize,
      totalCount: 0,
      totalPages: 1,
      appliedStatusFilter:
        query.status ?? null,
      appliedPropertyIdFilter:
        query.propertyId ?? null,
      appliedGuestUserIdFilter:
        query.guestUserId ?? null,
      appliedHostUserIdFilter:
        query.hostUserId ?? null,
      appliedCheckInFromFilter:
        query.checkInFrom ?? null,
      appliedCheckInToFilter:
        query.checkInTo ?? null,
    };
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
      return JSON.parse(responseText) as T;
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

    return String(value).split('T')[0];
  }
}