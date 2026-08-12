import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { HostApplicationStateService } from '../../services/host-application-state.service';

@Component({
  selector: 'app-step-information',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './step-information.html',
  styleUrl: './step-information.css',
})
export class StepInformationComponent {
  state = inject(HostApplicationStateService);
  private fb = inject(FormBuilder);
  private router = inject(Router);

  form = this.fb.nonNullable.group({
    displayName: ['', Validators.required],
    bio: ['', [Validators.required, Validators.maxLength(500)]],
    country: ['', Validators.required],
    city: ['', Validators.required],
    phoneNumber: ['', Validators.required],
  });

  constructor() {
    const app = this.state.application();
    if (app) {
      this.form.patchValue({
        displayName: app.displayName,
        bio: app.bio,
        country: app.country,
        city: app.city,
        phoneNumber: app.phoneNumber,
      });
    }
  }

  async onSubmit() {
    if (this.form.invalid) return;
    await this.state.saveBasicInfo(this.form.getRawValue());
  }

  cancel() {
    this.router.navigate(['/']);
  }
}
