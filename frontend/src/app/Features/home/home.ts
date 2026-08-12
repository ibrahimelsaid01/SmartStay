import { Component, OnInit, ChangeDetectorRef } from '@angular/core'
import { SearchBar } from './components/search-bar/search-bar';
import { HomeCards } from './components/home-cards/home-cards';
import { Properties } from './services/properties'
import { CommonModule } from '@angular/common'
import { StayCard } from './components/stay-card/stay-card'
import { Reviews } from './components/reviews/reviews';
import { Router } from '@angular/router';



import { RouterModule, RouterLink } from '@angular/router';
import {CityCard} from './components/city-card/city-card'
import {City} from './services/city'

@Component({
  standalone: true,
  selector: 'app-home',
  imports: [CommonModule,SearchBar,HomeCards, StayCard, CityCard, RouterModule, RouterLink, Reviews],
  templateUrl: './home.html',
  styleUrl: './home.css',
})
export class Home implements OnInit {
  //-------------------------cities logic start-------------------------
  citiesList: any[] = []
//-------------------------stays logic start-------------------------
  staysList: any[] = []

  constructor(private stayService: Properties,private cdr: ChangeDetectorRef,private router: Router,private cityService: City) {}

ngOnInit(): void {
  this.stayService.getPopularStays().subscribe({
    next: (data) => {
      console.log('Stays received:', data);
      // الترتيب ثم الحفظ
      this.staysList = data.sort((a, b) => (b.averageRating ?? 0) - (a.averageRating ?? 0));

      // 🚨 الحل السحري: اجبر الـ Angular يراجع الصفحة ويرسم الكروت حالاً
      this.cdr.detectChanges();
    },
    error: (err) => console.error('Error fetching stays', err)
  });

  this.cityService.getCities().subscribe({
    next: (data) => {
      this.citiesList = data;
      this.cdr.detectChanges(); // برضه لتأمين المدن
    },
    error: (err) => console.error('Error fetching cities', err)
  });
}



goToLogin() {
  this.router.navigate(['/login']);
}
  //-------------------------stays logic End-------------------------
}
