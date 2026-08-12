import { Component,Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
@Component({
  selector: 'app-city-card',
  imports: [CommonModule,RouterLink],
  templateUrl: './city-card.html',
  styleUrl: './city-card.css',
})
export class CityCard {
@Input() cityData: any;

}
