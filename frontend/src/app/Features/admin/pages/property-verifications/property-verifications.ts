import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs/operators';

import {
  AdminPendingPropertyItem,
  AdminPropertiesService,
  AdminPropertyDetails,
  AdminPropertyVerificationPage,
} from '../../services/admin-properties';

@Component({
  selector: 'app-property-verifications',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './property-verifications.html',
  styleUrl: './property-verifications.css',
})
export class PropertyVerifications implements OnInit, OnDestroy {
  properties: AdminPendingPropertyItem[] = [];
  selectedProperty: AdminPropertyDetails | null = null;

  loading = false;
  detailsLoading = false;
  actionLoading = false;
  documentLoading = false;
  hostIdentityLoading = false;

  errorMessage = '';
  successMessage = '';
  hostIdentityError = '';

  rejectionReason = '';

  page = 1;
  pageSize = 20;
  totalPages = 1;
  totalCount = 0;

  documentPageUrls: Record<string, string> = {};
  hostIdentityFrontUrl: string | null = null;

  constructor(
    private adminPropertiesService: AdminPropertiesService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadPendingProperties();
  }

  ngOnDestroy(): void {
    this.clearDocumentPageUrls();
    this.clearHostIdentityUrl();
  }

  loadPendingProperties(page = this.page): void {
    this.loading = true;
    this.errorMessage = '';
    this.successMessage = '';
    this.cdr.detectChanges();

    this.adminPropertiesService
      .getPendingProperties({
        page,
        pageSize: this.pageSize,
      })
      .pipe(
        finalize(() => {
          this.loading = false;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: response => {
          this.properties = response.items ?? [];
          this.page = response.page ?? 1;
          this.pageSize = response.pageSize ?? 20;
          this.totalCount = response.totalCount ?? 0;
          this.totalPages = response.totalPages ?? 1;
          this.cdr.detectChanges();
        },
        error: error => {
          this.properties = [];
          this.errorMessage = this.getErrorMessage(
            error,
            'Failed to load pending properties.'
          );
          this.cdr.detectChanges();
        },
      });
  }

  refresh(): void {
    this.loadPendingProperties(this.page);
  }

  viewDetails(property: AdminPendingPropertyItem): void {
    this.detailsLoading = true;
    this.errorMessage = '';
    this.successMessage = '';
    this.hostIdentityError = '';
    this.rejectionReason = '';
    this.selectedProperty = null;
    this.clearDocumentPageUrls();
    this.clearHostIdentityUrl();
    this.cdr.detectChanges();

    this.adminPropertiesService
      .getPropertyDetails(property.id)
      .pipe(
        finalize(() => {
          this.detailsLoading = false;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: details => {
          this.selectedProperty = {
            ...details,
            amenities: details.amenities ?? [],
            images: details.images ?? [],
          };

          this.loadHostIdentityDocument(details);
          this.loadVerificationDocumentPages(details);
          this.cdr.detectChanges();
        },
        error: error => {
          this.errorMessage = this.getErrorMessage(
            error,
            'Failed to load property details.'
          );
          this.cdr.detectChanges();
        },
      });
  }

  closeDetails(): void {
    this.selectedProperty = null;
    this.rejectionReason = '';
    this.hostIdentityError = '';
    this.clearDocumentPageUrls();
    this.clearHostIdentityUrl();
    this.cdr.detectChanges();
  }

  approveSelectedProperty(): void {
    if (!this.selectedProperty) {
      return;
    }

    const confirmed = confirm(
      `Approve "${this.selectedProperty.title}" and publish it for users?`
    );

    if (!confirmed) {
      return;
    }

    this.actionLoading = true;
    this.errorMessage = '';
    this.successMessage = '';
    this.cdr.detectChanges();

    this.adminPropertiesService
      .approveProperty(this.selectedProperty.id)
      .pipe(
        finalize(() => {
          this.actionLoading = false;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: response => {
          this.successMessage =
            response.message || 'Property approved successfully.';

          this.removePropertyFromList(this.selectedProperty?.id);
          this.closeDetails();
          this.loadPendingProperties(this.page);
        },
        error: error => {
          this.errorMessage = this.getErrorMessage(
            error,
            'Failed to approve property.'
          );
          this.cdr.detectChanges();
        },
      });
  }

  rejectSelectedProperty(): void {
    if (!this.selectedProperty) {
      return;
    }

    const reason = this.rejectionReason.trim();

    if (reason.length < 10) {
      this.errorMessage = 'Rejection reason must be at least 10 characters.';
      this.cdr.detectChanges();
      return;
    }

    const confirmed = confirm(
      `Reject "${this.selectedProperty.title}" and send the reason to the host?`
    );

    if (!confirmed) {
      return;
    }

    this.actionLoading = true;
    this.errorMessage = '';
    this.successMessage = '';
    this.cdr.detectChanges();

    this.adminPropertiesService
      .rejectProperty(this.selectedProperty.id, reason)
      .pipe(
        finalize(() => {
          this.actionLoading = false;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: response => {
          this.successMessage =
            response.message || 'Property rejected successfully.';

          this.removePropertyFromList(this.selectedProperty?.id);
          this.closeDetails();
          this.loadPendingProperties(this.page);
        },
        error: error => {
          this.errorMessage = this.getErrorMessage(
            error,
            'Failed to reject property.'
          );
          this.cdr.detectChanges();
        },
      });
  }

  goToPreviousPage(): void {
    if (this.page <= 1 || this.loading) {
      return;
    }

    this.loadPendingProperties(this.page - 1);
  }

  goToNextPage(): void {
    if (this.page >= this.totalPages || this.loading) {
      return;
    }

    this.loadPendingProperties(this.page + 1);
  }

  getPropertyCoverImage(property: AdminPendingPropertyItem): string {
    return property.coverImageUrl || 'Images/property-placeholder.jpg';
  }

  getDetailsCoverImage(property: AdminPropertyDetails): string {
    const cover = property.images?.find(image => image.isCover);

    return cover?.url || property.images?.[0]?.url || 'Images/property-placeholder.jpg';
  }

  getFullAddress(property: AdminPropertyDetails): string {
    const parts = [
      property.streetAddress,
      property.buildingNumber ? `Building: ${property.buildingNumber}` : null,
      property.floor ? `Floor: ${property.floor}` : null,
      property.apartmentNumber ? `Apartment: ${property.apartmentNumber}` : null,
      property.city,
      property.country,
    ].filter(Boolean);

    return parts.join(', ');
  }

  getDocumentPageUrl(page: AdminPropertyVerificationPage): string | null {
    return this.documentPageUrls[page.id] ?? null;
  }

  openHostIdentityDocument(): void {
    if (!this.hostIdentityFrontUrl) {
      return;
    }

    window.open(this.hostIdentityFrontUrl, '_blank', 'noopener,noreferrer');
  }

  openDocumentPage(page: AdminPropertyVerificationPage): void {
    const url = this.getDocumentPageUrl(page);

    if (!url) {
      return;
    }

    window.open(url, '_blank', 'noopener,noreferrer');
  }

  private loadHostIdentityDocument(property: AdminPropertyDetails): void {
    this.clearHostIdentityUrl();
    this.hostIdentityError = '';

    const hostProfileId = property.host?.hostProfileId;

    if (!hostProfileId) {
      this.hostIdentityError = 'Host identity document is not available.';
      this.cdr.detectChanges();
      return;
    }

    this.hostIdentityLoading = true;
    this.cdr.detectChanges();

    this.adminPropertiesService
      .getHostIdentityDocumentFront(hostProfileId)
      .pipe(
        finalize(() => {
          this.hostIdentityLoading = false;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: blob => {
          this.hostIdentityFrontUrl = URL.createObjectURL(blob);
          this.cdr.detectChanges();
        },
        error: () => {
          this.hostIdentityError = 'Host identity document image is not available.';
          this.cdr.detectChanges();
        },
      });
  }

  private loadVerificationDocumentPages(property: AdminPropertyDetails): void {
    this.clearDocumentPageUrls();

    const pages = property.verificationDocument?.pages ?? [];

    if (pages.length === 0) {
      this.documentLoading = false;
      this.cdr.detectChanges();
      return;
    }

    this.documentLoading = true;
    this.cdr.detectChanges();

    let remaining = pages.length;

    pages.forEach(page => {
      this.adminPropertiesService
        .getVerificationDocumentPageContent(property.id, page.id)
        .subscribe({
          next: blob => {
            this.documentPageUrls[page.id] = URL.createObjectURL(blob);
            this.cdr.detectChanges();
          },
          error: () => {
            this.documentPageUrls[page.id] = '';
            this.cdr.detectChanges();
          },
          complete: () => {
            remaining -= 1;

            if (remaining === 0) {
              this.documentLoading = false;
              this.cdr.detectChanges();
            }
          },
        });
    });
  }

  private clearDocumentPageUrls(): void {
    Object.values(this.documentPageUrls).forEach(url => {
      if (url) {
        URL.revokeObjectURL(url);
      }
    });

    this.documentPageUrls = {};
    this.documentLoading = false;
  }

  private clearHostIdentityUrl(): void {
    if (this.hostIdentityFrontUrl) {
      URL.revokeObjectURL(this.hostIdentityFrontUrl);
    }

    this.hostIdentityFrontUrl = null;
    this.hostIdentityLoading = false;
  }

  private removePropertyFromList(propertyId?: string): void {
    if (!propertyId) {
      return;
    }

    this.properties = this.properties.filter(property => property.id !== propertyId);
    this.totalCount = Math.max(0, this.totalCount - 1);
    this.cdr.detectChanges();
  }

  private getErrorMessage(error: unknown, fallbackMessage: string): string {
    const possibleError = error as {
      error?: unknown;
      message?: string;
    };

    if (typeof possibleError.error === 'string') {
      try {
        const parsedError = JSON.parse(possibleError.error) as {
          message?: string;
          title?: string;
          errors?: Record<string, string[]>;
        };

        if (parsedError.message) {
          return parsedError.message;
        }

        if (parsedError.title) {
          return parsedError.title;
        }

        if (parsedError.errors) {
          const firstError = Object.values(parsedError.errors)[0]?.[0];

          if (firstError) {
            return firstError;
          }
        }
      } catch {
        return possibleError.error;
      }
    }

    if (
      possibleError.error &&
      typeof possibleError.error === 'object' &&
      'message' in possibleError.error
    ) {
      return String((possibleError.error as { message: string }).message);
    }

    return possibleError.message || fallbackMessage;
  }
}