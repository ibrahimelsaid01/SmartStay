import { Component, OnInit } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Navbar } from '../../../Shared/components/navbar/navbar';
import { Footer } from '../../../Shared/components/footer/footer';

@Component({
  selector: 'app-main-layout',
  standalone: true,
  imports: [RouterOutlet,Navbar, Footer ],
  templateUrl: './main-layout.html',
  styleUrl: './main-layout.css'
})
export class MainLayout implements OnInit {
  showCookieBanner: boolean = false;

  ngOnInit(): void {
    this.checkCookieConsent();
  }

  checkCookieConsent(): void {
    const consent = localStorage.getItem('cookie_consent_accepted');
    if (!consent) {
      this.showCookieBanner = true;
    }
  }

  acceptAllCookies(): void {
    localStorage.setItem('cookie_consent_accepted', 'true');
    localStorage.setItem('cookie_analytics_allowed', 'true');
    localStorage.setItem('cookie_marketing_allowed', 'true');
    this.showCookieBanner = false;
  }
  saveCookiePreferences(): void {
  const analyticsAllowed = (document.getElementById('layoutAnalyticsCookies') as HTMLInputElement)?.checked;
  const marketingAllowed = (document.getElementById('layoutMarketingCookies') as HTMLInputElement)?.checked;

  localStorage.setItem('cookie_consent_accepted', 'true');
  localStorage.setItem('cookie_analytics_allowed', String(analyticsAllowed));
  localStorage.setItem('cookie_marketing_allowed', String(marketingAllowed));

  // نقفل البانر بعد ما يختار ويحفظ
  this.showCookieBanner = false;
  console.log('Saved from Layout:', { analyticsAllowed, marketingAllowed });
}
}
