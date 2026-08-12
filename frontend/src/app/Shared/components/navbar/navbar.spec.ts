import {
  signal,
} from '@angular/core';

import {
  ComponentFixture,
  TestBed,
} from '@angular/core/testing';

import {
  provideRouter,
} from '@angular/router';

import {
  NgbModal,
} from '@ng-bootstrap/ng-bootstrap';

import {
  of,
} from 'rxjs';

import {
  AuthState,
} from '../../../Features/auth/services/auth-state';

import {
  UserProfileService,
} from '../../../Features/profile/services/user-profile-service';

import {
  NotificationsService,
} from '../../services/notifications';

import {
  Navbar,
} from './navbar';

describe('Navbar', () => {
  let component: Navbar;
  let fixture: ComponentFixture<Navbar>;

  const profileServiceMock = {
    isAuthenticated: signal(false),

    isLoading: signal(false),

    currentUser: signal(null),

    logoutCurrentDevice: () =>
      of(undefined),
  };

  const authStateMock = {
    hasRole: (
      _role: string,
    ): boolean => false,

    isAdmin: (): boolean =>
      false,

    isHost: (): boolean =>
      false,

    isUser: (): boolean =>
      false,

    getRole: (): string =>
      '',
  };

  const notificationsServiceMock = {
    getNotifications: () =>
      of({
        items: [],
        page: 1,
        pageSize: 20,
        totalCount: 0,
        totalPages: 0,
        unreadCount: 0,
      }),

    getUnreadCount: () =>
      of({
        unreadCount: 0,
      }),

    markAsRead: (
      notificationId: string,
    ) =>
      of({
        id: notificationId,
        type: 'Info',
        title: 'Notification',
        message: 'Notification message',
        referenceType: 'General',
        referenceId: null,
        isRead: true,
        createdAt:
          new Date().toISOString(),
        readAt:
          new Date().toISOString(),
      }),

    markAllAsRead: () =>
      of({
        updatedCount: 0,
        readAt:
          new Date().toISOString(),
        message:
          'Notifications marked as read.',
      }),

    deleteNotification: (
      _notificationId: string,
    ) =>
      of(undefined),

    deleteAll: () =>
      of({
        deletedCount: 0,
        deletedAt:
          new Date().toISOString(),
        message:
          'Notifications deleted.',
      }),
  };

  const modalServiceMock = {
    open: () => ({
      componentInstance: {},
    }),
  };

  beforeEach(async () => {
    await TestBed
      .configureTestingModule({
        imports: [
          Navbar,
        ],

        providers: [
          provideRouter([]),

          {
            provide:
              UserProfileService,

            useValue:
              profileServiceMock,
          },

          {
            provide:
              AuthState,

            useValue:
              authStateMock,
          },

          {
            provide:
              NotificationsService,

            useValue:
              notificationsServiceMock,
          },

          {
            provide:
              NgbModal,

            useValue:
              modalServiceMock,
          },
        ],
      })
      .compileComponents();

    fixture =
      TestBed.createComponent(
        Navbar,
      );

    component =
      fixture.componentInstance;

    fixture.detectChanges();

    await fixture.whenStable();
  });

  it('should create', () => {
    expect(
      component,
    ).toBeTruthy();
  });
});