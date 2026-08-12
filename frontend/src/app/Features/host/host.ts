import { Component } from '@angular/core';
import {RouterOutlet } from '@angular/router';
import { ProfileSidebar } from "../profile/components/profile-sidebar/profile-sidebar";

@Component({
  selector: 'app-host',
  imports: [RouterOutlet, ProfileSidebar],
  templateUrl: './host.html',
  styleUrl: './host.css',
})
export class Host {}
