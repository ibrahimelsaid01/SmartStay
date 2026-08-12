import { Component, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';

@Component({
  selector: 'app-search-bar',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './search-bar.html',
  styleUrl: './search-bar.css',
})
export class SearchBar {
  constructor(
    private cdr: ChangeDetectorRef,
    private router: Router
  ) {}

  guests = 1;

  searchError = '';

  recentSearches = [
    {
      id: 1,
      name: 'Cairo, Egypt',
      dates: 'Popular destination',
    },
  ];

  suggestedDestinations = [
    {
      id: 1,
      name: 'Cairo, Egypt',
      description: 'Modern stays and verified apartments',
      icon: 'ti-building-skyscraper',
      iconBg: '#EFF6FF',
      iconColor: '#3b82f6',
    },
    {
      id: 2,
      name: 'Alexandria, Egypt',
      description: 'Sea view stays and coastal apartments',
      icon: 'ti-waves',
      iconBg: '#ECFEFF',
      iconColor: '#0891b2',
    },
    {
      id: 3,
      name: 'Giza, Egypt',
      description: 'Comfortable stays near key destinations',
      icon: 'ti-building',
      iconBg: '#FFFBEB',
      iconColor: '#d97706',
    },
    {
      id: 4,
      name: 'North Coast, Egypt',
      description: 'Beach stays and summer trips',
      icon: 'ti-sailboat',
      iconBg: '#FFF1F2',
      iconColor: '#e11d48',
    },
    {
      id: 5,
      name: 'Aswan, Egypt',
      description: 'Calm Nile stays and cultural trips',
      icon: 'ti-map-pin',
      iconBg: '#F0FDFA',
      iconColor: '#0f766e',
    },
  ];

  showWhere = false;
  showWhen = false;

  selectedDestination = '';
  selectedDatesLabel = '';

  selectedStart: Date | null = null;
  selectedEnd: Date | null = null;
  hoverDate: Date | null = null;

  currentMonth = new Date(
    new Date().getFullYear(),
    new Date().getMonth(),
    1
  );

  nextMonth = new Date(
    new Date().getFullYear(),
    new Date().getMonth() + 1,
    1
  );

  dayHeaders = ['S', 'M', 'T', 'W', 'T', 'F', 'S'];

  monthNames = [
    'January',
    'February',
    'March',
    'April',
    'May',
    'June',
    'July',
    'August',
    'September',
    'October',
    'November',
    'December',
  ];

  toggleWhere(): void {
    this.searchError = '';
    this.showWhere = !this.showWhere;
    this.showWhen = false;
  }

  selectDestination(name: string): void {
    this.searchError = '';
    this.selectedDestination = name;
    this.showWhere = false;
    this.showWhen = true;
  }

  toggleWhen(): void {
    this.searchError = '';
    this.showWhen = !this.showWhen;
    this.showWhere = false;
  }

  increaseGuests(): void {
    this.guests++;
  }

  decreaseGuests(): void {
    if (this.guests > 1) {
      this.guests--;
    }
  }

  getMonthLabel(date: Date): string {
    return `${this.monthNames[date.getMonth()]} ${date.getFullYear()}`;
  }

  getDaysInMonth(date: Date): (Date | null)[] {
    const year = date.getFullYear();
    const month = date.getMonth();

    const firstDay = new Date(year, month, 1).getDay();
    const daysInMonth = new Date(year, month + 1, 0).getDate();

    const cells: (Date | null)[] = Array(firstDay).fill(null);

    for (let day = 1; day <= daysInMonth; day++) {
      cells.push(new Date(year, month, day));
    }

    return cells;
  }

  prevMonth(): void {
    const year = this.currentMonth.getFullYear();
    const month = this.currentMonth.getMonth();

    this.currentMonth = new Date(year, month - 1, 1);
    this.nextMonth = new Date(year, month, 1);

    this.cdr.detectChanges();
  }

  nextMonthNav(): void {
    const year = this.currentMonth.getFullYear();
    const month = this.currentMonth.getMonth();

    this.currentMonth = new Date(year, month + 1, 1);
    this.nextMonth = new Date(year, month + 2, 1);

    this.cdr.detectChanges();
  }

  isPast(date: Date): boolean {
    const selectedDate = new Date(date);
    selectedDate.setHours(0, 0, 0, 0);

    const today = new Date();
    today.setHours(0, 0, 0, 0);

    return selectedDate < today;
  }

  isSameDay(firstDate: Date, secondDate: Date): boolean {
    return (
      firstDate.getFullYear() === secondDate.getFullYear() &&
      firstDate.getMonth() === secondDate.getMonth() &&
      firstDate.getDate() === secondDate.getDate()
    );
  }

  isSelected(date: Date): boolean {
    return (
      (!!this.selectedStart && this.isSameDay(date, this.selectedStart)) ||
      (!!this.selectedEnd && this.isSameDay(date, this.selectedEnd))
    );
  }

  isInRange(date: Date): boolean {
    const endDate = this.selectedEnd || this.hoverDate;

    if (!this.selectedStart || !endDate) {
      return false;
    }

    const start = this.selectedStart < endDate ? this.selectedStart : endDate;
    const end = this.selectedStart < endDate ? endDate : this.selectedStart;

    return date > start && date < end;
  }

  isRangeStart(date: Date): boolean {
    return !!this.selectedStart && this.isSameDay(date, this.selectedStart);
  }

  isRangeEnd(date: Date): boolean {
    return !!this.selectedEnd && this.isSameDay(date, this.selectedEnd);
  }

  onDateClick(date: Date): void {
    if (this.isPast(date)) {
      return;
    }

    this.searchError = '';

    if (!this.selectedStart || this.selectedEnd) {
      this.selectedStart = date;
      this.selectedEnd = null;
      this.selectedDatesLabel = this.formatDate(this.selectedStart);
    } else {
      if (date < this.selectedStart) {
        this.selectedEnd = this.selectedStart;
        this.selectedStart = date;
      } else {
        this.selectedEnd = date;
      }

      this.selectedDatesLabel = `${this.formatDate(this.selectedStart)} – ${this.formatDate(this.selectedEnd)}`;
      this.showWhen = false;
    }

    this.cdr.detectChanges();
  }

  onDateHover(date: Date): void {
    if (this.selectedStart && !this.selectedEnd) {
      this.hoverDate = date;
      this.cdr.detectChanges();
    }
  }

  formatDate(date: Date): string {
    const months = [
      'Jan',
      'Feb',
      'Mar',
      'Apr',
      'May',
      'Jun',
      'Jul',
      'Aug',
      'Sep',
      'Oct',
      'Nov',
      'Dec',
    ];

    return `${months[date.getMonth()]} ${date.getDate()}`;
  }

  onSearch(): void {
    this.searchError = '';

    if (this.selectedStart && !this.selectedEnd) {
      this.searchError = 'Please select a check-out date.';
      this.cdr.detectChanges();
      return;
    }

    const location = this.getDestinationQueryValue();

    const queryParams: Record<string, string | number> = {
      guests: this.guests,
    };

    if (location) {
      queryParams['location'] = location;
    }

    if (this.selectedStart && this.selectedEnd) {
      queryParams['checkIn'] = this.toDateOnly(this.selectedStart);
      queryParams['checkOut'] = this.toDateOnly(this.selectedEnd);
    }

    this.router.navigate(['/all-stays'], {
      queryParams,
    });
  }

  private getDestinationQueryValue(): string {
    return this.selectedDestination
      .replace(', Egypt', '')
      .replace('Egypt', '')
      .trim();
  }

  private toDateOnly(date: Date): string {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');

    return `${year}-${month}-${day}`;
  }
}