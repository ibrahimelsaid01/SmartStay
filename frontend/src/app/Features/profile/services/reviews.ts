import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import { environment } from '../../../../environments/environment';

export type ReviewStatus =
  | 'Pending'
  | 'Posted'
  | 'Rejected';

export interface ReviewPropertyResponse {
  id: string;
  title: string;
  country: string;
  city: string;
  coverImageUrl?: string | null;
}

export interface ReviewReplyResponse {
  id: string;
  hostProfileId: string;
  hostDisplayName: string;
  hostProfileImageUrl?: string | null;
  content: string;
  createdAt: string;
  updatedAt?: string | null;
}

export interface UserReviewResponse {
  id: string;
  bookingId: string;
  rating: number;
  positiveComment?: string | null;
  negativeComment?: string | null;
  status: ReviewStatus;
  rejectionReason?: string | null;
  checkInDate: string;
  checkOutDate: string;
  helpfulCount: number;
  canEdit: boolean;
  canDelete: boolean;
  property: ReviewPropertyResponse;
  reply?: ReviewReplyResponse | null;
  createdAt: string;
  updatedAt?: string | null;
  publishedAt?: string | null;
  rejectedAt?: string | null;
}

export interface MyReviewsResponse {
  items: UserReviewResponse[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface CreateReviewRequest {
  rating: number;
  positiveComment?: string | null;
  negativeComment?: string | null;
}

export interface UpdateReviewRequest {
  rating: number;
  positiveComment?: string | null;
  negativeComment?: string | null;
}

@Injectable({
  providedIn: 'root',
})
export class ReviewsService {
  private readonly reviewsApiUrl =
    `${environment.baseApi}/api/reviews`;

  private readonly bookingsApiUrl =
    `${environment.baseApi}/api/bookings`;

  constructor(
    private readonly http: HttpClient,
  ) {}

  getMyReviews(
    status?: ReviewStatus,
    page = 1,
    pageSize = 10,
  ): Observable<MyReviewsResponse> {
    let params = new HttpParams()
      .set('page', String(page))
      .set('pageSize', String(pageSize));

    if (status) {
      params = params.set(
        'status',
        status,
      );
    }

    return this.http
      .get(
        `${this.reviewsApiUrl}/my-reviews`,
        {
          params,
          responseType: 'text',
        },
      )
      .pipe(
        map((responseText) =>
          this.parseRequiredJsonResponse<MyReviewsResponse>(
            responseText,
            'My reviews API returned an empty response.',
          ),
        ),
      );
  }

  getReviewById(
    reviewId: string,
  ): Observable<UserReviewResponse> {
    return this.http
      .get(
        `${this.reviewsApiUrl}/${reviewId}`,
        {
          responseType: 'text',
        },
      )
      .pipe(
        map((responseText) =>
          this.parseRequiredJsonResponse<UserReviewResponse>(
            responseText,
            'Review details API returned an empty response.',
          ),
        ),
      );
  }

  createReview(
    bookingId: string,
    request: CreateReviewRequest,
  ): Observable<UserReviewResponse> {
    return this.http
      .post(
        `${this.bookingsApiUrl}/${bookingId}/review`,
        this.normalizeReviewRequest(request),
        {
          responseType: 'text',
        },
      )
      .pipe(
        map((responseText) =>
          this.parseRequiredJsonResponse<UserReviewResponse>(
            responseText,
            'Create review API returned an empty response.',
          ),
        ),
      );
  }

  updateReview(
    reviewId: string,
    request: UpdateReviewRequest,
  ): Observable<UserReviewResponse> {
    return this.http
      .put(
        `${this.reviewsApiUrl}/${reviewId}`,
        this.normalizeReviewRequest(request),
        {
          responseType: 'text',
        },
      )
      .pipe(
        map((responseText) =>
          this.parseRequiredJsonResponse<UserReviewResponse>(
            responseText,
            'Update review API returned an empty response.',
          ),
        ),
      );
  }

  deleteReview(
    reviewId: string,
  ): Observable<void> {
    return this.http
      .delete(
        `${this.reviewsApiUrl}/${reviewId}`,
        {
          responseType: 'text',
        },
      )
      .pipe(
        map(() => undefined),
      );
  }

  private normalizeReviewRequest(
    request:
      | CreateReviewRequest
      | UpdateReviewRequest,
  ):
    | CreateReviewRequest
    | UpdateReviewRequest {
    return {
      rating:
        request.rating,

      positiveComment:
        this.normalizeOptionalComment(
          request.positiveComment,
        ),

      negativeComment:
        this.normalizeOptionalComment(
          request.negativeComment,
        ),
    };
  }

  private normalizeOptionalComment(
    value: string | null | undefined,
  ): string | null {
    const normalizedValue =
      (value ?? '').trim();

    return normalizedValue || null;
  }

  private parseRequiredJsonResponse<T>(
    responseText: string | null | undefined,
    emptyResponseMessage: string,
  ): T {
    const normalizedResponse =
      (responseText ?? '')
        .replace(/^\uFEFF/, '')
        .trim();

    if (!normalizedResponse) {
      throw new Error(
        emptyResponseMessage,
      );
    }

    return JSON.parse(
      normalizedResponse,
    ) as T;
  }
}