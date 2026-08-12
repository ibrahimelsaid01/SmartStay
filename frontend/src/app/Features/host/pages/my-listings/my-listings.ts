import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { finalize } from 'rxjs/operators';

import {
  HostProperty,
  HostPropertyStatus,
  ListingSummary,
  ListingsService,
} from '../../services/listings.service';

export interface HealthWarning {
  id: string;
  title: string;
  description: string;
  type: 'danger' | 'warning' | string;
  icon: string;
}

export interface MaintenanceTask {
  id: string;
  title: string;
  property: string;
  status: 'In Progress' | 'Scheduled' | string;
  statusClass: string;
  icon: string;
  iconBgClass: string;
}

@Component({
  selector: 'app-my-listings',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './my-listings.html',
  styleUrl: './my-listings.css',
})
export class MyListingsComponent implements OnInit {
  private readonly listingsService = inject(ListingsService);
  private readonly router = inject(Router);

  readonly summary = signal<ListingSummary | null>(null);
  readonly properties = signal<HostProperty[]>([]);
  readonly searchQuery = signal<string>('');
  readonly statusFilter = signal<HostPropertyStatus>('');

  readonly isLoading = signal<boolean>(true);
  readonly actionLoadingId = signal<string | null>(null);

  readonly errorMessage = signal<string>('');
  readonly successMessage = signal<string>('');

  readonly page = signal<number>(1);
  readonly pageSize = signal<number>(10);
  readonly totalPages = signal<number>(1);
  readonly totalCount = signal<number>(0);

  readonly statusOptions: { label: string; value: HostPropertyStatus }[] = [
    { label: 'All Statuses', value: '' },
    { label: 'Draft', value: 'Draft' },
    { label: 'Pending', value: 'Pending' },
    { label: 'Published', value: 'Published' },
    { label: 'Rejected', value: 'Rejected' },
    { label: 'Unpublished', value: 'Unpublished' },
  ];

  readonly healthWarnings = signal<HealthWarning[]>([
    {
      id: 'h1',
      title: 'Pending review',
      description:
        'Submitted listings cannot be edited while they are waiting for admin review.',
      type: 'warning',
      icon: 'bi-hourglass-split',
    },
    {
      id: 'h2',
      title: 'Rejected listings',
      description:
        'If a listing is rejected, open it, review the rejection reason, update the data, then submit again.',
      type: 'danger',
      icon: 'bi-exclamation-circle-fill',
    },
  ]);

  readonly maintenanceTasks = signal<MaintenanceTask[]>([
    {
      id: 'm1',
      title: 'Complete drafts',
      property: 'Draft listings must be completed before submission.',
      status: 'Scheduled',
      statusClass: 'status-scheduled',
      icon: 'bi-list-check',
      iconBgClass: 'bg-gold-light',
    },
    {
      id: 'm2',
      title: 'Review published listings',
      property: 'Keep prices, images, and rules up to date.',
      status: 'In Progress',
      statusClass: 'status-in-progress',
      icon: 'bi-arrow-repeat',
      iconBgClass: 'bg-grey-light',
    },
  ]);

  readonly filteredProperties = computed(() => {
    const query = this.searchQuery().toLowerCase().trim();
    const allProperties = this.properties();

    if (!query) {
      return allProperties;
    }

    return allProperties.filter(property => {
      const title = property.title?.toLowerCase() ?? '';
      const type = property.propertyType?.toLowerCase() ?? '';
      const spaceType = property.spaceType?.toLowerCase() ?? '';
      const city = property.city?.toLowerCase() ?? '';
      const status = property.status?.toLowerCase() ?? '';

      return (
        title.includes(query) ||
        type.includes(query) ||
        spaceType.includes(query) ||
        city.includes(query) ||
        status.includes(query)
      );
    });
  });

  ngOnInit(): void {
    this.loadData();
  }

  loadData(page = this.page()): void {
    this.page.set(page);
    this.isLoading.set(true);
    this.errorMessage.set('');
    this.successMessage.set('');

    forkJoin({
      summary: this.listingsService.getSummaryMetrics(),
      propertiesResponse: this.listingsService.getProperties({
        page: this.page(),
        pageSize: this.pageSize(),
        status: this.statusFilter(),
      }),
    })
      .pipe(
        finalize(() => {
          this.isLoading.set(false);
        })
      )
      .subscribe({
        next: ({ summary, propertiesResponse }) => {
          this.summary.set(summary);
          this.properties.set(propertiesResponse.items ?? []);
          this.page.set(propertiesResponse.page ?? 1);
          this.pageSize.set(propertiesResponse.pageSize ?? 10);
          this.totalCount.set(propertiesResponse.totalCount ?? 0);
          this.totalPages.set(propertiesResponse.totalPages ?? 1);
        },
        error: (error: any) => {
          this.properties.set([]);
          this.summary.set(null);
          this.totalCount.set(0);
          this.totalPages.set(1);

          this.errorMessage.set(
            error?.error?.message ||
              error?.message ||
              'Failed to load your listings.'
          );
        },
      });
  }

  applyStatusFilter(): void {
    this.loadData(1);
  }

  resetFilters(): void {
    this.searchQuery.set('');
    this.statusFilter.set('');
    this.loadData(1);
  }

  refresh(): void {
    this.loadData(this.page());
  }

  goToPreviousPage(): void {
    if (this.page() <= 1 || this.isLoading()) {
      return;
    }

    this.loadData(this.page() - 1);
  }

  goToNextPage(): void {
    if (this.page() >= this.totalPages() || this.isLoading()) {
      return;
    }

    this.loadData(this.page() + 1);
  }

  openProperty(propertyId: string): void {
    this.router.navigate(['/host/listings', propertyId, 'edit']);
  }

  onEditProperty(property: HostProperty): void {
    this.router.navigate(['/host/listings', property.id, 'edit']);
  }

  onUnpublish(property: HostProperty): void {
    if (!property.canUnpublish) {
      this.errorMessage.set('This listing cannot be unpublished right now.');
      return;
    }

    const confirmed = confirm(
      `Are you sure you want to unpublish "${property.title}"?`
    );

    if (!confirmed) {
      return;
    }

    this.actionLoadingId.set(property.id);
    this.errorMessage.set('');
    this.successMessage.set('');

    this.listingsService
      .unpublishProperty(property.id)
      .pipe(
        finalize(() => {
          this.actionLoadingId.set(null);
        })
      )
      .subscribe({
        next: response => {
          this.successMessage.set(
            response.message || 'Property unpublished successfully.'
          );

          this.properties.update(properties =>
            properties.map(item =>
              item.id === property.id
                ? {
                    ...item,
                    status: response.status,
                    publishedAt: response.publishedAt,
                    updatedAt: response.updatedAt,
                    canUnpublish: false,
                  }
                : item
            )
          );

          this.listingsService.getSummaryMetrics().subscribe({
            next: summary => this.summary.set(summary),
          });
        },
        error: (error: any) => {
          this.errorMessage.set(
            error?.error?.message ||
              error?.message ||
              'Failed to unpublish property.'
          );
        },
      });
  }

  onSyncCalendar(property: HostProperty): void {
    alert(`Calendar sync for "${property.title}" will be implemented later.`);
  }

  getPropertyImage(property: HostProperty): string {
    return property.coverImageUrl || 'Images/property-placeholder.jpg';
  }

  getLocationLabel(property: HostProperty): string {
    return property.city || 'Location not completed';
  }

  getTypeLabel(property: HostProperty): string {
    if (property.propertyType && property.spaceType) {
      return `${property.propertyType} · ${property.spaceType}`;
    }

    return property.propertyType || property.spaceType || '—';
  }

  getPriceLabel(property: HostProperty): string {
    const price = property.pricePerNight ?? 0;
    const currency = property.currency || '';

    return `${price.toLocaleString()} ${currency}`;
  }

  getStatusLabel(status: string): string {
    const normalizedStatus = this.normalizeStatus(status);

    if (normalizedStatus === 'draft') {
      return 'Draft';
    }

    if (normalizedStatus === 'pending') {
      return 'Pending Review';
    }

    if (normalizedStatus === 'published') {
      return 'Published';
    }

    if (normalizedStatus === 'rejected') {
      return 'Rejected';
    }

    if (normalizedStatus === 'unpublished') {
      return 'Unpublished';
    }

    return status || 'Unknown';
  }

  getStatusHint(property: HostProperty): string {
    const normalizedStatus = this.normalizeStatus(property.status);

    if (normalizedStatus === 'draft') {
      return 'Complete this listing and submit it for admin review.';
    }

    if (normalizedStatus === 'pending') {
      return 'Waiting for admin review. Editing is disabled.';
    }

    if (normalizedStatus === 'published') {
      return 'Visible to users and available for booking.';
    }

    if (normalizedStatus === 'rejected') {
      return property.rejectionReason || 'Review the rejection reason and submit again.';
    }

    if (normalizedStatus === 'unpublished') {
      return 'This listing is not visible to users.';
    }

    return 'Listing status is currently unknown.';
  }

  getPrimaryActionLabel(property: HostProperty): string {
    const normalizedStatus = this.normalizeStatus(property.status);

    if (normalizedStatus === 'draft') {
      return 'Continue';
    }

    if (normalizedStatus === 'rejected') {
      return 'Edit';
    }

    if (normalizedStatus === 'pending') {
      return 'View';
    }

    if (normalizedStatus === 'published') {
      return 'View';
    }

    return 'View';
  }

  getStatusBadgeClass(status: string): string {
    const normalizedStatus = this.normalizeStatus(status);

    if (normalizedStatus === 'published') {
      return 'badge-success-pill';
    }

    if (normalizedStatus === 'rejected') {
      return 'badge-danger-pill';
    }

    if (normalizedStatus === 'pending') {
      return 'badge-warning-pill';
    }

    if (normalizedStatus === 'draft') {
      return 'badge-info-pill';
    }

    return 'badge-muted-pill';
  }

  getStatusDotClass(status: string): string {
    const normalizedStatus = this.normalizeStatus(status);

    if (normalizedStatus === 'published') {
      return 'dot-success';
    }

    if (normalizedStatus === 'rejected') {
      return 'dot-danger';
    }

    if (normalizedStatus === 'pending') {
      return 'dot-warning';
    }

    if (normalizedStatus === 'draft') {
      return 'dot-info';
    }

    return 'dot-muted';
  }

  getUpdatedLabel(property: HostProperty): string {
    return property.updatedAt || property.createdAt;
  }

  canOpenProperty(property: HostProperty): boolean {
    return Boolean(property.id);
  }

  canEditProperty(property: HostProperty): boolean {
    return property.canEdit;
  }

  canUnpublishProperty(property: HostProperty): boolean {
    return property.canUnpublish;
  }

  isPendingReview(property: HostProperty): boolean {
    return this.normalizeStatus(property.status) === 'pending';
  }

  isRejected(property: HostProperty): boolean {
    return this.normalizeStatus(property.status) === 'rejected';
  }

  private normalizeStatus(status?: string | null): string {
    return (status ?? '').toLowerCase().replace(/\s|_/g, '');
  }
}