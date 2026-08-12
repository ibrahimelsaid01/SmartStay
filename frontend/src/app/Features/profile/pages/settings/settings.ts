
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { SettingsService } from '../../services/settings';
import { Component, ChangeDetectorRef } from '@angular/core';
@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './settings.html',
  styleUrl: './settings.css',
})
export class Settings {
  // Currency & Language (local)
  selectedCurrency = localStorage.getItem('currency') || 'USD';
  selectedLanguage = localStorage.getItem('language') || 'en-US';

  // Email Change
  newEmail = '';
  otpCode = '';
  emailStep = 1;
  emailLoading = false;
  emailMessage = '';

  // Delete Account
  deleteLoading = false;
  showDeleteConfirm = false;

  currencies = [
    { value: 'USD', label: '$ U.S. dollar' },
    { value: 'EUR', label: '€ Euro' },
    { value: 'EGP', label: 'E£ Egyptian Pound' },
    { value: 'GBP', label: '£ British Pound' },
  ];

  languages = [
    { value: 'en-US', label: '🇺🇸 English (US)' },
    { value: 'de', label: '🇩🇪 Deutsch' },
    { value: 'fr', label: '🇫🇷 Français' },
    { value: 'es', label: '🇪🇸 Español' },
    { value: 'it', label: '🇮🇹 Italiano' },
  ];

constructor(private settingsService: SettingsService, private router: Router, private cdr: ChangeDetectorRef) {}

  saveCurrency() {
    localStorage.setItem('currency', this.selectedCurrency);
  }

  saveLanguage() {
    localStorage.setItem('language', this.selectedLanguage);
  }

 requestEmailChange() {
  if (!this.newEmail) return;
  this.emailLoading = true;
  this.settingsService.requestEmailChange(this.newEmail).subscribe({
    next: () => {
      this.emailLoading = false;
      this.emailStep = 2;
      this.emailMessage = 'OTP sent to your new email!';
      this.cdr.detectChanges();
    },
    error: () => {
      this.emailLoading = false;
      this.emailMessage = 'Something went wrong. Please try again.';
      this.cdr.detectChanges();
    }
  });
}

  confirmEmailChange() {
    if (!this.otpCode) return;
    this.emailLoading = true;
    this.settingsService.confirmEmailChange(this.newEmail, this.otpCode).subscribe({
      next: () => {
        this.emailLoading = false;
        this.emailMessage = 'Email changed successfully!';
        this.emailStep = 1;
        this.newEmail = '';
        this.otpCode = '';
      },
      error: () => {
        this.emailLoading = false;
        this.emailMessage = 'Invalid OTP. Please try again.';
      }
    });
  }

  deleteAccount() {
    this.deleteLoading = true;
    this.settingsService.deleteAccount().subscribe({
      next: () => {
        localStorage.clear();
        this.router.navigate(['/']);
      },
      error: () => {
        this.deleteLoading = false;
      }
    });
  }
}