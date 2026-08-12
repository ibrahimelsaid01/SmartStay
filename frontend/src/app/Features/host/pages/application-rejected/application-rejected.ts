import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { HostApplicationStateService } from '../../services/host-application-state.service';

@Component({
  selector: 'app-application-rejected',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './application-rejected.html',
  styleUrl: './application-rejected.css',
})
export class ApplicationRejectedComponent {
  state = inject(HostApplicationStateService);

  editApplication() {
    this.state.restartFromRejection();
  }
}
