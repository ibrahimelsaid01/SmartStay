import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs/operators';

import {
  AdminReviewDetails,
  AdminReviewListItem,
  AdminReviewModerationResponse,
  AdminReviewsQuery,
  AdminReviewsResponse,
  AdminReviewsService,
} from '../../services/admin-reviews';

@Component({
  selector: 'app-reviews-moderation',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './reviews-moderation.html',
  styleUrl: './reviews-moderation.css',
})
export class ReviewsModeration implements OnInit {
  reviews: AdminReviewListItem[] = [];
  selectedReview: AdminReviewDetails | null = null;

  loading = false;
  detailsLoading = false;
  actionLoading = false;

  errorMessage = '';
  successMessage = '';

  status = '1';
  page = 1;
  pageSize = 10;
  totalPages = 1;
  totalCount = 0;

  rejectionReason = '';

  statusOptions = [
    { value: '', label: 'All statuses' },
    { value: '1', label: 'Pending' },
    { value: '2', label: 'Posted' },
    { value: '3', label: 'Rejected' },
  ];

  constructor(
    private adminReviewsService: AdminReviewsService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadReviews();
  }

  loadReviews(page = this.page): void {
    this.page = page;
    this.loading = true;
    this.errorMessage = '';
    this.successMessage = '';
    this.cdr.detectChanges();

    const query: AdminReviewsQuery = {
      status: this.status ? Number(this.status) : null,
      page: this.page,
      pageSize: this.pageSize,
    };

    this.adminReviewsService
      .getReviews(query)
      .pipe(
        finalize(() => {
          this.loading = false;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: (response: AdminReviewsResponse) => {
          this.reviews = response.items ?? [];
          this.page = response.page || this.page;
          this.pageSize = response.pageSize || this.pageSize;
          this.totalCount = response.totalCount ?? 0;
          this.totalPages = response.totalPages ?? 1;

          this.cdr.detectChanges();
        },
        error: (error: any) => {
          this.reviews = [];
          this.totalCount = 0;
          this.totalPages = 1;

          this.errorMessage =
            error?.error?.message ||
            error?.message ||
            'Failed to load reviews.';

          this.cdr.detectChanges();
        },
      });
  }

  applyFilters(): void {
    this.loadReviews(1);
  }

  resetFilters(): void {
    this.status = '1';
    this.loadReviews(1);
  }

  refresh(): void {
    this.loadReviews(this.page);
  }

  viewDetails(reviewId: string): void {
    this.detailsLoading = true;
    this.errorMessage = '';
    this.successMessage = '';
    this.selectedReview = null;
    this.rejectionReason = '';
    this.cdr.detectChanges();

    this.adminReviewsService
      .getReviewDetails(reviewId)
      .pipe(
        finalize(() => {
          this.detailsLoading = false;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: (review: AdminReviewDetails) => {
          this.selectedReview = review;
          this.rejectionReason = review.rejectionReason || '';
          this.cdr.detectChanges();
        },
        error: (error: any) => {
          this.errorMessage =
            error?.error?.message ||
            error?.message ||
            'Failed to load review details.';

          this.cdr.detectChanges();
        },
      });
  }

  closeDetails(): void {
    this.selectedReview = null;
    this.rejectionReason = '';
    this.cdr.detectChanges();
  }

  approveReview(reviewId: string): void {
    this.actionLoading = true;
    this.errorMessage = '';
    this.successMessage = '';
    this.cdr.detectChanges();

    this.adminReviewsService
      .approveReview(reviewId)
      .pipe(
        finalize(() => {
          this.actionLoading = false;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: (response: AdminReviewModerationResponse) => {
          this.successMessage = response.message || 'Review approved successfully.';

          this.updateReviewStatus(response);

          if (this.selectedReview?.id === response.id) {
            this.selectedReview = {
              ...this.selectedReview,
              status: response.status,
              moderatedAt: response.moderatedAt,
              publishedAt: response.publishedAt,
              rejectedAt: response.rejectedAt,
              rejectionReason: response.rejectionReason,
            };
          }

          this.cdr.detectChanges();
        },
        error: (error: any) => {
          this.errorMessage =
            error?.error?.message ||
            error?.message ||
            'Failed to approve review.';

          this.cdr.detectChanges();
        },
      });
  }

  rejectReview(reviewId: string): void {
    if (this.rejectionReason.trim().length < 3) {
      this.errorMessage = 'Rejection reason must be at least 3 characters.';
      this.cdr.detectChanges();
      return;
    }

    this.actionLoading = true;
    this.errorMessage = '';
    this.successMessage = '';
    this.cdr.detectChanges();

    this.adminReviewsService
      .rejectReview(reviewId, this.rejectionReason.trim())
      .pipe(
        finalize(() => {
          this.actionLoading = false;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: (response: AdminReviewModerationResponse) => {
          this.successMessage = response.message || 'Review rejected successfully.';

          this.updateReviewStatus(response);

          if (this.selectedReview?.id === response.id) {
            this.selectedReview = {
              ...this.selectedReview,
              status: response.status,
              moderatedAt: response.moderatedAt,
              publishedAt: response.publishedAt,
              rejectedAt: response.rejectedAt,
              rejectionReason: response.rejectionReason,
            };
          }

          this.cdr.detectChanges();
        },
        error: (error: any) => {
          this.errorMessage =
            error?.error?.message ||
            error?.message ||
            'Failed to reject review.';

          this.cdr.detectChanges();
        },
      });
  }

  approveSelectedReview(): void {
    if (!this.selectedReview) {
      return;
    }

    this.approveReview(this.selectedReview.id);
  }

  rejectSelectedReview(): void {
    if (!this.selectedReview) {
      return;
    }

    this.rejectReview(this.selectedReview.id);
  }

  goToPreviousPage(): void {
    if (this.page <= 1) {
      return;
    }

    this.loadReviews(this.page - 1);
  }

  goToNextPage(): void {
    if (this.page >= this.totalPages) {
      return;
    }

    this.loadReviews(this.page + 1);
  }

  getStatusClass(status: string): string {
    const normalizedStatus = status.toLowerCase();

    if (normalizedStatus.includes('pending')) {
      return 'status-pending';
    }

    if (normalizedStatus.includes('posted')) {
      return 'status-posted';
    }

    if (normalizedStatus.includes('rejected')) {
      return 'status-rejected';
    }

    return 'status-default';
  }

  isPending(status: string | null | undefined): boolean {
    if (!status) {
      return false;
    }

    return status.toLowerCase().includes('pending');
  }

  private updateReviewStatus(response: AdminReviewModerationResponse): void {
    this.reviews = this.reviews.map(review =>
      review.id === response.id
        ? {
            ...review,
            status: response.status,
            publishedAt: response.publishedAt,
            rejectedAt: response.rejectedAt,
          }
        : review
    );
  }
}