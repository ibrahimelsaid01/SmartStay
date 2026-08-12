import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { HostApplicationStateService } from '../../services/host-application-state.service';
import { HostApplicationStep } from '../../models/host-application.models';

import { StepperComponent } from '../../components/stepper/stepper';
import { StepInformationComponent } from '../step-information/step-information';
import { StepProfileImageComponent } from '../step-profile-image/step-profile-image';
import { StepIdDocumentComponent } from '../step-id-document/step-id-document';
import { StepReviewComponent } from '../step-review/step-review';
import { ApplicationSubmittedComponent } from '../application-submitted/application-submitted';
import { ApplicationRejectedComponent } from '../application-rejected/application-rejected';
import { ApplicationApprovedComponent } from '../application-approved/application-approved';

@Component({
  selector: 'app-host-application',
  standalone: true,
  imports: [
    CommonModule,
    StepperComponent,
    StepInformationComponent,
    StepProfileImageComponent,
    StepIdDocumentComponent,
    StepReviewComponent,
    ApplicationSubmittedComponent,
    ApplicationRejectedComponent,
    ApplicationApprovedComponent,
  ],
  templateUrl: './host-application.html',
  styleUrl: './host-application.css',
})
export class HostApplicationComponent implements OnInit {
  state = inject(HostApplicationStateService);
  Step = HostApplicationStep;

  ngOnInit(): void {
    this.state.init();
  }
}
