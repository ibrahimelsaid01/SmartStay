import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PropertyImage } from '../../../services/propertydetailservice'
@Component({
  selector: 'app-property-gallery',
  imports: [],
  templateUrl: './property-gallery.html',
  styleUrl: './property-gallery.css',
})
export class PropertyGallery {
  @Input({ required: true }) images: PropertyImage[] = [];
}
