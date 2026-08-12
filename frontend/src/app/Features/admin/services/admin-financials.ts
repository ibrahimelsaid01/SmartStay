import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, map, timeout } from 'rxjs';
import { environment } from '../../../../environments/environment';

export type AdminFinancialTransactionType = 'all' | 'payment' | 'refund';

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

export interface AdminFinancialsSummaryResponse {
  generatedAt: string;
  currencies: AdminFinancialCurrencySummary[];
}

export interface AdminFinancialTransactionsQuery {
  search?: string;
  type?: AdminFinancialTransactionType;
  currency?: string;
  status?: string;
  fromDate?: string;
  toDate?: string;
  page?: number;
  pageSize?: number;
}

export interface AdminFinancialTransaction {
  transactionId: string;
  referenceCode: string;
  type: string;
  direction: string;
  provider: string;
  providerTransactionId?: string | null;
  bookingId?: string | null;
  paymentId?: string | null;
  refundId?: string | null;
  userId?: string | null;
  userName: string;
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
  completedAt?: string | null;
}

export interface AdminFinancialTransactionsResponse {
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  items: AdminFinancialTransaction[];
}

@Injectable({
  providedIn: 'root',
})
export class AdminFinancialsService {
  private readonly apiUrl =
    `${environment.baseApi}/api/admin/financials`;

  private readonly requestTimeoutMs = 30000;

  constructor(private readonly http: HttpClient) {}

  getSummary(): Observable<AdminFinancialsSummaryResponse> {
    return this.http
      .get(`${this.apiUrl}/summary`, {
        responseType: 'text',
        withCredentials: true,
      })
      .pipe(
        timeout(this.requestTimeoutMs),
        map((responseText) => {
          const normalizedResponse =
            this.normalizeResponseText(responseText);

          if (!normalizedResponse) {
            return {
              generatedAt: new Date().toISOString(),
              currencies: [],
            };
          }

          const response =
            this.parseJson<any>(normalizedResponse);

          const rawCurrencies =
            response.currencies ??
            response.Currencies ??
            [];

          return {
            generatedAt:
              response.generatedAt ??
              response.GeneratedAt ??
              new Date().toISOString(),

            currencies: Array.isArray(rawCurrencies)
              ? rawCurrencies.map((item: any) =>
                  this.mapCurrencySummary(item),
                )
              : [],
          };
        }),
      );
  }

  getTransactions(
    query: AdminFinancialTransactionsQuery = {},
  ): Observable<AdminFinancialTransactionsResponse> {
    const page = query.page ?? 1;
    const pageSize = query.pageSize ?? 10;

    let params = new HttpParams()
      .set('page', String(page))
      .set('pageSize', String(pageSize))
      .set('type', query.type ?? 'all')
      .set('_ts', String(Date.now()));

    if (query.search?.trim()) {
      params = params.set(
        'search',
        query.search.trim(),
      );
    }

    if (query.currency?.trim()) {
      params = params.set(
        'currency',
        query.currency.trim().toUpperCase(),
      );
    }

    if (query.status?.trim()) {
      params = params.set(
        'status',
        query.status.trim(),
      );
    }

    if (query.fromDate) {
      params = params.set(
        'fromDate',
        query.fromDate,
      );
    }

    if (query.toDate) {
      params = params.set(
        'toDate',
        query.toDate,
      );
    }

    return this.http
      .get(`${this.apiUrl}/transactions`, {
        params,
        responseType: 'text',
        withCredentials: true,
      })
      .pipe(
        timeout(this.requestTimeoutMs),
        map((responseText) => {
          const normalizedResponse =
            this.normalizeResponseText(responseText);

          if (!normalizedResponse) {
            return this.createEmptyTransactionsResponse(
              page,
              pageSize,
            );
          }

          const response =
            this.parseJson<any>(normalizedResponse);

          const rawItems =
            response.items ??
            response.Items ??
            [];

          return {
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

            items: Array.isArray(rawItems)
              ? rawItems.map((item: any) =>
                  this.mapTransaction(item),
                )
              : [],
          };
        }),
      );
  }

  private mapCurrencySummary(
    item: any,
  ): AdminFinancialCurrencySummary {
    return {
      currency:
        item.currency ??
        item.Currency ??
        'EGP',

      totalPaymentAttempts:
        item.totalPaymentAttempts ??
        item.TotalPaymentAttempts ??
        0,

      pendingPayments:
        item.pendingPayments ??
        item.PendingPayments ??
        0,

      successfulPayments:
        item.successfulPayments ??
        item.SuccessfulPayments ??
        0,

      failedPayments:
        item.failedPayments ??
        item.FailedPayments ??
        0,

      cancelledPayments:
        item.cancelledPayments ??
        item.CancelledPayments ??
        0,

      partiallyRefundedPayments:
        item.partiallyRefundedPayments ??
        item.PartiallyRefundedPayments ??
        0,

      fullyRefundedPayments:
        item.fullyRefundedPayments ??
        item.FullyRefundedPayments ??
        0,

      grossVolume:
        item.grossVolume ??
        item.GrossVolume ??
        0,

      platformRevenue:
        item.platformRevenue ??
        item.PlatformRevenue ??
        0,

      totalRefundedAmount:
        item.totalRefundedAmount ??
        item.TotalRefundedAmount ??
        0,

      netVolume:
        item.netVolume ??
        item.NetVolume ??
        0,

      totalRefundRequests:
        item.totalRefundRequests ??
        item.TotalRefundRequests ??
        0,

      pendingRefundRequests:
        item.pendingRefundRequests ??
        item.PendingRefundRequests ??
        0,

      successfulRefundRequests:
        item.successfulRefundRequests ??
        item.SuccessfulRefundRequests ??
        0,

      failedRefundRequests:
        item.failedRefundRequests ??
        item.FailedRefundRequests ??
        0,

      successRatePercentage:
        item.successRatePercentage ??
        item.SuccessRatePercentage ??
        0,

      pendingPayoutRequests:
        item.pendingPayoutRequests ??
        item.PendingPayoutRequests ??
        0,

      pendingPayoutAmount:
        item.pendingPayoutAmount ??
        item.PendingPayoutAmount ??
        0,
    };
  }

  private mapTransaction(
    item: any,
  ): AdminFinancialTransaction {
    return {
      transactionId:
        item.transactionId ??
        item.TransactionId ??
        '',

      referenceCode:
        item.referenceCode ??
        item.ReferenceCode ??
        '',

      type:
        item.type ??
        item.Type ??
        '',

      direction:
        item.direction ??
        item.Direction ??
        '',

      provider:
        item.provider ??
        item.Provider ??
        '',

      providerTransactionId:
        item.providerTransactionId ??
        item.ProviderTransactionId ??
        null,

      bookingId:
        item.bookingId ??
        item.BookingId ??
        null,

      paymentId:
        item.paymentId ??
        item.PaymentId ??
        null,

      refundId:
        item.refundId ??
        item.RefundId ??
        null,

      userId:
        item.userId ??
        item.UserId ??
        null,

      userName:
        item.userName ??
        item.UserName ??
        'Unknown User',

      userEmail:
        item.userEmail ??
        item.UserEmail ??
        null,

      propertyTitle:
        item.propertyTitle ??
        item.PropertyTitle ??
        null,

      currency:
        item.currency ??
        item.Currency ??
        'EGP',

      amount:
        item.amount ??
        item.Amount ??
        0,

      signedAmount:
        item.signedAmount ??
        item.SignedAmount ??
        0,

      platformFee:
        item.platformFee ??
        item.PlatformFee ??
        0,

      refundedAmount:
        item.refundedAmount ??
        item.RefundedAmount ??
        0,

      netAmount:
        item.netAmount ??
        item.NetAmount ??
        0,

      status:
        item.status ??
        item.Status ??
        '',

      failureReason:
        item.failureReason ??
        item.FailureReason ??
        null,

      createdAt:
        item.createdAt ??
        item.CreatedAt ??
        new Date().toISOString(),

      completedAt:
        item.completedAt ??
        item.CompletedAt ??
        null,
    };
  }

  private createEmptyTransactionsResponse(
    page: number,
    pageSize: number,
  ): AdminFinancialTransactionsResponse {
    return {
      page,
      pageSize,
      totalCount: 0,
      totalPages: 1,
      items: [],
    };
  }

  private normalizeResponseText(
    responseText: string,
  ): string {
    return (responseText ?? '')
      .replace(/^\uFEFF/, '')
      .trim();
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
}