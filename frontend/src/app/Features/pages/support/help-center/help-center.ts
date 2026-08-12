import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

interface Category {
  icon: string;
  title: string;
  description: string;
}

interface Faq {
  question: string;
  answer: string;
  open: boolean;
}

@Component({
  selector: 'app-help-center',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './help-center.html',
  styleUrls: ['./help-center.css']
})
export class HelpCenterComponent {
  searchTerm = '';

  categories: Category[] = [
    {
      icon: '&#128241;',
      title: 'Smart Access & IoT',
      description: 'Troubleshoot smart locks, thermostats, and lighting controls.'
    },
    {
      icon: '&#128197;',
      title: 'Booking & Reservations',
      description: 'Manage your stays, extend bookings, and check-in details.'
    },
    {
      icon: '&#128250;',
      title: 'Payments & Refunds',
      description: 'Billing statements, cancellation policies, and refund status.'
    },
    {
      icon: '&#127968;',
      title: 'Hosting on Smart Stay',
      description: 'Onboarding your property, host payouts, and guest management.'
    }
  ];

  faqs: Faq[] = [
    {
      question: 'How do I change my smart lock code?',
      answer:
        'Open the Smart Stay app, go to your property\u2019s Access settings, select the lock you want to update, and tap "Change Code." Enter a new 4\u20138 digit code and confirm. The new code syncs to the lock within a few seconds as long as it has an active connection.',
      open: false
    },
    {
      question: 'What happens if I lose my internet connection?',
      answer:
        'Smart locks and thermostats continue to work locally even without internet, so you can still check in and out. Remote features like app-based unlocking may be delayed until connectivity is restored. If you\u2019re locked out, contact our 24/7 support team for a manual override.',
      open: false
    },
    {
      question: 'How do payouts work for hosts?',
      answer:
        'Payouts are processed automatically within 48 hours of a guest\u2019s successful check-in, once verification is complete. Funds are sent to the payout method on file in your Host settings, and you can track the status of each payout from your Earnings dashboard.',
      open: false
    },
    {
      question: 'Can I extend my stay after checking in?',
      answer:
        'Yes, as long as the property is available for the additional dates. Open your active booking in the app and tap "Extend Stay" to see available dates and the updated price. The host will be notified and the extension is confirmed once payment is processed.',
      open: false
    }
  ];

  guideChecklist: string[] = [
    'Optimizing your check-in experience',
    'Personalizing smart room presets',
    'Troubleshooting connectivity on the go'
  ];

  toggleFaq(faq: Faq): void {
    faq.open = !faq.open;
  }

  onSearch(): void {
    // Hook this up to your help-search endpoint / route.
    console.log('Searching for:', this.searchTerm);
  }

  onContactSupport(): void {
    // Hook this up to your support flow (e.g. route to /support or open a form).
  }
}

