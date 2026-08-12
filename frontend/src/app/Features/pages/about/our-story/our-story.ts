import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { AuthState } from '../../../auth/services/auth-state';

@Component({
  selector: 'app-our-story',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './our-story.html',
  styleUrl: './our-story.css',
})
export class OurStory {
  constructor(private router: Router, private authState: AuthState) {}

  bookStay() {
    if (this.authState.isLoggedIn()) {
      this.router.navigate(['/']);
    } else {
      this.router.navigate(['/login']);
    }
  }
}