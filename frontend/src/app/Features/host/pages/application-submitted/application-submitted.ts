import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';

@Component({
  selector: 'app-application-submitted',
  standalone: true,
  imports: [],
  templateUrl: './application-submitted.html',
  styleUrl: './application-submitted.css',
})
export class ApplicationSubmittedComponent {
  private router = inject(Router);

  goToDashboard() {
    this.router.navigateByUrl('/dashboard');
  }
}
