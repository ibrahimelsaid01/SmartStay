import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';
import { HostApplicationStep } from '../../models/host-application.models';

interface StepDef {
  step: HostApplicationStep;
  label: string;
}

@Component({
  selector: 'app-host-stepper',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './stepper.html',
  styleUrl: './stepper.css',
})
export class StepperComponent {
  @Input() currentStep: HostApplicationStep = HostApplicationStep.Information;

  readonly steps: StepDef[] = [
    { step: HostApplicationStep.Information, label: 'Information' },
    { step: HostApplicationStep.ProfileImage, label: 'Profile Image' },
    { step: HostApplicationStep.IdDocument, label: 'ID document' },
    { step: HostApplicationStep.Review, label: 'Review & submit' },
  ];
}
