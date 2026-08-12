import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs/operators';

import {
  Admin,
  AdminDashboardSummary,
  AdminHostApplicationDetails,
  AdminHostApplicationSummary,
} from '../../services/admin';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: './admin-dashboard.html',
  styleUrl: './admin-dashboard.css',
})
export class AdminDashboard implements OnInit {
  summary: AdminDashboardSummary | null = null;
  pendingApplications: AdminHostApplicationSummary[] = [];

  summaryLoading = false;
  applicationsLoading = false;
  loadingDetails = false;
  actionLoadingId: string | null = null;

  selectedApp: AdminHostApplicationDetails | null = null;

  showRejectModal = false;
  appToReject: AdminHostApplicationSummary | null = null;
  rejectionReason = '';

  errorMessage = '';
  successMessage = '';

  constructor(
    private adminService: Admin,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadDashboard();
  }

  loadDashboard(): void {
    this.loadSummary();
    this.loadPendingApplications();
  }

  loadSummary(): void {
    this.summaryLoading = true;
    this.errorMessage = '';
    this.cdr.detectChanges();

    this.adminService
      .getDashboardSummary()
      .pipe(
        finalize(() => {
          this.summaryLoading = false;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: (summary: AdminDashboardSummary) => {
          this.summary = summary;
          this.cdr.detectChanges();
        },
        error: (error: any) => {
          this.summary = null;

          this.errorMessage =
            error?.error?.message ||
            error?.message ||
            'Failed to load admin dashboard summary.';

          this.cdr.detectChanges();
        },
      });
  }

  loadPendingApplications(): void {
    this.applicationsLoading = true;
    this.errorMessage = '';
    this.successMessage = '';
    this.cdr.detectChanges();

    this.adminService
      .getPendingHostApplications()
      .pipe(
        finalize(() => {
          this.applicationsLoading = false;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: (applications: AdminHostApplicationSummary[]) => {
          this.pendingApplications = applications ?? [];
          this.cdr.detectChanges();
        },
        error: (error: any) => {
          this.pendingApplications = [];

          this.errorMessage =
            error?.error?.message ||
            error?.message ||
            'Failed to load pending host applications.';

          this.cdr.detectChanges();
        },
      });
  }

  viewDetails(applicationId: string): void {
    this.loadingDetails = true;
    this.errorMessage = '';
    this.selectedApp = null;
    this.cdr.detectChanges();

    this.adminService
      .getHostApplication(applicationId)
      .pipe(
        finalize(() => {
          this.loadingDetails = false;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: (application: AdminHostApplicationDetails) => {
          this.selectedApp = application;
          this.cdr.detectChanges();
        },
        error: (error: any) => {
          this.errorMessage =
            error?.error?.message ||
            error?.message ||
            'Failed to load host application details.';

          this.cdr.detectChanges();
        },
      });
  }

  closeDetails(): void {
    this.selectedApp = null;
    this.cdr.detectChanges();
  }

  approve(id: string): void {
    this.actionLoadingId = id;
    this.errorMessage = '';
    this.successMessage = '';
    this.cdr.detectChanges();

    this.adminService
      .approveHostApplication(id)
      .pipe(
        finalize(() => {
          this.actionLoadingId = null;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: (approvedApplication: AdminHostApplicationDetails) => {
          this.pendingApplications = this.pendingApplications.filter(
            app => app.id !== id
          );

          if (this.summary) {
            this.summary = {
              ...this.summary,
              pendingHostApplications: Math.max(
                0,
                this.summary.pendingHostApplications - 1
              ),
              approvedHostApplications:
                this.summary.approvedHostApplications + 1,
              totalHosts: this.summary.totalHosts + 1,
            };
          }

          this.selectedApp = null;
          this.successMessage = `${approvedApplication.userFullName} has been approved as a host.`;

          this.cdr.detectChanges();
        },
        error: (error: any) => {
          this.errorMessage =
            error?.error?.message ||
            error?.message ||
            'Failed to approve host application.';

          this.cdr.detectChanges();
        },
      });
  }

  openRejectModal(app: AdminHostApplicationSummary): void {
    this.appToReject = app;
    this.rejectionReason = '';
    this.showRejectModal = true;
    this.errorMessage = '';
    this.successMessage = '';
    this.cdr.detectChanges();
  }

  closeRejectModal(): void {
    this.showRejectModal = false;
    this.appToReject = null;
    this.rejectionReason = '';
    this.cdr.detectChanges();
  }

  confirmReject(): void {
    if (!this.appToReject) {
      return;
    }

    if (this.rejectionReason.trim().length < 10) {
      this.errorMessage = 'Rejection reason must be at least 10 characters.';
      this.cdr.detectChanges();
      return;
    }

    const applicationId = this.appToReject.id;

    this.actionLoadingId = applicationId;
    this.errorMessage = '';
    this.successMessage = '';
    this.cdr.detectChanges();

    this.adminService
      .rejectHostApplication(applicationId, this.rejectionReason.trim())
      .pipe(
        finalize(() => {
          this.actionLoadingId = null;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: (rejectedApplication: AdminHostApplicationDetails) => {
          this.pendingApplications = this.pendingApplications.filter(
            app => app.id !== applicationId
          );

          if (this.summary) {
            this.summary = {
              ...this.summary,
              pendingHostApplications: Math.max(
                0,
                this.summary.pendingHostApplications - 1
              ),
              rejectedHostApplications:
                this.summary.rejectedHostApplications + 1,
            };
          }

          this.selectedApp = null;
          this.successMessage = `${rejectedApplication.userFullName}'s application has been rejected.`;

          this.closeRejectModal();
          this.cdr.detectChanges();
        },
        error: (error: any) => {
          this.errorMessage =
            error?.error?.message ||
            error?.message ||
            'Failed to reject host application.';

          this.cdr.detectChanges();
        },
      });
  }

  get pendingCount(): number {
    return this.pendingApplications.length;
  }

  get isAnyLoading(): boolean {
    return this.summaryLoading || this.applicationsLoading;
  }
}