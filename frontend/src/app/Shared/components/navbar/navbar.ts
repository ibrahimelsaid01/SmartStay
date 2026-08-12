import { CommonModule } from '@angular/common';
import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  effect,
  inject,
} from '@angular/core';
import { Params, Router, RouterModule } from '@angular/router';
import { NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { Subject } from 'rxjs';
import { finalize, takeUntil } from 'rxjs/operators';
import { AuthState } from '../../../Features/auth/services/auth-state';
import { UserProfileService } from '../../../Features/profile/services/user-profile-service';
import {
  NotificationListItem,
  NotificationsService,
} from '../../services/notifications';
import { ChatbotModalComponent } from '../chatbot-modal/chatbot-modal';

interface NotificationNavigationTarget {
  commands: string[];
  queryParams?: Params;
}

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [RouterModule, CommonModule],
  templateUrl: './navbar.html',
  styleUrl: './navbar.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Navbar {
  readonly profileService = inject(UserProfileService);

  notifications: NotificationListItem[] = [];
  unreadNotificationsCount = 0;
  notificationsTotalCount = 0;

  notificationsLoading = false;
  unreadNotificationsCountLoading = false;
  notificationActionLoadingId: string | null = null;
  markAllNotificationsLoading = false;
  deleteAllNotificationsLoading = false;

  notificationsLoaded = false;
  notificationsErrorMessage = '';
  notificationsSuccessMessage = '';

  isLoggingOut = false;

  private readonly authState = inject(AuthState);
  private readonly router = inject(Router);
  private readonly modalService = inject(NgbModal);
  private readonly notificationsService = inject(NotificationsService);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly notificationRequestsCancelled$ = new Subject<void>();

  private wasAuthenticated = false;

  constructor() {
    effect(() => {
      const isAuthenticated =
        this.profileService.isAuthenticated();

      if (
        isAuthenticated &&
        !this.wasAuthenticated
      ) {
        this.wasAuthenticated = true;
        this.loadUnreadNotificationsCount();
        return;
      }

      if (!isAuthenticated) {
        this.wasAuthenticated = false;
        this.resetNotificationsState();
      }
    });
  }

  hasRole(role: string): boolean {
    return this.authState.hasRole(role);
  }

  isHost(): boolean {
    return this.authState.isHost();
  }

  isAdmin(): boolean {
    return this.authState.isAdmin();
  }

  isRegularUser(): boolean {
    return (
      this.authState.isUser() &&
      !this.authState.isHost() &&
      !this.authState.isAdmin()
    );
  }

  canShowHostButton(): boolean {
    return (
      this.profileService.isAuthenticated() &&
      this.isRegularUser()
    );
  }

  canShowHostPersonalLinks(): boolean {
    return (
      this.isHost() &&
      !this.isAdmin()
    );
  }

  getDisplayRole(): string {
    return this.authState.getRole() || 'User';
  }

  openNotificationsDropdown(): void {
    if (
      !this.profileService.isAuthenticated()
    ) {
      return;
    }

    this.loadNotifications();
  }

  loadNotifications(): void {
    if (
      !this.profileService.isAuthenticated()
    ) {
      this.resetNotificationsState();
      return;
    }

    this.notificationsLoading = true;
    this.notificationsErrorMessage = '';
    this.notificationsSuccessMessage = '';

    this.cdr.detectChanges();

    this.notificationsService
      .getNotifications(
        false,
        1,
        20,
      )
      .pipe(
        takeUntil(this.notificationRequestsCancelled$),
        finalize(() => {
          this.notificationsLoading =
            false;

          this.cdr.detectChanges();
        }),
      )
      .subscribe({
        next: (response) => {
          this.notifications =
            response.items ?? [];

          this.unreadNotificationsCount =
            response.unreadCount ?? 0;

          this.notificationsTotalCount =
            response.totalCount ?? 0;

          this.notificationsLoaded = true;

          this.cdr.detectChanges();
        },
        error: (
          error: unknown,
        ) => {
          this.notificationsLoaded = true;

          this.notificationsErrorMessage =
            this.extractErrorMessage(
              error,
            ) ||
            'Failed to load notifications.';

          this.cdr.detectChanges();
        },
      });
  }

  loadUnreadNotificationsCount(): void {
    if (
      !this.profileService.isAuthenticated()
    ) {
      this.unreadNotificationsCount = 0;
      return;
    }

    this.unreadNotificationsCountLoading =
      true;

    this.notificationsService
      .getUnreadCount()
      .pipe(
        takeUntil(this.notificationRequestsCancelled$),
        finalize(() => {
          this.unreadNotificationsCountLoading =
            false;

          this.cdr.detectChanges();
        }),
      )
      .subscribe({
        next: (response) => {
          this.unreadNotificationsCount =
            response.unreadCount ?? 0;

          this.cdr.detectChanges();
        },
        error: (
          error: unknown,
        ) => {
          this.notificationsErrorMessage =
            this.extractErrorMessage(
              error,
            ) ||
            'Failed to load unread notifications count.';

          this.cdr.detectChanges();
        },
      });
  }

  openNotification(
    notification:
      NotificationListItem,
  ): void {
    if (!notification.isRead) {
      this.markNotificationAsRead(
        notification,
      );
    }

    const target =
      this.getNotificationRoute(
        notification,
      );

    if (!target) {
      return;
    }

    void this.router.navigate(
      target.commands,
      {
        queryParams:
          target.queryParams,
      },
    );
  }

  markNotificationAsRead(
    notification:
      NotificationListItem,
  ): void {
    if (
      notification.isRead ||
      this.notificationActionLoadingId
    ) {
      return;
    }

    this.notificationActionLoadingId =
      notification.id;

    this.notificationsErrorMessage = '';
    this.notificationsSuccessMessage = '';

    this.cdr.detectChanges();

    this.notificationsService
      .markAsRead(
        notification.id,
      )
      .pipe(
        takeUntil(this.notificationRequestsCancelled$),
        finalize(() => {
          this.notificationActionLoadingId =
            null;

          this.cdr.detectChanges();
        }),
      )
      .subscribe({
        next: (
          updatedNotification,
        ) => {
          const wasUnread =
            !notification.isRead;

          this.notifications =
            this.notifications.map(
              (
                currentNotification,
              ) =>
                currentNotification.id ===
                updatedNotification.id
                  ? updatedNotification
                  : currentNotification,
            );

          if (wasUnread) {
            this.unreadNotificationsCount =
              Math.max(
                0,
                this
                  .unreadNotificationsCount -
                  1,
              );
          }

          this.cdr.detectChanges();
        },
        error: (
          error: unknown,
        ) => {
          this.notificationsErrorMessage =
            this.extractErrorMessage(
              error,
            ) ||
            'Failed to mark the notification as read.';

          this.cdr.detectChanges();
        },
      });
  }

  markAllNotificationsAsRead(): void {
    if (
      this.markAllNotificationsLoading ||
      this.unreadNotificationsCount ===
        0
    ) {
      return;
    }

    this.markAllNotificationsLoading =
      true;

    this.notificationsErrorMessage = '';
    this.notificationsSuccessMessage = '';

    this.cdr.detectChanges();

    this.notificationsService
      .markAllAsRead()
      .pipe(
        takeUntil(this.notificationRequestsCancelled$),
        finalize(() => {
          this.markAllNotificationsLoading =
            false;

          this.cdr.detectChanges();
        }),
      )
      .subscribe({
        next: (response) => {
          this.notifications =
            this.notifications.map(
              (notification) => ({
                ...notification,

                isRead: true,

                readAt:
                  notification.readAt ??
                  response.readAt,
              }),
            );

          this.unreadNotificationsCount = 0;

          this.notificationsSuccessMessage =
            response.message;

          this.cdr.detectChanges();
        },
        error: (
          error: unknown,
        ) => {
          this.notificationsErrorMessage =
            this.extractErrorMessage(
              error,
            ) ||
            'Failed to mark all notifications as read.';

          this.cdr.detectChanges();
        },
      });
  }

  deleteNotification(
    notification:
      NotificationListItem,
    event?: Event,
  ): void {
    event?.preventDefault();
    event?.stopPropagation();

    if (
      this.notificationActionLoadingId
    ) {
      return;
    }

    this.notificationActionLoadingId =
      notification.id;

    this.notificationsErrorMessage = '';
    this.notificationsSuccessMessage = '';

    this.cdr.detectChanges();

    this.notificationsService
      .deleteNotification(
        notification.id,
      )
      .pipe(
        takeUntil(this.notificationRequestsCancelled$),
        finalize(() => {
          this.notificationActionLoadingId =
            null;

          this.cdr.detectChanges();
        }),
      )
      .subscribe({
        next: () => {
          this.notifications =
            this.notifications.filter(
              (
                currentNotification,
              ) =>
                currentNotification.id !==
                notification.id,
            );

          this.notificationsTotalCount =
            Math.max(
              0,
              this
                .notificationsTotalCount -
                1,
            );

          if (!notification.isRead) {
            this.unreadNotificationsCount =
              Math.max(
                0,
                this
                  .unreadNotificationsCount -
                  1,
              );
          }

          this.notificationsSuccessMessage =
            'Notification deleted successfully.';

          this.cdr.detectChanges();
        },
        error: (
          error: unknown,
        ) => {
          this.notificationsErrorMessage =
            this.extractErrorMessage(
              error,
            ) ||
            'Failed to delete the notification.';

          this.cdr.detectChanges();
        },
      });
  }

  deleteAllNotifications(): void {
    if (
      this.deleteAllNotificationsLoading ||
      this.notificationsTotalCount ===
        0
    ) {
      return;
    }

    this.deleteAllNotificationsLoading =
      true;

    this.notificationsErrorMessage = '';
    this.notificationsSuccessMessage = '';

    this.cdr.detectChanges();

    this.notificationsService
      .deleteAll()
      .pipe(
        takeUntil(this.notificationRequestsCancelled$),
        finalize(() => {
          this.deleteAllNotificationsLoading =
            false;

          this.cdr.detectChanges();
        }),
      )
      .subscribe({
        next: (response) => {
          this.notifications = [];
          this.unreadNotificationsCount = 0;
          this.notificationsTotalCount = 0;

          this.notificationsSuccessMessage =
            response.message;

          this.cdr.detectChanges();
        },
        error: (
          error: unknown,
        ) => {
          this.notificationsErrorMessage =
            this.extractErrorMessage(
              error,
            ) ||
            'Failed to delete notifications.';

          this.cdr.detectChanges();
        },
      });
  }

  getNotificationIcon(
    notification:
      NotificationListItem,
  ): string {
    const referenceType =
      notification.referenceType
        .trim()
        .toLowerCase();

    switch (referenceType) {
      case 'booking':
        return 'bi-calendar-check';

      case 'payment':
        return 'bi-credit-card';

      case 'property':
        return 'bi-house-check';

      case 'hostapplication':
        return 'bi-person-badge';

      case 'review':
        return 'bi-star';

      default:
        return 'bi-bell';
    }
  }

  getNotificationTypeClass(
    notification:
      NotificationListItem,
  ): string {
    const type =
      notification.type
        .trim()
        .toLowerCase();

    if (
      type.includes('failed') ||
      type.includes('rejected') ||
      type.includes('cancelled')
    ) {
      return 'notification-danger';
    }

    if (
      type.includes('pending') ||
      type.includes('refunded') ||
      type.includes('expired')
    ) {
      return 'notification-warning';
    }

    if (
      type.includes('succeeded') ||
      type.includes('confirmed') ||
      type.includes('approved') ||
      type.includes('published') ||
      type.includes('completed')
    ) {
      return 'notification-success';
    }

    return 'notification-info';
  }

  logout(): void {
    if (this.isLoggingOut) {
      return;
    }

    this.isLoggingOut = true;

    /*
     * Clear the displayed notification state immediately
     * while the backend revokes the refresh token.
     */
    this.resetNotificationsState();

    this.cdr.detectChanges();

    this.profileService
      .logoutCurrentDevice()
      .pipe(
        finalize(() => {
          this.isLoggingOut = false;

          this.cdr.detectChanges();
        }),
      )
      .subscribe({
        next: () => {
          this.navigateAfterLogout();
        },
        error: () => {
          /*
           * UserProfileService clears the local authentication
           * state even when the backend Logout request fails.
           */
          this.navigateAfterLogout();
        },
      });
  }

  openAiChat(
    event: Event,
  ): void {
    event.preventDefault();

    this.modalService.open(
      ChatbotModalComponent,
      {
        size: 'lg',
        backdrop: 'static',
        centered: true,
        windowClass:
          'chatbot-modal-window',
      },
    );
  }

  private navigateAfterLogout(): void {
    void this.router.navigate(
      ['/'],
      {
        replaceUrl: true,
      },
    );
  }

  private getNotificationRoute(
    notification:
      NotificationListItem,
  ): NotificationNavigationTarget | null {
    const referenceType =
      notification.referenceType
        .trim()
        .toLowerCase();

    const notificationType =
      notification.type
        .trim()
        .toLowerCase();

    const notificationTitle =
      notification.title
        .trim()
        .toLowerCase();

    const referenceId =
      notification.referenceId
        ?.trim();

    switch (referenceType) {
      case 'booking':
        return this.getBookingNotificationRoute(
          notificationType,
          notificationTitle,
          referenceId,
        );

      case 'payment':
        /*
         * Payment notifications store Payment.Id in ReferenceId.
         * BookingConfirmation expects that exact payment ID and then
         * resolves the related BookingId through GET /api/payments/{id}.
         */
        return referenceId
          ? {
              commands: [
                '/booking-confirmation',
                referenceId,
              ],
            }
          : {
              commands: [
                '/profile/payment-account',
              ],
            };

      case 'property':
        if (!referenceId) {
          return {
            commands: [
              '/host/my-listings',
            ],
          };
        }

        if (
          notificationType ===
          'propertypublished'
        ) {
          return {
            commands: [
              '/property-details',
              referenceId,
            ],
          };
        }

        if (
          notificationType ===
          'propertyrejected'
        ) {
          return {
            commands: [
              '/host/listings',
              referenceId,
              'edit',
            ],
          };
        }

        return {
          commands: [
            '/host/my-listings',
          ],
        };

      case 'hostapplication':
        /*
         * HostApplication notifications store HostProfile.Id. The
         * current application endpoint is user-scoped, so the page
         * does not accept that ID in its route.
         */
        return {
          commands: [
            '/become-host',
          ],
        };

      case 'review':
        /*
         * Review notifications store Review.Id. My Reviews already
         * supports ?reviewId= and loads the focused review directly.
         */
        return {
          commands: [
            '/profile/my-reviews',
          ],
          queryParams: referenceId
            ? {
                reviewId:
                  referenceId,
              }
            : undefined,
        };

      default:
        return null;
    }
  }

  private getBookingNotificationRoute(
    notificationType: string,
    notificationTitle: string,
    bookingId?: string,
  ): NotificationNavigationTarget {
    /*
     * Every Booking reference contains Booking.Id. The notification
     * payload currently has no recipient-role field, so host copies of
     * shared booking events are distinguished by the stable host titles
     * generated by BookingPaymentNotificationInterceptor.
     */
    const isHostBookingNotification =
      notificationType ===
        'newbookingreceived' ||
      notificationTitle.startsWith(
        'reservation',
      );

    if (isHostBookingNotification) {
      return {
        commands: [
          '/host/dashboard',
        ],
      };
    }

    if (
      notificationType ===
        'bookingpendingpayment' &&
      bookingId
    ) {
      return {
        commands: [
          '/checkout',
          bookingId,
        ],
      };
    }

    if (
      notificationType ===
        'bookingcompleted' &&
      bookingId
    ) {
      return {
        commands: [
          '/profile/my-reviews',
        ],
        queryParams: {
          bookingId,
        },
      };
    }

    if (
      notificationType ===
      'bookingconfirmed'
    ) {
      return {
        commands: [
          '/profile/bookings/active',
        ],
      };
    }

    if (
      notificationType ===
      'bookingcancelled'
    ) {
      return {
        commands: [
          '/profile/bookings/canceled',
        ],
      };
    }

    return {
      commands: [
        '/profile/bookings/all',
      ],
    };
  }

  private resetNotificationsState(): void {
    /*
     * Stop every in-flight notification request before clearing the
     * displayed state. Without cancellation, a response that started
     * before Logout could arrive afterwards and restore data belonging
     * to the previous authenticated session.
     */
    this.notificationRequestsCancelled$.next();

    this.notifications = [];
    this.unreadNotificationsCount = 0;
    this.notificationsTotalCount = 0;

    this.notificationsLoading = false;

    this.unreadNotificationsCountLoading =
      false;

    this.notificationActionLoadingId =
      null;

    this.markAllNotificationsLoading =
      false;

    this.deleteAllNotificationsLoading =
      false;

    this.notificationsLoaded = false;
    this.notificationsErrorMessage = '';
    this.notificationsSuccessMessage = '';

    this.cdr.markForCheck();
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
          typedError.message || ''
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
          };

        return (
          parsedError.detail ||
          parsedError.message ||
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