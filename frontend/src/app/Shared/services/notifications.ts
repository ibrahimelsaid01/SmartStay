import {
  HttpClient,
  HttpParams,
} from '@angular/common/http';
import { Injectable } from '@angular/core';
import {
  Observable,
  map,
  timeout,
} from 'rxjs';

import { environment } from '../../../environments/environment';

export interface NotificationListItem {
  id: string;
  type: string;
  title: string;
  message: string;
  referenceType: string;
  referenceId?: string | null;
  isRead: boolean;
  createdAt: string;
  readAt?: string | null;
}

export interface NotificationsResponse {
  items: NotificationListItem[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  unreadCount: number;
}

export interface UnreadNotificationsCountResponse {
  unreadCount: number;
}

export interface MarkAllNotificationsReadResponse {
  updatedCount: number;
  readAt: string;
  message: string;
}

export interface DeleteAllNotificationsResponse {
  deletedCount: number;
  deletedAt: string;
  message: string;
}

@Injectable({
  providedIn: 'root',
})
export class NotificationsService {
  private readonly notificationsApiUrl =
    `${environment.baseApi}/api/notifications`;

  private readonly requestTimeoutMs =
    30000;

  constructor(
    private readonly http: HttpClient,
  ) {}

  getNotifications(
    unreadOnly = false,
    page = 1,
    pageSize = 20,
  ): Observable<NotificationsResponse> {
    const params = new HttpParams()
      .set(
        'unreadOnly',
        String(unreadOnly),
      )
      .set(
        'page',
        String(page),
      )
      .set(
        'pageSize',
        String(pageSize),
      );

    return this.http
      .get(
        this.notificationsApiUrl,
        {
          params,
          responseType: 'text',
        },
      )
      .pipe(
        timeout(
          this.requestTimeoutMs,
        ),

        map((responseText) =>
          this.parseRequiredJsonResponse<NotificationsResponse>(
            responseText,
            'Notifications API returned an empty response.',
          ),
        ),
      );
  }

  getUnreadCount():
    Observable<UnreadNotificationsCountResponse> {
    return this.http
      .get(
        `${this.notificationsApiUrl}/unread-count`,
        {
          responseType: 'text',
        },
      )
      .pipe(
        timeout(
          this.requestTimeoutMs,
        ),

        map((responseText) =>
          this.parseRequiredJsonResponse<UnreadNotificationsCountResponse>(
            responseText,
            'Unread notifications count API returned an empty response.',
          ),
        ),
      );
  }

  markAsRead(
    notificationId: string,
  ): Observable<NotificationListItem> {
    return this.http
      .patch(
        `${this.notificationsApiUrl}/${notificationId}/read`,
        {},
        {
          responseType: 'text',
        },
      )
      .pipe(
        timeout(
          this.requestTimeoutMs,
        ),

        map((responseText) =>
          this.parseRequiredJsonResponse<NotificationListItem>(
            responseText,
            'Mark notification as read API returned an empty response.',
          ),
        ),
      );
  }

  markAllAsRead():
    Observable<MarkAllNotificationsReadResponse> {
    return this.http
      .patch(
        `${this.notificationsApiUrl}/read-all`,
        {},
        {
          responseType: 'text',
        },
      )
      .pipe(
        timeout(
          this.requestTimeoutMs,
        ),

        map((responseText) =>
          this.parseRequiredJsonResponse<MarkAllNotificationsReadResponse>(
            responseText,
            'Mark all notifications as read API returned an empty response.',
          ),
        ),
      );
  }

  deleteNotification(
    notificationId: string,
  ): Observable<void> {
    return this.http
      .delete(
        `${this.notificationsApiUrl}/${notificationId}`,
        {
          responseType: 'text',
        },
      )
      .pipe(
        timeout(
          this.requestTimeoutMs,
        ),

        /*
         * The delete-one endpoint can legitimately return an empty
         * response body, so no JSON parsing is required here.
         */
        map(() => undefined),
      );
  }

  deleteAll():
    Observable<DeleteAllNotificationsResponse> {
    return this.http
      .delete(
        this.notificationsApiUrl,
        {
          responseType: 'text',
        },
      )
      .pipe(
        timeout(
          this.requestTimeoutMs,
        ),

        map((responseText) =>
          this.parseRequiredJsonResponse<DeleteAllNotificationsResponse>(
            responseText,
            'Delete all notifications API returned an empty response.',
          ),
        ),
      );
  }

  private parseRequiredJsonResponse<T>(
    responseText:
      | string
      | null
      | undefined,

    emptyResponseMessage: string,
  ): T {
    const normalizedResponse =
      (responseText ?? '')
        .replace(
          /^\uFEFF/,
          '',
        )
        .trim();

    if (!normalizedResponse) {
      throw new Error(
        emptyResponseMessage,
      );
    }

    try {
      return JSON.parse(
        normalizedResponse,
      ) as T;
    } catch {
      throw new Error(
        'Notifications API returned an invalid JSON response.',
      );
    }
  }
}