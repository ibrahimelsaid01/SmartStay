import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';

/**
 * Exact public Host payload returned inside
 * PublicPropertyDetailsResponse by the Backend.
 */
export interface PublicPropertyHostResponse {
  userId: string;
  firstName: string;
  fullName: string;
  displayName: string;
  bio: string;
  country: string;
  city: string;
  profileImageUrl: string | null;
}

/**
 * Kept as a compatibility alias because the existing Host Profile
 * component imports Host from this service.
 */
export type Host = PublicPropertyHostResponse;

export interface PublicPropertyImageResponse {
  id: string;
  url: string;
  isCover: boolean;
  displayOrder: number;
}

/**
 * Kept as a compatibility alias for the Property Gallery.
 */
export type PropertyImage = PublicPropertyImageResponse;

export interface PublicPropertyAmenityResponse {
  id: string;
  code: string;
  name: string;
  category: string;
  iconKey: string | null;
  displayOrder: number;
}

/**
 * Kept as a compatibility alias for the Property Details page.
 */
export type Amenity = PublicPropertyAmenityResponse;

/**
 * Exact response returned by:
 * GET /api/properties/{propertyId}
 */
export interface PublicPropertyDetailsResponse {
  id: string;
  title: string;
  description: string;
  propertyType: string;
  spaceType: string;

  country: string;
  city: string;
  streetAddress: string;
  postalCode: string | null;
  latitude: number | null;
  longitude: number | null;
  fullAddress: string;

  maxGuests: number;
  bedrooms: number;
  beds: number;
  bathrooms: number;

  pricePerNight: number;
  currency: string;

  averageRating: number;
  reviewsCount: number;

  checkInTime: string;
  checkOutTime: string;
  cancellationPolicy: string;

  allowsSmoking: boolean;
  allowsPets: boolean;
  allowsParties: boolean;
  allowsChildren: boolean;
  additionalHouseRules: string | null;

  host: PublicPropertyHostResponse;
  images: PublicPropertyImageResponse[];
  amenities: PublicPropertyAmenityResponse[];

  publishedAt: string | null;
}

/**
 * Existing components import PropertyDetails, so retain that public
 * name while keeping the API response definition explicit.
 */
export type PropertyDetails = PublicPropertyDetailsResponse;

/**
 * Frontend-only view models. They are deliberately not included in
 * PublicPropertyDetailsResponse because the current Backend endpoint
 * does not return extra services, discount data, or a map image.
 */
export interface ExtraService {
  id: string;
  title: string;
  description?: string;
  imageUrl: string;
  price: number;
}

export interface Discount {
  code: string;
  label: string;
  details?: string;
}

export interface LocationInfo {
  address?: string;
  mapImageUrl?: string;
}

@Injectable({
  providedIn: 'root',
})
export class Propertydetailservice {
  private readonly http = inject(HttpClient);

  private readonly apiUrl =
    `${environment.baseApi}/api/properties`;

  getPropertyById(
    id: string,
  ): Observable<PublicPropertyDetailsResponse> {
    return this.http.get<PublicPropertyDetailsResponse>(
      `${this.apiUrl}/${id}`,
    );
  }
}