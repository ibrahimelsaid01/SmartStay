import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map, timeout } from 'rxjs';

import { environment } from '../../../../environments/environment';

export interface PropertyItem {
  id: string;
  title: string;
  propertyType: string;
  spaceType: string;
  country: string;
  city: string;
  pricePerNight: number;
  currency: string;
  coverImageUrl: string;
  maxGuests: number;
  bedrooms: number;
  beds: number;
  bathrooms: number;
  publishedAt: string;
  averageRating?: number;
}

export interface PaginatedPropertiesResponse {
  items: PropertyItem[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

@Injectable({
  providedIn: 'root',
})
export class Properties {
  private readonly apiUrl = `${environment.baseApi}/api/properties`;

  constructor(private http: HttpClient) {}

  getPopularStays(): Observable<PropertyItem[]> {
    return this.http
      .get(this.apiUrl, {
        responseType: 'text',
      })
      .pipe(
        timeout(30000),
        map(responseText => {
          if (!responseText) {
            return [];
          }

          const parsedResponse = JSON.parse(responseText) as any;

          const rawItems =
            parsedResponse.items ??
            parsedResponse.Items ??
            parsedResponse.data ??
            parsedResponse.Data ??
            parsedResponse;

          if (!Array.isArray(rawItems)) {
            return [];
          }

          return rawItems.map((item: any) => this.mapPropertyItem(item));
        })
      );
  }

  private mapPropertyItem(item: any): PropertyItem {
    return {
      id: item.id ?? item.Id ?? item.propertyId ?? item.PropertyId ?? '',
      title: item.title ?? item.Title ?? item.name ?? item.Name ?? 'Untitled stay',
      propertyType: item.propertyType ?? item.PropertyType ?? '',
      spaceType: item.spaceType ?? item.SpaceType ?? '',
      country: item.country ?? item.Country ?? '',
      city: item.city ?? item.City ?? '',
      pricePerNight: Number(item.pricePerNight ?? item.PricePerNight ?? item.price ?? item.Price ?? 0),
      currency: item.currency ?? item.Currency ?? 'EGP',
      coverImageUrl:
        item.coverImageUrl ??
        item.CoverImageUrl ??
        item.image ??
        item.Image ??
        'Images/placeholder.jpg',
      maxGuests: Number(item.maxGuests ?? item.MaxGuests ?? item.guests ?? item.Guests ?? 0),
      bedrooms: Number(item.bedrooms ?? item.Bedrooms ?? 0),
      beds: Number(item.beds ?? item.Beds ?? 0),
      bathrooms: Number(item.bathrooms ?? item.Bathrooms ?? item.baths ?? item.Baths ?? 0),
      publishedAt: item.publishedAt ?? item.PublishedAt ?? '',
      averageRating: Number(item.averageRating ?? item.AverageRating ?? item.rating ?? item.Rating ?? 0),
    };
  }
}