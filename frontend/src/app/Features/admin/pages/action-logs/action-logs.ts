import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs';
import {
  AdminActionLogResponse,
  AdminActionLogsQuery,
  AdminActionLogsResponse,
  AdminActionLogsService,
  AdminActionTargetType,
  AdminActionType,
} from '../../services/admin-action-logs';

interface SelectOption<T extends string> {
  value: T | '';
  label: string;
}

@Component({
  selector: 'app-action-logs',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './action-logs.html',
  styleUrl: './action-logs.css',
})
export class ActionLogs implements OnInit {
  logs: AdminActionLogResponse[] = [];
  selectedLog: AdminActionLogResponse | null = null;

  loading = false;
  detailsLoading = false;
  selectedDetailsLogId: string | null = null;

  errorMessage = '';
  filterErrorMessage = '';

  search = '';
  adminUserId = '';
  actionType: AdminActionType | '' = '';
  targetType: AdminActionTargetType | '' = '';
  targetId = '';
  from = '';
  to = '';

  page = 1;
  pageSize = 20;
  totalPages = 1;
  totalCount = 0;

  private loadRequestId = 0;

  readonly actionTypeOptions: Array<SelectOption<AdminActionType>> = [
    { value: '', label: 'All actions' },
    { value: 'Created', label: 'Created' },
    { value: 'Updated', label: 'Updated' },
    { value: 'Approved', label: 'Approved' },
    { value: 'Rejected', label: 'Rejected' },
    { value: 'Replied', label: 'Replied' },
    { value: 'Resolved', label: 'Resolved' },
    {
      value: 'DecisionApplied',
      label: 'Decision Applied',
    },
    { value: 'PayoutHeld', label: 'Payout Held' },
    {
      value: 'PayoutReleased',
      label: 'Payout Released',
    },
    {
      value: 'PayoutBlocked',
      label: 'Payout Blocked',
    },
    {
      value: 'PayoutMarkedRefunded',
      label: 'Payout Marked Refunded',
    },
    {
      value: 'UserRestrictionRemoved',
      label: 'User Restriction Removed',
    },
    { value: 'Deleted', label: 'Deleted' },
    { value: 'Other', label: 'Other' },
  ];

  readonly targetTypeOptions: Array<SelectOption<AdminActionTargetType>> = [
    { value: '', label: 'All targets' },
    { value: 'System', label: 'System' },
    { value: 'User', label: 'User' },
    { value: 'HostProfile', label: 'Host Profile' },
    { value: 'Property', label: 'Property' },
    { value: 'Booking', label: 'Booking' },
    { value: 'Payment', label: 'Payment' },
    { value: 'Payout', label: 'Payout' },
    {
      value: 'SupportTicket',
      label: 'Support Ticket',
    },
    { value: 'Review', label: 'Review' },
    { value: 'Refund', label: 'Refund' },
    {
      value: 'UserBookingRestriction',
      label: 'User Booking Restriction',
    },
  ];

  constructor(
    private readonly actionLogsService: AdminActionLogsService,
    private readonly cdr: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    this.loadLogs();
  }

  loadLogs(page = this.page): void {
    if (this.loading) {
      return;
    }

    if (!this.validateFilters()) {
      this.cdr.detectChanges();
      return;
    }

    const requestId = ++this.loadRequestId;

    this.page = page;
    this.loading = true;
    this.errorMessage = '';
    this.filterErrorMessage = '';

    this.cdr.detectChanges();

    const query: AdminActionLogsQuery = {
      search: this.search,
      adminUserId: this.adminUserId,
      actionType: this.actionType,
      targetType: this.targetType,
      targetId: this.targetId,
      from: this.from ? this.toLocalBoundaryIso(this.from, false) : null,
      to: this.to ? this.toLocalBoundaryIso(this.to, true) : null,
      page: this.page,
      pageSize: this.pageSize,
    };

    this.actionLogsService
      .getActionLogs(query)
      .pipe(
        finalize(() => {
          if (requestId === this.loadRequestId) {
            this.loading = false;
            this.cdr.detectChanges();
          }
        }),
      )
      .subscribe({
        next: (response: AdminActionLogsResponse) => {
          if (requestId !== this.loadRequestId) {
            return;
          }

          this.logs = response.items ?? [];
          this.page = response.page || this.page;
          this.pageSize = response.pageSize || this.pageSize;
          this.totalCount = response.totalCount ?? 0;
          this.totalPages = Math.max(1, response.totalPages ?? 1);
        },
        error: (error: unknown) => {
          if (requestId !== this.loadRequestId) {
            return;
          }

          this.logs = [];
          this.totalCount = 0;
          this.totalPages = 1;
          this.errorMessage =
            this.extractErrorMessage(error) || 'Failed to load admin action logs.';
        },
      });
  }

  applyFilters(): void {
    if (this.loading) {
      return;
    }

    this.loadLogs(1);
  }

  resetFilters(): void {
    if (this.loading) {
      return;
    }

    this.search = '';
    this.adminUserId = '';
    this.actionType = '';
    this.targetType = '';
    this.targetId = '';
    this.from = '';
    this.to = '';
    this.filterErrorMessage = '';

    this.loadLogs(1);
  }

  refresh(): void {
    if (this.loading || this.detailsLoading) {
      return;
    }

    this.loadLogs(this.page);
  }

  viewDetails(log: AdminActionLogResponse): void {
    if (!log.logId || this.detailsLoading) {
      return;
    }

    this.detailsLoading = true;
    this.selectedDetailsLogId = log.logId;
    this.errorMessage = '';
    this.selectedLog = null;

    this.cdr.detectChanges();

    this.actionLogsService
      .getActionLogById(log.logId)
      .pipe(
        finalize(() => {
          this.detailsLoading = false;
          this.selectedDetailsLogId = null;
          this.cdr.detectChanges();
        }),
      )
      .subscribe({
        next: (logDetails: AdminActionLogResponse) => {
          this.selectedLog = logDetails;
        },
        error: (error: unknown) => {
          this.selectedLog = log;
          this.errorMessage =
            this.extractErrorMessage(error) ||
            'Failed to load the complete action log. Showing the list data instead.';
        },
      });
  }

  closeDetails(): void {
    if (this.detailsLoading) {
      return;
    }

    this.selectedLog = null;
    this.cdr.detectChanges();
  }

  goToPreviousPage(): void {
    if (this.page <= 1 || this.loading) {
      return;
    }

    this.loadLogs(this.page - 1);
  }

  goToNextPage(): void {
    if (this.page >= this.totalPages || this.loading) {
      return;
    }

    this.loadLogs(this.page + 1);
  }

  trackLog(_index: number, log: AdminActionLogResponse): string {
    return log.logId;
  }

  getAdminDisplayName(log: AdminActionLogResponse | null): string {
    if (!log) {
      return '—';
    }

    return log.adminName || log.adminEmail || log.adminUserId || '—';
  }

  formatActionType(actionType: string | null | undefined): string {
    return this.splitPascalCase(actionType);
  }

  formatTargetType(targetType: string | null | undefined): string {
    return this.splitPascalCase(targetType);
  }

  getActionClass(actionType: string | null | undefined): string {
    const label = this.formatActionType(actionType).toLowerCase();

    if (label.includes('approved') || label.includes('released') || label.includes('created')) {
      return 'action-positive';
    }

    if (
      label.includes('rejected') ||
      label.includes('blocked') ||
      label.includes('deleted') ||
      label.includes('refunded')
    ) {
      return 'action-danger';
    }

    if (label.includes('held') || label.includes('decision') || label.includes('restriction')) {
      return 'action-warning';
    }

    if (label.includes('resolved') || label.includes('replied') || label.includes('updated')) {
      return 'action-info';
    }

    return 'action-default';
  }

  getTargetClass(targetType: string | null | undefined): string {
    const label = this.formatTargetType(targetType).toLowerCase();

    if (label.includes('payout') || label.includes('payment') || label.includes('refund')) {
      return 'target-money';
    }

    if (label.includes('support')) {
      return 'target-support';
    }

    if (label.includes('user') || label.includes('host')) {
      return 'target-user';
    }

    if (label.includes('property') || label.includes('booking') || label.includes('review')) {
      return 'target-booking';
    }

    return 'target-default';
  }

  formatMetadata(metadataJson: string | null | undefined): string {
    if (!metadataJson) {
      return 'No metadata was recorded.';
    }

    try {
      return JSON.stringify(JSON.parse(metadataJson), null, 2);
    } catch {
      return metadataJson;
    }
  }

  private validateFilters(): boolean {
    this.filterErrorMessage = '';

    if (this.adminUserId.trim() && !this.isGuid(this.adminUserId)) {
      this.filterErrorMessage = 'Admin User ID must be a valid identifier.';
      return false;
    }

    if (this.targetId.trim() && !this.isGuid(this.targetId)) {
      this.filterErrorMessage = 'Target ID must be a valid identifier.';
      return false;
    }

    if (this.from && this.to && this.from > this.to) {
      this.filterErrorMessage = 'The From date must be earlier than or equal to the To date.';
      return false;
    }

    return true;
  }

  private toLocalBoundaryIso(dateValue: string, endOfDay: boolean): string {
    const [year, month, day] = dateValue.split('-').map(Number);

    const date = new Date(
      year,
      month - 1,
      day,
      endOfDay ? 23 : 0,
      endOfDay ? 59 : 0,
      endOfDay ? 59 : 0,
      endOfDay ? 999 : 0,
    );

    return date.toISOString();
  }

  private isGuid(value: string): boolean {
    return /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i.test(
      value.trim(),
    );
  }

  private splitPascalCase(value: string | null | undefined): string {
    if (!value) {
      return '—';
    }

    return value.replace(/([a-z])([A-Z])/g, '$1 $2');
  }

  private extractErrorMessage(error: unknown): string {
    const typedError = error as {
      error?: unknown;
      message?: string;
      status?: number;
    };

    if (typedError.error && typeof typedError.error === 'object') {
      const errorBody = typedError.error as {
        detail?: string;
        message?: string;
        title?: string;
        errors?: Record<string, string[]>;
      };

      const firstValidationError = errorBody.errors
        ? Object.values(errorBody.errors)[0]?.[0]
        : undefined;

      return (
        errorBody.detail ||
        errorBody.message ||
        firstValidationError ||
        errorBody.title ||
        typedError.message ||
        ''
      );
    }

    if (typeof typedError.error === 'string' && typedError.error.trim()) {
      try {
        const parsedError = JSON.parse(typedError.error) as {
          detail?: string;
          message?: string;
          title?: string;
          errors?: Record<string, string[]>;
        };

        const firstValidationError = parsedError.errors
          ? Object.values(parsedError.errors)[0]?.[0]
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
        return typedError.error.trim();
      }
    }

    if (typedError.status === 0) {
      return 'Cannot reach the server. Check your connection and try again.';
    }

    return typedError.message || '';
  }
}