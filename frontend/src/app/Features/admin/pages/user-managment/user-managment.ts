import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { finalize } from 'rxjs/operators';

import {
  Admin,
  AdminUserListItem,
  AdminUserQuery,
  AdminUsersResponse,
  AdminUserStatusResponse,
} from '../../services/admin';

import {
  AdminUserBookingRestrictionsService,
  UserBookingRestrictionResponse,
} from '../../services/admin-user-booking-restrictions';

type BooleanFilter = 'all' | 'true' | 'false';
type RoleFilter = '' | 'Admin' | 'Host' | 'User';

@Component({
  selector: 'app-user-managment',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './user-managment.html',
  styleUrl: './user-managment.css',
})
export class UserManagment implements OnInit {
  users: AdminUserListItem[] = [];

  selectedUser: AdminUserListItem | null = null;
  activeRestriction: UserBookingRestrictionResponse | null = null;
  userRestrictions: UserBookingRestrictionResponse[] = [];

  loading = false;
  restrictionsLoading = false;
  actionLoadingId: string | null = null;
  restrictionActionLoadingId: string | null = null;

  errorMessage = '';
  successMessage = '';
  restrictionsErrorMessage = '';
  restrictionsSuccessMessage = '';

  search = '';
  role: RoleFilter = '';
  isActive: BooleanFilter = 'all';
  isProfileCompleted: BooleanFilter = 'all';

  restrictionRemovalNotes: Record<string, string> = {};

  temporarySuspensionFormRestrictionId: string | null = null;
  temporarySuspensionDurationDays = 7;
  temporarySuspensionReason = '';

  page = 1;
  pageSize = 20;
  totalPages = 1;
  totalCount = 0;

  constructor(
    private readonly adminService: Admin,
    private readonly bookingRestrictionsService: AdminUserBookingRestrictionsService,
    private readonly cdr: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    this.loadUsers();
  }

  loadUsers(page = this.page): void {
    this.page = page;
    this.loading = true;
    this.errorMessage = '';
    this.successMessage = '';
    this.cdr.detectChanges();

    const query: AdminUserQuery = {
      page: this.page,
      pageSize: this.pageSize,
      search: this.search,
      role: this.role,
      isActive: this.mapBooleanFilter(this.isActive),
      isProfileCompleted: this.mapBooleanFilter(this.isProfileCompleted),
    };

    this.adminService
      .getUsers(query)
      .pipe(
        finalize(() => {
          this.loading = false;
          this.cdr.detectChanges();
        }),
      )
      .subscribe({
        next: (response: AdminUsersResponse) => {
          this.users = response.items ?? [];
          this.totalCount = response.totalCount ?? 0;
          this.totalPages = response.totalPages ?? 1;
          this.page = response.page || this.page;

          this.cdr.detectChanges();
        },
        error: (error: unknown) => {
          this.users = [];
          this.totalCount = 0;
          this.totalPages = 1;

          this.errorMessage =
            this.extractErrorMessage(error) ||
            'Failed to load users.';

          this.cdr.detectChanges();
        },
      });
  }

  applyFilters(): void {
    this.loadUsers(1);
  }

  resetFilters(): void {
    this.search = '';
    this.role = '';
    this.isActive = 'all';
    this.isProfileCompleted = 'all';
    this.loadUsers(1);
  }

  toggleUserStatus(user: AdminUserListItem): void {
    this.actionLoadingId = user.userId;
    this.errorMessage = '';
    this.successMessage = '';
    this.cdr.detectChanges();

    const request$ = user.isActive
      ? this.adminService.deactivateUser(user.userId)
      : this.adminService.activateUser(user.userId);

    request$
      .pipe(
        finalize(() => {
          this.actionLoadingId = null;
          this.cdr.detectChanges();
        }),
      )
      .subscribe({
        next: (response: AdminUserStatusResponse) => {
          this.users = this.users.map((currentUser) =>
            currentUser.userId === response.userId
              ? { ...currentUser, isActive: response.isActive }
              : currentUser,
          );

          if (this.selectedUser?.userId === response.userId) {
            this.selectedUser = {
              ...this.selectedUser,
              isActive: response.isActive,
            };
          }

          this.successMessage = response.message;
          this.cdr.detectChanges();
        },
        error: (error: unknown) => {
          this.errorMessage =
            this.extractErrorMessage(error) ||
            'Failed to update user status.';

          this.cdr.detectChanges();
        },
      });
  }

  openRestrictionsPanel(user: AdminUserListItem): void {
    this.selectedUser = user;
    this.activeRestriction = null;
    this.userRestrictions = [];
    this.restrictionRemovalNotes = {};
    this.resetTemporarySuspensionForm();
    this.restrictionsErrorMessage = '';
    this.restrictionsSuccessMessage = '';
    this.loadUserRestrictions(user.userId);
  }

  closeRestrictionsPanel(): void {
    this.selectedUser = null;
    this.activeRestriction = null;
    this.userRestrictions = [];
    this.restrictionRemovalNotes = {};
    this.resetTemporarySuspensionForm();
    this.restrictionsErrorMessage = '';
    this.restrictionsSuccessMessage = '';
    this.cdr.detectChanges();
  }

  loadUserRestrictions(
    userId: string,
    successMessage = '',
  ): void {
    this.restrictionsLoading = true;
    this.restrictionsErrorMessage = '';
    this.restrictionsSuccessMessage = '';
    this.cdr.detectChanges();

    forkJoin({
      active: this.bookingRestrictionsService.getActiveRestriction(userId),
      history: this.bookingRestrictionsService.getUserRestrictions(userId),
    })
      .pipe(
        finalize(() => {
          this.restrictionsLoading = false;
          this.cdr.detectChanges();
        }),
      )
      .subscribe({
        next: (response) => {
          this.activeRestriction = response.active;
          this.userRestrictions = response.history ?? [];
          this.initializeRemovalNotes();
          this.restrictionsSuccessMessage = successMessage;

          this.cdr.detectChanges();
        },
        error: (error: unknown) => {
          this.activeRestriction = null;
          this.userRestrictions = [];
          this.restrictionsErrorMessage =
            this.extractErrorMessage(error) ||
            'Failed to load user booking restrictions.';

          this.cdr.detectChanges();
        },
      });
  }

  openTemporarySuspensionForm(
    restriction: UserBookingRestrictionResponse,
  ): void {
    const restrictionId = this.getRestrictionId(restriction);

    if (!restrictionId) {
      this.restrictionsErrorMessage = 'Restriction ID is missing.';
      this.cdr.detectChanges();
      return;
    }

    if (!this.canApplyTemporarySuspension(restriction)) {
      this.restrictionsErrorMessage =
        'Only an active admin review flag can be converted into a temporary suspension.';
      this.cdr.detectChanges();
      return;
    }

    this.temporarySuspensionFormRestrictionId = restrictionId;
    this.temporarySuspensionDurationDays = 7;
    this.temporarySuspensionReason = '';
    this.restrictionsErrorMessage = '';
    this.restrictionsSuccessMessage = '';
    this.cdr.detectChanges();
  }

  closeTemporarySuspensionForm(): void {
    this.resetTemporarySuspensionForm();
    this.cdr.detectChanges();
  }

  applyTemporaryBookingRestriction(
    restriction: UserBookingRestrictionResponse,
  ): void {
    const restrictionId = this.getRestrictionId(restriction);
    const durationDays = Number(this.temporarySuspensionDurationDays);
    const reason = this.temporarySuspensionReason.trim();

    if (!restrictionId) {
      this.restrictionsErrorMessage = 'Restriction ID is missing.';
      this.cdr.detectChanges();
      return;
    }

    if (!this.canApplyTemporarySuspension(restriction)) {
      this.restrictionsErrorMessage =
        'Only an active admin review flag can be converted into a temporary suspension.';
      this.cdr.detectChanges();
      return;
    }

    if (
      !Number.isInteger(durationDays) ||
      durationDays < 1 ||
      durationDays > 90
    ) {
      this.restrictionsErrorMessage =
        'Suspension duration must be between 1 and 90 whole days.';
      this.cdr.detectChanges();
      return;
    }

    if (reason.length < 10 || reason.length > 1000) {
      this.restrictionsErrorMessage =
        'Suspension reason must contain between 10 and 1000 characters.';
      this.cdr.detectChanges();
      return;
    }

    this.restrictionActionLoadingId = restrictionId;
    this.restrictionsErrorMessage = '';
    this.restrictionsSuccessMessage = '';
    this.cdr.detectChanges();

    this.bookingRestrictionsService
      .applyTemporaryBookingRestriction(
        restrictionId,
        durationDays,
        reason,
      )
      .pipe(
        finalize(() => {
          this.restrictionActionLoadingId = null;
          this.cdr.detectChanges();
        }),
      )
      .subscribe({
        next: () => {
          const selectedUserId = this.selectedUser?.userId;

          this.resetTemporarySuspensionForm();

          if (!selectedUserId) {
            this.restrictionsSuccessMessage =
              'Temporary booking suspension applied successfully.';
            this.cdr.detectChanges();
            return;
          }

          this.loadUserRestrictions(
            selectedUserId,
            'Temporary booking suspension applied successfully.',
          );
        },
        error: (error: unknown) => {
          this.restrictionsErrorMessage =
            this.extractErrorMessage(error) ||
            'Failed to apply the temporary booking suspension.';

          this.cdr.detectChanges();
        },
      });
  }

  removeRestriction(restriction: UserBookingRestrictionResponse): void {
    const restrictionId = this.getRestrictionId(restriction);
    const removalNote = this.restrictionRemovalNotes[restrictionId]?.trim();

    if (!restrictionId) {
      this.restrictionsErrorMessage = 'Restriction ID is missing.';
      this.cdr.detectChanges();
      return;
    }

    if (!removalNote || removalNote.length < 5) {
      this.restrictionsErrorMessage =
        'Removal note must be at least 5 characters.';
      this.cdr.detectChanges();
      return;
    }

    this.restrictionActionLoadingId = restrictionId;
    this.restrictionsErrorMessage = '';
    this.restrictionsSuccessMessage = '';
    this.cdr.detectChanges();

    this.bookingRestrictionsService
      .removeRestriction(restrictionId, removalNote)
      .pipe(
        finalize(() => {
          this.restrictionActionLoadingId = null;
          this.cdr.detectChanges();
        }),
      )
      .subscribe({
        next: (updatedRestriction) => {
          this.restrictionsSuccessMessage =
            'Booking restriction removed successfully.';

          this.activeRestriction =
            this.activeRestriction &&
            this.getRestrictionId(this.activeRestriction) ===
              this.getRestrictionId(updatedRestriction)
              ? null
              : this.activeRestriction;

          this.userRestrictions = this.userRestrictions.map((current) =>
            this.getRestrictionId(current) ===
            this.getRestrictionId(updatedRestriction)
              ? updatedRestriction
              : current,
          );

          this.initializeRemovalNotes();
          this.cdr.detectChanges();
        },
        error: (error: unknown) => {
          this.restrictionsErrorMessage =
            this.extractErrorMessage(error) ||
            'Failed to remove booking restriction.';

          this.cdr.detectChanges();
        },
      });
  }

  goToPreviousPage(): void {
    if (this.page <= 1) {
      return;
    }

    this.loadUsers(this.page - 1);
  }

  goToNextPage(): void {
    if (this.page >= this.totalPages) {
      return;
    }

    this.loadUsers(this.page + 1);
  }

  getRoleBadgeClass(role: string): string {
    switch (role) {
      case 'Admin':
        return 'role-admin';
      case 'Host':
        return 'role-host';
      case 'User':
        return 'role-user';
      default:
        return 'role-default';
    }
  }

  getRestrictionId(restriction: UserBookingRestrictionResponse): string {
    return restriction.restrictionId ?? restriction.id ?? '';
  }

  getRestrictionTypeLabel(type: string | number | null | undefined): string {
    const normalized = String(type ?? '').toLowerCase();

    if (normalized === '1' || normalized.includes('warning')) {
      return 'Warning';
    }

    if (
      normalized === '2' ||
      normalized.includes('temporary') ||
      normalized.includes('restriction')
    ) {
      return 'Temporary booking restriction';
    }

    if (normalized === '3' || normalized.includes('review')) {
      return 'Admin review flag';
    }

    return this.formatValue(type);
  }

  getRestrictionStatusLabel(status: string | number | null | undefined): string {
    const normalized = String(status ?? '').toLowerCase();

    if (normalized === '1' || normalized.includes('active')) {
      return 'Active';
    }

    if (normalized === '2' || normalized.includes('expired')) {
      return 'Expired';
    }

    if (normalized === '3' || normalized.includes('removed')) {
      return 'Removed';
    }

    return this.formatValue(status);
  }

  getRestrictionTypeClass(type: string | number | null | undefined): string {
    const label = this.getRestrictionTypeLabel(type).toLowerCase();

    if (label.includes('temporary')) {
      return 'restriction-temporary';
    }

    if (label.includes('review')) {
      return 'restriction-review';
    }

    return 'restriction-warning';
  }

  getRestrictionStatusClass(status: string | number | null | undefined): string {
    const label = this.getRestrictionStatusLabel(status).toLowerCase();

    if (label.includes('active')) {
      return 'restriction-status-active';
    }

    if (label.includes('expired')) {
      return 'restriction-status-expired';
    }

    if (label.includes('removed')) {
      return 'restriction-status-removed';
    }

    return 'restriction-status-default';
  }

  canRemoveRestriction(restriction: UserBookingRestrictionResponse): boolean {
    return this.getRestrictionStatusLabel(restriction.status) === 'Active';
  }

  canApplyTemporarySuspension(
    restriction: UserBookingRestrictionResponse,
  ): boolean {
    return (
      this.getRestrictionTypeLabel(restriction.type) === 'Admin review flag' &&
      this.getRestrictionStatusLabel(restriction.status) === 'Active'
    );
  }

  isTemporarySuspensionFormOpen(
    restriction: UserBookingRestrictionResponse,
  ): boolean {
    const restrictionId = this.getRestrictionId(restriction);

    return (
      !!restrictionId &&
      this.temporarySuspensionFormRestrictionId === restrictionId
    );
  }

  formatValue(value: string | number | null | undefined): string {
    if (value === null || value === undefined || value === '') {
      return '—';
    }

    return String(value).replace(/([a-z])([A-Z])/g, '$1 $2');
  }

  private initializeRemovalNotes(): void {
    const notes: Record<string, string> = {};

    for (const restriction of this.userRestrictions) {
      const restrictionId = this.getRestrictionId(restriction);

      if (restrictionId) {
        notes[restrictionId] =
          this.restrictionRemovalNotes[restrictionId] ?? '';
      }
    }

    this.restrictionRemovalNotes = notes;
  }

  private resetTemporarySuspensionForm(): void {
    this.temporarySuspensionFormRestrictionId = null;
    this.temporarySuspensionDurationDays = 7;
    this.temporarySuspensionReason = '';
  }

  private mapBooleanFilter(value: BooleanFilter): boolean | null {
    if (value === 'true') {
      return true;
    }

    if (value === 'false') {
      return false;
    }

    return null;
  }

  private extractErrorMessage(error: unknown): string {
    const typedError = error as {
      error?: unknown;
      message?: string;
      status?: number;
    };

    if (typeof typedError.error === 'string') {
      try {
        const parsedError = JSON.parse(typedError.error);

        return (
          parsedError?.detail ||
          parsedError?.message ||
          parsedError?.title ||
          typedError.message ||
          ''
        );
      } catch {
        return typedError.error;
      }
    }

    if (typedError.error && typeof typedError.error === 'object') {
      const parsedError = typedError.error as {
        detail?: string;
        message?: string;
        title?: string;
      };

      return (
        parsedError.detail ||
        parsedError.message ||
        parsedError.title ||
        typedError.message ||
        ''
      );
    }

    return typedError.message || '';
  }
}