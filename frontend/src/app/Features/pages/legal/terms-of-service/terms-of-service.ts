import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

interface UsageRule {
  text: string;
}

interface PaymentPoint {
  title: string;
  text: string;
}

@Component({
  selector: 'app-terms-of-service',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './terms-of-service.html',
  styleUrls: ['./terms-of-service.css']
})
export class TermsOfServiceComponent {
  lastUpdated = 'October 24, 2024';

  guestRules: UsageRule[] = [
    { text: 'Identity verification is required for all bookings.' },
    { text: 'Compliance with local noise ordinances.' },
    { text: 'Respect for host property and smart equipment.' }
  ];

  hostRules: UsageRule[] = [
    { text: 'Accurate representation of property amenities.' },
    { text: 'Maintenance of functional smart access hardware.' },
    { text: 'Adherence to safety and cleanliness standards.' }
  ];

  paymentPoints: PaymentPoint[] = [
    {
      title: 'Service Fees',
      text: 'A platform service fee is applied to each transaction to maintain our digital infrastructure and support services.'
    },
    {
      title: 'Payouts',
      text: 'Host payouts are processed within 48 hours of successful guest check-in, subject to verification.'
    },
    {
      title: 'Cancellations',
      text: 'Refunds are handled according to the specific cancellation policy selected by the host at the time of booking.'
    }
  ];

  onPrint(): void {
    window.print();
  }

  onDownloadPdf(): void {
    // Hook this up to your PDF generation / download endpoint.
    window.print();
  }
}

