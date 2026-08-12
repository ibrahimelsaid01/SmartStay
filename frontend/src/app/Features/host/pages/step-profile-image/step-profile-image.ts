import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { HostApplicationStateService } from '../../services/host-application-state.service';

const MAX_SIZE_BYTES = 5 * 1024 * 1024; // 5MB
const ACCEPTED_TYPES = ['image/jpeg', 'image/png', 'image/webp'];

@Component({
  selector: 'app-step-profile-image',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './step-profile-image.html',
  styleUrl: './step-profile-image.css',
})
export class StepProfileImageComponent {
  state = inject(HostApplicationStateService);

  selectedFile = signal<File | null>(null);
  previewUrl = signal<string | null>(null);
  validationError = signal<string | null>(null);

  onFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    if (!ACCEPTED_TYPES.includes(file.type)) {
      this.validationError.set('Only JPG, PNG or WebP files are allowed.');
      return;
    }
    if (file.size > MAX_SIZE_BYTES) {
      this.validationError.set('File size must not exceed 5MB.');
      return;
    }

    this.validationError.set(null);
    this.selectedFile.set(file);
    this.previewUrl.set(URL.createObjectURL(file));
  }

  async save() {
    const file = this.selectedFile();
    if (!file) return;
    await this.state.saveProfileImage(file);
  }
}
