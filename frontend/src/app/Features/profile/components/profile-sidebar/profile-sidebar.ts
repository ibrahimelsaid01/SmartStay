import { CommonModule } from '@angular/common';
import { DestroyRef, Component, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import {
  UserProfile,
  UserProfileService,
} from '../../services/user-profile-service';

@Component({
  selector: 'app-profile-sidebar',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive],
  templateUrl: './profile-sidebar.html',
  styleUrl: './profile-sidebar.css',
})
export class ProfileSidebar implements OnInit {
  user: UserProfile | null = null;
  isBookingsOpen = false;
  isCollapsed = false;

  private readonly destroyRef = inject(DestroyRef);

  constructor(
    private readonly userProfileService: UserProfileService,
    public readonly router: Router,
  ) {}

  ngOnInit(): void {
    this.isBookingsOpen = this.isBookingsRoute;

    this.userProfileService.currentUser$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((data) => {
        this.user = data;
      });
  }

  get isAdminMenu(): boolean {
    return this.router.url.startsWith('/admin');
  }

  get isHostMenu(): boolean {
    return this.router.url.startsWith('/host');
  }

  get isProfileMenu(): boolean {
    return !this.isAdminMenu && !this.isHostMenu;
  }

  get isBookingsRoute(): boolean {
    return this.router.url.startsWith('/profile/bookings');
  }

  get currentRole(): string {
    if (this.hasRole('Admin')) {
      return 'Admin';
    }

    if (this.hasRole('Host')) {
      return 'Host';
    }

    return 'User';
  }

  hasRole(role: string): boolean {
    return !!this.user?.roles?.some(
      (userRole) => userRole.toLowerCase() === role.toLowerCase(),
    );
  }

  toggleBookings(): void {
    this.isBookingsOpen = !this.isBookingsOpen;
  }

  toggleSidebar(): void {
    this.isCollapsed = !this.isCollapsed;

    if (this.isCollapsed) {
      this.isBookingsOpen = false;
    }
  }

  closeMobileSidebar(): void {
    if (typeof document === 'undefined') {
      return;
    }

    const openedDrawer = document.querySelector<HTMLElement>('.offcanvas.show');
    const closeButton = openedDrawer?.querySelector<HTMLButtonElement>(
      '[data-bs-dismiss="offcanvas"]',
    );

    closeButton?.click();
  }

  logout(): void {
    this.userProfileService.clearAuth();
    this.router.navigate(['/']);
  }
}