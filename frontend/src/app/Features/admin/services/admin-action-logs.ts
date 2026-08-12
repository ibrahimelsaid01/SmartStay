import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, map, timeout } from 'rxjs';
import { environment } from '../../../../environments/environment';

export type AdminActionType =
  | 'Created'
  | 'Updated'
  | 'Approved'
  | 'Rejected'
  | 'Replied'
  | 'Resolved'
  | 'DecisionApplied'
  | 'PayoutHeld'
  | 'PayoutReleased'
  | 'PayoutBlocked'
  | 'PayoutMarkedRefunded'
  | 'UserRestrictionRemoved'
  | 'Deleted'
  | 'Other';

export type AdminActionTargetType =
  | 'System'
  | 'User'
  | 'HostProfile'
  | 'Property'
  | 'Booking'
  | 'Payment'
  | 'Payout'
  | 'SupportTicket'
  | 'Review'
  | 'Refund'
  | 'UserBookingRestriction';

export interface AdminActionLogsQuery {
  search?: string | null;
  adminUserId?: string | null;
  actionType?: AdminActionType | string | null;
  targetType?: AdminActionTargetType | string | null;
  targetId?: string | null;
  from?: string | null;
  to?: string | null;
  page?: number;
  pageSize?: number;
}

export interface AdminActionLogsResponse {
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  items: AdminActionLogResponse[];
}

export interface AdminActionLogResponse {
  logId: string;
  adminUserId: string;
  adminName: string;
  adminEmail?: string | null;
  actionType: AdminActionType | string;
  targetType: AdminActionTargetType | string;
  targetId?: string | null;
  targetReference?: string | null;
  summary: string;
  details?: string | null;
  metadataJson?: string | null;
  ipAddress?: string | null;
  userAgent?: string | null;
  createdAt: string;
}

@Injectable({
  providedIn: 'root',
})
export class AdminActionLogsService {
  private readonly apiUrl = `${environment.baseApi}/api/admin/action-logs`;

  private readonly requestTimeoutMs = 30000;

  constructor(private readonly http: HttpClient) {}

  getActionLogs(query: AdminActionLogsQuery = {}): Observable<AdminActionLogsResponse> {
    const page = query.page ?? 1;
    const pageSize = query.pageSize ?? 20;

    let params = new HttpParams()
      .set('page', String(page))
      .set('pageSize', String(pageSize))
      .set('_ts', String(Date.now()));

    if (query.search?.trim()) {
      params = params.set('search', query.search.trim());
    }

    if (query.adminUserId?.trim()) {
      params = params.set('adminUserId', query.adminUserId.trim());
    }

    if (query.actionType?.trim()) {
      params = params.set('actionType', query.actionType.trim());
    }

    if (query.targetType?.trim()) {
      params = params.set('targetType', query.targetType.trim());
    }

    if (query.targetId?.trim()) {
      params = params.set('targetId', query.targetId.trim());
    }

    if (query.from?.trim()) {
      params = params.set('from', query.from.trim());
    }

    if (query.to?.trim()) {
      params = params.set('to', query.to.trim());
    }

    return this.http
      .get(this.apiUrl, {
        params,
        responseType: 'text',
        withCredentials: true,
      })
      .pipe(
        timeout(this.requestTimeoutMs),
        map((responseText) => {
          const normalizedResponse = this.normalizeResponseText(responseText);

          if (!normalizedResponse) {
            return this.createEmptyResponse(page, pageSize);
          }

          const response = this.parseJson<any>(normalizedResponse);

          const rawItems = response.items ?? response.Items ?? [];

          return {
            page: response.page ?? response.Page ?? page,

            pageSize: response.pageSize ?? response.PageSize ?? pageSize,

            totalCount: response.totalCount ?? response.TotalCount ?? 0,

            totalPages: Math.max(1, response.totalPages ?? response.TotalPages ?? 1),

            items: Array.isArray(rawItems) ? rawItems.map((item: any) => this.mapLog(item)) : [],
          };
        }),
      );
  }

  getActionLogById(logId: string): Observable<AdminActionLogResponse> {
    return this.http
      .get(`${this.apiUrl}/${logId}`, {
        responseType: 'text',
        withCredentials: true,
      })
      .pipe(
        timeout(this.requestTimeoutMs),
        map((responseText) => {
          const response = this.parseRequiredJson<any>(responseText);

          return this.mapLog(response);
        }),
      );
  }

  private mapLog(item: any): AdminActionLogResponse {
    return {
      logId: item.logId ?? item.LogId ?? '',

      adminUserId: item.adminUserId ?? item.AdminUserId ?? '',

      adminName: item.adminName ?? item.AdminName ?? 'Unknown Admin',

      adminEmail: item.adminEmail ?? item.AdminEmail ?? null,

      actionType: item.actionType ?? item.ActionType ?? 'Other',

      targetType: item.targetType ?? item.TargetType ?? 'System',

      targetId: item.targetId ?? item.TargetId ?? null,

      targetReference: item.targetReference ?? item.TargetReference ?? null,

      summary: item.summary ?? item.Summary ?? '',

      details: item.details ?? item.Details ?? null,

      metadataJson: item.metadataJson ?? item.MetadataJson ?? null,

      ipAddress: item.ipAddress ?? item.IpAddress ?? null,

      userAgent: item.userAgent ?? item.UserAgent ?? null,

      createdAt: item.createdAt ?? item.CreatedAt ?? new Date().toISOString(),
    };
  }

  private createEmptyResponse(page: number, pageSize: number): AdminActionLogsResponse {
    return {
      page,
      pageSize,
      totalCount: 0,
      totalPages: 1,
      items: [],
    };
  }

  private parseRequiredJson<T>(responseText: string): T {
    const normalizedResponse = this.normalizeResponseText(responseText);

    if (!normalizedResponse) {
      throw new Error('The server returned an empty response.');
    }

    return this.parseJson<T>(normalizedResponse);
  }

  private normalizeResponseText(responseText: string): string {
    return (responseText ?? '').replace(/^\uFEFF/, '').trim();
  }

  private parseJson<T>(responseText: string): T {
    try {
      return JSON.parse(responseText) as T;
    } catch {
      throw new Error('The server returned an invalid JSON response.');
    }
  }
}