import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';

export interface AdminPendingPropertiesQuery {
  page?: number;
  pageSize?: number;
}

export interface AdminPendingPropertiesResponse {
  items: AdminPendingPropertyItem[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface AdminPendingPropertyItem {
  id: string;
  title: string;
  propertyType: string;
  spaceType: string;
  city?: string | null;
  pricePerNight?: number | null;
  currency: string;
  coverImageUrl?: string | null;
  hostUserId: string;
  hostName: string;
  hostEmail: string;
  submittedAt?: string | null;
  createdAt: string;
}

export interface AdminPropertyDetails {
  id: string;
  title: string;
  description: string;
  propertyType: string;
  spaceType: string;
  status: string;

  host: AdminPropertyHost;

  country: string;
  city: string;
  streetAddress: string;
  buildingNumber?: string | null;
  floor?: string | null;
  apartmentNumber?: string | null;
  postalCode?: string | null;
  latitude?: number | null;
  longitude?: number | null;

  maxGuests: number;
  bedrooms: number;
  beds: number;
  bathrooms: number;

  pricePerNight: number;
  currency: string;
  checkInTime: string;
  checkOutTime: string;
  cancellationPolicy: string;

  allowsSmoking: boolean;
  allowsPets: boolean;
  allowsParties: boolean;
  allowsChildren: boolean;
  additionalHouseRules?: string | null;

  amenities: AdminPropertyAmenity[];
  images: AdminPropertyImage[];
  verificationDocument?: AdminPropertyVerificationDocument | null;

  rejectionReason?: string | null;
  createdAt: string;
  updatedAt?: string | null;
  submittedAt?: string | null;
  reviewedAt?: string | null;
  publishedAt?: string | null;
}

export interface AdminPropertyHost {
  userId: string;
  hostProfileId: string;
  firstName: string;
  lastName: string;
  fullName: string;
  email: string;
  phoneNumber?: string | null;
  isActive: boolean;
  hostStatus: string;
}

export interface AdminPropertyAmenity {
  id: string;
  code: string;
  name: string;
  category: string;
  iconKey?: string | null;
  displayOrder: number;
}

export interface AdminPropertyImage {
  id: string;
  url: string;
  format: string;
  isCover: boolean;
  displayOrder: number;
  createdAt: string;
}

export interface AdminPropertyVerificationDocument {
  id: string;
  documentType: string;
  pagesCount: number;
  pages: AdminPropertyVerificationPage[];
  createdAt: string;
  updatedAt?: string | null;
}

export interface AdminPropertyVerificationPage {
  id: string;
  pageNumber: number;
  format: string;
  createdAt: string;
}

export interface AdminPropertyReviewResponse {
  id: string;
  status: string;
  reviewedAt?: string | null;
  publishedAt?: string | null;
  rejectionReason?: string | null;
  message: string;
}

@Injectable({
  providedIn: 'root',
})
export class AdminPropertiesService {
  private readonly apiUrl = `${environment.baseApi}/api/admin/properties`;
  private readonly hostApplicationsUrl = `${environment.baseApi}/api/admin/host-applications`;

  constructor(private http: HttpClient) {}

  getPendingProperties(
    query: AdminPendingPropertiesQuery = {}
  ): Observable<AdminPendingPropertiesResponse> {
    let params = new HttpParams()
      .set('page', String(query.page ?? 1))
      .set('pageSize', String(query.pageSize ?? 20));

    return this.http
      .get(`${this.apiUrl}/pending`, {
        params,
        responseType: 'text',
      })
      .pipe(
        map(responseText => {
          if (!responseText) {
            return {
              items: [],
              page: query.page ?? 1,
              pageSize: query.pageSize ?? 20,
              totalCount: 0,
              totalPages: 1,
            };
          }

          return JSON.parse(responseText) as AdminPendingPropertiesResponse;
        })
      );
  }

  getPropertyDetails(propertyId: string): Observable<AdminPropertyDetails> {
    return this.http
      .get(`${this.apiUrl}/${propertyId}`, {
        responseType: 'text',
      })
      .pipe(
        map(responseText => {
          return JSON.parse(responseText) as AdminPropertyDetails;
        })
      );
  }

  approveProperty(propertyId: string): Observable<AdminPropertyReviewResponse> {
    return this.http
      .post(`${this.apiUrl}/${propertyId}/approve`, {}, {
        responseType: 'text',
      })
      .pipe(
        map(responseText => {
          return JSON.parse(responseText) as AdminPropertyReviewResponse;
        })
      );
  }

  rejectProperty(
    propertyId: string,
    reason: string
  ): Observable<AdminPropertyReviewResponse> {
    return this.http
      .post(`${this.apiUrl}/${propertyId}/reject`, { reason }, {
        responseType: 'text',
      })
      .pipe(
        map(responseText => {
          return JSON.parse(responseText) as AdminPropertyReviewResponse;
        })
      );
  }

  getVerificationDocumentPageContent(
    propertyId: string,
    pageId: string
  ): Observable<Blob> {
    return this.http.get(
      `${this.apiUrl}/${propertyId}/verification-document/pages/${pageId}/content`,
      {
        responseType: 'blob',
      }
    );
  }

  getHostIdentityDocumentFront(hostProfileId: string): Observable<Blob> {
    return this.http.get(
      `${this.hostApplicationsUrl}/${hostProfileId}/identity-document/front`,
      {
        responseType: 'blob',
      }
    );
  }
}