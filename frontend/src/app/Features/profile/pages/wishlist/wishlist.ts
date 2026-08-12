import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { RouterModule } from '@angular/router';
import { finalize } from 'rxjs';
import { WishListItemResponse, WishlistService } from '../../services/wishlist';

@Component({
  selector: 'app-wishlist',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './wishlist.html',
  styleUrl: './wishlist.css',
})
export class Wishlist implements OnInit {
  wishlist: WishListItemResponse[] = [];
  loading = false;
  errorMessage = '';

  private readonly removingPropertyIds = new Set<string>();

  constructor(
    private readonly wishlistService: WishlistService,
    private readonly changeDetectorRef: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    this.loadWishlist();
  }

  loadWishlist(): void {
    if (this.loading) {
      return;
    }

    this.loading = true;
    this.errorMessage = '';

    this.wishlistService
      .getDefaultWishListItems()
      .pipe(
        finalize(() => {
          this.loading = false;
          this.changeDetectorRef.detectChanges();
        }),
      )
      .subscribe({
        next: (response) => {
          this.wishlist = response.items ?? [];
        },
        error: (error: unknown) => {
          this.errorMessage = this.getErrorMessage(
            error,
            'Failed to load your wishlist. Please try again.',
          );
        },
      });
  }

  removeFromWishlist(propertyId: string): void {
    if (this.isRemoving(propertyId)) {
      return;
    }

    this.errorMessage = '';
    this.removingPropertyIds.add(propertyId);

    this.wishlistService
      .removeFromDefaultWishList(propertyId)
      .pipe(
        finalize(() => {
          this.removingPropertyIds.delete(propertyId);
          this.changeDetectorRef.detectChanges();
        }),
      )
      .subscribe({
        next: () => {
          this.wishlist = this.wishlist.filter(
            (item) => item.propertyId !== propertyId,
          );
        },
        error: (error: unknown) => {
          this.errorMessage = this.getErrorMessage(
            error,
            'Failed to remove the item. Please try again.',
          );
        },
      });
  }

  isRemoving(propertyId: string): boolean {
    return this.removingPropertyIds.has(propertyId);
  }

  trackWishlistItem(_index: number, item: WishListItemResponse): string {
    return item.propertyId;
  }

  private getErrorMessage(error: unknown, fallbackMessage: string): string {
    if (!(error instanceof HttpErrorResponse)) {
      return fallbackMessage;
    }

    const errorBody = this.parseErrorBody(error.error);

    return (
      errorBody?.['detail'] ||
      errorBody?.['title'] ||
      errorBody?.['message'] ||
      fallbackMessage
    );
  }

  private parseErrorBody(errorBody: unknown): Record<string, string> | null {
    if (errorBody && typeof errorBody === 'object') {
      return errorBody as Record<string, string>;
    }

    if (typeof errorBody !== 'string' || !errorBody.trim()) {
      return null;
    }

    try {
      return JSON.parse(errorBody) as Record<string, string>;
    } catch {
      return null;
    }
  }
}