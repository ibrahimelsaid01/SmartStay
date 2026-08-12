import { Injectable, signal, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, forkJoin, of } from 'rxjs';
import { finalize, map, tap } from 'rxjs/operators';
import { environment } from '../../../../environments/environment';
import {
  DashboardStats,
  RecentActivity,
  BookingRequest,
} from '../models/dashboard.model';

interface HostBookingAmountByCurrencyResponse {
  currency: string;
  confirmedBookingSubtotal: number;
  completedBookingSubtotal: number;
}

interface HostBookingSummaryResponse {
  totalBookings: number;
  pendingBookings: number;
  confirmedBookings: number;
  cancelledBookings: number;
  completedBookings: number;
  expiredBookings: number;
  upcomingBookings: number;
  currentStays: number;
  amountsByCurrency: HostBookingAmountByCurrencyResponse[];
}

interface HostPropertyStatusSummaryResponse {
  totalProperties: number;
  draftProperties: number;
  pendingProperties: number;
  publishedProperties: number;
  rejectedProperties: number;
  unpublishedProperties: number;
}

interface HostBookingPropertyResponse {
  id: string;
  title: string;
  country: string;
  city: string;
  coverImageUrl?: string | null;
}

interface HostBookingGuestResponse {
  userId: string;
  firstName: string;
  lastName: string;
  fullName: string;
  email: string;
  phoneNumber?: string | null;
}

interface HostBookingListItemResponse {
  bookingId: string;
  property: HostBookingPropertyResponse;
  guest: HostBookingGuestResponse;
  checkInDate: string;
  checkOutDate: string;
  nights: number;
  guestsCount: number;
  subtotal: number;
  serviceFee: number;
  totalAmount: number;
  currency: string;
  status: string;
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

interface HostBookingsResponse {
  items: HostBookingListItemResponse[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  appliedStatusFilter?: string | null;
}

@Injectable({
  providedIn: 'root',
})
export class HostDashboardservice {
  private readonly http = inject(HttpClient);

  private readonly hostBookingsApiUrl = `${environment.baseApi}/api/host/bookings`;
  private readonly hostPropertiesApiUrl = `${environment.baseApi}/api/host/properties`;

  readonly stats = signal<DashboardStats | null>(null);
  readonly activities = signal<RecentActivity[]>([]);
  readonly bookingRequests = signal<BookingRequest[]>([]);
  readonly isLoading = signal<boolean>(false);

  getDashboardStats(): Observable<DashboardStats> {
    this.isLoading.set(true);

    return forkJoin({
      bookingSummary: this.http.get<HostBookingSummaryResponse>(
        `${this.hostBookingsApiUrl}/summary`
      ),
      propertySummary: this.http.get<HostPropertyStatusSummaryResponse>(
        `${this.hostPropertiesApiUrl}/summary`
      ),
    }).pipe(
      map(({ bookingSummary, propertySummary }) =>
        this.mapSummariesToDashboardStats(bookingSummary, propertySummary)
      ),
      tap(stats => this.stats.set(stats)),
      finalize(() => this.isLoading.set(false))
    );
  }

  getDashboardOverview(): Observable<{
    activities: RecentActivity[];
    requests: BookingRequest[];
  }> {
    return this.http
      .get<HostBookingsResponse>(
        `${this.hostBookingsApiUrl}?page=1&pageSize=5`
      )
      .pipe(
        map(response => {
          const recentBookings = response.items ?? [];

          return {
            activities: recentBookings.map(booking =>
              this.mapBookingToActivity(booking)
            ),
            requests: recentBookings
              .filter(booking => booking.status === 'Pending')
              .map(booking => this.mapBookingToRequest(booking)),
          };
        }),
        tap(data => {
          this.activities.set(data.activities);
          this.bookingRequests.set(data.requests);
        })
      );
  }

  handleBookingRequest(
    id: string,
    action: 'approve' | 'decline'
  ): Observable<void> {
    /*
     * ملاحظة:
     * الباك الحالي لا يحتوي على endpoint لـ approve/decline booking من host dashboard.
     * لذلك سنعمل UI update فقط مؤقتًا بدل استدعاء endpoint غير موجود يسبب 404.
     */
    this.bookingRequests.update(requests =>
      requests.filter(request => request.id !== id)
    );

    console.warn(
      `Booking ${id} was ${action}d locally. Backend endpoint is not implemented yet.`
    );

    return of(void 0);
  }

  toggleSmartControl(
    propertyId: string,
    device: 'door' | 'ac',
    status: boolean
  ): Observable<void> {
    /*
     * ملاحظة:
     * الباك الحالي لا يحتوي على Smart Controls / IoT endpoints.
     * نخليها local مؤقتًا حتى لا يطلع 404.
     */
    console.warn(
      `Smart control changed locally. propertyId=${propertyId}, device=${device}, status=${status}`
    );

    return of(void 0);
  }

  private mapSummariesToDashboardStats(
    bookingSummary: HostBookingSummaryResponse,
    propertySummary: HostPropertyStatusSummaryResponse
  ): DashboardStats {
    const firstCurrencyAmount = bookingSummary.amountsByCurrency?.[0];

    const confirmedRevenue =
      firstCurrencyAmount?.confirmedBookingSubtotal ?? 0;

    const completedRevenue =
      firstCurrencyAmount?.completedBookingSubtotal ?? 0;

    return {
      activeListings: {
        count: propertySummary.publishedProperties,
        lastAddedText: `${propertySummary.totalProperties} total properties`,
      },
      totalViews: {
        /*
         * ملاحظة:
         * الباك الحالي لا يرجع views summary للـ host dashboard.
         * لذلك نستخدم totalBookings كمؤشر مؤقت بدل mock ثابت.
         */
        count: bookingSummary.totalBookings,
        percentageChange: 0,
        isPositive: true,
      },
      totalReviews: {
        /*
         * ملاحظة:
         * يوجد host reviews feature في الباك، لكن لا يوجد dashboard summary واضح هنا.
         * هنسيبها 0 مؤقتًا إلى أن نربط reviews dashboard لاحقًا.
         */
        count: 0,
        averageRating: 0,
      },
      viewsChartData: [
        propertySummary.draftProperties,
        propertySummary.pendingProperties,
        propertySummary.publishedProperties,
        propertySummary.rejectedProperties,
        propertySummary.unpublishedProperties,
        propertySummary.totalProperties,
        bookingSummary.totalBookings,
      ],
      earningsChartData: [
        confirmedRevenue,
        completedRevenue,
        bookingSummary.confirmedBookings,
        bookingSummary.completedBookings,
        bookingSummary.pendingBookings,
        bookingSummary.cancelledBookings,
        bookingSummary.expiredBookings,
      ],
    };
  }

  private mapBookingToActivity(
    booking: HostBookingListItemResponse
  ): RecentActivity {
    return {
      id: booking.bookingId,
      icon: this.getActivityIcon(booking.status),
      message: `${booking.status} booking for`,
      targetName: booking.property?.title || 'Property',
      timeAgo: this.formatTimeAgo(booking.createdAt),
    };
  }

  private mapBookingToRequest(
    booking: HostBookingListItemResponse
  ): BookingRequest {
    return {
      id: booking.bookingId,
      guestName:
        booking.guest?.fullName ||
        `${booking.guest?.firstName ?? ''} ${booking.guest?.lastName ?? ''}`.trim() ||
        'Guest',
      guestInitials: this.getInitials(
        booking.guest?.fullName ||
          `${booking.guest?.firstName ?? ''} ${booking.guest?.lastName ?? ''}`
      ),
      propertyName: booking.property?.title || 'Property',
      dates: this.formatDateRange(booking.checkInDate, booking.checkOutDate),
    };
  }

  private getActivityIcon(status: string): string {
    switch (status) {
      case 'Confirmed':
        return 'bi-calendar-check';
      case 'Pending':
        return 'bi-hourglass-split';
      case 'Cancelled':
        return 'bi-calendar-x';
      case 'Completed':
        return 'bi-check-circle';
      case 'Expired':
        return 'bi-clock-history';
      default:
        return 'bi-calendar-plus';
    }
  }

  private getInitials(name: string): string {
    const words = name.trim().split(' ').filter(Boolean);

    if (words.length === 0) {
      return 'G';
    }

    if (words.length === 1) {
      return words[0].slice(0, 2).toUpperCase();
    }

    return `${words[0][0]}${words[1][0]}`.toUpperCase();
  }

  private formatDateRange(checkInDate: string, checkOutDate: string): string {
    const checkIn = new Date(checkInDate);
    const checkOut = new Date(checkOutDate);

    if (Number.isNaN(checkIn.getTime()) || Number.isNaN(checkOut.getTime())) {
      return `${checkInDate} - ${checkOutDate}`;
    }

    const formatter = new Intl.DateTimeFormat('en', {
      month: 'short',
      day: 'numeric',
    });

    return `${formatter.format(checkIn)} - ${formatter.format(checkOut)}`;
  }

  private formatTimeAgo(dateValue: string): string {
    const date = new Date(dateValue);

    if (Number.isNaN(date.getTime())) {
      return 'recently';
    }

    const diffInMs = Date.now() - date.getTime();
    const diffInMinutes = Math.floor(diffInMs / 60_000);
    const diffInHours = Math.floor(diffInMinutes / 60);
    const diffInDays = Math.floor(diffInHours / 24);

    if (diffInMinutes < 1) {
      return 'just now';
    }

    if (diffInMinutes < 60) {
      return `${diffInMinutes} minutes ago`;
    }

    if (diffInHours < 24) {
      return `${diffInHours} hours ago`;
    }

    return `${diffInDays} days ago`;
  }
}