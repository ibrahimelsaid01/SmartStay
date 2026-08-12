import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

interface SafetyCard {
  icon: string;
  title: string;
  points: string[];
}

interface TransparencyItem {
  icon: string;
  title: string;
  text: string;
  variant: 'warning' | 'info';
}

interface TrustPartner {
  icon: string;
  name: string;
}

@Component({
  selector: 'app-safety',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './safety-information.html',
  styleUrls: ['./safety-information.css']
})
export class SafetyInformationComponent {
  safetyCards: SafetyCard[] = [
    {
      icon: '&#128737;',
      title: 'Guest Safety',
      points: [
        'Strict host identity verification and background screening.',
        'Secure smart-lock entry with unique, time-sensitive access codes.',
        '24/7 dedicated global support for any safety or access issues.'
      ]
    },
    {
      icon: '&#128737;',
      title: 'Host Protection',
      points: [
        'Guest identity matching and multi-factor authentication.',
        'Comprehensive property damage protection up to $1M.',
        'Smart noise monitoring to prevent unauthorized gatherings.'
      ]
    },
    {
      icon: '&#128274;',
      title: 'Data Privacy',
      points: [
        'AES-256 bit encryption for all financial transactions.',
        'Secure handling and limited retention of personal data.',
        'Transparent privacy controls and GDPR/CCPA compliance.'
      ]
    }
  ];

  transparencyItems: TransparencyItem[] = [
    {
      icon: '&#128247;',
      title: 'No Indoor Cameras',
      text: 'Internal cameras are strictly prohibited in any private spaces including bedrooms, bathrooms, and common living areas.',
      variant: 'warning'
    },
    {
      icon: '&#128065;',
      title: 'Mandatory Disclosure',
      text: 'Hosts must explicitly list the location and purpose of any external security cameras or noise level sensors in the listing details.',
      variant: 'info'
    }
  ];

  trustPartners: TrustPartner[] = [
    { icon: '&#9989;', name: 'Verified Identity' },
    { icon: '&#128274;', name: 'Secure Payments' },
    { icon: '&#127974;', name: 'ISO 27001' },
    { icon: '&#128737;', name: 'Travelers Protect' }
  ];

  onReportSafetyConcern(): void {
    // Hook this up to your safety-report flow.
  }

  onContactSupport(): void {
    // Hook this up to your 24/7 support flow.
  }
}
