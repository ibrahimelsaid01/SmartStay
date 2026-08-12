import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
@Injectable({
  providedIn: 'root',
})
export class City {
  private apiUrl = 'https://api.publichint.io/api/cities';


  private mockCities = [
    { id: 1, name: 'Cairo', propertiesCount: 25, image: '/Images/cairo.jpg' },
    { id: 2, name: 'Alexandria', propertiesCount: 18, image: '/Images/alex.jpg' },
    { id: 3, name: 'Giza', propertiesCount: 14, image: '/Images/giza.jpg' },
    { id: 4, name: 'North Coast', propertiesCount: 20, image: '/Images/sokhna.jpg' },
    { id: 5, name: 'Aswan', propertiesCount: 9, image: '/Images/aswan.jpg' }
  ];

  constructor(private http: HttpClient) { }

  getCities(): Observable<any[]> {
    return of(this.mockCities)
    // return this.http.get<any[]>(this.apiUrl);
}
}
