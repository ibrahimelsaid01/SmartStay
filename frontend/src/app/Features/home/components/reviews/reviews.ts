import { CommonModule } from '@angular/common';
import {
  ChangeDetectorRef,
  Component,
  DestroyRef,
  OnInit,
  inject,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterModule } from '@angular/router';
import { finalize } from 'rxjs';

import { AuthState } from '../../../auth/services/auth-state';
import {
  FeaturedReview,
  FeaturedReviewsService,
} from '../../services/featured-reviews';

export type HomeReviewSource =
  | 'real'
  | 'mock';

export interface HomeReviewCard {
  id: string;
  source: HomeReviewSource;
  reviewerName: string;
  reviewerImageUrl: string;
  location: string;
  rating: number;
  comment: string;
  propertyId?: string | null;
  propertyTitle?: string | null;
  publishedAt?: string | null;
}

@Component({
  selector: 'app-reviews',
  standalone: true,
  imports: [
    RouterModule,
    CommonModule,
  ],
  templateUrl: './reviews.html',
  styleUrl: './reviews.css',
})
export class Reviews implements OnInit {
  readonly starOptions = [
    1,
    2,
    3,
    4,
    5,
  ];

  readonly maximumDisplayedReviews = 3;

  displayedReviews: HomeReviewCard[] = [];

  reviewsLoading = false;
  realReviewsCount = 0;

  private readonly destroyRef =
    inject(DestroyRef);

  private readonly mockReviews: HomeReviewCard[] = [
    {
      id: 'mock-ahmed-cairo',
      source: 'mock',
      reviewerName: 'Ahmed K.',
      reviewerImageUrl:
        '/Images/default-avatar.png',
      location: 'Cairo',
      rating: 5,
      comment:
        'The AI smart search recommended the perfect studio in Zamalek for my business trip. It saved me hours of searching.',
      propertyId: null,
      propertyTitle: null,
      publishedAt: null,
    },
    {
      id: 'mock-layla-alexandria',
      source: 'mock',
      reviewerName: 'Layla M.',
      reviewerImageUrl:
        '/Images/default-avatar.png',
      location: 'Alexandria',
      rating: 5,
      comment:
        'As a solo female traveler, safety is my top priority. Seeing the AI-Verified Host badge gave me 100% peace of mind.',
      propertyId: null,
      propertyTitle: null,
      publishedAt: null,
    },
    {
      id: 'mock-omar-hurghada',
      source: 'mock',
      reviewerName: 'Omar S.',
      reviewerImageUrl:
        '/Images/default-avatar.png',
      location: 'Hurghada',
      rating: 5,
      comment:
        'Listing my villa on SmartStay has been amazing. The AI Guest Screening strictly checks profiles to ensure high-trust bookings.',
      propertyId: null,
      propertyTitle: null,
      publishedAt: null,
    },
  ];

  constructor(
    public readonly authState: AuthState,
    private readonly featuredReviewsService:
      FeaturedReviewsService,
    private readonly cdr: ChangeDetectorRef,
  ) {
    /*
     * Render the three Mock reviews immediately.
     * Real reviews replace them only after the API
     * responds successfully.
     */
    this.displayedReviews = [
      ...this.mockReviews,
    ];
  }

  ngOnInit(): void {
    this.loadFeaturedReviews();
  }

  loadFeaturedReviews(): void {
    if (this.reviewsLoading) {
      return;
    }

    this.reviewsLoading = true;

    this.featuredReviewsService
      .getFeaturedReviews(
        this.maximumDisplayedReviews,
      )
      .pipe(
        takeUntilDestroyed(
          this.destroyRef,
        ),
        finalize(() => {
          this.reviewsLoading = false;
          this.cdr.detectChanges();
        }),
      )
      .subscribe({
        next: (
          reviews: FeaturedReview[],
        ) => {
          const realReviews =
            this.mapFeaturedReviews(
              reviews,
            );

          this.realReviewsCount =
            realReviews.length;

          /*
           * Every Real review removes one Mock review.
           *
           * Examples:
           *
           * 0 Real → Mock 0, 1, 2
           * 1 Real → Real 0 + Mock 1, 2
           * 2 Real → Real 0, 1 + Mock 2
           * 3 Real → Real 0, 1, 2
           */
          this.displayedReviews = [
            ...realReviews,
            ...this.mockReviews.slice(
              realReviews.length,
            ),
          ].slice(
            0,
            this.maximumDisplayedReviews,
          );

          this.cdr.detectChanges();
        },
        error: () => {
          /*
           * The Home page must remain usable when the
           * public reviews endpoint is unavailable.
           * Keep all three Mock reviews as fallback.
           */
          this.realReviewsCount = 0;

          this.displayedReviews = [
            ...this.mockReviews,
          ];

          this.cdr.detectChanges();
        },
      });
  }

  isRealReview(
    review: HomeReviewCard,
  ): boolean {
    return review.source === 'real';
  }

  trackReviewById(
    _index: number,
    review: HomeReviewCard,
  ): string {
    return review.id;
  }

  useDefaultReviewerImage(
    event: Event,
  ): void {
    const image =
      event.target as HTMLImageElement | null;

    if (!image) {
      return;
    }

    image.onerror = null;
    image.src =
      '/Images/default-avatar.png';
  }

  private mapFeaturedReviews(
    reviews: FeaturedReview[],
  ): HomeReviewCard[] {
    const uniqueUserIds =
      new Set<string>();

    const mappedReviews:
      HomeReviewCard[] = [];

    for (const review of reviews) {
      const userId =
        review.author.userId.trim();

      /*
       * The Backend already returns one review per
       * user. This additional check protects the Home
       * UI if an older API response contains duplicates.
       */
      if (
        userId &&
        uniqueUserIds.has(userId)
      ) {
        continue;
      }

      if (userId) {
        uniqueUserIds.add(userId);
      }

      mappedReviews.push(
        this.mapFeaturedReview(
          review,
        ),
      );

      if (
        mappedReviews.length >=
        this.maximumDisplayedReviews
      ) {
        break;
      }
    }

    return mappedReviews;
  }

  private mapFeaturedReview(
    review: FeaturedReview,
  ): HomeReviewCard {
    return {
      id: review.id,
      source: 'real',

      reviewerName:
        review.author.displayName,

      reviewerImageUrl:
        review.author.profileImageUrl ||
        '/Images/default-avatar.png',

      location:
        this.buildReviewLocation(
          review,
        ),

      rating:
        this.normalizeRating(
          review.rating,
        ),

      comment:
        review.comment,

      propertyId:
        review.property.id || null,

      propertyTitle:
        review.property.title || null,

      publishedAt:
        review.publishedAt || null,
    };
  }

  private buildReviewLocation(
    review: FeaturedReview,
  ): string {
    return [
      review.property.city,
      review.property.country,
    ]
      .map((value) =>
        value.trim(),
      )
      .filter(Boolean)
      .join(', ') ||
      review.property.title ||
      'SmartStay guest';
  }

  private normalizeRating(
    rating: number,
  ): number {
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
}