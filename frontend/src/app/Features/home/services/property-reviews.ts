import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, map, timeout } from 'rxjs';

import { environment } from '../../../../environments/environment';

export interface PropertyReviewAuthor {
  userId: string;
  displayName: string;
  profileImageUrl?: string | null;
}

export interface PropertyReviewReply {
  id: string;
  hostProfileId: string;
  hostDisplayName: string;
  hostProfileImageUrl?: string | null;
  content: string;
  createdAt: string;
  updatedAt?: string | null;
}

export interface PropertyReview {
  id: string;
  rating: number;
  positiveComment?: string | null;
  negativeComment?: string | null;
  helpfulCount: number;
  isHelpfulByCurrentUser: boolean;
  author: PropertyReviewAuthor;
  reply?: PropertyReviewReply | null;
  createdAt: string;
  publishedAt?: string | null;
}

export interface PropertyReviewsResponse {
  propertyId: string;
  items: PropertyReview[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface PropertyRatingSummary {
  propertyId: string;
  averageRating: number;
  reviewsCount: number;
  distribution: Record<number, number>;
}

export interface ReviewHelpfulResponse {
  reviewId: string;
  helpfulCount: number;
  isHelpfulByCurrentUser: boolean;
}

@Injectable({
  providedIn: 'root',
})
export class PropertyReviewsService {
  private readonly propertiesApiUrl =
    `${environment.baseApi}/api/properties`;

  private readonly reviewsApiUrl =
    `${environment.baseApi}/api/reviews`;

  private readonly requestTimeoutMs = 30000;
  private readonly maximumPageSize = 100;

  constructor(
    private readonly http: HttpClient,
  ) {}

  getPropertyReviews(
    propertyId: string,
    page = 1,
    pageSize = 6,
  ): Observable<PropertyReviewsResponse> {
    const normalizedPage =
      this.normalizePage(page);

    const normalizedPageSize =
      this.normalizePageSize(pageSize);

    const params = new HttpParams()
      .set('page', String(normalizedPage))
      .set('pageSize', String(normalizedPageSize))
      .set('_ts', String(Date.now()));

    return this.http
      .get(
        `${this.propertiesApiUrl}/${propertyId}/reviews`,
        {
          params,
          responseType: 'text',
        },
      )
      .pipe(
        timeout(this.requestTimeoutMs),

        map((responseText) =>
          this.mapPropertyReviewsResponse(
            responseText,
            propertyId,
            normalizedPage,
            normalizedPageSize,
          ),
        ),
      );
  }

  getRatingSummary(
    propertyId: string,
  ): Observable<PropertyRatingSummary> {
    const params = new HttpParams()
      .set('_ts', String(Date.now()));

    return this.http
      .get(
        `${this.propertiesApiUrl}/${propertyId}/rating-summary`,
        {
          params,
          responseType: 'text',
        },
      )
      .pipe(
        timeout(this.requestTimeoutMs),

        map((responseText) =>
          this.mapRatingSummaryResponse(
            responseText,
            propertyId,
          ),
        ),
      );
  }

  markHelpful(
    reviewId: string,
  ): Observable<ReviewHelpfulResponse> {
    return this.http
      .post(
        `${this.reviewsApiUrl}/${reviewId}/helpful`,
        {},
        {
          responseType: 'text',
        },
      )
      .pipe(
        timeout(this.requestTimeoutMs),

        map((responseText) =>
          this.mapHelpfulResponse(
            responseText,
            reviewId,
          ),
        ),
      );
  }

  removeHelpful(
    reviewId: string,
  ): Observable<ReviewHelpfulResponse> {
    return this.http
      .delete(
        `${this.reviewsApiUrl}/${reviewId}/helpful`,
        {
          responseType: 'text',
        },
      )
      .pipe(
        timeout(this.requestTimeoutMs),

        map((responseText) =>
          this.mapHelpfulResponse(
            responseText,
            reviewId,
          ),
        ),
      );
  }

  private mapPropertyReviewsResponse(
    responseText: string | null | undefined,
    fallbackPropertyId: string,
    fallbackPage: number,
    fallbackPageSize: number,
  ): PropertyReviewsResponse {
    const response =
      this.parseRequiredJsonRecord(
        responseText,
        'Property reviews API returned an empty response.',
      );

    const rawItems =
      response['items'] ??
      response['Items'];

    return {
      propertyId:
        this.toStringValue(
          response['propertyId'] ??
          response['PropertyId'],
        ) || fallbackPropertyId,

      items:
        Array.isArray(rawItems)
          ? rawItems
              .map((item) =>
                this.mapPropertyReview(item),
              )
              .filter(
                (
                  item,
                ): item is PropertyReview =>
                  item !== null,
              )
          : [],

      page:
        this.toPositiveInteger(
          response['page'] ??
          response['Page'],
          fallbackPage,
        ),

      pageSize:
        this.toPositiveInteger(
          response['pageSize'] ??
          response['PageSize'],
          fallbackPageSize,
        ),

      totalCount:
        this.toNonNegativeInteger(
          response['totalCount'] ??
          response['TotalCount'],
        ),

      totalPages:
        this.toNonNegativeInteger(
          response['totalPages'] ??
          response['TotalPages'],
        ),
    };
  }

  private mapPropertyReview(
    value: unknown,
  ): PropertyReview | null {
    const review =
      this.toRecord(value);

    const id =
      this.toStringValue(
        review['id'] ??
        review['Id'],
      );

    if (!id) {
      return null;
    }

    const author =
      this.toRecord(
        review['author'] ??
        review['Author'],
      );

    const replyValue =
      review['reply'] ??
      review['Reply'];

    return {
      id,

      rating:
        this.normalizeRating(
          review['rating'] ??
          review['Rating'],
        ),

      positiveComment:
        this.toNullableStringValue(
          review['positiveComment'] ??
          review['PositiveComment'],
        ),

      negativeComment:
        this.toNullableStringValue(
          review['negativeComment'] ??
          review['NegativeComment'],
        ),

      helpfulCount:
        this.toNonNegativeInteger(
          review['helpfulCount'] ??
          review['HelpfulCount'],
        ),

      isHelpfulByCurrentUser:
        this.toBooleanValue(
          review['isHelpfulByCurrentUser'] ??
          review['IsHelpfulByCurrentUser'],
        ),

      author: {
        userId:
          this.toStringValue(
            author['userId'] ??
            author['UserId'],
          ),

        displayName:
          this.toStringValue(
            author['displayName'] ??
            author['DisplayName'],
          ) || 'SmartStay guest',

        profileImageUrl:
          this.toNullableStringValue(
            author['profileImageUrl'] ??
            author['ProfileImageUrl'],
          ),
      },

      reply:
        replyValue
          ? this.mapReply(
              replyValue,
            )
          : null,

      createdAt:
        this.toStringValue(
          review['createdAt'] ??
          review['CreatedAt'],
        ),

      publishedAt:
        this.toNullableStringValue(
          review['publishedAt'] ??
          review['PublishedAt'],
        ),
    };
  }

  private mapReply(
    value: unknown,
  ): PropertyReviewReply | null {
    const reply =
      this.toRecord(value);

    const id =
      this.toStringValue(
        reply['id'] ??
        reply['Id'],
      );

    const content =
      this.toStringValue(
        reply['content'] ??
        reply['Content'],
      );

    if (!id || !content) {
      return null;
    }

    return {
      id,

      hostProfileId:
        this.toStringValue(
          reply['hostProfileId'] ??
          reply['HostProfileId'],
        ),

      hostDisplayName:
        this.toStringValue(
          reply['hostDisplayName'] ??
          reply['HostDisplayName'],
        ) || 'Host',

      hostProfileImageUrl:
        this.toNullableStringValue(
          reply['hostProfileImageUrl'] ??
          reply['HostProfileImageUrl'],
        ),

      content,

      createdAt:
        this.toStringValue(
          reply['createdAt'] ??
          reply['CreatedAt'],
        ),

      updatedAt:
        this.toNullableStringValue(
          reply['updatedAt'] ??
          reply['UpdatedAt'],
        ),
    };
  }

  private mapRatingSummaryResponse(
    responseText: string | null | undefined,
    fallbackPropertyId: string,
  ): PropertyRatingSummary {
    const response =
      this.parseRequiredJsonRecord(
        responseText,
        'Property rating summary API returned an empty response.',
      );

    const distributionValue =
      this.toRecord(
        response['distribution'] ??
        response['Distribution'],
      );

    const distribution:
      Record<number, number> = {};

    for (
      let rating = 1;
      rating <= 5;
      rating += 1
    ) {
      distribution[rating] =
        this.toNonNegativeInteger(
          distributionValue[
            String(rating)
          ] ??
          distributionValue[rating],
        );
    }

    return {
      propertyId:
        this.toStringValue(
          response['propertyId'] ??
          response['PropertyId'],
        ) || fallbackPropertyId,

      averageRating:
        this.toNonNegativeNumber(
          response['averageRating'] ??
          response['AverageRating'],
        ),

      reviewsCount:
        this.toNonNegativeInteger(
          response['reviewsCount'] ??
          response['ReviewsCount'],
        ),

      distribution,
    };
  }

  private mapHelpfulResponse(
    responseText: string | null | undefined,
    fallbackReviewId: string,
  ): ReviewHelpfulResponse {
    const response =
      this.parseRequiredJsonRecord(
        responseText,
        'Helpful review API returned an empty response.',
      );

    return {
      reviewId:
        this.toStringValue(
          response['reviewId'] ??
          response['ReviewId'],
        ) || fallbackReviewId,

      helpfulCount:
        this.toNonNegativeInteger(
          response['helpfulCount'] ??
          response['HelpfulCount'],
        ),

      isHelpfulByCurrentUser:
        this.toBooleanValue(
          response[
            'isHelpfulByCurrentUser'
          ] ??
          response[
            'IsHelpfulByCurrentUser'
          ],
        ),
    };
  }

  private parseRequiredJsonRecord(
    responseText: string | null | undefined,
    emptyResponseMessage: string,
  ): Record<string, unknown> {
    const normalizedResponse =
      (responseText ?? '')
        .replace(/^\uFEFF/, '')
        .trim();

    if (!normalizedResponse) {
      throw new Error(
        emptyResponseMessage,
      );
    }

    try {
      return this.toRecord(
        JSON.parse(
          normalizedResponse,
        ) as unknown,
      );
    } catch {
      throw new Error(
        'The server returned an invalid JSON response.',
      );
    }
  }

  private normalizePage(
    page: number,
  ): number {
    if (!Number.isFinite(page)) {
      return 1;
    }

    return Math.max(
      1,
      Math.trunc(page),
    );
  }

  private normalizePageSize(
    pageSize: number,
  ): number {
    if (!Number.isFinite(pageSize)) {
      return 6;
    }

    return Math.min(
      this.maximumPageSize,
      Math.max(
        1,
        Math.trunc(pageSize),
      ),
    );
  }

  private normalizeRating(
    value: unknown,
  ): number {
    const rating =
      Number(value);

    if (!Number.isFinite(rating)) {
      return 1;
    }

    return Math.min(
      5,
      Math.max(
        1,
        Math.round(rating),
      ),
    );
  }

  private toRecord(
    value: unknown,
  ): Record<string, unknown> {
    return value &&
      typeof value === 'object' &&
      !Array.isArray(value)
      ? value as Record<
          string,
          unknown
        >
      : {};
  }

  private toStringValue(
    value: unknown,
  ): string {
    return typeof value === 'string'
      ? value.trim()
      : '';
  }

  private toNullableStringValue(
    value: unknown,
  ): string | null {
    const normalizedValue =
      this.toStringValue(value);

    return normalizedValue || null;
  }

  private toBooleanValue(
    value: unknown,
  ): boolean {
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

    return Number(value) === 1;
  }

  private toPositiveInteger(
    value: unknown,
    fallback: number,
  ): number {
    const normalizedValue =
      Math.trunc(
        Number(value),
      );

    return (
      Number.isFinite(
        normalizedValue,
      ) &&
      normalizedValue > 0
    )
      ? normalizedValue
      : fallback;
  }

  private toNonNegativeInteger(
    value: unknown,
  ): number {
    const normalizedValue =
      Math.trunc(
        Number(value),
      );

    return Number.isFinite(
      normalizedValue,
    )
      ? Math.max(
          0,
          normalizedValue,
        )
      : 0;
  }

  private toNonNegativeNumber(
    value: unknown,
  ): number {
    const normalizedValue =
      Number(value);

    return Number.isFinite(
      normalizedValue,
    )
      ? Math.max(
          0,
          normalizedValue,
        )
      : 0;
  }
}