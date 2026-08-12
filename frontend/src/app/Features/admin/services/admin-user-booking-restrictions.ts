import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, catchError, map, of } from 'rxjs';
import { environment } from '../../../../environments/environment';

export interface UserBookingRestrictionResponse {
  restrictionId?: string;
  id?: string;
  userId: string;
  userFullName?: string | null;
  userEmail?: string | null;
  type: string | number;
  status: string | number;
  reason: string;
  cancellationCountSnapshot: number;
  restrictedFrom: string;
  restrictedUntil?: string | null;
  createdBySystem: boolean;
  createdByAdminId?: string | null;
  createdByAdminName?: string | null;
  createdAt: string;
  updatedAt?: string | null;
  removedByAdminId?: string | null;
  removedByAdminName?: string | null;
  removedAt?: string | null;
  removalNote?: string | null;
}

export interface ApplyTemporaryBookingRestrictionRequest {
  durationDays: number;
  reason: string;
}

export interface RemoveUserBookingRestrictionRequest {
  removalNote: string;
}

@Injectable({
  providedIn: 'root',
})
export class AdminUserBookingRestrictionsService {
  private readonly adminApiUrl = `${environment.baseApi}/api/admin`;

  constructor(private readonly http: HttpClient) {}

  getUserRestrictions(
    userId: string,
  ): Observable<UserBookingRestrictionResponse[]> {
    return this.http
      .get(`${this.adminApiUrl}/users/${userId}/booking-restrictions`, {
        responseType: 'text',
      })
      .pipe(
        map((responseText) => {
          const parsedResponse = this.parseJsonResponse<unknown>(responseText);

          if (Array.isArray(parsedResponse)) {
            return parsedResponse as UserBookingRestrictionResponse[];
          }

          if (
            parsedResponse &&
            typeof parsedResponse === 'object' &&
            'items' in parsedResponse &&
            Array.isArray(parsedResponse.items)
          ) {
            return parsedResponse.items as UserBookingRestrictionResponse[];
          }

          if (
            parsedResponse &&
            typeof parsedResponse === 'object' &&
            'data' in parsedResponse &&
            Array.isArray(parsedResponse.data)
          ) {
            return parsedResponse.data as UserBookingRestrictionResponse[];
          }

          return [];
        }),
      );
  }

  getActiveRestriction(
    userId: string,
  ): Observable<UserBookingRestrictionResponse | null> {
    return this.http
      .get(`${this.adminApiUrl}/users/${userId}/booking-restrictions/active`, {
        responseType: 'text',
      })
      .pipe(
        map((responseText) =>
          this.parseJsonResponse<UserBookingRestrictionResponse>(responseText),
        ),
        catchError((error) => {
          if (error?.status === 404 || error?.status === 204) {
            return of(null);
          }

          throw error;
        }),
      );
  }

  applyTemporaryBookingRestriction(
    adminReviewFlagId: string,
    durationDays: number,
    reason: string,
  ): Observable<UserBookingRestrictionResponse> {
    const payload: ApplyTemporaryBookingRestrictionRequest = {
      durationDays,
      reason: reason.trim(),
    };

    return this.http
      .post(
        `${this.adminApiUrl}/user-booking-restrictions/${adminReviewFlagId}/temporary-suspension`,
        payload,
        {
          responseType: 'text',
        },
      )
      .pipe(
        map(
          (responseText) =>
            this.parseJsonResponse<UserBookingRestrictionResponse>(
              responseText,
            ) ?? ({} as UserBookingRestrictionResponse),
        ),
      );
  }

  removeRestriction(
    restrictionId: string,
    removalNote: string,
  ): Observable<UserBookingRestrictionResponse> {
    const payload: RemoveUserBookingRestrictionRequest = {
      removalNote: removalNote.trim(),
    };

    return this.http
      .patch(
        `${this.adminApiUrl}/user-booking-restrictions/${restrictionId}/remove`,
        payload,
        {
          responseType: 'text',
        },
      )
      .pipe(
        map(
          (responseText) =>
            this.parseJsonResponse<UserBookingRestrictionResponse>(
              responseText,
            ) ?? ({} as UserBookingRestrictionResponse),
        ),
      );
  }

  private parseJsonResponse<T>(
    responseText: string | null | undefined,
  ): T | null {
    const normalizedResponse = (responseText ?? '')
      .replace(/^\uFEFF/, '')
      .trim();

    if (!normalizedResponse) {
      return null;
    }

    return JSON.parse(normalizedResponse) as T;
  }
}