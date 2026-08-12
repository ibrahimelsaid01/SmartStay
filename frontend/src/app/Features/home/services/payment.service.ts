import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, map, timeout } from 'rxjs';

import { environment } from '../../../../environments/environment';

export type PaymentStatus =
  | 'Pending'
  | 'Succeeded'
  | 'Failed'
  | 'Cancelled'
  | 'PartiallyRefunded'
  | 'Refunded';

export type PaymentBookingStatus =
  | 'Pending'
  | 'Confirmed'
  | 'Cancelled'
  | 'Completed'
  | 'Expired';

export interface StartPaymentRequest {
  bookingId: string;
}

export interface StartPaymentResponse {
  paymentId: string;
  bookingId: string;
  amount: number;
  currency: string;
  provider: string;
  providerPaymentId: string;
  clientSecret: string;
  status: PaymentStatus;
  providerStatus: string;
  bookingExpiresAt: string | null;
  createdAt: string;
  wasAlreadyProcessed: boolean;
  message: string;
}

export interface PaymentStatusResponse {
  paymentId: string;
  bookingId: string;
  bookingStatus: PaymentBookingStatus;
  amount: number;
  refundedAmount: number;
  currency: string;
  provider: string;
  providerPaymentId: string | null;
  providerReference: string | null;
  status: PaymentStatus;
  failureCode: string | null;
  failureMessage: string | null;
  bookingExpiresAt: string | null;
  createdAt: string;
  updatedAt: string | null;
  succeededAt: string | null;
  failedAt: string | null;
  cancelledAt: string | null;
  refundedAt: string | null;
  isFinal: boolean;
}

@Injectable({
  providedIn: 'root',
})
export class PaymentService {
  private readonly apiUrl = `${environment.baseApi}/api/payments`;
  private readonly requestTimeoutMs = 30000;

  constructor(private readonly http: HttpClient) {}

  startPayment(
    bookingId: string,
    idempotencyKey: string,
  ): Observable<StartPaymentResponse> {
    const headers = new HttpHeaders({
      'Idempotency-Key': idempotencyKey,
    });

    const request: StartPaymentRequest = {
      bookingId,
    };

    return this.http
      .post(this.apiUrl, request, {
        headers,
        responseType: 'text',
      })
      .pipe(
        timeout(this.requestTimeoutMs),
        map((responseText) => {
          const response =
            this.parseRequiredJson<Record<string, unknown>>(
              responseText,
              'Payment API returned an empty response.',
            );

          const mappedResponse: StartPaymentResponse = {
            paymentId: this.getString(
              response,
              'paymentId',
              'PaymentId',
            ),

            bookingId: this.getString(
              response,
              'bookingId',
              'BookingId',
            ),

            amount: this.getNumber(
              response,
              'amount',
              'Amount',
            ),

            currency: this.getString(
              response,
              'currency',
              'Currency',
            ),

            provider: this.getString(
              response,
              'provider',
              'Provider',
            ),

            providerPaymentId: this.getString(
              response,
              'providerPaymentId',
              'ProviderPaymentId',
            ),

            clientSecret: this.getString(
              response,
              'clientSecret',
              'ClientSecret',
            ),

            status: this.normalizePaymentStatus(
              response['status'] ??
                response['Status'],
            ),

            providerStatus: this.getString(
              response,
              'providerStatus',
              'ProviderStatus',
            ),

            bookingExpiresAt: this.getNullableString(
              response,
              'bookingExpiresAt',
              'BookingExpiresAt',
            ),

            createdAt: this.getString(
              response,
              'createdAt',
              'CreatedAt',
            ),

            wasAlreadyProcessed: this.getBoolean(
              response,
              'wasAlreadyProcessed',
              'WasAlreadyProcessed',
            ),

            message: this.getString(
              response,
              'message',
              'Message',
            ),
          };

          if (!mappedResponse.paymentId) {
            throw new Error(
              'Payment API response does not contain a payment identifier.',
            );
          }

          if (!mappedResponse.bookingId) {
            throw new Error(
              'Payment API response does not contain a booking identifier.',
            );
          }

          return mappedResponse;
        }),
      );
  }

  getPaymentStatus(
    paymentId: string,
  ): Observable<PaymentStatusResponse> {
    return this.http
      .get(`${this.apiUrl}/${paymentId}`, {
        responseType: 'text',
      })
      .pipe(
        timeout(this.requestTimeoutMs),
        map((responseText) => {
          const response =
            this.parseRequiredJson<Record<string, unknown>>(
              responseText,
              'Payment status API returned an empty response.',
            );

          const mappedResponse: PaymentStatusResponse = {
            paymentId: this.getString(
              response,
              'paymentId',
              'PaymentId',
            ),

            bookingId: this.getString(
              response,
              'bookingId',
              'BookingId',
            ),

            bookingStatus: this.normalizeBookingStatus(
              response['bookingStatus'] ??
                response['BookingStatus'],
            ),

            amount: this.getNumber(
              response,
              'amount',
              'Amount',
            ),

            refundedAmount: this.getNumber(
              response,
              'refundedAmount',
              'RefundedAmount',
            ),

            currency: this.getString(
              response,
              'currency',
              'Currency',
            ),

            provider: this.getString(
              response,
              'provider',
              'Provider',
            ),

            providerPaymentId: this.getNullableString(
              response,
              'providerPaymentId',
              'ProviderPaymentId',
            ),

            providerReference: this.getNullableString(
              response,
              'providerReference',
              'ProviderReference',
            ),

            status: this.normalizePaymentStatus(
              response['status'] ??
                response['Status'],
            ),

            failureCode: this.getNullableString(
              response,
              'failureCode',
              'FailureCode',
            ),

            failureMessage: this.getNullableString(
              response,
              'failureMessage',
              'FailureMessage',
            ),

            bookingExpiresAt: this.getNullableString(
              response,
              'bookingExpiresAt',
              'BookingExpiresAt',
            ),

            createdAt: this.getString(
              response,
              'createdAt',
              'CreatedAt',
            ),

            updatedAt: this.getNullableString(
              response,
              'updatedAt',
              'UpdatedAt',
            ),

            succeededAt: this.getNullableString(
              response,
              'succeededAt',
              'SucceededAt',
            ),

            failedAt: this.getNullableString(
              response,
              'failedAt',
              'FailedAt',
            ),

            cancelledAt: this.getNullableString(
              response,
              'cancelledAt',
              'CancelledAt',
            ),

            refundedAt: this.getNullableString(
              response,
              'refundedAt',
              'RefundedAt',
            ),

            isFinal: this.getBoolean(
              response,
              'isFinal',
              'IsFinal',
            ),
          };

          if (!mappedResponse.paymentId) {
            throw new Error(
              'Payment status API response does not contain a payment identifier.',
            );
          }

          if (!mappedResponse.bookingId) {
            throw new Error(
              'Payment status API response does not contain a booking identifier.',
            );
          }

          return mappedResponse;
        }),
      );
  }

  private parseRequiredJson<T>(
    responseText: string,
    emptyMessage: string,
  ): T {
    const normalizedResponse = (responseText ?? '')
      .replace(/^\uFEFF/, '')
      .trim();

    if (!normalizedResponse) {
      throw new Error(emptyMessage);
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

  private normalizePaymentStatus(
    value: unknown,
  ): PaymentStatus {
    const normalizedValue = String(value ?? '')
      .trim()
      .toLowerCase();

    switch (normalizedValue) {
      case '1':
      case 'pending':
        return 'Pending';

      case '2':
      case 'succeeded':
        return 'Succeeded';

      case '3':
      case 'failed':
        return 'Failed';

      case '4':
      case 'cancelled':
      case 'canceled':
        return 'Cancelled';

      case '5':
      case 'partiallyrefunded':
      case 'partially_refunded':
      case 'partially-refunded':
        return 'PartiallyRefunded';

      case '6':
      case 'refunded':
        return 'Refunded';

      default:
        return 'Pending';
    }
  }

  private normalizeBookingStatus(
    value: unknown,
  ): PaymentBookingStatus {
    const normalizedValue = String(value ?? '')
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

  private getString(
    response: Record<string, unknown>,
    camelCaseKey: string,
    pascalCaseKey: string,
  ): string {
    const value =
      response[camelCaseKey] ??
      response[pascalCaseKey];

    if (
      value === null ||
      value === undefined
    ) {
      return '';
    }

    return String(value).trim();
  }

  private getNullableString(
    response: Record<string, unknown>,
    camelCaseKey: string,
    pascalCaseKey: string,
  ): string | null {
    const value =
      response[camelCaseKey] ??
      response[pascalCaseKey];

    if (
      value === null ||
      value === undefined
    ) {
      return null;
    }

    const normalizedValue =
      String(value).trim();

    return normalizedValue || null;
  }

  private getNumber(
    response: Record<string, unknown>,
    camelCaseKey: string,
    pascalCaseKey: string,
  ): number {
    const value =
      response[camelCaseKey] ??
      response[pascalCaseKey];

    if (
      value === null ||
      value === undefined ||
      value === ''
    ) {
      return 0;
    }

    const numericValue = Number(value);

    return Number.isFinite(numericValue)
      ? numericValue
      : 0;
  }

  private getBoolean(
    response: Record<string, unknown>,
    camelCaseKey: string,
    pascalCaseKey: string,
  ): boolean {
    const value =
      response[camelCaseKey] ??
      response[pascalCaseKey];

    if (typeof value === 'boolean') {
      return value;
    }

    if (typeof value === 'string') {
      return (
        value
          .trim()
          .toLowerCase() ===
        'true'
      );
    }

    return Boolean(value);
  }
}