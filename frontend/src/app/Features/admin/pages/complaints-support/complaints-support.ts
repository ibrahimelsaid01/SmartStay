import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs/operators';

import {
  AdminSupportTicketAttachment,
  AdminSupportTicketDecisionAction,
  AdminSupportTicketDecisionRequest,
  AdminSupportTicketDecisionStatus,
  AdminSupportTicketDetails,
  AdminSupportTicketsQuery,
  AdminSupportTicketSummary,
  AdminSupportTicketsResponse,
  AdminSupportTicketsService,
  CreateSupportTicketRefundRequest,
  PaymentRefundResponse,
} from '../../services/admin-support-tickets';

interface DecisionStatusOption {
  value: AdminSupportTicketDecisionStatus;
  label: string;
  hint: string;
}

interface DecisionActionOption {
  value: AdminSupportTicketDecisionAction;
  label: string;
  hint: string;
}

@Component({
  selector: 'app-complaints-support',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './complaints-support.html',
  styleUrl: './complaints-support.css',
})
export class ComplaintsSupport implements OnInit {
  tickets: AdminSupportTicketSummary[] = [];

  selectedTicket: AdminSupportTicketDetails | null = null;
  lastRefundResponse: PaymentRefundResponse | null = null;

  loading = false;
  loadingDetails = false;
  replyLoading = false;
  decisionLoading = false;
  refundLoading = false;
  resolveLoading = false;

  errorMessage = '';
  successMessage = '';

  search = '';
  status = '';
  category = '';
  urgency = '';

  page = 1;
  pageSize = 10;
  totalPages = 1;
  totalCount = 0;

  replyMessage = '';
  decisionStatus: AdminSupportTicketDecisionStatus = 'ValidComplaint';
  decisionAction: AdminSupportTicketDecisionAction = 'NoAction';
  decisionNote = '';
  adminMessage = '';
  refundAmount: number | null = null;
  refundNote = '';
  resolutionNote = '';

  readonly statusOptions = [
    { value: '', label: 'All statuses' },
    { value: 'Open', label: 'Open' },
    { value: 'InProgress', label: 'In progress' },
    { value: 'Resolved', label: 'Resolved' },
    { value: 'Closed', label: 'Closed' },
  ];

  readonly categoryOptions = [
    { value: '', label: 'All categories' },
    { value: 'General', label: 'General' },
    { value: 'PaymentIssue', label: 'Payment issue' },
    { value: 'BookingIssue', label: 'Booking issue' },
    { value: 'PropertyIssue', label: 'Property issue' },
    { value: 'HostIssue', label: 'Host issue' },
    { value: 'AccountIssue', label: 'Account issue' },
    { value: 'RefundIssue', label: 'Refund issue' },
    { value: 'TechnicalIssue', label: 'Technical issue' },
    { value: 'Other', label: 'Other' },
  ];

  readonly urgencyOptions = [
    { value: '', label: 'All urgency levels' },
    { value: 'Low', label: 'Low' },
    { value: 'Medium', label: 'Medium' },
    { value: 'High', label: 'High' },
    { value: 'Critical', label: 'Critical' },
  ];

  readonly decisionStatusOptions: DecisionStatusOption[] = [
    {
      value: 'ValidComplaint',
      label: 'Valid complaint',
      hint: 'The available evidence supports the guest complaint.',
    },
    {
      value: 'InvalidComplaint',
      label: 'Invalid complaint',
      hint: 'The available evidence does not support the complaint.',
    },
    {
      value: 'NeedsMoreEvidence',
      label: 'Needs more evidence',
      hint: 'Keep the ticket open and explain which evidence is required.',
    },
  ];

  private readonly validComplaintActionOptions: DecisionActionOption[] = [
    {
      value: 'NoAction',
      label: 'No financial action',
      hint: 'Accept the complaint without a refund or payout action.',
    },
    {
      value: 'PartialRefundRecommended',
      label: 'Recommend partial refund',
      hint: 'Block the payout and refund part of the guest payment.',
    },
    {
      value: 'FullRefundRecommended',
      label: 'Recommend full refund',
      hint: 'Block the payout and refund the remaining guest payment.',
    },
    {
      value: 'HostWarningRecommended',
      label: 'Recommend host warning',
      hint: 'Record a warning against the host after validating the complaint.',
    },
    {
      value: 'HidePropertyRecommended',
      label: 'Recommend hiding property',
      hint: 'Recommend hiding the listing pending a separate property action.',
    },
    {
      value: 'HoldPayoutRecommended',
      label: 'Keep payout on hold',
      hint: 'Keep the payout held while a final remedy is still pending.',
    },
  ];

  private readonly automaticDecisionActionOptions: DecisionActionOption[] = [
    {
      value: 'NoAction',
      label: 'Automatic payout policy',
      hint:
        'The server will hold or release the payout automatically when applicable.',
    },
  ];

  constructor(
    private readonly adminService: AdminSupportTicketsService,
    private readonly cdr: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    this.loadTickets();
  }

  get decisionActionOptions(): DecisionActionOption[] {
    return this.decisionStatus === 'ValidComplaint'
      ? this.validComplaintActionOptions
      : this.automaticDecisionActionOptions;
  }

  loadTickets(page = this.page): void {
    this.page = page;
    this.loading = true;
    this.errorMessage = '';
    this.successMessage = '';
    this.cdr.detectChanges();

    const query: AdminSupportTicketsQuery = {
      search: this.search,
      status: this.status,
      category: this.category,
      urgency: this.urgency,
      page: this.page,
      pageSize: this.pageSize,
    };

    this.adminService
      .getSupportTickets(query)
      .pipe(
        finalize(() => {
          this.loading = false;
          this.cdr.detectChanges();
        }),
      )
      .subscribe({
        next: (response: AdminSupportTicketsResponse) => {
          this.tickets = response.items ?? [];
          this.totalCount = response.totalCount ?? 0;
          this.totalPages = Math.max(response.totalPages ?? 1, 1);
          this.page = response.page || this.page;
          this.cdr.detectChanges();
        },
        error: (error: unknown) => {
          this.tickets = [];
          this.totalCount = 0;
          this.totalPages = 1;
          this.errorMessage =
            this.extractErrorMessage(error) ||
            'Failed to load support tickets.';
          this.cdr.detectChanges();
        },
      });
  }

  applyFilters(): void {
    this.loadTickets(1);
  }

  resetFilters(): void {
    this.search = '';
    this.status = '';
    this.category = '';
    this.urgency = '';
    this.loadTickets(1);
  }

  viewDetails(ticketId: string): void {
    this.loadingDetails = true;
    this.errorMessage = '';
    this.successMessage = '';
    this.selectedTicket = null;
    this.resetActionForms();
    this.cdr.detectChanges();

    this.adminService
      .getSupportTicketDetails(ticketId)
      .pipe(
        finalize(() => {
          this.loadingDetails = false;
          this.cdr.detectChanges();
        }),
      )
      .subscribe({
        next: (ticket: AdminSupportTicketDetails) => {
          this.selectedTicket = ticket;
          this.syncDecisionForm(ticket);
          this.resolutionNote = ticket.resolutionNote || '';
          this.cdr.detectChanges();
        },
        error: (error: unknown) => {
          this.errorMessage =
            this.extractErrorMessage(error) ||
            'Failed to load support ticket details.';
          this.cdr.detectChanges();
        },
      });
  }

  closeDetails(): void {
    this.selectedTicket = null;
    this.resetActionForms();
    this.cdr.detectChanges();
  }

  sendReply(): void {
    if (!this.selectedTicket || this.isSelectedTicketReadOnly) {
      return;
    }

    const normalizedMessage = this.replyMessage.trim();

    if (normalizedMessage.length < 2) {
      this.errorMessage = 'Reply message is required.';
      this.cdr.detectChanges();
      return;
    }

    const ticketId = this.selectedTicket.ticketId;

    this.replyLoading = true;
    this.errorMessage = '';
    this.successMessage = '';
    this.cdr.detectChanges();

    this.adminService
      .replyToSupportTicket(ticketId, normalizedMessage)
      .pipe(
        finalize(() => {
          this.replyLoading = false;
          this.cdr.detectChanges();
        }),
      )
      .subscribe({
        next: (updatedTicket: AdminSupportTicketDetails) => {
          this.selectedTicket = updatedTicket;
          this.replyMessage = '';
          this.successMessage = 'Reply sent successfully.';
          this.updateTicketInList(updatedTicket);
          this.cdr.detectChanges();
        },
        error: (error: unknown) => {
          this.errorMessage =
            this.extractErrorMessage(error) || 'Failed to send reply.';
          this.cdr.detectChanges();
        },
      });
  }

  onDecisionStatusChanged(): void {
    if (this.decisionStatus !== 'ValidComplaint') {
      this.decisionAction = 'NoAction';
    } else if (
      !this.validComplaintActionOptions.some(
        (option) => option.value === this.decisionAction,
      )
    ) {
      this.decisionAction = 'NoAction';
    }

    if (this.decisionStatus !== 'NeedsMoreEvidence') {
      this.adminMessage = '';
    }

    this.refundAmount = null;
    this.refundNote = '';
    this.lastRefundResponse = null;
    this.cdr.detectChanges();
  }

  onDecisionActionChanged(): void {
    this.refundAmount = null;
    this.refundNote = '';
    this.lastRefundResponse = null;
    this.cdr.detectChanges();
  }

  applyDecision(): void {
    if (!this.selectedTicket || this.isSelectedTicketReadOnly) {
      return;
    }

    const normalizedDecisionNote = this.decisionNote.trim();
    const normalizedAdminMessage = this.adminMessage.trim();

    if (normalizedDecisionNote.length < 5) {
      this.errorMessage = 'Decision note must be at least 5 characters.';
      this.cdr.detectChanges();
      return;
    }

    if (
      this.decisionStatus === 'NeedsMoreEvidence' &&
      normalizedAdminMessage.length < 5
    ) {
      this.errorMessage =
        'Write a message explaining which additional evidence is required.';
      this.cdr.detectChanges();
      return;
    }

    const ticketId = this.selectedTicket.ticketId;

    const request: AdminSupportTicketDecisionRequest = {
      decisionStatus: this.decisionStatus,
      decisionAction: this.decisionAction,
      decisionNote: normalizedDecisionNote,
      adminMessage: normalizedAdminMessage || null,
      resolveTicket: false,
    };

    this.decisionLoading = true;
    this.errorMessage = '';
    this.successMessage = '';
    this.lastRefundResponse = null;
    this.cdr.detectChanges();

    this.adminService
      .applySupportTicketDecision(ticketId, request)
      .pipe(
        finalize(() => {
          this.decisionLoading = false;
          this.cdr.detectChanges();
        }),
      )
      .subscribe({
        next: (updatedTicket: AdminSupportTicketDetails) => {
          this.selectedTicket = updatedTicket;
          this.syncDecisionForm(updatedTicket);
          this.adminMessage = '';
          this.successMessage = 'Complaint decision saved successfully.';
          this.updateTicketInList(updatedTicket);
          this.cdr.detectChanges();
        },
        error: (error: unknown) => {
          this.errorMessage =
            this.extractErrorMessage(error) ||
            'Failed to save complaint decision.';
          this.cdr.detectChanges();
        },
      });
  }

  executeRefund(): void {
    if (!this.selectedTicket || !this.canExecuteRefund) {
      return;
    }

    const isPartialRefund = this.isPartialRefundDecision;
    const normalizedRefundAmount = this.normalizeRefundAmount(this.refundAmount);

    if (isPartialRefund && normalizedRefundAmount === null) {
      this.errorMessage =
        'Enter a valid partial refund amount greater than zero.';
      this.cdr.detectChanges();
      return;
    }

    const request: CreateSupportTicketRefundRequest = {
      refundAmount: isPartialRefund ? normalizedRefundAmount : null,
      refundNote: this.refundNote.trim() || null,
    };

    const ticketId = this.selectedTicket.ticketId;

    this.refundLoading = true;
    this.errorMessage = '';
    this.successMessage = '';
    this.lastRefundResponse = null;
    this.cdr.detectChanges();

    this.adminService
      .executeSupportTicketRefund(ticketId, request)
      .pipe(
        finalize(() => {
          this.refundLoading = false;
          this.cdr.detectChanges();
        }),
      )
      .subscribe({
        next: (response: PaymentRefundResponse) => {
          this.lastRefundResponse = response;
          this.successMessage =
            response.message || 'Refund operation completed successfully.';
          this.refreshSelectedTicketAfterRefund(ticketId);
          this.cdr.detectChanges();
        },
        error: (error: unknown) => {
          this.errorMessage =
            this.extractErrorMessage(error) ||
            'Failed to execute the support ticket refund.';
          this.cdr.detectChanges();
        },
      });
  }

  resolveTicket(): void {
    if (!this.selectedTicket || this.isSelectedTicketReadOnly) {
      return;
    }

    const normalizedResolutionNote = this.resolutionNote.trim();

    if (normalizedResolutionNote.length < 5) {
      this.errorMessage = 'Resolution note must be at least 5 characters.';
      this.cdr.detectChanges();
      return;
    }

    const ticketId = this.selectedTicket.ticketId;

    this.resolveLoading = true;
    this.errorMessage = '';
    this.successMessage = '';
    this.cdr.detectChanges();

    this.adminService
      .resolveSupportTicket(ticketId, normalizedResolutionNote)
      .pipe(
        finalize(() => {
          this.resolveLoading = false;
          this.cdr.detectChanges();
        }),
      )
      .subscribe({
        next: (updatedTicket: AdminSupportTicketDetails) => {
          this.selectedTicket = updatedTicket;
          this.successMessage = 'Ticket resolved successfully.';
          this.updateTicketInList(updatedTicket);
          this.cdr.detectChanges();
        },
        error: (error: unknown) => {
          this.errorMessage =
            this.extractErrorMessage(error) || 'Failed to resolve ticket.';
          this.cdr.detectChanges();
        },
      });
  }

  goToPreviousPage(): void {
    if (this.page <= 1) {
      return;
    }

    this.loadTickets(this.page - 1);
  }

  goToNextPage(): void {
    if (this.page >= this.totalPages) {
      return;
    }

    this.loadTickets(this.page + 1);
  }

  getStatusClass(status: string | null | undefined): string {
    const normalized = this.normalizeToken(status);

    if (normalized === 'resolved' || normalized === 'closed') {
      return 'status-resolved';
    }

    if (normalized === 'inprogress') {
      return 'status-progress';
    }

    if (normalized === 'open' || normalized === 'pending') {
      return 'status-open';
    }

    return 'status-default';
  }

  getUrgencyClass(urgency: string | null | undefined): string {
    const normalized = this.normalizeToken(urgency);

    if (normalized === 'critical') {
      return 'urgency-critical';
    }

    if (normalized === 'high' || normalized === 'urgent') {
      return 'urgency-high';
    }

    if (normalized === 'medium') {
      return 'urgency-medium';
    }

    if (normalized === 'low') {
      return 'urgency-low';
    }

    return 'urgency-default';
  }

  getDecisionClass(value: string | null | undefined): string {
    const normalized = this.normalizeToken(value);

    if (
      normalized === 'validcomplaint' ||
      normalized === 'fullrefundrecommended' ||
      normalized === 'partialrefundrecommended'
    ) {
      return 'decision-positive';
    }

    if (
      normalized === 'invalidcomplaint' ||
      normalized === 'hidepropertyrecommended'
    ) {
      return 'decision-danger';
    }

    if (
      normalized === 'needsmoreevidence' ||
      normalized === 'holdpayoutrecommended'
    ) {
      return 'decision-warning';
    }

    return 'decision-default';
  }

  getAttachmentId(
    index: number,
    attachment: AdminSupportTicketAttachment,
  ): string {
    return attachment.attachmentId ?? attachment.id ?? String(index);
  }

  formatValue(value: string | number | null | undefined): string {
    if (value === null || value === undefined || value === '') {
      return '—';
    }

    return String(value).replace(/([a-z])([A-Z])/g, '$1 $2');
  }

  formatFileSize(sizeInBytes: number | null | undefined): string {
    if (!sizeInBytes || sizeInBytes <= 0) {
      return '0 KB';
    }

    if (sizeInBytes < 1024 * 1024) {
      return `${Math.ceil(sizeInBytes / 1024)} KB`;
    }

    return `${(sizeInBytes / (1024 * 1024)).toFixed(1)} MB`;
  }

  getSelectedDecisionStatusHint(): string {
    return (
      this.decisionStatusOptions.find(
        (option) => option.value === this.decisionStatus,
      )?.hint ?? ''
    );
  }

  getSelectedDecisionActionHint(): string {
    return (
      this.decisionActionOptions.find(
        (option) => option.value === this.decisionAction,
      )?.hint ?? ''
    );
  }

  get isSelectedTicketReadOnly(): boolean {
    if (!this.selectedTicket) {
      return false;
    }

    const status = this.normalizeToken(this.selectedTicket.status);
    return status === 'resolved' || status === 'closed';
  }

  get isSelectedTicketResolved(): boolean {
    return this.isSelectedTicketReadOnly;
  }

  get isNeedsMoreEvidenceDecision(): boolean {
    return this.decisionStatus === 'NeedsMoreEvidence';
  }

  get isPartialRefundDecision(): boolean {
    return (
      this.normalizeToken(this.selectedTicket?.decisionStatus) ===
        'validcomplaint' &&
      this.normalizeToken(this.selectedTicket?.decisionAction) ===
        'partialrefundrecommended'
    );
  }

  get isFullRefundDecision(): boolean {
    return (
      this.normalizeToken(this.selectedTicket?.decisionStatus) ===
        'validcomplaint' &&
      this.normalizeToken(this.selectedTicket?.decisionAction) ===
        'fullrefundrecommended'
    );
  }

  get canExecuteRefund(): boolean {
    return Boolean(
      this.selectedTicket &&
        !this.isSelectedTicketReadOnly &&
        this.selectedTicket.bookingId &&
        (this.isPartialRefundDecision || this.isFullRefundDecision),
    );
  }

  get refundActionTitle(): string {
    if (this.isPartialRefundDecision) {
      return 'Execute partial refund';
    }

    if (this.isFullRefundDecision) {
      return 'Execute full refund';
    }

    return 'Execute refund';
  }

  get refundActionHint(): string {
    if (this.isPartialRefundDecision) {
      return (
        'Enter the guest refund amount. The backend will recalculate the ' +
        'remaining host payout after Stripe confirms the refund.'
      );
    }

    if (this.isFullRefundDecision) {
      return (
        'The backend calculates and refunds the complete remaining payment ' +
        'amount. Do not enter an amount.'
      );
    }

    return 'Save a valid partial or full refund decision first.';
  }

  private refreshSelectedTicketAfterRefund(ticketId: string): void {
    this.adminService.getSupportTicketDetails(ticketId).subscribe({
      next: (updatedTicket: AdminSupportTicketDetails) => {
        this.selectedTicket = updatedTicket;
        this.syncDecisionForm(updatedTicket);
        this.updateTicketInList(updatedTicket);
        this.cdr.detectChanges();
      },
      error: (error: unknown) => {
        const refreshError = this.extractErrorMessage(error);

        if (refreshError) {
          this.errorMessage =
            `The refund response was received, but ticket refresh failed: ${refreshError}`;
        }

        this.cdr.detectChanges();
      },
    });
  }

  private updateTicketInList(updatedTicket: AdminSupportTicketDetails): void {
    this.tickets = this.tickets.map((ticket) =>
      ticket.ticketId === updatedTicket.ticketId
        ? {
            ...ticket,
            status: updatedTicket.status,
            decisionStatus: updatedTicket.decisionStatus,
            decisionAction: updatedTicket.decisionAction,
            messagesCount: updatedTicket.messages.length,
            attachmentsCount: updatedTicket.attachments?.length,
            updatedAt: updatedTicket.updatedAt,
            resolvedAt: updatedTicket.resolvedAt,
          }
        : ticket,
    );
  }

  private syncDecisionForm(ticket: AdminSupportTicketDetails): void {
    this.decisionStatus = this.parseDecisionStatus(ticket.decisionStatus);
    this.decisionAction = this.parseDecisionAction(ticket.decisionAction);

    if (this.decisionStatus !== 'ValidComplaint') {
      this.decisionAction = 'NoAction';
    }

    this.decisionNote = ticket.decisionNote || '';
  }

  private resetActionForms(): void {
    this.replyMessage = '';
    this.decisionStatus = 'ValidComplaint';
    this.decisionAction = 'NoAction';
    this.decisionNote = '';
    this.adminMessage = '';
    this.refundAmount = null;
    this.refundNote = '';
    this.resolutionNote = '';
    this.lastRefundResponse = null;
  }

  private parseDecisionStatus(
    value: string | null | undefined,
  ): AdminSupportTicketDecisionStatus {
    const normalized = this.normalizeToken(value);

    if (normalized === 'invalidcomplaint') {
      return 'InvalidComplaint';
    }

    if (normalized === 'needsmoreevidence') {
      return 'NeedsMoreEvidence';
    }

    return 'ValidComplaint';
  }

  private parseDecisionAction(
    value: string | null | undefined,
  ): AdminSupportTicketDecisionAction {
    const normalized = this.normalizeToken(value);

    const actionMap: Record<string, AdminSupportTicketDecisionAction> = {
      partialrefundrecommended: 'PartialRefundRecommended',
      fullrefundrecommended: 'FullRefundRecommended',
      hostwarningrecommended: 'HostWarningRecommended',
      hidepropertyrecommended: 'HidePropertyRecommended',
      holdpayoutrecommended: 'HoldPayoutRecommended',
      releasepayoutrecommended: 'ReleasePayoutRecommended',
      noaction: 'NoAction',
    };

    return actionMap[normalized] ?? 'NoAction';
  }

  private normalizeRefundAmount(value: number | null): number | null {
    if (value === null || value === undefined || !Number.isFinite(value)) {
      return null;
    }

    const roundedValue = Math.round((value + Number.EPSILON) * 100) / 100;
    return roundedValue > 0 ? roundedValue : null;
  }

  private normalizeToken(value: string | null | undefined): string {
    return (value ?? '')
      .trim()
      .replace(/[^a-zA-Z0-9]/g, '')
      .toLowerCase();
  }

  private extractErrorMessage(error: unknown): string {
    const candidate = error as {
      error?: unknown;
      message?: string;
    };

    if (typeof candidate?.error === 'string') {
      try {
        const parsedError = JSON.parse(candidate.error) as {
          detail?: string;
          message?: string;
          title?: string;
        };

        return (
          parsedError.detail ||
          parsedError.message ||
          parsedError.title ||
          ''
        );
      } catch {
        return candidate.error;
      }
    }

    if (
      candidate?.error &&
      typeof candidate.error === 'object'
    ) {
      const objectError = candidate.error as {
        detail?: string;
        message?: string;
        title?: string;
      };

      return (
        objectError.detail ||
        objectError.message ||
        objectError.title ||
        candidate.message ||
        ''
      );
    }

    return candidate?.message || '';
  }
}