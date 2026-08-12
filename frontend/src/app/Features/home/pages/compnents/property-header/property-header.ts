import { DecimalPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import {
  ChangeDetectorRef,
  Component,
  Input,
  OnChanges,
  SimpleChanges,
} from '@angular/core';
import { Router } from '@angular/router';
import {
  Observable,
  finalize,
} from 'rxjs';

import { WishlistService } from '../../../../profile/services/wishlist';
import { UserProfileService } from '../../../../profile/services/user-profile-service';
import { PropertyDetails } from '../../../services/propertydetailservice';

@Component({
  selector: 'app-property-header',
  imports: [
    DecimalPipe,
  ],
  templateUrl:
    './property-header.html',
  styleUrl:
    './property-header.css',
})
export class PropertyHeader
  implements OnChanges {
  @Input({
    required: true,
  })
  property!:
    PropertyDetails;

  isWishlisted =
    false;

  wishlistLoading =
    false;

  constructor(
    private readonly wishlistService:
      WishlistService,

    private readonly userProfileService:
      UserProfileService,

    private readonly router:
      Router,

    private readonly changeDetectorRef:
      ChangeDetectorRef,
  ) {}

  get displayAddress():
    string {
    const fullAddress =
      this.property
        ?.fullAddress
        ?.trim();

    if (fullAddress) {
      return fullAddress;
    }

    const addressParts = [
      this.property
        ?.streetAddress,

      this.property
        ?.city,

      this.property
        ?.postalCode,

      this.property
        ?.country,
    ]
      .map((part) =>
        part?.trim(),
      )
      .filter(
        (
          part,
        ): part is string =>
          !!part,
      );

    return (
      addressParts.join(
        ', ',
      ) ||
      'Location not provided'
    );
  }

  get locationBadge():
    string {
    return (
      this.property
        ?.city
        ?.trim() ||
      this.property
        ?.country
        ?.trim() ||
      'Property'
    );
  }

  get averageRating():
    number {
    const rating =
      Number(
        this.property
          ?.averageRating,
      );

    return Number.isFinite(
      rating,
    )
      ? Math.max(
          0,
          Math.min(
            5,
            rating,
          ),
        )
      : 0;
  }

  get reviewsLabel():
    string {
    const reviewsCount =
      Math.max(
        0,
        this.property
          ?.reviewsCount ??
          0,
      );

    return `${reviewsCount} ${
      reviewsCount === 1
        ? 'review'
        : 'reviews'
    }`;
  }

  ngOnChanges(
    changes:
      SimpleChanges,
  ): void {
    if (
      changes['property']
    ) {
      this.loadWishlistStatus();
    }
  }

  toggleWishlist():
    void {
    const propertyId =
      this.property?.id;

    if (
      !propertyId ||
      this.wishlistLoading
    ) {
      return;
    }

    if (
      !this
        .userProfileService
        .isAuthenticated()
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

    this.wishlistLoading =
      true;

    const wishlistRequest:
      Observable<unknown> =
      this.isWishlisted
        ? this
            .wishlistService
            .removeFromDefaultWishList(
              propertyId,
            )
        : this
            .wishlistService
            .addToDefaultWishList(
              propertyId,
            );

    wishlistRequest
      .pipe(
        finalize(() => {
          this.wishlistLoading =
            false;

          this
            .changeDetectorRef
            .detectChanges();
        }),
      )
      .subscribe({
        next: () => {
          this.isWishlisted =
            !this.isWishlisted;
        },

        error: (
          error: unknown,
        ) => {
          if (
            error instanceof
              HttpErrorResponse &&
            error.status ===
              409
          ) {
            this.isWishlisted =
              true;
          }
        },
      });
  }

  private loadWishlistStatus():
    void {
    const propertyId =
      this.property?.id;

    if (
      !propertyId ||
      !this
        .userProfileService
        .isAuthenticated()
    ) {
      this.isWishlisted =
        false;

      this.wishlistLoading =
        false;

      return;
    }

    this.wishlistLoading =
      true;

    this.wishlistService
      .isPropertyWishlisted(
        propertyId,
      )
      .pipe(
        finalize(() => {
          if (
            this.property
              ?.id ===
            propertyId
          ) {
            this.wishlistLoading =
              false;

            this
              .changeDetectorRef
              .detectChanges();
          }
        }),
      )
      .subscribe({
        next: (
          isWishlisted,
        ) => {
          if (
            this.property
              ?.id ===
            propertyId
          ) {
            this.isWishlisted =
              isWishlisted;
          }
        },

        error: () => {
          if (
            this.property
              ?.id ===
            propertyId
          ) {
            this.isWishlisted =
              false;
          }
        },
      });
  }
}