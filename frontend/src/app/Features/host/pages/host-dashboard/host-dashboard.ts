import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NgApexchartsModule } from 'ng-apexcharts';
import { HostDashboardservice } from '../../services/HostDashboardservice'
@Component({
  selector: 'app-host-dashboard',
  imports: [CommonModule, NgApexchartsModule],
  templateUrl: './host-dashboard.html',
  styleUrl: './host-dashboard.css',
})
export class HostDashboard implements OnInit {
  public readonly dashboardService = inject(HostDashboardservice);

  doorUnlocked = signal<boolean>(false);
  acOn = signal<boolean>(false);
  smartControlLoading = signal<string | null>(null);

  viewsChartOptions = signal<any>(null);
  earningsChartOptions = signal<any>(null);
   cachedProfile = JSON.parse(localStorage.getItem('current-user-profile') || '{}');

   ngOnInit(): void {
  this.dashboardService.getDashboardStats().subscribe({
      next: (stats) => {
        if (stats) {
          // بنبني الـ Options كاملة هنا أول ما الـ Stats توصل
          this.viewsChartOptions.set(this.buildChartOptions(stats.viewsChartData, '#42CCCC'));
          this.earningsChartOptions.set(this.buildChartOptions(stats.earningsChartData, '#52C41a'));
        }
      }
    });
    this.dashboardService.getDashboardOverview().subscribe();
  }
private buildChartOptions(data: number[], color: string) {
    return {
      series: [{ name: 'Total', data: data }],
      chart: { type: 'area', height: 160, sparkline: { enabled: true } },
      stroke: { curve: 'smooth', width: 3 },
      colors: [color],
      fill: {
        type: 'gradient',
        gradient: { shadeIntensity: 1, opacityFrom: 0.4, opacityTo: 0.05 }
      },
      tooltip: { enabled: true }
    };
  }

  onHandleBooking(id: string, action: 'approve' | 'decline'): void {
    this.dashboardService.handleBookingRequest(id, action).subscribe({
      next: () => {
        console.log(`Booking ${id} ${action}d successfully`);
      }
    });
  }

  onToggleDevice(device: 'door' | 'ac'): void {
    this.smartControlLoading.set(device);
    const nextStatus = device === 'door' ? !this.doorUnlocked() : !this.acOn();

    this.dashboardService.toggleSmartControl('property-123', device, nextStatus).subscribe({
      next: () => {
        if (device === 'door') this.doorUnlocked.set(nextStatus);
        if (device === 'ac') this.acOn.set(nextStatus);
        this.smartControlLoading.set(null);
      },
      error: () => this.smartControlLoading.set(null)
    });
  }

  getChartOptions(data: number[], color: string) {
    return {
      series: [{ name: 'Data', data: data }],
      chart: { type: 'area', height: 160, sparkline: { enabled: true } },
      stroke: { curve: 'smooth', width: 3 },
      colors: [color],
      fill: {
        type: 'gradient',
        gradient: { shadeIntensity: 1, opacityFrom: 0.4, opacityTo: 0.05 }
      },
      tooltip: { enabled: true }
    };
  }
}
