import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import {
  ChangeDetectorRef,
  Component,
  Input,
  OnChanges,
  SimpleChanges,
} from '@angular/core';
import { Router } from '@angular/router';
import { Observable, finalize } from 'rxjs';
import { WishlistService } from '../../../profile/services/wishlist';
import { UserProfileService } from '../../../profile/services/user-profile-service';

@Component({
  selector: 'app-stay-card',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './stay-card.html',
  styleUrl: './stay-card.css',
})
export class StayCard implements OnChanges {
  @Input() stayData: any;

  isWishlisted = false;
  wishlistLoading = false;

  constructor(
    private readonly router: Router,
    private readonly wishlistService: WishlistService,
    private readonly userProfileService: UserProfileService,
    private readonly changeDetectorRef: ChangeDetectorRef,
  ) {}

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['stayData']) {
      this.loadWishlistStatus();
    }
  }

  get propertyId(): string | null {
    return this.stayData?.id ?? this.stayData?.propertyId ?? null;
  }

  get title(): string {
    return this.stayData?.title ?? this.stayData?.name ?? 'Untitled stay';
  }

  get coverImageUrl(): string {
    return (
      this.stayData?.coverImageUrl ??
      this.stayData?.image ??
      this.stayData?.Image ??
      'Images/section1.jpg'
    );
  }

  get location(): string {
    const city = this.stayData?.city ?? '';
    const country = this.stayData?.country ?? '';

    if (city && country) {
      return `${city}, ${country}`;
    }

    return this.stayData?.location ?? 'Location not specified';
  }

  get averageRating(): number {
    return this.stayData?.averageRating ?? this.stayData?.rating ?? 0;
  }

  get pricePerNight(): number {
    return this.stayData?.pricePerNight ?? this.stayData?.price ?? 0;
  }

  get currency(): string {
    return this.stayData?.currency ?? 'EGP';
  }

  get maxGuests(): number {
    return this.stayData?.maxGuests ?? this.stayData?.guests ?? 0;
  }

  get bedrooms(): number {
    return this.stayData?.bedrooms ?? this.stayData?.beds ?? 0;
  }

  get bathrooms(): number {
    return this.stayData?.bathrooms ?? this.stayData?.baths ?? 0;
  }

  toggleWishlist(event: MouseEvent): void {
    event.stopPropagation();
    event.preventDefault();

    const propertyId = this.propertyId;

    if (!propertyId || this.wishlistLoading) {
      return;
    }

    if (!this.userProfileService.isAuthenticated()) {
      this.router.navigate(['/login']);
      return;
    }

    this.wishlistLoading = true;

    const wishlistRequest: Observable<unknown> = this.isWishlisted
      ? this.wishlistService.removeFromDefaultWishList(propertyId)
      : this.wishlistService.addToDefaultWishList(propertyId);

    wishlistRequest
      .pipe(
        finalize(() => {
          this.wishlistLoading = false;
          this.changeDetectorRef.detectChanges();
        }),
      )
      .subscribe({
        next: () => {
          this.isWishlisted = !this.isWishlisted;
        },
        error: (error: unknown) => {
          if (error instanceof HttpErrorResponse && error.status === 409) {
            this.isWishlisted = true;
          }
        },
      });
  }

  goToDetails(): void {
    const propertyId = this.propertyId;

    if (!propertyId) {
      return;
    }

    this.router.navigate(['/property-details', propertyId]);
  }

  private loadWishlistStatus(): void {
    const propertyId = this.propertyId;

    if (!propertyId || !this.userProfileService.isAuthenticated()) {
      this.isWishlisted = false;
      this.wishlistLoading = false;
      return;
    }

    this.wishlistLoading = true;

    this.wishlistService
      .isPropertyWishlisted(propertyId)
      .pipe(
        finalize(() => {
          if (this.propertyId === propertyId) {
            this.wishlistLoading = false;
            this.changeDetectorRef.detectChanges();
          }
        }),
      )
      .subscribe({
        next: (isWishlisted) => {
          if (this.propertyId === propertyId) {
            this.isWishlisted = isWishlisted;
          }
        },
        error: () => {
          if (this.propertyId === propertyId) {
            this.isWishlisted = false;
          }
        },
      });
  }
}