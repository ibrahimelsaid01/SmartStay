export interface DashboardStats {
  activeListings: { count: number; lastAddedText: string };
  totalViews: { count: number; percentageChange: number; isPositive: boolean };
  totalReviews: { count: number; averageRating: number };
  viewsChartData: number[];
  earningsChartData: number[];
}

export interface RecentActivity {
  id: string;
  icon: string;
  message: string;
  targetName: string;
  timeAgo: string;
}

export interface BookingRequest {
  id: string;
  guestName: string;
  guestInitials: string;
  propertyName: string;
  dates: string;
}
