import { Component, OnDestroy, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Params, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { Subscription, TimeoutError, finalize } from 'rxjs';

import { Properties, PropertyItem } from '../../services/properties';
import { StayCard } from '../../components/stay-card/stay-card';

@Component({
  selector: 'app-all-stays',
  standalone: true,
  imports: [CommonModule, StayCard, FormsModule],
  templateUrl: './all-stays.html',
  styleUrl: './all-stays.css',
})
export class AllStays implements OnInit, OnDestroy {
  originalStaysList: PropertyItem[] = [];
  filteredStaysList: PropertyItem[] = [];

  selectedLocation = '';
  selectedCheckInDate = '';
  selectedCheckOutDate = '';
  selectedGuests = 0;

  currentPage = 1;
  pageSize = 12;

  isLoading = false;
  errorMessage = '';
  dateError = '';

  readonly today = new Date().toISOString().split('T')[0];

  private queryParamsSubscription?: Subscription;

  constructor(
    private readonly stayService: Properties,
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly cdr: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    this.queryParamsSubscription = this.route.queryParams.subscribe((params: Params) => {
      this.applyQueryParams(params);
      this.applyFilters();
      this.cdr.detectChanges();
    });

    this.loadStays();
  }

  ngOnDestroy(): void {
    this.queryParamsSubscription?.unsubscribe();
  }

  loadStays(): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.cdr.detectChanges();

    this.stayService
      .getPopularStays()
      .pipe(
        finalize(() => {
          this.isLoading = false;
          this.cdr.detectChanges();
        }),
      )
      .subscribe({
        next: (data) => {
          this.originalStaysList = Array.isArray(data) ? data : [];

          console.log('All stays loaded:', this.originalStaysList);

          this.applyFilters();

          this.isLoading = false;
          this.cdr.detectChanges();
        },
        error: (error) => {
          console.error('Error loading stays:', error);

          this.originalStaysList = [];
          this.filteredStaysList = [];
          this.isLoading = false;

          if (error instanceof TimeoutError || error?.name === 'TimeoutError') {
            this.errorMessage = 'Loading stays took too long. Please refresh and try again.';
          } else {
            this.errorMessage = 'Could not load stays right now. Please check the API connection.';
          }

          this.cdr.detectChanges();
        },
      });
  }

  applyFilters(): void {
    this.dateError = '';

    if (this.selectedCheckInDate && this.selectedCheckInDate < this.today) {
      this.dateError = 'Check-in date cannot be in the past.';
    }

    if (
      this.selectedCheckInDate &&
      this.selectedCheckOutDate &&
      this.selectedCheckOutDate < this.selectedCheckInDate
    ) {
      this.dateError = 'Check-out date cannot be before check-in date.';
    }

    if (this.dateError) {
      this.filteredStaysList = [];
      this.currentPage = 1;
      this.cdr.detectChanges();
      return;
    }

    const locationValue = this.selectedLocation.trim().toLowerCase();
    const guestsValue = Number(this.selectedGuests);

    this.filteredStaysList = this.originalStaysList.filter((stay) => {
      const stayCity = String(stay.city ?? '').toLowerCase();
      const stayCountry = String(stay.country ?? '').toLowerCase();

      const matchLocation = locationValue
        ? stayCity.includes(locationValue) || stayCountry.includes(locationValue)
        : true;

      const matchGuests = guestsValue > 0 ? Number(stay.maxGuests ?? 0) >= guestsValue : true;

      return matchLocation && matchGuests;
    });

    this.currentPage = 1;
    this.cdr.detectChanges();
  }

  resetFilters(): void {
    this.selectedLocation = '';
    this.selectedCheckInDate = '';
    this.selectedCheckOutDate = '';
    this.selectedGuests = 0;

    this.applyFilters();

    this.router.navigate(['/all-stays']);
    this.cdr.detectChanges();
  }

  get paginatedStays(): PropertyItem[] {
    const startIndex = (this.currentPage - 1) * this.pageSize;

    return this.filteredStaysList.slice(startIndex, startIndex + this.pageSize);
  }

  get totalPages(): number {
    return Math.ceil(this.filteredStaysList.length / this.pageSize);
  }

  get hasActiveFilters(): boolean {
    return Boolean(
      this.selectedLocation ||
      this.selectedCheckInDate ||
      this.selectedCheckOutDate ||
      this.selectedGuests,
    );
  }

  changePage(page: number): void {
    if (page >= 1 && page <= this.totalPages) {
      this.currentPage = page;
      this.cdr.detectChanges();
    }
  }

  private applyQueryParams(params: Params): void {
    this.selectedLocation = String(params['location'] ?? '');
    this.selectedCheckInDate = String(params['checkIn'] ?? '');
    this.selectedCheckOutDate = String(params['checkOut'] ?? '');

    const guestsFromQuery = Number(params['guests'] ?? 0);

    this.selectedGuests = Number.isFinite(guestsFromQuery) ? guestsFromQuery : 0;
  }
}
