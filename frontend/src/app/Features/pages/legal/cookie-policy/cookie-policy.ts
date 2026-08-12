import { Component } from '@angular/core';
import html2pdf from 'html2pdf.js';
@Component({
  selector: 'app-cookie-policy',
  imports: [],
  templateUrl: './cookie-policy.html',
  styleUrl: './cookie-policy.css',
})
export class CookiePolicy {
  printPage(): void {
  window.print();
}
downloadPDF(): void {
  const element = document.getElementById('pdf-content');

  if (!element) {
    return;
  }

  const options = {
    margin:       0.5,
    filename:     'Smart-Stay-Cookie-Policy.pdf',
    image:        { type: 'jpeg' as const, quality: 0.98 },
    html2canvas:  { scale: 2 },
    jsPDF:        { unit: 'in', format: 'letter', orientation: 'portrait' as const }
  };

  html2pdf().from(element).set(options).save();
}
saveCookiePreferences(): void {
  const analyticsAllowed = (document.getElementById('analyticsCookies') as HTMLInputElement)?.checked;
  const marketingAllowed = (document.getElementById('marketingCookies') as HTMLInputElement)?.checked;

  localStorage.setItem('cookie_analytics_allowed', String(analyticsAllowed));
  localStorage.setItem('cookie_marketing_allowed', String(marketingAllowed));

}
}
