import { Component } from '@angular/core';
import { RouterOutlet } from "@angular/router";
import{ ProfileSidebar } from './components/profile-sidebar/profile-sidebar'
import { CommonModule } from '@angular/common'
@Component({
  selector: 'app-profile',
  imports: [RouterOutlet, ProfileSidebar, CommonModule],
  templateUrl: './profile.html',
  styleUrl: './profile.css',
})
export class Profile {
  isDesktopCollapsed = false;

  toggleDesktopSidebar() {
    this.isDesktopCollapsed = !this.isDesktopCollapsed;
  }
}
