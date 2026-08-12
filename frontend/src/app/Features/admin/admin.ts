import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { ProfileSidebar } from '../profile/components/profile-sidebar/profile-sidebar';

@Component({
  selector: 'app-admin',
  standalone: true,
  imports: [CommonModule, RouterModule, ProfileSidebar],
  templateUrl: './admin.html',
  styleUrl: './admin.css',
})
export class AdminLayout {}