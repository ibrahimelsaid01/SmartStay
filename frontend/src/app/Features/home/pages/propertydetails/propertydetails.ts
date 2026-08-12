import { CommonModule } from '@angular/common';
import {
  ChangeDetectorRef,
  Component,
  DestroyRef,
  OnInit,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import {
  ActivatedRoute,
  Router,
  RouterModule,
} from '@angular/router';
import { finalize } from 'rxjs';

import { AuthState } from '../../../auth/services/auth-state';
import { BookingWidget } from '../compnents/booking-widget/booking-widget';
import { ExtraServices } from '../compnents/extra-services/extra-services';
import { HostProfile } from '../compnents/host-profile/host-profile';
import { LocationMap } from '../compnents/location-map/location-map';
import { PropertyGallery } from '../compnents/property-gallery/property-gallery';
import { PropertyHeader } from '../compnents/property-header/property-header';
import {
  Amenity,
  PropertyDetails,
  Propertydetailservice,
} from '../../services/propertydetailservice';
import {
  PropertyRatingSummary,
  PropertyReview,
  PropertyReviewsResponse,
  PropertyReviewsService,
  ReviewHelpfulResponse,
} from '../../services/property-reviews';

@Component({
  selector: 'app-propertydetails',
  imports: [
    CommonModule,
    RouterModule,
    FormsModule,
    PropertyGallery,
    PropertyHeader,
    BookingWidget,
    ExtraServices,
    HostProfile,
    LocationMap,
  ],
  templateUrl: './propertydetails.html',
  styleUrl: './propertydetails.css',
})
export class Propertydetails implements OnInit {
  readonly starOptions = [1, 2, 3, 4, 5];
  readonly ratingDistributionOrder = [5, 4, 3, 2, 1];
  readonly reviewsPageSize = 6;

  private readonly amenityIconClasses: Readonly<
    Record<string, string>
  > = {
    wifi: 'bi bi-wifi',
    snowflake: 'bi bi-snow',
    flame: 'bi bi-fire',
    washer: 'bi bi-droplet',
    iron: 'bi bi-lightning-charge',
    briefcase: 'bi bi-briefcase',
    bed: 'bi bi-moon-stars',
    'cooking-pot': 'bi bi-cup-hot',
    refrigerator: 'bi bi-box',
    microwave: 'bi bi-grid-3x3-gap',
    oven: 'bi bi-thermometer-high',
    stove: 'bi bi-fire',
    coffee: 'bi bi-cup-hot',
    kettle: 'bi bi-cup-straw',
    utensils: 'bi bi-fork-knife',
    'shower-head': 'bi bi-droplet',
    wind: 'bi bi-wind',
    bath: 'bi bi-water',
    towel: 'bi bi-layers',
    package: 'bi bi-box-seam',
    tv: 'bi bi-tv',
    play: 'bi bi-play-circle',
    'book-open': 'bi bi-book',
    gamepad: 'bi bi-controller',
    building: 'bi bi-building',
    trees: 'bi bi-tree',
    armchair: 'bi bi-house-heart',
    waves: 'bi bi-water',
    car: 'bi bi-car-front',
    'circle-dollar-sign': 'bi bi-currency-dollar',
    'parking-circle': 'bi bi-p-circle',
    'door-open': 'bi bi-door-open',
    'alarm-smoke': 'bi bi-bell',
    'badge-alert': 'bi bi-exclamation-triangle',
    'fire-extinguisher': 'bi bi-fire',
    'briefcase-medical': 'bi bi-bandaid',
    'lock-keyhole': 'bi bi-lock',
    'arrow-up-down': 'bi bi-arrow-down-up',
    'move-horizontal': 'bi bi-arrows-expand',
    accessibility: 'bi bi-universal-access',
    'square-parking': 'bi bi-p-square',
  };

  private readonly amenityCategoryIconClasses: Readonly<
    Record<string, string>
  > = {
    essentials: 'bi bi-check-circle',
    kitchenanddining: 'bi bi-cup-hot',
    bathroom: 'bi bi-droplet',
    entertainment: 'bi bi-play-circle',
    outdoor: 'bi bi-tree',
    parkingandaccess: 'bi bi-car-front',
    safety: 'bi bi-shield-check',
    accessibility: 'bi bi-universal-access',
  };

  readonly property =
    signal<PropertyDetails | null>(
      null,
    );

  readonly reviews =
    signal<PropertyReview[]>(
      [],
    );

  readonly ratingSummary =
    signal<PropertyRatingSummary | null>(
      null,
    );

  propertyLoading = false;
  propertyErrorMessage = '';

  reviewsLoading = false;
  reviewsErrorMessage = '';
  reviewsSuccessMessage = '';

  reviewsPage = 1;
  reviewsTotalCount = 0;
  reviewsTotalPages = 0;

  helpfulLoadingReviewId:
    string | null =
    null;

  private readonly propertyService =
    inject(
      Propertydetailservice,
    );

  private readonly propertyReviewsService =
    inject(
      PropertyReviewsService,
    );

  private readonly route =
    inject(
      ActivatedRoute,
    );

  private readonly router =
    inject(
      Router,
    );

  private readonly authState =
    inject(
      AuthState,
    );

  private readonly cdr =
    inject(
      ChangeDetectorRef,
    );

  private readonly destroyRef =
    inject(
      DestroyRef,
    );

  private propertyId = '';
  private propertyRequestId = 0;
  private reviewsRequestId = 0;
  private ratingRequestId = 0;

  ngOnInit(): void {
    this.route.paramMap
      .pipe(
        takeUntilDestroyed(
          this.destroyRef,
        ),
      )
      .subscribe((params) => {
        const propertyId =
          (
            params.get('id') ??
            ''
          ).trim();

        if (
          !this.isGuid(
            propertyId,
          )
        ) {
          this.resetPageState();

          this.propertyErrorMessage =
            'The property ID is invalid.';

          this.cdr.detectChanges();

          return;
        }

        this.propertyId =
          propertyId;

        this.resetPageState();

        /*
         * These requests start independently so the property,
         * reviews, and rating summary load in parallel.
         */
        this.loadProperty();
        this.loadReviews(1);
        this.loadRatingSummary();
      });
  }

  loadProperty(): void {
    if (!this.propertyId) {
      return;
    }

    const requestId =
      ++this.propertyRequestId;

    this.propertyLoading =
      true;

    this.propertyErrorMessage =
      '';

    this.cdr.detectChanges();

    this.propertyService
      .getPropertyById(
        this.propertyId,
      )
      .pipe(
        finalize(() => {
          if (
            requestId ===
            this.propertyRequestId
          ) {
            this.propertyLoading =
              false;

            this.cdr.detectChanges();
          }
        }),
      )
      .subscribe({
        next: (
          data:
            PropertyDetails,
        ) => {
          if (
            requestId !==
            this.propertyRequestId
          ) {
            return;
          }

          this.property.set(
            data,
          );

          this.cdr.detectChanges();
        },

        error: (
          error: unknown,
        ) => {
          if (
            requestId !==
            this.propertyRequestId
          ) {
            return;
          }

          this.property.set(
            null,
          );

          this.propertyErrorMessage =
            this.extractErrorMessage(
              error,
            ) ||
            'Failed to load the property details.';

          this.cdr.detectChanges();
        },
      });
  }

  loadReviews(
    page =
      this.reviewsPage,

    preserveMessages =
      false,
  ): void {
    if (!this.propertyId) {
      return;
    }

    const requestId =
      ++this.reviewsRequestId;

    this.reviewsPage =
      Math.max(
        1,
        page,
      );

    this.reviewsLoading =
      true;

    this.reviewsErrorMessage =
      '';

    if (
      !preserveMessages
    ) {
      this.reviewsSuccessMessage =
        '';
    }

    this.cdr.detectChanges();

    this.propertyReviewsService
      .getPropertyReviews(
        this.propertyId,
        this.reviewsPage,
        this.reviewsPageSize,
      )
      .pipe(
        finalize(() => {
          if (
            requestId ===
            this.reviewsRequestId
          ) {
            this.reviewsLoading =
              false;

            this.cdr.detectChanges();
          }
        }),
      )
      .subscribe({
        next: (
          response:
            PropertyReviewsResponse,
        ) => {
          if (
            requestId !==
            this.reviewsRequestId
          ) {
            return;
          }

          this.reviews.set(
            response.items ??
              [],
          );

          this.reviewsPage =
            response.page ||
            this.reviewsPage;

          this.reviewsTotalCount =
            response.totalCount ??
            0;

          this.reviewsTotalPages =
            response.totalPages ??
            0;

          if (
            this.reviews()
              .length === 0 &&
            this.reviewsTotalCount >
              0 &&
            this.reviewsPage >
              1
          ) {
            this.loadReviews(
              this.reviewsPage -
                1,

              preserveMessages,
            );

            return;
          }

          this.cdr.detectChanges();
        },

        error: (
          error: unknown,
        ) => {
          if (
            requestId !==
            this.reviewsRequestId
          ) {
            return;
          }

          this.reviews.set(
            [],
          );

          this.reviewsTotalCount =
            0;

          this.reviewsTotalPages =
            0;

          this.reviewsErrorMessage =
            this.extractErrorMessage(
              error,
            ) ||
            'Failed to load property reviews.';

          this.cdr.detectChanges();
        },
      });
  }

  loadRatingSummary(): void {
    if (!this.propertyId) {
      return;
    }

    const requestId =
      ++this.ratingRequestId;

    this.propertyReviewsService
      .getRatingSummary(
        this.propertyId,
      )
      .pipe(
        takeUntilDestroyed(
          this.destroyRef,
        ),
      )
      .subscribe({
        next: (
          summary:
            PropertyRatingSummary,
        ) => {
          if (
            requestId !==
            this.ratingRequestId
          ) {
            return;
          }

          this.ratingSummary.set(
            summary,
          );

          this.cdr.detectChanges();
        },

        error: () => {
          if (
            requestId !==
            this.ratingRequestId
          ) {
            return;
          }

          /*
           * Reviews may still be displayed when the separate
           * summary endpoint is temporarily unavailable.
           */
          this.ratingSummary.set(
            null,
          );

          this.cdr.detectChanges();
        },
      });
  }

  refreshReviews(): void {
    if (
      this.reviewsLoading ||
      !!this
        .helpfulLoadingReviewId
    ) {
      return;
    }

    this.loadReviews(
      this.reviewsPage,
    );

    this.loadRatingSummary();
  }

  toggleHelpful(
    review:
      PropertyReview,
  ): void {
    if (
      this.reviewsLoading ||
      this
        .helpfulLoadingReviewId
    ) {
      return;
    }

    if (
      !this.authState
        .isLoggedIn()
    ) {
      void this.router.navigate(
        ['/login'],
        {
          queryParams: {
            returnUrl:
              this.router.url,
          },
        },
      );

      return;
    }

    this.helpfulLoadingReviewId =
      review.id;

    this.reviewsErrorMessage =
      '';

    this.reviewsSuccessMessage =
      '';

    this.cdr.detectChanges();

    const request$ =
      review
        .isHelpfulByCurrentUser
        ? this
            .propertyReviewsService
            .removeHelpful(
              review.id,
            )
        : this
            .propertyReviewsService
            .markHelpful(
              review.id,
            );

    request$
      .pipe(
        finalize(() => {
          this.helpfulLoadingReviewId =
            null;

          this.cdr.detectChanges();
        }),
      )
      .subscribe({
        next: (
          response:
            ReviewHelpfulResponse,
        ) => {
          this.applyHelpfulResponse(
            response,
          );

          this.reviewsSuccessMessage =
            response
              .isHelpfulByCurrentUser
              ? 'Review marked as helpful.'
              : 'Helpful vote removed.';

          this.cdr.detectChanges();
        },

        error: (
          error: unknown,
        ) => {
          this.reviewsErrorMessage =
            this.extractErrorMessage(
              error,
            ) ||
            'Failed to update the helpful vote.';

          this.cdr.detectChanges();
        },
      });
  }

  goToPreviousReviewsPage():
    void {
    if (
      this.reviewsPage <=
        1 ||
      this.reviewsLoading ||
      !!this
        .helpfulLoadingReviewId
    ) {
      return;
    }

    this.loadReviews(
      this.reviewsPage -
        1,
    );
  }

  goToNextReviewsPage():
    void {
    if (
      this.reviewsPage >=
        this
          .reviewsTotalPages ||
      this.reviewsLoading ||
      !!this
        .helpfulLoadingReviewId
    ) {
      return;
    }

    this.loadReviews(
      this.reviewsPage +
        1,
    );
  }

  isHelpfulLoading(
    reviewId:
      string,
  ): boolean {
    return (
      this
        .helpfulLoadingReviewId ===
      reviewId
    );
  }

  getRatingCount(
    rating:
      number,
  ): number {
    return (
      this.ratingSummary()
        ?.distribution[
        rating
      ] ?? 0
    );
  }

  getRatingPercentage(
    rating:
      number,
  ): number {
    const reviewsCount =
      this.ratingSummary()
        ?.reviewsCount ??
      0;

    if (
      reviewsCount <=
      0
    ) {
      return 0;
    }

    return Math.min(
      100,
      Math.max(
        0,
        Math.round(
          (
            this.getRatingCount(
              rating,
            ) /
            reviewsCount
          ) *
            100,
        ),
      ),
    );
  }

  getAmenityIconClass(
    amenity:
      Amenity,
  ): string {
    const iconKey =
      amenity.iconKey
        ?.trim()
        .toLowerCase() ??
      '';

    if (
      iconKey &&
      this
        .amenityIconClasses[
        iconKey
      ]
    ) {
      return this
        .amenityIconClasses[
        iconKey
      ];
    }

    const category =
      amenity.category
        ?.replace(
          /[^a-z]/gi,
          '',
        )
        .toLowerCase() ??
      '';

    return (
      this
        .amenityCategoryIconClasses[
        category
      ] ??
      'bi bi-check-circle'
    );
  }

  trackAmenityById(
    _index:
      number,

    amenity:
      Amenity,
  ): string {
    return amenity.id;
  }

  getReviewDisplayDate(
    review:
      PropertyReview,
  ): string {
    return (
      review.publishedAt ??
      review.createdAt
    );
  }

  trackReviewById(
    _index:
      number,

    review:
      PropertyReview,
  ): string {
    return review.id;
  }

  useDefaultProfileImage(
    event:
      Event,
  ): void {
    const image =
      event.target as
        | HTMLImageElement
        | null;

    if (!image) {
      return;
    }

    image.onerror =
      null;

    image.src =
      '/Images/default-avatar.png';
  }

  private applyHelpfulResponse(
    response:
      ReviewHelpfulResponse,
  ): void {
    this.reviews.update(
      (items) =>
        items.map(
          (review) =>
            review.id ===
            response.reviewId
              ? {
                  ...review,

                  helpfulCount:
                    response
                      .helpfulCount,

                  isHelpfulByCurrentUser:
                    response
                      .isHelpfulByCurrentUser,
                }
              : review,
        ),
    );
  }

  private resetPageState():
    void {
    this.propertyRequestId +=
      1;

    this.reviewsRequestId +=
      1;

    this.ratingRequestId +=
      1;

    this.property.set(
      null,
    );

    this.reviews.set(
      [],
    );

    this.ratingSummary.set(
      null,
    );

    this.propertyLoading =
      false;

    this.propertyErrorMessage =
      '';

    this.reviewsLoading =
      false;

    this.reviewsErrorMessage =
      '';

    this.reviewsSuccessMessage =
      '';

    this.reviewsPage =
      1;

    this.reviewsTotalCount =
      0;

    this.reviewsTotalPages =
      0;

    this.helpfulLoadingReviewId =
      null;
  }

  private isGuid(
    value:
      string,
  ): boolean {
    return /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i
      .test(
        value,
      );
  }

  private extractErrorMessage(
    error:
      unknown,
  ): string {
    const typedError =
      error as {
        error?:
          unknown;

        message?:
          string;
      };

    if (
      typeof typedError.error ===
      'string'
    ) {
      const normalizedError =
        typedError.error
          .replace(
            /^\uFEFF/,
            '',
          )
          .trim();

      if (
        !normalizedError
      ) {
        return (
          typedError.message ??
          ''
        );
      }

      try {
        return (
          this.extractProblemDetailsMessage(
            JSON.parse(
              normalizedError,
            ) as unknown,
          ) ||
          normalizedError
        );
      } catch {
        return normalizedError;
      }
    }

    if (
      typedError.error &&
      typeof typedError.error ===
        'object'
    ) {
      return (
        this.extractProblemDetailsMessage(
          typedError.error,
        ) ||
        typedError.message ||
        ''
      );
    }

    return (
      typedError.message ??
      ''
    );
  }

  private extractProblemDetailsMessage(
    value:
      unknown,
  ): string {
    if (
      !value ||
      typeof value !==
        'object'
    ) {
      return '';
    }

    const problem =
      value as {
        detail?:
          string;

        message?:
          string;

        title?:
          string;

        errors?:
          Record<
            string,
            string[]
          >;
      };

    const firstValidationError =
      problem.errors
        ? Object.values(
            problem.errors,
          )[0]?.[0]
        : undefined;

    return (
      problem.detail ||
      problem.message ||
      firstValidationError ||
      problem.title ||
      ''
    );
  }
}