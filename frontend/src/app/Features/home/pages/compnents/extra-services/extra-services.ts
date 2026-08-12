import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-extra-services',
  imports: [],
  templateUrl: './extra-services.html',
  styleUrl: './extra-services.css',
})
export class ExtraServices {
  @Input() services = [
    { name: 'Laundry Service', price: '180EGP', image: 'Images/laundry.jpg' },
    { name: 'Guided City Tour', price: '150EGP', image: 'Images/tour.jpg' },
    { name: 'Extra Cleaning', price: '200EGP', image: 'Images/cleaning.jpg' },
    { name: 'Cooked Meals', price: '400EGP', image: 'Images/meals.jpg' }
  ];
}
