import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { HostApplicationStateService } from '../../services/host-application-state.service';

const MAX_SIZE_BYTES = 5 * 1024 * 1024;
const ACCEPTED_TYPES = ['image/jpeg', 'image/png', 'image/webp'];

@Component({
  selector: 'app-step-id-document',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './step-id-document.html',
  styleUrl: './step-id-document.css',
})
export class StepIdDocumentComponent {
  state = inject(HostApplicationStateService);

  frontFile = signal<File | null>(null);
  backFile = signal<File | null>(null);
  frontPreview = signal<string | null>(null);
  backPreview = signal<string | null>(null);
  validationError = signal<string | null>(null);

  onFileSelected(event: Event, side: 'front' | 'back') {
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
    const url = URL.createObjectURL(file);
    if (side === 'front') {
      this.frontFile.set(file);
      this.frontPreview.set(url);
    } else {
      this.backFile.set(file);
      this.backPreview.set(url);
    }
  }

  async save() {
    const front = this.frontFile();
    const back = this.backFile();
    if (!front || !back) return;
    await this.state.saveNationalId(front, back);
  }
}
