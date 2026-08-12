import { Component } from '@angular/core';
import { UserProfileService } from '../../../profile/services/user-profile-service';
import {RouterLink} from '@angular/router';
@Component({
  selector: 'app-how-it-works',
  imports: [RouterLink],
  templateUrl: './how-it-works.html',
  styleUrl: './how-it-works.css',
})
export class HowItWorks {
  constructor(private profileService: UserProfileService) {}

  isHost(): boolean {
  const user = this.profileService.currentUser();
  return !!(user && user.roles && user.roles.includes('Host'));
  }
 isRegularUser(): boolean {
  const user = this.profileService.currentUser();
  if (!user || !user.roles) return true;
  return !user.roles.includes('Host') && !user.roles.includes('Admin');
}
}

