import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { catchError, finalize, of, switchMap, tap, timeout } from 'rxjs';
import {
  AiRecommendationService,
  NearbyRecommendation,
} from '../../../../Shared/services/ai-recommendation.service';
import {
  GuestBookingConfirmationProperty,
  GuestBookingsService,
} from '../../../profile/services/guest-bookings';

@Component({
  selector: 'app-ai-recommendation',
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './ai-recommendation.html',
  styleUrls: ['./ai-recommendation.css'],
})
export class AiRecommendation implements OnInit {
  bookingId = '';
  property: GuestBookingConfirmationProperty | null = null;
  recommendations: NearbyRecommendation[] = [];
  isLoading = false;
  errorMessage = '';
  selectedCategory = 'coffee';
  recommendationRequested = false;

  constructor(
    private route: ActivatedRoute,
    private guestBookingsService: GuestBookingsService,
    private aiRecommendationService: AiRecommendationService,
  ) {}

  ngOnInit(): void {
    this.bookingId = this.route.snapshot.paramMap.get('bookingId') ?? '';
    this.selectedCategory = this.normalizeCategory(
      this.route.snapshot.queryParamMap.get('category'),
    );

    if (!this.bookingId) {
      this.errorMessage = 'Booking data is not available.';
      return;
    }
  }

  requestRecommendations(): void {
    this.recommendationRequested = true;
    this.loadPropertyAndRecommendations();
  }

  changeCategory(category: string): void {
    this.selectedCategory = this.normalizeCategory(category);
  }

  private normalizeCategory(category: string | null): string {
    const normalized = (category ?? '').trim().toLowerCase();
    return ['coffee', 'restaurant', 'pharmacy'].includes(normalized) ? normalized : 'coffee';
  }

  private loadPropertyAndRecommendations(): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.recommendations = [];

    this.guestBookingsService
      .getBookingConfirmation(this.bookingId)
      .pipe(
        timeout(10000),
        tap((confirmation) => {
          this.property = confirmation.property;
        }),
        switchMap((confirmation) => {
          const latitude = Number(confirmation.property.latitude);
          const longitude = Number(confirmation.property.longitude);

          if (
            !Number.isFinite(latitude) ||
            !Number.isFinite(longitude) ||
            latitude < -90 ||
            latitude > 90 ||
            longitude < -180 ||
            longitude > 180
          ) {
            this.errorMessage =
              'This property does not have valid coordinates for recommendations.';
            return of([] as NearbyRecommendation[]);
          }

          return this.aiRecommendationService
            .getRecommendations({
              latitude,
              longitude,
              category: this.selectedCategory,
            })
            .pipe(timeout(10000));
        }),
        catchError((error) => {
          if (error?.name === 'TimeoutError') {
            this.errorMessage =
              'Recommendation service is taking too long to respond. Please try again in a moment.';
          } else {
            this.errorMessage =
              error?.error?.message ||
              error?.message ||
              'Unable to load AI recommendations at this time.';
          }
          return of([] as NearbyRecommendation[]);
        }),
        finalize(() => {
          this.isLoading = false;
        }),
      )
      .subscribe((recommendations) => {
        this.recommendations = recommendations.slice(0, 5);
      });
  }

  retryRecommendations(): void {
    this.loadPropertyAndRecommendations();
  }
}
