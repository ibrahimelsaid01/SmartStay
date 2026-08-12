import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, RouterLink } from '@angular/router';
@Component({
  selector: 'app-footer',
  standalone: true,
  imports: [CommonModule,RouterModule, RouterLink ],
  templateUrl: './footer.html',
  styleUrls: ['./footer.css']
})
export class Footer {}
