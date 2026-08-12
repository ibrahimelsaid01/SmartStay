import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';

import { HostApplicationStateService } from '../../services/host-application-state.service';

@Component({
  selector: 'app-application-approved',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './application-approved.html',
  styleUrl: './application-approved.css',
})
export class ApplicationApprovedComponent {
  readonly state = inject(HostApplicationStateService);

  private readonly router = inject(Router);

  async goToHostDashboard(): Promise<void> {
    const hostSessionIsReady =
      await this.state.ensureApprovedHostSession();

    if (!hostSessionIsReady) {
      return;
    }

    await this.router.navigateByUrl('/host/dashboard', {
      replaceUrl: true,
    });
  }
}