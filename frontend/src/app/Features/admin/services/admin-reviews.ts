import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';

export interface AdminReviewsQuery {
  status?: number | null;
  page?: number;
  pageSize?: number;
}

export interface AdminReviewsResponse {
  items: AdminReviewListItem[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface AdminReviewListItem {
  id: string;
  bookingId: string;
  rating: number;
  status: string;
  author: ReviewAuthor;
  property: ReviewProperty;
  createdAt: string;
  updatedAt?: string | null;
  publishedAt?: string | null;
  rejectedAt?: string | null;
}

export interface AdminReviewDetails extends AdminReviewListItem {
  positiveComment?: string | null;
  negativeComment?: string | null;
  rejectionReason?: string | null;
  checkInDate: string;
  checkOutDate: string;
  helpfulCount: number;
  reply?: ReviewReply | null;
  moderatedAt?: string | null;
}

export interface ReviewAuthor {
  userId: string;
  displayName: string;
  profileImageUrl?: string | null;
}

export interface ReviewProperty {
  id: string;
  title: string;
  country: string;
  city: string;
  coverImageUrl?: string | null;
}

export interface ReviewReply {
  id: string;
  hostProfileId: string;
  hostDisplayName: string;
  hostProfileImageUrl?: string | null;
  content: string;
  createdAt: string;
  updatedAt?: string | null;
}

export interface AdminReviewModerationResponse {
  id: string;
  status: string;
  moderatedAt: string;
  publishedAt?: string | null;
  rejectedAt?: string | null;
  rejectionReason?: string | null;
  message: string;
}

@Injectable({
  providedIn: 'root',
})
export class AdminReviewsService {
  private readonly apiUrl = `${environment.baseApi}/api/admin/reviews`;

  constructor(private http: HttpClient) {}

  getReviews(query: AdminReviewsQuery = {}): Observable<AdminReviewsResponse> {
    let params = new HttpParams()
      .set('Page', String(query.page ?? 1))
      .set('PageSize', String(query.pageSize ?? 10));

    if (query.status !== null && query.status !== undefined) {
      params = params.set('Status', String(query.status));
    }

    return this.http
      .get(this.apiUrl, {
        params,
        responseType: 'text',
      })
      .pipe(
        map(responseText => {
          if (!responseText) {
            return {
              items: [],
              page: query.page ?? 1,
              pageSize: query.pageSize ?? 10,
              totalCount: 0,
              totalPages: 1,
            };
          }

          const parsedResponse = JSON.parse(responseText);

          return {
            items: parsedResponse.items ?? [],
            page: parsedResponse.page ?? query.page ?? 1,
            pageSize: parsedResponse.pageSize ?? query.pageSize ?? 10,
            totalCount: parsedResponse.totalCount ?? 0,
            totalPages: parsedResponse.totalPages ?? 1,
          } as AdminReviewsResponse;
        })
      );
  }

  getReviewDetails(reviewId: string): Observable<AdminReviewDetails> {
    return this.http
      .get(`${this.apiUrl}/${reviewId}`, {
        responseType: 'text',
      })
      .pipe(
        map(responseText => JSON.parse(responseText) as AdminReviewDetails)
      );
  }

  approveReview(reviewId: string): Observable<AdminReviewModerationResponse> {
    return this.http
      .post(`${this.apiUrl}/${reviewId}/approve`, {}, {
        responseType: 'text',
      })
      .pipe(
        map(responseText => JSON.parse(responseText) as AdminReviewModerationResponse)
      );
  }

  rejectReview(
    reviewId: string,
    reason: string
  ): Observable<AdminReviewModerationResponse> {
    return this.http
      .post(`${this.apiUrl}/${reviewId}/reject`, { reason }, {
        responseType: 'text',
      })
      .pipe(
        map(responseText => JSON.parse(responseText) as AdminReviewModerationResponse)
      );
  }
}