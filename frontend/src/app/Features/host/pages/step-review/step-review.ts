import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { HostApplicationStateService } from '../../services/host-application-state.service';

@Component({
  selector: 'app-step-review',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './step-review.html',
  styleUrl: './step-review.css',
})
export class StepReviewComponent {
  state = inject(HostApplicationStateService);

  async onSubmit() {
    await this.state.submit();
  }
}
