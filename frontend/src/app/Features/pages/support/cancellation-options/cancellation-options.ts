import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-cancellation-options',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './cancellation-options.html',
  styleUrl: './cancellation-options.css',
})
export class CancellationOptions {
  openQuestion: number | null = null;

  questions = [
    {
      id: 1,
      q: 'What if my flight is cancelled or delayed?',
      a: 'If your flight is cancelled or delayed, contact us immediately. We will work with the host to accommodate your situation based on the property\'s policy.'
    },
    {
      id: 2,
      q: 'How long do refunds take to process?',
      a: 'Refunds are processed within 5-10 business days depending on your payment method and bank.'
    },
    {
      id: 3,
      q: 'Can I change my dates instead of cancelling?',
      a: 'Yes! Date changes are subject to availability and host approval. Contact support to request a date modification.'
    },
    {
      id: 4,
      q: 'Are cleaning fees refunded?',
      a: 'Cleaning fees are fully refunded if you cancel before check-in, regardless of the cancellation policy.'
    },
  ];

  toggle(id: number) {
    this.openQuestion = this.openQuestion === id ? null : id;
  }
}