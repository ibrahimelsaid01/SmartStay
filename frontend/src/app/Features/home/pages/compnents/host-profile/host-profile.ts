import { Component, Input, inject } from '@angular/core';
import { Router } from '@angular/router';

import { AuthState } from '../../../../auth/services/auth-state';
import { ChatService } from '../../../../host/pages/meesages/services/chat-service';
import { Host } from '../../../services/propertydetailservice';

@Component({
  selector: 'app-host-profile',
  imports: [],
  templateUrl: './host-profile.html',
  styleUrl: './host-profile.css',
})
export class HostProfile {
  @Input() host!: Host;

  private readonly chatService =
    inject(ChatService);

  private readonly router =
    inject(Router);

  private readonly authState =
    inject(AuthState);

  get displayName(): string {
    return (
      this.host?.displayName?.trim() ||
      this.host?.fullName?.trim() ||
      this.host?.firstName?.trim() ||
      'SmartStay Host'
    );
  }

  get location(): string {
    return [
      this.host?.city?.trim(),
      this.host?.country?.trim(),
    ]
      .filter((value): value is string => !!value)
      .join(', ');
  }

  get profileImageUrl(): string {
    return (
      this.host?.profileImageUrl?.trim() ||
      '/Images/default-avatar.png'
    );
  }

  get bio(): string {
    return (
      this.host?.bio?.trim() ||
      `Hello, I'm ${this.displayName}. Contact me to learn more about this property.`
    );
  }

  sendMessageToHost(): void {
    const hostUserId =
      (this.host?.userId ?? '').trim();

    if (!hostUserId) {
      return;
    }

    if (!this.authState.isLoggedIn()) {
      this.navigateToLogin();
      return;
    }

    const role =
      this.getAuthenticatedRole();

    /*
     * A missing or malformed role means that the
     * stored authentication data cannot be trusted.
     * Redirect to Login instead of building an invalid
     * route or throwing a runtime error.
     */
    if (!role) {
      this.navigateToLogin();
      return;
    }

    this.chatService.startNewThread(
      hostUserId,
    );

    void this.router.navigate(
      this.getMessagesRoute(role),
    );
  }

  private getAuthenticatedRole(): string {
    try {
      return this.authState
        .getRole()
        .trim()
        .toLowerCase();
    } catch {
      return '';
    }
  }

  private getMessagesRoute(
    role: string,
  ): string[] {
    switch (role) {
      case 'admin':
        return [
          '/admin/messages',
        ];

      case 'host':
        return [
          '/host/messages',
        ];

      case 'user':
      default:
        return [
          '/profile/messages',
        ];
    }
  }

  private navigateToLogin(): void {
    void this.router.navigate(
      ['/login'],
      {
        queryParams: {
          returnUrl:
            this.router.url,
        },
      },
    );
  }
}