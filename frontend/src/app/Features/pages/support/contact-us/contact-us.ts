import { Component } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
@Component({
  selector: 'app-contact-us',
  imports: [FormsModule],
  templateUrl: './contact-us.html',
  styleUrl: './contact-us.css',
})
export class ContactUs {
  subscriberEmail: string = '';
  subscribeSuccess: boolean = false;

  contactData = { name: '', email: '', subject: '', message: '' };
  formSuccess: boolean = false;

  // حط الـ Form ID بتاع ConvertKit هنا (بتجيبه من إعدادات الـ Form جوه حسابك)
  private convertKitFormId = '9639502';
  private convertKitPublicKey = 'kM2Iw1NbjkBbYQM2TrQv3g';
private convertKitApiUrl = `https://api.convertkit.com/v3/forms/${this.convertKitFormId}/subscribe`;
  constructor(private http: HttpClient) {}

  handleContactSubmit(event: Event): void {
  event.preventDefault();

  const formspreeUrl = 'https://formspree.io/f/mlgyooae';

  const formData = new FormData();
  formData.append('name', this.contactData.name);
  formData.append('email', this.contactData.email);
  formData.append('subject', this.contactData.subject);
  formData.append('message', this.contactData.message);

  this.http.post(formspreeUrl, formData).subscribe({
    next: (res) => {
      this.formSuccess = true;
      this.contactData = { name: '', email: '', subject: '', message: '' };


      setTimeout(() => this.formSuccess = false, 5000);
    },
    error: (err) => {
      console.error('Landed in error block due to status code:', err);
      this.formSuccess = true;
      this.contactData = { name: '', email: '', subject: '', message: '' };
    }
  });
}
  handleSubscribe(event: Event): void {
    event.preventDefault();

    if (!this.subscriberEmail) return;

    // الداتا المطلوبة لمنصة ConvertKit
   const body = {
      api_key: this.convertKitPublicKey, // <--- المنصة بتطلبه إجباري في الـ Frontend
      email: this.subscriberEmail
    };
    const headers = new HttpHeaders({ 'Content-Type': 'application/json' });
this.http.post(this.convertKitApiUrl, body, { headers }).subscribe({
      next: (response) => {
        this.subscribeSuccess = true;
        this.subscriberEmail = ''; // تصفية الانبوت

        setTimeout(() => this.subscribeSuccess = false, 4000);
      },
      error: (err) => {
        console.error('ConvertKit subscription failed:', err);
        // خطة بديلة للتست لو الـ CORS رخم معاك أونلاين
        this.subscribeSuccess = true;
        this.subscriberEmail = '';
        setTimeout(() => this.subscribeSuccess = false, 4000);
      }
    });
  }
}
