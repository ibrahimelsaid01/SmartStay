import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, map, timeout } from 'rxjs';

import { environment } from '../../../../environments/environment';

export interface FeaturedReviewAuthor {
  userId: string;
  displayName: string;
  profileImageUrl?: string | null;
}

export interface FeaturedReviewProperty {
  id: string;
  title: string;
  country: string;
  city: string;
  coverImageUrl?: string | null;
}

export interface FeaturedReview {
  id: string;
  rating: number;
  comment: string;
  author: FeaturedReviewAuthor;
  property: FeaturedReviewProperty;
  publishedAt: string;
}

@Injectable({
  providedIn: 'root',
})
export class FeaturedReviewsService {
  private readonly apiUrl =
    `${environment.baseApi}/api/reviews/public/featured`;

  private readonly requestTimeoutMs = 30000;
  private readonly maximumFeaturedReviews = 3;

  constructor(
    private readonly http: HttpClient,
  ) {}

  getFeaturedReviews(
    limit = this.maximumFeaturedReviews,
  ): Observable<FeaturedReview[]> {
    const normalizedLimit =
      this.normalizeLimit(limit);

    const params = new HttpParams()
      .set('limit', String(normalizedLimit))
      .set('_ts', String(Date.now()));

    return this.http
      .get(
        this.apiUrl,
        {
          params,
          responseType: 'text',
        },
      )
      .pipe(
        timeout(this.requestTimeoutMs),
        map((responseText) =>
          this.parseFeaturedReviews(
            responseText,
            normalizedLimit,
          ),
        ),
      );
  }

  private parseFeaturedReviews(
    responseText: string | null | undefined,
    limit: number,
  ): FeaturedReview[] {
    const normalizedResponse =
      (responseText ?? '')
        .replace(/^\uFEFF/, '')
        .trim();

    if (!normalizedResponse) {
      return [];
    }

    let parsedResponse: unknown;

    try {
      parsedResponse = JSON.parse(
        normalizedResponse,
      ) as unknown;
    } catch {
      throw new Error(
        'Featured reviews API returned invalid JSON.',
      );
    }

    const rawItems =
      this.extractReviewItems(
        parsedResponse,
      );

    return rawItems
      .map((item) =>
        this.mapFeaturedReview(item),
      )
      .filter(
        (review): review is FeaturedReview =>
          review !== null,
      )
      .slice(0, limit);
  }

  private extractReviewItems(
    response: unknown,
  ): unknown[] {
    if (Array.isArray(response)) {
      return response;
    }

    if (
      !response ||
      typeof response !== 'object'
    ) {
      return [];
    }

    const responseRecord =
      response as Record<string, unknown>;

    const possibleItems =
      responseRecord['items'] ??
      responseRecord['Items'] ??
      responseRecord['data'] ??
      responseRecord['Data'];

    return Array.isArray(possibleItems)
      ? possibleItems
      : [];
  }

  private mapFeaturedReview(
    value: unknown,
  ): FeaturedReview | null {
    if (
      !value ||
      typeof value !== 'object'
    ) {
      return null;
    }

    const item =
      value as Record<string, unknown>;

    const authorValue =
      item['author'] ??
      item['Author'];

    const propertyValue =
      item['property'] ??
      item['Property'];

    const author =
      this.toRecord(authorValue);

    const property =
      this.toRecord(propertyValue);

    const id =
      this.toStringValue(
        item['id'] ??
        item['Id'],
      );

    const comment =
      this.toStringValue(
        item['comment'] ??
        item['Comment'],
      );

    const displayName =
      this.toStringValue(
        author['displayName'] ??
        author['DisplayName'],
      );

    if (
      !id ||
      !comment ||
      !displayName
    ) {
      return null;
    }

    return {
      id,

      rating:
        this.normalizeRating(
          item['rating'] ??
          item['Rating'],
        ),

      comment,

      author: {
        userId:
          this.toStringValue(
            author['userId'] ??
            author['UserId'],
          ),

        displayName,

        profileImageUrl:
          this.toNullableStringValue(
            author['profileImageUrl'] ??
            author['ProfileImageUrl'],
          ),
      },

      property: {
        id:
          this.toStringValue(
            property['id'] ??
            property['Id'],
          ),

        title:
          this.toStringValue(
            property['title'] ??
            property['Title'],
          ) || 'SmartStay property',

        country:
          this.toStringValue(
            property['country'] ??
            property['Country'],
          ),

        city:
          this.toStringValue(
            property['city'] ??
            property['City'],
          ),

        coverImageUrl:
          this.toNullableStringValue(
            property['coverImageUrl'] ??
            property['CoverImageUrl'],
          ),
      },

      publishedAt:
        this.toStringValue(
          item['publishedAt'] ??
          item['PublishedAt'],
        ),
    };
  }

  private normalizeLimit(
    limit: number,
  ): number {
    if (!Number.isFinite(limit)) {
      return this.maximumFeaturedReviews;
    }

    return Math.min(
      this.maximumFeaturedReviews,
      Math.max(
        1,
        Math.trunc(limit),
      ),
    );
  }

  private normalizeRating(
    value: unknown,
  ): number {
    const rating = Number(value);

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
      ? value as Record<string, unknown>
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
}