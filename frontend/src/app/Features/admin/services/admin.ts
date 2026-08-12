import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../../environments/environment';

export interface AdminDashboardFinancialSummary {
  currency: string;
  totalPaymentAttempts: number;
  pendingPayments: number;
  successfulPayments: number;
  failedPayments: number;
  cancelledPayments: number;
  partiallyRefundedPayments: number;
  fullyRefundedPayments: number;
  grossVolume: number;
  platformRevenue: number;
  totalRefundedAmount: number;
  netVolume: number;
  successRatePercentage: number;
}
export interface AdminBookingsQuery {
  page?: number;
  pageSize?: number;
  status?: number | null;
  propertyId?: string;
  guestUserId?: string;
  hostUserId?: string;
  checkInFrom?: string;
  checkInTo?: string;
}

export interface AdminBookingsResponse {
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  items: AdminBookingListItem[];
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
  property?: AdminBookingPropertySummary | null;
  guest?: AdminBookingUserSummary | null;
  host?: AdminBookingUserSummary | null;

  status: string | number;
  paymentStatus?: string | number | null;

  checkInDate?: string | null;
  checkOutDate?: string | null;
  nights?: number;
  guestsCount?: number;

  pricePerNight?: number;
  subtotal?: number;
  serviceFee?: number;
  totalAmount?: number;
  currency?: string;

  createdAt?: string | null;
  updatedAt?: string | null;
  expiresAt?: string | null;
  confirmedAt?: string | null;
  cancelledAt?: string | null;
  completedAt?: string | null;
  expiredAt?: string | null;
}

export interface AdminBookingDetails extends AdminBookingListItem {
  cancellationPolicySnapshot?: string | number | null;
  cancellationReason?: string | null;
  paymentId?: string | null;
  providerPaymentId?: string | null;
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
  amountsByCurrency: AdminBookingsCurrencyAmount[];
}

export interface AdminBookingsCurrencyAmount {
  currency: string;
  confirmedGrossAmount: number;
  confirmedServiceFees: number;
  completedGrossAmount: number;
  completedServiceFees: number;
}

export interface AdminDashboardSummary {
  generatedAt: string;

  totalUsers: number;
  activeUsers: number;
  inactiveUsers: number;
  totalGuests: number;
  totalHosts: number;
  totalAdmins: number;

  totalHostApplications: number;
  draftHostApplications: number;
  pendingHostApplications: number;
  approvedHostApplications: number;
  rejectedHostApplications: number;

  totalProperties: number;
  totalListings: number;
  draftProperties: number;
  pendingPropertyVerifications: number;
  publishedProperties: number;
  rejectedProperties: number;
  unpublishedProperties: number;
  pendingVerifications: number;

  totalBookings: number;
  pendingBookings: number;
  confirmedBookings: number;
  cancelledBookings: number;
  completedBookings: number;
  expiredBookings: number;

  financials: AdminDashboardFinancialSummary[];
}

export interface AdminHostApplicationSummary {
  id: string;
  displayName: string;
  userFullName: string;
  email: string;
  phoneNumber: string;
  country: string;
  city: string;
  profileImageUrl?: string | null;
  status: string;
  hasIdentityDocument: boolean;
  createdAt: string;
  submittedAt?: string | null;
}

export interface AdminHostApplicationDetails
  extends AdminHostApplicationSummary {
  bio: string;
  rejectionReason?: string | null;
  hasProfileImage: boolean;
  updatedAt?: string | null;
  reviewedAt?: string | null;
}

export interface RejectHostApplicationRequest {
  reason: string;
}

export interface AdminUsersResponse {
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  items: AdminUserListItem[];
}

export interface AdminUserListItem {
  userId: string;
  email?: string | null;
  phoneNumber?: string | null;
  fullName: string;
  firstName?: string | null;
  lastName?: string | null;
  profileImageUrl?: string | null;
  isActive: boolean;
  isProfileCompleted: boolean;
  createdAt: string;
  updatedAt?: string | null;
  roles: string[];
  isHost: boolean;
  hostProfileId?: string | null;
  hostStatus?: string | null;
  propertiesCount: number;
  guestBookingsCount: number;
}

export interface AdminUserStatusResponse {
  userId: string;
  isActive: boolean;
  message: string;
}

export interface AdminUserQuery {
  page?: number;
  pageSize?: number;
  search?: string;
  role?: 'Admin' | 'Host' | 'User' | '';
  isActive?: boolean | null;
  isProfileCompleted?: boolean | null;
}

export interface AdminPendingPropertySummary {
  propertyId: string;
  title: string;
  hostName: string;
  hostEmail: string;
  country: string;
  city: string;
  coverImageUrl?: string | null;
  propertyType: string;
  spaceType: string;
  pricePerNight: number;
  currency: string;
  status: string;
  submittedAt?: string | null;
  createdAt: string;
  imagesCount: number;
  hasVerificationDocument: boolean;
}

export interface AdminPropertyDetails extends AdminPendingPropertySummary {
  description: string;
  streetAddress?: string | null;
  buildingNumber?: string | null;
  floor?: string | null;
  apartmentNumber?: string | null;
  postalCode?: string | null;
  latitude?: number | null;
  longitude?: number | null;
  maxGuests: number;
  bedrooms: number;
  beds: number;
  bathrooms: number;
  cancellationPolicy: string;
  checkInTime?: string | null;
  checkOutTime?: string | null;
  allowsSmoking: boolean;
  allowsPets: boolean;
  allowsParties: boolean;
  allowsChildren: boolean;
  additionalHouseRules?: string | null;
  images: AdminPropertyImage[];
  verificationDocument?: AdminPropertyVerificationDocument | null;
}

export interface AdminPropertyImage {
  id: string;
  imageUrl: string;
  isCover: boolean;
}

export interface AdminPropertyVerificationDocument {
  id: string;
  documentType: string;
  fileUrl: string;
  uploadedAt: string;
}

export interface RejectPropertyRequest {
  reason: string;
}

export interface AdminFinancialsSummaryResponse {
  generatedAt: string;
  currencies: AdminFinancialCurrencySummary[];
}

export interface AdminFinancialCurrencySummary {
  currency: string;
  totalPaymentAttempts: number;
  pendingPayments: number;
  successfulPayments: number;
  failedPayments: number;
  cancelledPayments: number;
  partiallyRefundedPayments: number;
  fullyRefundedPayments: number;
  grossVolume: number;
  platformRevenue: number;
  totalRefundedAmount: number;
  netVolume: number;
  totalRefundRequests: number;
  pendingRefundRequests: number;
  successfulRefundRequests: number;
  failedRefundRequests: number;
  successRatePercentage: number;
  pendingPayoutRequests: number;
  pendingPayoutAmount: number;
}

export interface AdminFinancialTransactionsQuery {
  search?: string;
  type?: string;
  currency?: string;
  status?: string;
  fromDate?: string;
  toDate?: string;
  page?: number;
  pageSize?: number;
}

export interface AdminFinancialTransactionsResponse {
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  items: AdminFinancialTransaction[];
}

export interface AdminFinancialTransaction {
  transactionId: string;
  referenceCode?: string | null;
  type: string;
  direction: string;
  provider?: string | null;
  providerTransactionId?: string | null;
  bookingId?: string | null;
  paymentId?: string | null;
  refundId?: string | null;
  userId?: string | null;
  userName?: string | null;
  userEmail?: string | null;
  propertyTitle?: string | null;
  currency: string;
  amount: number;
  signedAmount: number;
  platformFee: number;
  refundedAmount: number;
  netAmount: number;
  status: string;
  failureReason?: string | null;
  createdAt: string;
}
export interface AdminSupportTicketsQuery {
  search?: string;
  status?: string;
  category?: string;
  urgency?: string;
  page?: number;
  pageSize?: number;
}

export interface AdminSupportTicketsResponse {
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  items: AdminSupportTicketSummary[];
}

export interface AdminSupportTicketSummary {
  ticketId: string;
  referenceCode?: string | null;
  subject: string;
  category: string;
  urgency: string;
  status: string;
  createdByUserId: string;
  createdByName: string;
  createdByEmail: string;
  bookingId?: string | null;
  propertyId?: string | null;
  propertyTitle?: string | null;
  messagesCount: number;
  createdAt: string;
  updatedAt?: string | null;
  resolvedAt?: string | null;
}

export interface AdminSupportTicketDetails extends AdminSupportTicketSummary {
  description: string;
  resolutionNote?: string | null;
  messages: AdminSupportTicketMessage[];
}

export interface AdminSupportTicketMessage {
  messageId: string;
  senderUserId: string;
  senderName: string;
  senderEmail: string;
  isAdminMessage: boolean;
  message: string;
  createdAt: string;
}

export interface AdminSupportReplyRequest {
  message: string;
}

export interface AdminSupportResolveRequest {
  resolutionNote: string;
}
@Injectable({
  providedIn: 'root',
})
export class Admin {
  private readonly apiUrl = `${environment.baseApi}/api/admin`;

  constructor(private http: HttpClient) {}

  getDashboardSummary(): Observable<AdminDashboardSummary> {
    return this.http.get<AdminDashboardSummary>(
      `${this.apiUrl}/dashboard/summary`
    );
  }

  getPendingHostApplications(): Observable<AdminHostApplicationSummary[]> {
    return this.http
      .get(`${this.apiUrl}/host-applications/pending`, {
        responseType: 'text',
      })
      .pipe(
        map(responseText => {
          if (!responseText) {
            return [];
          }

          const parsedResponse = JSON.parse(responseText);

          if (Array.isArray(parsedResponse)) {
            return parsedResponse as AdminHostApplicationSummary[];
          }

          if (Array.isArray(parsedResponse.items)) {
            return parsedResponse.items as AdminHostApplicationSummary[];
          }

          if (Array.isArray(parsedResponse.data)) {
            return parsedResponse.data as AdminHostApplicationSummary[];
          }

          return [];
        })
      );

      
  }

  getPendingProperties(): Observable<AdminPendingPropertySummary[]> {
  return this.http
    .get(`${this.apiUrl}/properties/pending`, {
      responseType: 'text',
    })
    .pipe(
      map(responseText => {
        if (!responseText) {
          return [];
        }

        const parsedResponse = JSON.parse(responseText);

        if (Array.isArray(parsedResponse)) {
          return parsedResponse as AdminPendingPropertySummary[];
        }

        if (Array.isArray(parsedResponse.items)) {
          return parsedResponse.items as AdminPendingPropertySummary[];
        }

        if (Array.isArray(parsedResponse.data)) {
          return parsedResponse.data as AdminPendingPropertySummary[];
        }

        return [];
      })
    );
}

getPropertyDetails(propertyId: string): Observable<AdminPropertyDetails> {
  return this.http.get<AdminPropertyDetails>(
    `${this.apiUrl}/properties/${propertyId}`
  );
}

approveProperty(propertyId: string): Observable<AdminPropertyDetails> {
  return this.http.post<AdminPropertyDetails>(
    `${this.apiUrl}/properties/${propertyId}/approve`,
    {}
  );
}

rejectProperty(
  propertyId: string,
  reason: string
): Observable<AdminPropertyDetails> {
  const payload: RejectPropertyRequest = {
    reason,
  };

  return this.http.post<AdminPropertyDetails>(
    `${this.apiUrl}/properties/${propertyId}/reject`,
    payload
  );
}

  getHostApplication(id: string): Observable<AdminHostApplicationDetails> {
    return this.http.get<AdminHostApplicationDetails>(
      `${this.apiUrl}/host-applications/${id}`
    );
  }

  approveHostApplication(id: string): Observable<AdminHostApplicationDetails> {
    return this.http.post<AdminHostApplicationDetails>(
      `${this.apiUrl}/host-applications/${id}/approve`,
      {}
    );
  }

  rejectHostApplication(
    id: string,
    reason: string
  ): Observable<AdminHostApplicationDetails> {
    const payload: RejectHostApplicationRequest = {
      reason,
    };

    return this.http.post<AdminHostApplicationDetails>(
      `${this.apiUrl}/host-applications/${id}/reject`,
      payload
    );
  }
getFinancialsSummary(): Observable<AdminFinancialsSummaryResponse> {
  return this.http
    .get(`${this.apiUrl}/financials/summary`, {
      responseType: 'text',
    })
    .pipe(
      map(responseText => {
        if (!responseText) {
          return {
            generatedAt: new Date().toISOString(),
            currencies: [],
          };
        }

        const parsedResponse = JSON.parse(responseText);

        return {
          generatedAt: parsedResponse.generatedAt,
          currencies: parsedResponse.currencies ?? [],
        } as AdminFinancialsSummaryResponse;
      })
    );
}

getFinancialTransactions(
  query: AdminFinancialTransactionsQuery = {}
): Observable<AdminFinancialTransactionsResponse> {
  let params = new HttpParams()
    .set('Page', query.page ?? 1)
    .set('PageSize', query.pageSize ?? 10);

  if (query.search?.trim()) {
    params = params.set('Search', query.search.trim());
  }

  if (query.type?.trim()) {
    params = params.set('Type', query.type.trim());
  }

  if (query.currency?.trim()) {
    params = params.set('Currency', query.currency.trim());
  }

  if (query.status?.trim()) {
    params = params.set('Status', query.status.trim());
  }

  if (query.fromDate) {
    params = params.set('FromDate', query.fromDate);
  }

  if (query.toDate) {
    params = params.set('ToDate', query.toDate);
  }

  return this.http
    .get(`${this.apiUrl}/financials/transactions`, {
      params,
      responseType: 'text',
    })
    .pipe(
      map(responseText => {
        if (!responseText) {
          return {
            page: query.page ?? 1,
            pageSize: query.pageSize ?? 10,
            totalCount: 0,
            totalPages: 1,
            items: [],
          };
        }

        const parsedResponse = JSON.parse(responseText);

        return {
          page: parsedResponse.page ?? query.page ?? 1,
          pageSize: parsedResponse.pageSize ?? query.pageSize ?? 10,
          totalCount: parsedResponse.totalCount ?? 0,
          totalPages: parsedResponse.totalPages ?? 1,
          items: parsedResponse.items ?? parsedResponse.data ?? [],
        } as AdminFinancialTransactionsResponse;
      })
    );
}
getAdminBookingsSummary(): Observable<AdminBookingsSummary> {
  return this.http
    .get(`${this.apiUrl}/bookings/summary`, {
      responseType: 'text',
    })
    .pipe(
      map(responseText => {
        if (!responseText) {
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

        const parsedResponse = JSON.parse(responseText);

        return {
          totalBookings: parsedResponse.totalBookings ?? 0,
          pendingBookings: parsedResponse.pendingBookings ?? 0,
          confirmedBookings: parsedResponse.confirmedBookings ?? 0,
          cancelledBookings: parsedResponse.cancelledBookings ?? 0,
          completedBookings: parsedResponse.completedBookings ?? 0,
          expiredBookings: parsedResponse.expiredBookings ?? 0,
          upcomingBookings: parsedResponse.upcomingBookings ?? 0,
          currentStays: parsedResponse.currentStays ?? 0,
          amountsByCurrency: parsedResponse.amountsByCurrency ?? [],
        } as AdminBookingsSummary;
      })
    );
}

getAdminBookings(
  query: AdminBookingsQuery = {}
): Observable<AdminBookingsResponse> {
  let params = new HttpParams()
    .set('Page', String(query.page ?? 1))
    .set('PageSize', String(query.pageSize ?? 10));

  if (query.status !== null && query.status !== undefined) {
    params = params.set('Status', String(query.status));
  }

  if (query.propertyId?.trim()) {
    params = params.set('PropertyId', query.propertyId.trim());
  }

  if (query.guestUserId?.trim()) {
    params = params.set('GuestUserId', query.guestUserId.trim());
  }

  if (query.hostUserId?.trim()) {
    params = params.set('HostUserId', query.hostUserId.trim());
  }

  if (query.checkInFrom) {
    params = params.set('CheckInFrom', query.checkInFrom);
  }

  if (query.checkInTo) {
    params = params.set('CheckInTo', query.checkInTo);
  }

  return this.http
    .get(`${this.apiUrl}/bookings`, {
      params,
      responseType: 'text',
    })
    .pipe(
      map(responseText => {
        if (!responseText) {
          return {
            page: query.page ?? 1,
            pageSize: query.pageSize ?? 10,
            totalCount: 0,
            totalPages: 1,
            items: [],
          };
        }

        const parsedResponse = JSON.parse(responseText);

        return {
          page: parsedResponse.page ?? query.page ?? 1,
          pageSize: parsedResponse.pageSize ?? query.pageSize ?? 10,
          totalCount: parsedResponse.totalCount ?? 0,
          totalPages: parsedResponse.totalPages ?? 1,
          items: parsedResponse.items ?? [],
        } as AdminBookingsResponse;
      })
    );
}

getAdminBookingDetails(bookingId: string): Observable<AdminBookingDetails> {
  return this.http
    .get(`${this.apiUrl}/bookings/${bookingId}`, {
      responseType: 'text',
    })
    .pipe(
      map(responseText => {
        return JSON.parse(responseText) as AdminBookingDetails;
      })
    );
}
getSupportTickets(
  query: AdminSupportTicketsQuery = {}
): Observable<AdminSupportTicketsResponse> {
  let params = new HttpParams()
    .set('Page', query.page ?? 1)
    .set('PageSize', query.pageSize ?? 10);

  if (query.search?.trim()) {
    params = params.set('Search', query.search.trim());
  }

  if (query.status?.trim()) {
    params = params.set('Status', query.status.trim());
  }

  if (query.category?.trim()) {
    params = params.set('Category', query.category.trim());
  }

  if (query.urgency?.trim()) {
    params = params.set('Urgency', query.urgency.trim());
  }

  return this.http
    .get(`${this.apiUrl}/support/tickets`, {
      params,
      responseType: 'text',
    })
    .pipe(
      map(responseText => {
        if (!responseText) {
          return {
            page: query.page ?? 1,
            pageSize: query.pageSize ?? 10,
            totalCount: 0,
            totalPages: 1,
            items: [],
          };
        }

        const parsedResponse = JSON.parse(responseText);

        return {
          page: parsedResponse.page ?? query.page ?? 1,
          pageSize: parsedResponse.pageSize ?? query.pageSize ?? 10,
          totalCount: parsedResponse.totalCount ?? 0,
          totalPages: parsedResponse.totalPages ?? 1,
          items: parsedResponse.items ?? parsedResponse.data ?? [],
        } as AdminSupportTicketsResponse;
      })
    );
}

getSupportTicketDetails(
  ticketId: string
): Observable<AdminSupportTicketDetails> {
  return this.http
    .get(`${this.apiUrl}/support/tickets/${ticketId}`, {
      responseType: 'text',
    })
    .pipe(
      map(responseText => {
        return JSON.parse(responseText) as AdminSupportTicketDetails;
      })
    );
}

replyToSupportTicket(
  ticketId: string,
  message: string
): Observable<AdminSupportTicketDetails> {
  const payload: AdminSupportReplyRequest = {
    message,
  };

  return this.http
    .post(`${this.apiUrl}/support/tickets/${ticketId}/reply`, payload, {
      responseType: 'text',
    })
    .pipe(
      map(responseText => {
        return JSON.parse(responseText) as AdminSupportTicketDetails;
      })
    );
}

resolveSupportTicket(
  ticketId: string,
  resolutionNote: string
): Observable<AdminSupportTicketDetails> {
  const payload: AdminSupportResolveRequest = {
    resolutionNote,
  };

  return this.http
    .patch(`${this.apiUrl}/support/tickets/${ticketId}/resolve`, payload, {
      responseType: 'text',
    })
    .pipe(
      map(responseText => {
        return JSON.parse(responseText) as AdminSupportTicketDetails;
      })
    );
}
 getUsers(query: AdminUserQuery = {}): Observable<AdminUsersResponse> {
  let params = new HttpParams()
    .set('page', query.page ?? 1)
    .set('pageSize', query.pageSize ?? 20);

  if (query.search?.trim()) {
    params = params.set('search', query.search.trim());
  }

  if (query.role) {
    params = params.set('role', query.role);
  }

  if (query.isActive !== undefined && query.isActive !== null) {
    params = params.set('isActive', query.isActive);
  }

  if (
    query.isProfileCompleted !== undefined &&
    query.isProfileCompleted !== null
  ) {
    params = params.set('isProfileCompleted', query.isProfileCompleted);
  }

  return this.http
    .get(`${this.apiUrl}/users`, {
      params,
      responseType: 'text',
    })
    .pipe(
      map(responseText => {
        if (!responseText) {
          return {
            page: query.page ?? 1,
            pageSize: query.pageSize ?? 20,
            totalCount: 0,
            totalPages: 1,
            items: [],
          };
        }

        const parsedResponse = JSON.parse(responseText);

        return {
          page: parsedResponse.page ?? query.page ?? 1,
          pageSize: parsedResponse.pageSize ?? query.pageSize ?? 20,
          totalCount: parsedResponse.totalCount ?? 0,
          totalPages: parsedResponse.totalPages ?? 1,
          items: parsedResponse.items ?? parsedResponse.data ?? [],
        } as AdminUsersResponse;
      })
    );
}
activateUser(userId: string): Observable<AdminUserStatusResponse> {
  return this.http.patch<AdminUserStatusResponse>(
    `${this.apiUrl}/users/${userId}/activate`,
    {}
  );
}
  deactivateUser(userId: string): Observable<AdminUserStatusResponse> {
    return this.http.patch<AdminUserStatusResponse>(
      `${this.apiUrl}/users/${userId}/deactivate`,
      {}
    );
  }
}