import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, map, timeout } from 'rxjs';
import { environment } from '../../../../environments/environment';

interface SelectLikeValue {
  value?: string | number | null;
  label?: string | null;
  name?: string | null;
  text?: string | null;
}

export type AdminSupportTicketDecisionStatus =
  | 'NoDecision'
  | 'ValidComplaint'
  | 'InvalidComplaint'
  | 'NeedsMoreEvidence';

export type AdminSupportTicketDecisionAction =
  | 'NoAction'
  | 'PartialRefundRecommended'
  | 'FullRefundRecommended'
  | 'HostWarningRecommended'
  | 'HidePropertyRecommended'
  | 'HoldPayoutRecommended'
  | 'ReleasePayoutRecommended';

export interface AdminSupportTicketsQuery {
  search?: string | null;
  status?: string | null;
  category?: string | null;
  urgency?: string | null;
  page?: number;
  pageSize?: number;
}

export interface AdminSupportTicketSearchRequest
  extends AdminSupportTicketsQuery {}

export interface AdminSupportTicketsResponse {
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  items: AdminSupportTicketSummary[];
}

export interface AdminSupportTicketSummary {
  ticketId: string;
  referenceCode: string;
  subject: string;
  category: string;
  urgency: string;
  status: string;
  decisionStatus?: string | null;
  decisionAction?: string | null;
  createdByUserId: string;
  createdByName: string;
  createdByEmail?: string | null;
  bookingId?: string | null;
  propertyId?: string | null;
  propertyTitle?: string | null;
  messagesCount: number;
  attachmentsCount?: number;
  createdAt: string;
  updatedAt: string;
  resolvedAt?: string | null;
}

export type AdminSupportTicketListItem = AdminSupportTicketSummary;

export interface AdminSupportTicketMessage {
  messageId: string;
  senderUserId: string;
  senderName: string;
  senderEmail?: string | null;
  isAdminMessage: boolean;
  message: string;
  createdAt: string;
}

export interface AdminSupportTicketAttachment {
  attachmentId?: string;
  id?: string;
  uploadedByUserId?: string;
  uploadedByName?: string | null;
  uploadedByEmail?: string | null;
  type?: string | null;
  url: string;
  fileName: string;
  contentType: string;
  fileSizeInBytes: number;
  createdAt: string;
}

export interface AdminSupportTicketDetails {
  ticketId: string;
  referenceCode: string;
  createdByUserId: string;
  createdByName: string;
  createdByEmail?: string | null;
  bookingId?: string | null;
  propertyId?: string | null;
  propertyTitle?: string | null;
  subject: string;
  description: string;
  category: string;
  urgency: string;
  status: string;
  decisionStatus?: string | null;
  decisionAction?: string | null;
  decisionNote?: string | null;
  decidedAt?: string | null;
  decidedByAdminId?: string | null;
  decidedByAdminName?: string | null;
  createdAt: string;
  updatedAt: string;
  resolvedAt?: string | null;
  resolutionNote?: string | null;
  messages: AdminSupportTicketMessage[];
  attachments?: AdminSupportTicketAttachment[];
}

export interface CreateAdminSupportTicketMessageRequest {
  message: string;
}

export interface AdminSupportTicketDecisionRequest {
  decisionStatus: string | number | null | undefined | SelectLikeValue;
  decisionAction: string | number | null | undefined | SelectLikeValue;
  decisionNote?: string | null;
  adminMessage?: string | null;
  resolveTicket?: boolean;
}

export interface ApplySupportTicketDecisionRequest
  extends AdminSupportTicketDecisionRequest {}

export interface ResolveAdminSupportTicketRequest {
  resolutionNote?: string | null;
}

export interface CreateSupportTicketRefundRequest {
  refundAmount?: number | null;
  refundNote?: string | null;
}

export interface PaymentRefundResponse {
  refundId: string;
  paymentId: string;
  bookingId: string;
  amount: number;
  currency: string;
  provider: string;
  providerRefundId?: string | null;
  status: string;
  failureReason?: string | null;
  createdAt: string;
  updatedAt?: string | null;
  succeededAt?: string | null;
  failedAt?: string | null;
  cancelledAt?: string | null;
  wasAlreadyProcessed: boolean;
  message: string;
}

interface ApplySupportTicketDecisionApiRequest {
  decisionStatus: AdminSupportTicketDecisionStatus;
  decisionAction: AdminSupportTicketDecisionAction;
  decisionNote: string | null;
  adminMessage: string | null;
  resolveTicket: boolean;
}

interface CreateSupportTicketRefundApiRequest {
  refundAmount: number | null;
  refundNote: string | null;
}

@Injectable({
  providedIn: 'root',
})
export class AdminSupportTicketsService {
  private readonly adminSupportTicketsApiUrl =
    `${environment.baseApi}/api/admin/support/tickets`;

  private readonly requestTimeoutMs = 30000;

  constructor(private readonly http: HttpClient) {}

  getSupportTickets(
    request: AdminSupportTicketsQuery = {},
  ): Observable<AdminSupportTicketsResponse> {
    return this.getTickets(request);
  }

  getTickets(
    request: AdminSupportTicketSearchRequest = {},
  ): Observable<AdminSupportTicketsResponse> {
    const params = this.buildSearchParams(request);

    return this.http
      .get(this.adminSupportTicketsApiUrl, {
        params,
        responseType: 'text',
        withCredentials: true,
      })
      .pipe(
        timeout(this.requestTimeoutMs),
        map((responseText) =>
          this.parseRequiredJson<AdminSupportTicketsResponse>(responseText),
        ),
      );
  }

  getSupportTicketDetails(
    ticketId: string,
  ): Observable<AdminSupportTicketDetails> {
    return this.getTicketById(ticketId);
  }

  getTicketById(ticketId: string): Observable<AdminSupportTicketDetails> {
    return this.http
      .get(`${this.adminSupportTicketsApiUrl}/${ticketId}`, {
        responseType: 'text',
        withCredentials: true,
      })
      .pipe(
        timeout(this.requestTimeoutMs),
        map((responseText) =>
          this.parseRequiredJson<AdminSupportTicketDetails>(responseText),
        ),
      );
  }

  replyToSupportTicket(
    ticketId: string,
    request: string | CreateAdminSupportTicketMessageRequest,
  ): Observable<AdminSupportTicketDetails> {
    const message =
      typeof request === 'string'
        ? request
        : request.message;

    return this.replyToTicket(ticketId, message);
  }

  replyToTicket(
    ticketId: string,
    message: string,
  ): Observable<AdminSupportTicketDetails> {
    const payload: CreateAdminSupportTicketMessageRequest = {
      message: message.trim(),
    };

    return this.http
      .post(`${this.adminSupportTicketsApiUrl}/${ticketId}/reply`, payload, {
        responseType: 'text',
        withCredentials: true,
      })
      .pipe(
        timeout(this.requestTimeoutMs),
        map((responseText) =>
          this.parseRequiredJson<AdminSupportTicketDetails>(responseText),
        ),
      );
  }

  applySupportTicketDecision(
    ticketId: string,
    request: AdminSupportTicketDecisionRequest,
  ): Observable<AdminSupportTicketDetails> {
    const payload: ApplySupportTicketDecisionApiRequest = {
      decisionStatus: this.normalizeDecisionStatus(request.decisionStatus),
      decisionAction: this.normalizeDecisionAction(request.decisionAction),
      decisionNote: this.normalizeOptionalText(request.decisionNote),
      adminMessage: this.normalizeOptionalText(request.adminMessage),
      resolveTicket: request.resolveTicket === true,
    };

    return this.http
      .patch(`${this.adminSupportTicketsApiUrl}/${ticketId}/decision`, payload, {
        responseType: 'text',
        withCredentials: true,
      })
      .pipe(
        timeout(this.requestTimeoutMs),
        map((responseText) =>
          this.parseRequiredJson<AdminSupportTicketDetails>(responseText),
        ),
      );
  }

  executeSupportTicketRefund(
    ticketId: string,
    request: CreateSupportTicketRefundRequest,
  ): Observable<PaymentRefundResponse> {
    const payload: CreateSupportTicketRefundApiRequest = {
      refundAmount: this.normalizeOptionalAmount(request.refundAmount),
      refundNote: this.normalizeOptionalText(request.refundNote),
    };

    return this.http
      .post(`${this.adminSupportTicketsApiUrl}/${ticketId}/refund`, payload, {
        responseType: 'text',
        withCredentials: true,
      })
      .pipe(
        timeout(this.requestTimeoutMs),
        map((responseText) =>
          this.parseRequiredJson<PaymentRefundResponse>(responseText),
        ),
      );
  }

  resolveSupportTicket(
    ticketId: string,
    resolutionNote: string,
  ): Observable<AdminSupportTicketDetails> {
    const payload: ResolveAdminSupportTicketRequest = {
      resolutionNote: this.normalizeOptionalText(resolutionNote),
    };

    return this.http
      .patch(`${this.adminSupportTicketsApiUrl}/${ticketId}/resolve`, payload, {
        responseType: 'text',
        withCredentials: true,
      })
      .pipe(
        timeout(this.requestTimeoutMs),
        map((responseText) =>
          this.parseRequiredJson<AdminSupportTicketDetails>(responseText),
        ),
      );
  }

  private buildSearchParams(
    request: AdminSupportTicketSearchRequest,
  ): HttpParams {
    let params = new HttpParams();

    if (request.search?.trim()) {
      params = params.set('search', request.search.trim());
    }

    if (request.status?.trim()) {
      params = params.set('status', request.status.trim());
    }

    if (request.category?.trim()) {
      params = params.set('category', request.category.trim());
    }

    if (request.urgency?.trim()) {
      params = params.set('urgency', request.urgency.trim());
    }

    params = params.set('page', String(request.page ?? 1));
    params = params.set('pageSize', String(request.pageSize ?? 10));
    params = params.set('_ts', String(Date.now()));

    return params;
  }

  private normalizeDecisionStatus(
    value: string | number | null | undefined | SelectLikeValue,
  ): AdminSupportTicketDecisionStatus {
    const normalizedValue = this.extractSelectValue(value);

    const statusMap: Record<string, AdminSupportTicketDecisionStatus> = {
      '1': 'NoDecision',
      '2': 'ValidComplaint',
      '3': 'InvalidComplaint',
      '4': 'NeedsMoreEvidence',

      nodecision: 'NoDecision',
      pending: 'NoDecision',
      none: 'NoDecision',

      validcomplaint: 'ValidComplaint',
      guestclaimaccepted: 'ValidComplaint',
      guestaccepted: 'ValidComplaint',
      claimaccepted: 'ValidComplaint',
      accepted: 'ValidComplaint',
      accept: 'ValidComplaint',
      valid: 'ValidComplaint',

      invalidcomplaint: 'InvalidComplaint',
      guestclaimrejected: 'InvalidComplaint',
      guestrejected: 'InvalidComplaint',
      claimrejected: 'InvalidComplaint',
      rejected: 'InvalidComplaint',
      reject: 'InvalidComplaint',
      invalid: 'InvalidComplaint',

      needsmoreevidence: 'NeedsMoreEvidence',
      moreevidence: 'NeedsMoreEvidence',
      needevidence: 'NeedsMoreEvidence',
      evidenceneeded: 'NeedsMoreEvidence',
    };

    return statusMap[normalizedValue] ?? 'NoDecision';
  }

  private normalizeDecisionAction(
    value: string | number | null | undefined | SelectLikeValue,
  ): AdminSupportTicketDecisionAction {
    const normalizedValue = this.extractSelectValue(value);

    const actionMap: Record<string, AdminSupportTicketDecisionAction> = {
      '1': 'NoAction',
      '2': 'PartialRefundRecommended',
      '3': 'FullRefundRecommended',
      '4': 'HostWarningRecommended',
      '5': 'HidePropertyRecommended',
      '6': 'HoldPayoutRecommended',
      '7': 'ReleasePayoutRecommended',

      noaction: 'NoAction',
      noactionyet: 'NoAction',
      none: 'NoAction',

      partialrefundrecommended: 'PartialRefundRecommended',
      partialrefund: 'PartialRefundRecommended',

      fullrefundrecommended: 'FullRefundRecommended',
      fullrefund: 'FullRefundRecommended',

      hostwarningrecommended: 'HostWarningRecommended',
      hostwarning: 'HostWarningRecommended',
      warnhost: 'HostWarningRecommended',

      hidepropertyrecommended: 'HidePropertyRecommended',
      hideproperty: 'HidePropertyRecommended',
      propertyhide: 'HidePropertyRecommended',

      holdpayoutrecommended: 'HoldPayoutRecommended',
      holdhostpayout: 'HoldPayoutRecommended',
      holdpayout: 'HoldPayoutRecommended',

      releasepayoutrecommended: 'ReleasePayoutRecommended',
      releasehostpayout: 'ReleasePayoutRecommended',
      releasepayout: 'ReleasePayoutRecommended',
    };

    return actionMap[normalizedValue] ?? 'NoAction';
  }

  private extractSelectValue(
    value: string | number | null | undefined | SelectLikeValue,
  ): string {
    if (value === null || value === undefined) {
      return '';
    }

    if (typeof value === 'number') {
      return String(value);
    }

    if (typeof value === 'string') {
      return this.normalizeToken(value);
    }

    const candidate =
      value.value ??
      value.label ??
      value.name ??
      value.text ??
      '';

    return this.normalizeToken(String(candidate));
  }

  private normalizeToken(value: string): string {
    return value
      .trim()
      .replace(/([a-z])([A-Z])/g, '$1 $2')
      .replace(/[^a-zA-Z0-9]/g, '')
      .toLowerCase();
  }

  private normalizeOptionalText(value?: string | null): string | null {
    const normalizedValue = value?.trim();

    return normalizedValue ? normalizedValue : null;
  }

  private normalizeOptionalAmount(value?: number | null): number | null {
    if (value === null || value === undefined) {
      return null;
    }

    if (!Number.isFinite(value)) {
      throw new Error('The refund amount must be a valid number.');
    }

    return Math.round((value + Number.EPSILON) * 100) / 100;
  }

  private parseRequiredJson<T>(responseText: string): T {
    const normalizedResponse = this.normalizeResponseText(responseText);

    if (!normalizedResponse) {
      throw new Error('The server returned an empty response.');
    }

    try {
      return JSON.parse(normalizedResponse) as T;
    } catch {
      throw new Error('The server returned an invalid JSON response.');
    }
  }

  private normalizeResponseText(responseText: string): string {
    return (responseText ?? '')
      .replace(/^\uFEFF/, '')
      .trim();
  }
}