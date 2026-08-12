import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import {
  Observable,
  map,
  timeout,
} from 'rxjs';
import { environment } from '../../../../environments/environment';

export type AdminBookingPayoutStatus =
  | 'Pending'
  | 'Held'
  | 'Available'
  | 'Paid'
  | 'Blocked'
  | 'Refunded';

export interface AdminBookingPayoutResponse {
  payoutId: string;
  bookingId: string;
  bookingPaymentId: string;
  hostProfileId: string;
  amount: number;
  currency: string;
  status: AdminBookingPayoutStatus;
  availableAt?: string | null;
  heldAt?: string | null;
  holdReason?: string | null;
  releasedAt?: string | null;
  releaseNote?: string | null;
  paidAt?: string | null;
  blockedAt?: string | null;
  blockReason?: string | null;
  refundedAt?: string | null;
  createdAt: string;
  updatedAt?: string | null;
}

export interface HoldBookingPayoutRequest {
  reason: string;
}

export interface ReleaseBookingPayoutRequest {
  releaseNote?: string | null;
}

export interface BlockBookingPayoutRequest {
  reason: string;
}

export interface MarkBookingPayoutRefundedRequest {
  refundNote?: string | null;
}

@Injectable({
  providedIn: 'root',
})
export class AdminBookingPayoutsService {
  private readonly apiUrl =
    `${environment.baseApi}/api/admin/booking-payouts/bookings`;

  private readonly requestTimeoutMs = 30000;

  constructor(
    private readonly http: HttpClient,
  ) {}

  getBookingPayoutByBookingId(
    bookingId: string,
  ): Observable<AdminBookingPayoutResponse> {
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
        map((responseText) =>
          this.mapPayoutResponse(
            responseText,
          ),
        ),
      );
  }

  holdBookingPayout(
    bookingId: string,
    reason: string,
  ): Observable<AdminBookingPayoutResponse> {
    const payload:
      HoldBookingPayoutRequest = {
        reason: reason.trim(),
      };

    return this.patchPayout(
      `${this.apiUrl}/${bookingId}/hold`,
      payload,
    );
  }

  releaseBookingPayout(
    bookingId: string,
    releaseNote?: string,
  ): Observable<AdminBookingPayoutResponse> {
    const normalizedNote =
      releaseNote?.trim();

    const payload:
      ReleaseBookingPayoutRequest = {
        releaseNote:
          normalizedNote || null,
      };

    return this.patchPayout(
      `${this.apiUrl}/${bookingId}/release`,
      payload,
    );
  }

  blockBookingPayout(
    bookingId: string,
    reason: string,
  ): Observable<AdminBookingPayoutResponse> {
    const payload:
      BlockBookingPayoutRequest = {
        reason: reason.trim(),
      };

    return this.patchPayout(
      `${this.apiUrl}/${bookingId}/block`,
      payload,
    );
  }

  markBookingPayoutRefunded(
    bookingId: string,
    refundNote?: string,
  ): Observable<AdminBookingPayoutResponse> {
    const normalizedNote =
      refundNote?.trim();

    const payload:
      MarkBookingPayoutRefundedRequest = {
        refundNote:
          normalizedNote || null,
      };

    return this.patchPayout(
      `${this.apiUrl}/${bookingId}/refunded`,
      payload,
    );
  }

  private patchPayout(
    url: string,
    payload:
      | HoldBookingPayoutRequest
      | ReleaseBookingPayoutRequest
      | BlockBookingPayoutRequest
      | MarkBookingPayoutRefundedRequest,
  ): Observable<AdminBookingPayoutResponse> {
    return this.http
      .patch(
        url,
        payload,
        {
          responseType: 'text',
          withCredentials: true,
        },
      )
      .pipe(
        timeout(this.requestTimeoutMs),
        map((responseText) =>
          this.mapPayoutResponse(
            responseText,
          ),
        ),
      );
  }

  private mapPayoutResponse(
    responseText: string,
  ): AdminBookingPayoutResponse {
    const response =
      this.parseRequiredJson<any>(
        responseText,
      );

    return {
      payoutId:
        response.payoutId ??
        response.PayoutId ??
        '',
      bookingId:
        response.bookingId ??
        response.BookingId ??
        '',
      bookingPaymentId:
        response.bookingPaymentId ??
        response.BookingPaymentId ??
        '',
      hostProfileId:
        response.hostProfileId ??
        response.HostProfileId ??
        '',
      amount:
        response.amount ??
        response.Amount ??
        0,
      currency:
        response.currency ??
        response.Currency ??
        'EGP',
      status:
        this.normalizePayoutStatus(
          response.status ??
          response.Status,
        ),
      availableAt:
        response.availableAt ??
        response.AvailableAt ??
        null,
      heldAt:
        response.heldAt ??
        response.HeldAt ??
        null,
      holdReason:
        response.holdReason ??
        response.HoldReason ??
        null,
      releasedAt:
        response.releasedAt ??
        response.ReleasedAt ??
        null,
      releaseNote:
        response.releaseNote ??
        response.ReleaseNote ??
        null,
      paidAt:
        response.paidAt ??
        response.PaidAt ??
        null,
      blockedAt:
        response.blockedAt ??
        response.BlockedAt ??
        null,
      blockReason:
        response.blockReason ??
        response.BlockReason ??
        null,
      refundedAt:
        response.refundedAt ??
        response.RefundedAt ??
        null,
      createdAt:
        response.createdAt ??
        response.CreatedAt ??
        new Date().toISOString(),
      updatedAt:
        response.updatedAt ??
        response.UpdatedAt ??
        null,
    };
  }

  private normalizePayoutStatus(
    value: unknown,
  ): AdminBookingPayoutStatus {
    const normalizedValue =
      String(value ?? '')
        .trim()
        .toLowerCase();

    switch (normalizedValue) {
      case '1':
      case 'pending':
        return 'Pending';

      case '2':
      case 'held':
        return 'Held';

      case '3':
      case 'available':
        return 'Available';

      case '4':
      case 'paid':
        return 'Paid';

      case '5':
      case 'blocked':
        return 'Blocked';

      case '6':
      case 'refunded':
        return 'Refunded';

      default:
        return 'Pending';
    }
  }

  private parseRequiredJson<T>(
    responseText: string,
  ): T {
    const normalizedResponse =
      (responseText ?? '')
        .replace(/^\uFEFF/, '')
        .trim();

    if (!normalizedResponse) {
      throw new Error(
        'The server returned an empty response.',
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
}