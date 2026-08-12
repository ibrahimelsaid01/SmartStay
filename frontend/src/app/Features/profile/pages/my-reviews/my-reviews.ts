import { CommonModule } from '@angular/common';
import {
  ChangeDetectorRef,
  Component,
  OnInit,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  ActivatedRoute,
  Router,
  RouterModule,
} from '@angular/router';
import { finalize } from 'rxjs';

import {
  MyReviewsResponse,
  ReviewStatus,
  ReviewsService,
  UserReviewResponse,
} from '../../services/reviews';

type ReviewFilter =
  | 'All'
  | ReviewStatus;

type ReviewFormMode =
  | 'create'
  | 'edit';

@Component({
  selector: 'app-my-reviews',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterModule,
  ],
  templateUrl: './my-reviews.html',
  styleUrl: './my-reviews.css',
})
export class MyReviews implements OnInit {
  readonly statusFilters: ReviewFilter[] = [
    'All',
    'Pending',
    'Posted',
    'Rejected',
  ];

  readonly ratingOptions = [
    1,
    2,
    3,
    4,
    5,
  ];

  reviews: UserReviewResponse[] = [];

  selectedStatus: ReviewFilter =
    'All';

  page = 1;
  pageSize = 10;
  totalCount = 0;
  totalPages = 0;

  loading = false;
  focusedReviewLoading = false;

  errorMessage = '';
  successMessage = '';

  focusedReviewId: string | null =
    null;

  reviewFormMode: ReviewFormMode | null =
    null;

  reviewFormReviewId: string | null =
    null;

  reviewFormBookingId: string | null =
    null;

  reviewFormPropertyTitle = '';

  reviewFormRating = 5;

  reviewFormPositiveComment = '';

  reviewFormNegativeComment = '';

  reviewFormLoading = false;

  reviewFormErrorMessage = '';

  deleteConfirmationReviewId: string | null =
    null;

  deleteLoadingReviewId: string | null =
    null;

  private loadRequestId = 0;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly reviewsService: ReviewsService,
    private readonly cdr: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    const reviewId =
      this.route.snapshot.queryParamMap
        .get('reviewId')
        ?.trim();

    const requestedReviewStatus =
      this.normalizeReviewStatusQuery(
        this.route.snapshot.queryParamMap
          .get('status'),
      );

    /*
     * View Review has priority over Write Review.
     *
     * The selected review is loaded directly from
     * GET /api/reviews/{reviewId}, so it does not
     * need to be present on the first list page.
     */
    if (reviewId) {
      if (this.isGuid(reviewId)) {
        this.loadFocusedReview(
          reviewId,
          requestedReviewStatus,
        );

        return;
      }

      this.errorMessage =
        'The review ID is invalid.';
    }

    const bookingId =
      this.route.snapshot.queryParamMap
        .get('bookingId')
        ?.trim();

    const propertyTitle =
      this.route.snapshot.queryParamMap
        .get('propertyTitle')
        ?.trim();

    if (bookingId) {
      if (this.isGuid(bookingId)) {
        this.openCreateReviewForm(
          bookingId,
          propertyTitle ?? '',
        );
      } else {
        this.errorMessage =
          'The booking ID is invalid.';
      }
    }

    this.loadReviews(
      1,
      !!this.errorMessage,
    );
  }

  loadReviews(
    page = this.page,
    preserveSuccessMessage = false,
  ): void {
    const requestId =
      ++this.loadRequestId;

    /*
     * Loading the normal list exits the focused
     * review mode.
     */
    this.focusedReviewId =
      null;

    this.page = page;
    this.loading = true;
    this.errorMessage = '';

    if (!preserveSuccessMessage) {
      this.successMessage = '';
    }

    this.cdr.detectChanges();

    this.reviewsService
      .getMyReviews(
        this.selectedStatus === 'All'
          ? undefined
          : this.selectedStatus,
        this.page,
        this.pageSize,
      )
      .pipe(
        finalize(() => {
          if (
            requestId ===
            this.loadRequestId
          ) {
            this.loading = false;
            this.cdr.detectChanges();
          }
        }),
      )
      .subscribe({
        next: (
          response: MyReviewsResponse,
        ) => {
          if (
            requestId !==
            this.loadRequestId
          ) {
            return;
          }

          this.reviews =
            response.items ?? [];

          this.page =
            response.page ||
            this.page;

          this.totalCount =
            response.totalCount ?? 0;

          this.totalPages =
            response.totalPages ?? 0;

          /*
           * If the current page became empty after
           * deleting its last review, go back one page.
           */
          if (
            this.reviews.length === 0 &&
            this.totalCount > 0 &&
            this.page > 1
          ) {
            this.loadReviews(
              this.page - 1,
              preserveSuccessMessage,
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
            this.loadRequestId
          ) {
            return;
          }

          this.reviews = [];
          this.totalCount = 0;
          this.totalPages = 0;

          this.errorMessage =
            this.extractErrorMessage(
              error,
            ) ||
            'Failed to load your reviews.';

          this.cdr.detectChanges();
        },
      });
  }

  loadFocusedReview(
    reviewId: string,
    requestedStatus?: ReviewStatus,
  ): void {
    if (
      this.focusedReviewLoading ||
      this.loading
    ) {
      return;
    }

    this.focusedReviewId =
      reviewId;

    this.focusedReviewLoading =
      true;

    this.loading = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.reviews = [];
    this.totalCount = 0;
    this.totalPages = 0;

    /*
     * This is only an initial UI hint.
     * The real status returned by the API replaces it.
     */
    if (requestedStatus) {
      this.selectedStatus =
        requestedStatus;
    }

    this.cdr.detectChanges();

    this.reviewsService
      .getReviewById(
        reviewId,
      )
      .pipe(
        finalize(() => {
          this.focusedReviewLoading =
            false;

          this.loading =
            false;

          this.cdr.detectChanges();
        }),
      )
      .subscribe({
        next: (
          review: UserReviewResponse,
        ) => {
          /*
           * The Backend response is the source of truth
           * for the review status.
           */
          this.focusedReviewId =
            review.id;

          this.selectedStatus =
            review.status;

          /*
           * The normal card UI is reused to display only
           * the requested review.
           */
          this.reviews = [
            review,
          ];

          this.page = 1;
          this.totalCount = 1;
          this.totalPages = 1;

          this.cdr.detectChanges();

          this.scrollToFocusedReview();
        },
        error: (
          error: unknown,
        ) => {
          this.focusedReviewId =
            null;

          this.reviews = [];

          this.errorMessage =
            this.extractErrorMessage(
              error,
            ) ||
            'Failed to load the selected review.';

          this.cdr.detectChanges();
        },
      });
  }

  showAllReviews(): void {
    if (
      this.loading ||
      this.reviewFormLoading ||
      !!this.deleteLoadingReviewId
    ) {
      return;
    }

    this.focusedReviewId =
      null;

    this.clearReviewQueryParams();

    this.loadReviews(1);
  }

  selectStatus(
    status: ReviewFilter,
  ): void {
    /*
     * Pressing the active filter while viewing a
     * focused review should still return to the list.
     */
    if (
      (
        status === this.selectedStatus &&
        !this.focusedReviewId
      ) ||
      this.loading ||
      this.reviewFormLoading ||
      !!this.deleteLoadingReviewId
    ) {
      return;
    }

    this.selectedStatus =
      status;

    this.focusedReviewId =
      null;

    this.closeDeleteConfirmation();

    this.clearReviewQueryParams();

    this.loadReviews(1);
  }

  refreshReviews(): void {
    if (
      this.loading ||
      this.reviewFormLoading ||
      !!this.deleteLoadingReviewId
    ) {
      return;
    }

    /*
     * While viewing a selected review, refresh only
     * that review rather than loading the whole list.
     */
    if (this.focusedReviewId) {
      this.loadFocusedReview(
        this.focusedReviewId,
        this.selectedStatus === 'All'
          ? undefined
          : this.selectedStatus,
      );

      return;
    }

    this.loadReviews(
      this.page,
    );
  }

  openCreateReviewForm(
    bookingId: string,
    propertyTitle = '',
  ): void {
    if (!this.isGuid(bookingId)) {
      this.errorMessage =
        'The booking ID is invalid.';

      this.cdr.detectChanges();
      return;
    }

    this.focusedReviewId =
      null;

    this.reviewFormMode =
      'create';

    this.reviewFormReviewId =
      null;

    this.reviewFormBookingId =
      bookingId;

    this.reviewFormPropertyTitle =
      propertyTitle.trim();

    this.reviewFormRating =
      5;

    this.reviewFormPositiveComment =
      '';

    this.reviewFormNegativeComment =
      '';

    this.reviewFormErrorMessage =
      '';

    this.errorMessage = '';
    this.successMessage = '';

    this.closeDeleteConfirmation();

    this.cdr.detectChanges();
  }

  openEditReviewForm(
    review: UserReviewResponse,
  ): void {
    if (!review.canEdit) {
      this.errorMessage =
        'This review can no longer be edited.';

      this.cdr.detectChanges();
      return;
    }

    this.reviewFormMode =
      'edit';

    this.reviewFormReviewId =
      review.id;

    this.reviewFormBookingId =
      review.bookingId;

    this.reviewFormPropertyTitle =
      review.property.title;

    this.reviewFormRating =
      review.rating;

    this.reviewFormPositiveComment =
      review.positiveComment ?? '';

    this.reviewFormNegativeComment =
      review.negativeComment ?? '';

    this.reviewFormErrorMessage =
      '';

    this.errorMessage = '';
    this.successMessage = '';

    this.closeDeleteConfirmation();

    this.cdr.detectChanges();
  }

  closeReviewForm(): void {
    if (this.reviewFormLoading) {
      return;
    }

    this.resetReviewForm();

    this.clearReviewQueryParams();

    this.cdr.detectChanges();
  }

  setReviewRating(
    rating: number,
  ): void {
    if (
      this.reviewFormLoading ||
      !Number.isInteger(rating) ||
      rating < 1 ||
      rating > 5
    ) {
      return;
    }

    this.reviewFormRating =
      rating;

    this.reviewFormErrorMessage =
      '';

    this.cdr.detectChanges();
  }

  submitReview(): void {
    if (
      !this.reviewFormMode ||
      this.reviewFormLoading
    ) {
      return;
    }

    const rating =
      Number(
        this.reviewFormRating,
      );

    const positiveComment =
      this.reviewFormPositiveComment
        .trim();

    const negativeComment =
      this.reviewFormNegativeComment
        .trim();

    if (
      !Number.isInteger(rating) ||
      rating < 1 ||
      rating > 5
    ) {
      this.reviewFormErrorMessage =
        'Rating must be a whole number between 1 and 5.';

      this.cdr.detectChanges();
      return;
    }

    if (
      !positiveComment &&
      !negativeComment
    ) {
      this.reviewFormErrorMessage =
        'Add at least one positive or negative comment.';

      this.cdr.detectChanges();
      return;
    }

    if (
      positiveComment.length >
      2000
    ) {
      this.reviewFormErrorMessage =
        'Positive comment cannot exceed 2000 characters.';

      this.cdr.detectChanges();
      return;
    }

    if (
      negativeComment.length >
      2000
    ) {
      this.reviewFormErrorMessage =
        'Negative comment cannot exceed 2000 characters.';

      this.cdr.detectChanges();
      return;
    }

    const request = {
      rating,

      positiveComment:
        positiveComment ||
        null,

      negativeComment:
        negativeComment ||
        null,
    };

    const request$ =
      this.reviewFormMode === 'create'
        ? this.reviewFormBookingId
          ? this.reviewsService
              .createReview(
                this.reviewFormBookingId,
                request,
              )
          : null
        : this.reviewFormReviewId
          ? this.reviewsService
              .updateReview(
                this.reviewFormReviewId,
                request,
              )
          : null;

    if (!request$) {
      this.reviewFormErrorMessage =
        this.reviewFormMode === 'create'
          ? 'The booking ID is missing.'
          : 'The review ID is missing.';

      this.cdr.detectChanges();
      return;
    }

    const successMessage =
      this.reviewFormMode === 'create'
        ? 'Your review was submitted and is pending moderation.'
        : 'Your review was updated and submitted for moderation.';

    this.reviewFormLoading =
      true;

    this.reviewFormErrorMessage =
      '';

    this.errorMessage = '';
    this.successMessage = '';

    this.cdr.detectChanges();

    request$
      .pipe(
        finalize(() => {
          this.reviewFormLoading =
            false;

          this.cdr.detectChanges();
        }),
      )
      .subscribe({
        next: () => {
          this.resetReviewForm();

          this.focusedReviewId =
            null;

          /*
           * New and updated reviews both enter
           * the Pending moderation state.
           */
          this.selectedStatus =
            'Pending';

          this.clearReviewQueryParams();

          this.successMessage =
            successMessage;

          this.loadReviews(
            1,
            true,
          );
        },
        error: (
          error: unknown,
        ) => {
          this.reviewFormErrorMessage =
            this.extractErrorMessage(
              error,
            ) ||
            'Failed to save your review.';

          this.cdr.detectChanges();
        },
      });
  }

  openDeleteConfirmation(
    review: UserReviewResponse,
  ): void {
    if (
      !review.canDelete ||
      this.deleteLoadingReviewId
    ) {
      return;
    }

    this.deleteConfirmationReviewId =
      review.id;

    this.errorMessage = '';
    this.successMessage = '';

    this.cdr.detectChanges();
  }

  closeDeleteConfirmation(): void {
    if (this.deleteLoadingReviewId) {
      return;
    }

    this.deleteConfirmationReviewId =
      null;

    this.cdr.detectChanges();
  }

  deleteReview(
    review: UserReviewResponse,
  ): void {
    if (
      !review.canDelete ||
      this.deleteLoadingReviewId ||
      this.reviewFormLoading
    ) {
      return;
    }

    const wasFocusedReview =
      this.focusedReviewId ===
      review.id;

    this.deleteLoadingReviewId =
      review.id;

    this.errorMessage = '';
    this.successMessage = '';

    this.cdr.detectChanges();

    this.reviewsService
      .deleteReview(
        review.id,
      )
      .pipe(
        finalize(() => {
          this.deleteLoadingReviewId =
            null;

          this.cdr.detectChanges();
        }),
      )
      .subscribe({
        next: () => {
          this.deleteConfirmationReviewId =
            null;

          if (
            this.reviewFormReviewId ===
            review.id
          ) {
            this.resetReviewForm();
          }

          this.focusedReviewId =
            null;

          this.clearReviewQueryParams();

          this.successMessage =
            'Review deleted successfully.';

          const targetPage =
            !wasFocusedReview &&
            this.reviews.length === 1 &&
            this.page > 1
              ? this.page - 1
              : 1;

          this.loadReviews(
            targetPage,
            true,
          );
        },
        error: (
          error: unknown,
        ) => {
          this.errorMessage =
            this.extractErrorMessage(
              error,
            ) ||
            'Failed to delete the review.';

          this.cdr.detectChanges();
        },
      });
  }

  goToPreviousPage(): void {
    if (
      this.focusedReviewId ||
      this.page <= 1 ||
      this.loading ||
      this.reviewFormLoading ||
      !!this.deleteLoadingReviewId
    ) {
      return;
    }

    this.loadReviews(
      this.page - 1,
    );
  }

  goToNextPage(): void {
    if (
      this.focusedReviewId ||
      this.page >=
        this.totalPages ||
      this.loading ||
      this.reviewFormLoading ||
      !!this.deleteLoadingReviewId
    ) {
      return;
    }

    this.loadReviews(
      this.page + 1,
    );
  }

  isReviewFormOpen(): boolean {
    return (
      this.reviewFormMode !==
      null
    );
  }

  isFocusedReviewView(): boolean {
    return !!this.focusedReviewId;
  }

  isFocusedReview(
    reviewId: string,
  ): boolean {
    return (
      this.focusedReviewId ===
      reviewId
    );
  }

  isEditingReview(
    reviewId: string,
  ): boolean {
    return (
      this.reviewFormMode ===
        'edit' &&
      this.reviewFormReviewId ===
        reviewId
    );
  }

  isDeleteConfirmationOpen(
    reviewId: string,
  ): boolean {
    return (
      this.deleteConfirmationReviewId ===
      reviewId
    );
  }

  isDeletingReview(
    reviewId: string,
  ): boolean {
    return (
      this.deleteLoadingReviewId ===
      reviewId
    );
  }

  getStatusClass(
    status: ReviewStatus,
  ): string {
    switch (status) {
      case 'Pending':
        return 'review-status-pending';

      case 'Posted':
        return 'review-status-posted';

      case 'Rejected':
        return 'review-status-rejected';

      default:
        return 'review-status-default';
    }
  }

  getStatusDescription(
    status: ReviewStatus,
  ): string {
    switch (status) {
      case 'Pending':
        return 'Waiting for admin moderation.';

      case 'Posted':
        return 'Published on the property page.';

      case 'Rejected':
        return 'Update the review and resubmit it for moderation.';

      default:
        return 'Review status is unavailable.';
    }
  }

  getReviewDisplayDate(
    review: UserReviewResponse,
  ): string {
    return (
      review.publishedAt ??
      review.updatedAt ??
      review.createdAt
    );
  }

  getReviewLocation(
    review: UserReviewResponse,
  ): string {
    return [
      review.property.city,
      review.property.country,
    ]
      .filter(
        (value) =>
          !!value?.trim(),
      )
      .join(', ');
  }

  trackReviewById(
    _index: number,
    review: UserReviewResponse,
  ): string {
    return review.id;
  }

  private resetReviewForm(): void {
    this.reviewFormMode =
      null;

    this.reviewFormReviewId =
      null;

    this.reviewFormBookingId =
      null;

    this.reviewFormPropertyTitle =
      '';

    this.reviewFormRating =
      5;

    this.reviewFormPositiveComment =
      '';

    this.reviewFormNegativeComment =
      '';

    this.reviewFormErrorMessage =
      '';
  }

  private clearReviewQueryParams(): void {
    void this.router.navigate(
      [],
      {
        relativeTo:
          this.route,

        queryParams: {
          bookingId:
            null,

          propertyTitle:
            null,

          reviewId:
            null,

          status:
            null,
        },

        queryParamsHandling:
          'merge',

        replaceUrl:
          true,
      },
    );
  }

  private scrollToFocusedReview(): void {
    setTimeout(() => {
      const focusedElement =
        document.getElementById(
          'focused-review-card',
        );

      focusedElement?.scrollIntoView({
        behavior: 'smooth',
        block: 'center',
      });
    });
  }

  private normalizeReviewStatusQuery(
    value: string | null,
  ): ReviewStatus | undefined {
    switch (
      (value ?? '')
        .trim()
        .toLowerCase()
    ) {
      case 'pending':
      case '1':
        return 'Pending';

      case 'posted':
      case '2':
        return 'Posted';

      case 'rejected':
      case '3':
        return 'Rejected';

      default:
        return undefined;
    }
  }

  private isGuid(
    value: string,
  ): boolean {
    return /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i
      .test(
        value,
      );
  }

  private extractErrorMessage(
    error: unknown,
  ): string {
    const typedError =
      error as {
        error?: unknown;
        message?: string;
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

      if (!normalizedError) {
        return (
          typedError.message ||
          ''
        );
      }

      try {
        const parsedError =
          JSON.parse(
            normalizedError,
          ) as {
            detail?: string;
            message?: string;
            title?: string;

            errors?: Record<
              string,
              string[]
            >;
          };

        const firstValidationError =
          parsedError.errors
            ? Object.values(
                parsedError.errors,
              )[0]?.[0]
            : undefined;

        return (
          parsedError.detail ||
          parsedError.message ||
          firstValidationError ||
          parsedError.title ||
          typedError.message ||
          ''
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
      const parsedError =
        typedError.error as {
          detail?: string;
          message?: string;
          title?: string;

          errors?: Record<
            string,
            string[]
          >;
        };

      const firstValidationError =
        parsedError.errors
          ? Object.values(
              parsedError.errors,
            )[0]?.[0]
          : undefined;

      return (
        parsedError.detail ||
        parsedError.message ||
        firstValidationError ||
        parsedError.title ||
        typedError.message ||
        ''
      );
    }

    return (
      typedError.message ||
      ''
    );
  }
}