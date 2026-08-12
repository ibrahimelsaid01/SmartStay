import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { map, Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';

export type HostPropertyStatus =
  | ''
  | 'Draft'
  | 'Pending'
  | 'Published'
  | 'Rejected'
  | 'Unpublished';

export interface HostPropertiesQuery {
  page?: number;
  pageSize?: number;
  status?: HostPropertyStatus;
}

export interface HostPropertiesResponse {
  items: HostProperty[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  appliedStatusFilter?: string | null;
}

export interface ListingSummary {
  totalProperties: number;
  draftProperties: number;
  pendingProperties: number;
  publishedProperties: number;
  rejectedProperties: number;
  unpublishedProperties: number;
}

export interface HostProperty {
  id: string;
  title: string;
  propertyType: string;
  spaceType: string;
  status: string;
  city?: string | null;
  pricePerNight?: number | null;
  currency: string;
  coverImageUrl?: string | null;
  imagesCount: number;
  canEdit: boolean;
  canUnpublish: boolean;
  rejectionReason?: string | null;
  createdAt: string;
  updatedAt?: string | null;
  submittedAt?: string | null;
  reviewedAt?: string | null;
  publishedAt?: string | null;
}

export interface HostPropertyUnpublishResponse {
  id: string;
  status: string;
  publishedAt?: string | null;
  updatedAt: string;
  message: string;
}

export interface CreatePropertyDraftRequest {
  title: string;
  description: string;
  propertyType: number;
  spaceType: number;
}

export interface UpdatePropertyBasicInformationRequest {
  title: string;
  description: string;
  propertyType: number;
  spaceType: number;
}

export interface UpdatePropertyLocationRequest {
  country: string;
  city: string;
  streetAddress: string;
  buildingNumber?: string | null;
  floor?: string | null;
  apartmentNumber?: string | null;
  postalCode?: string | null;
  latitude: number;
  longitude: number;
}

export interface UpdatePropertyCapacityRequest {
  maxGuests: number;
  bedrooms: number;
  beds: number;
  bathrooms: number;
}

export interface UpdatePropertyPricingAndPoliciesRequest {
  pricePerNight: number;
  currency: string;
  checkInTime: string;
  checkOutTime: string;
  cancellationPolicy: number;
}

export interface UpdatePropertyHouseRulesRequest {
  allowsSmoking: boolean;
  allowsPets: boolean;
  allowsParties: boolean;
  allowsChildren: boolean;
  additionalHouseRules?: string | null;
}

export interface UpdatePropertyAmenitiesRequest {
  amenityIds: string[];
}

export interface UpdatePropertyImageOrderRequest {
  imageIds: string[];
}

export interface PropertyDraftResponse {
  id: string;
  title: string;
  description: string;
  propertyType: string;
  spaceType: string;
  currency: string;
  status: string;
  createdAt: string;
  updatedAt?: string | null;
}

export interface PropertyLocationResponse {
  id: string;
  country: string;
  city: string;
  streetAddress: string;
  buildingNumber?: string | null;
  floor?: string | null;
  apartmentNumber?: string | null;
  postalCode?: string | null;
  latitude: number;
  longitude: number;
  status: string;
  updatedAt?: string | null;
}

export interface PropertyCapacityResponse {
  id: string;
  maxGuests: number;
  bedrooms: number;
  beds: number;
  bathrooms: number;
  status: string;
  updatedAt?: string | null;
}

export interface PropertyPricingAndPoliciesResponse {
  id: string;
  pricePerNight: number;
  currency: string;
  checkInTime: string;
  checkOutTime: string;
  cancellationPolicy: string;
  status: string;
  updatedAt?: string | null;
}

export interface PropertyHouseRulesResponse {
  id: string;
  allowsSmoking: boolean;
  allowsPets: boolean;
  allowsParties: boolean;
  allowsChildren: boolean;
  additionalHouseRules?: string | null;
  status: string;
  updatedAt?: string | null;
}

export interface AmenityResponse {
  id: string;
  code: string;
  name: string;
  category: string;
  iconKey: string;
  displayOrder: number;
}

export interface PropertyAmenitiesResponse {
  propertyId: string;
  selectedAmenitiesCount: number;
  amenities: AmenityResponse[];
  status: string;
  updatedAt?: string | null;
}

export interface PropertyImageResponse {
  id: string;
  url: string;
  format: string;
  isCover: boolean;
  displayOrder: number;
  createdAt: string;
}

export interface PropertyImagesResponse {
  propertyId: string;
  imagesCount: number;
  coverImageId?: string | null;
  images: PropertyImageResponse[];
  status: string;
  updatedAt?: string | null;
}

export interface PropertyVerificationDocumentPageResponse {
  id: string;
  pageNumber: number;
  format: string;
  createdAt: string;
}

export interface PropertyVerificationDocumentResponse {
  propertyId: string;
  documentId: string;
  documentType: string;
  pagesCount: number;
  pages: PropertyVerificationDocumentPageResponse[];
  status: string;
  createdAt: string;
  updatedAt?: string | null;
}

export interface PropertyEditorCompletionResponse {
  isEditable: boolean;
  basicInformation: boolean;
  location: boolean;
  capacity: boolean;
  pricingAndPolicies: boolean;
  houseRules: boolean;
  images: boolean;
  verificationDocument: boolean;
  canSubmit: boolean;
  submissionErrors: string[];
}

export interface PropertyEditorResponse {
  propertyId: string;
  basicInformation: PropertyDraftResponse;
  location: PropertyLocationResponse;
  capacity: PropertyCapacityResponse;
  pricingAndPolicies: PropertyPricingAndPoliciesResponse;
  houseRules: PropertyHouseRulesResponse;
  amenities: PropertyAmenitiesResponse;
  images: PropertyImagesResponse;
  verificationDocument?: PropertyVerificationDocumentResponse | null;
  completion: PropertyEditorCompletionResponse;
  status: string;
  rejectionReason?: string | null;
  createdAt: string;
  updatedAt?: string | null;
  submittedAt?: string | null;
  reviewedAt?: string | null;
  publishedAt?: string | null;
}

export interface PropertySubmissionResponse {
  id: string;
  status: string;
  submittedAt: string;
  updatedAt?: string | null;
  message: string;
}

@Injectable({
  providedIn: 'root',
})
export class ListingsService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.baseApi}/api/host/properties`;
  private readonly amenitiesUrl = `${environment.baseApi}/api/amenities`;

  getSummaryMetrics(): Observable<ListingSummary> {
    return this.http
      .get(`${this.apiUrl}/summary`, {
        responseType: 'text',
      })
      .pipe(
        map(responseText => {
          if (!responseText) {
            return this.getEmptySummary();
          }

          const parsedResponse = JSON.parse(responseText);

          return {
            totalProperties: parsedResponse.totalProperties ?? 0,
            draftProperties: parsedResponse.draftProperties ?? 0,
            pendingProperties: parsedResponse.pendingProperties ?? 0,
            publishedProperties: parsedResponse.publishedProperties ?? 0,
            rejectedProperties: parsedResponse.rejectedProperties ?? 0,
            unpublishedProperties: parsedResponse.unpublishedProperties ?? 0,
          };
        })
      );
  }

  getProperties(
    query: HostPropertiesQuery = {}
  ): Observable<HostPropertiesResponse> {
    let params = new HttpParams()
      .set('page', String(query.page ?? 1))
      .set('pageSize', String(query.pageSize ?? 10));

    if (query.status) {
      params = params.set('status', query.status);
    }

    return this.http
      .get(this.apiUrl, {
        params,
        responseType: 'text',
      })
      .pipe(
        map(responseText => {
          if (!responseText) {
            return {
              items: [],
              page: query.page ?? 1,
              pageSize: query.pageSize ?? 10,
              totalCount: 0,
              totalPages: 1,
              appliedStatusFilter: query.status || null,
            };
          }

          const parsedResponse = JSON.parse(responseText);

          return {
            items: parsedResponse.items ?? [],
            page: parsedResponse.page ?? query.page ?? 1,
            pageSize: parsedResponse.pageSize ?? query.pageSize ?? 10,
            totalCount: parsedResponse.totalCount ?? 0,
            totalPages: parsedResponse.totalPages ?? 1,
            appliedStatusFilter:
              parsedResponse.appliedStatusFilter ?? query.status ?? null,
          };
        })
      );
  }

  unpublishProperty(
    propertyId: string
  ): Observable<HostPropertyUnpublishResponse> {
    return this.http
      .post(`${this.apiUrl}/${propertyId}/unpublish`, {}, {
        responseType: 'text',
      })
      .pipe(
        map(responseText => {
          return JSON.parse(responseText) as HostPropertyUnpublishResponse;
        })
      );
  }

  createDraft(
    request: CreatePropertyDraftRequest
  ): Observable<PropertyDraftResponse> {
    return this.http
      .post(`${this.apiUrl}/draft`, request, {
        responseType: 'text',
      })
      .pipe(
        map(responseText => {
          return JSON.parse(responseText) as PropertyDraftResponse;
        })
      );
  }

  getPropertyById(propertyId: string): Observable<PropertyDraftResponse> {
    return this.http
      .get(`${this.apiUrl}/${propertyId}`, {
        responseType: 'text',
      })
      .pipe(
        map(responseText => {
          return JSON.parse(responseText) as PropertyDraftResponse;
        })
      );
  }

  getEditor(propertyId: string): Observable<PropertyEditorResponse> {
    return this.http
      .get(`${this.apiUrl}/${propertyId}/editor`, {
        responseType: 'text',
      })
      .pipe(
        map(responseText => {
          return JSON.parse(responseText) as PropertyEditorResponse;
        })
      );
  }

  updateBasicInformation(
    propertyId: string,
    request: UpdatePropertyBasicInformationRequest
  ): Observable<PropertyDraftResponse> {
    return this.http
      .put(`${this.apiUrl}/${propertyId}/basic-information`, request, {
        responseType: 'text',
      })
      .pipe(
        map(responseText => {
          return JSON.parse(responseText) as PropertyDraftResponse;
        })
      );
  }

  updateLocation(
    propertyId: string,
    request: UpdatePropertyLocationRequest
  ): Observable<PropertyLocationResponse> {
    return this.http
      .put(`${this.apiUrl}/${propertyId}/location`, request, {
        responseType: 'text',
      })
      .pipe(
        map(responseText => {
          return JSON.parse(responseText) as PropertyLocationResponse;
        })
      );
  }

  updateCapacity(
    propertyId: string,
    request: UpdatePropertyCapacityRequest
  ): Observable<PropertyCapacityResponse> {
    return this.http
      .put(`${this.apiUrl}/${propertyId}/capacity`, request, {
        responseType: 'text',
      })
      .pipe(
        map(responseText => {
          return JSON.parse(responseText) as PropertyCapacityResponse;
        })
      );
  }

  updatePricingAndPolicies(
    propertyId: string,
    request: UpdatePropertyPricingAndPoliciesRequest
  ): Observable<PropertyPricingAndPoliciesResponse> {
    return this.http
      .put(`${this.apiUrl}/${propertyId}/pricing-and-policies`, request, {
        responseType: 'text',
      })
      .pipe(
        map(responseText => {
          return JSON.parse(responseText) as PropertyPricingAndPoliciesResponse;
        })
      );
  }

  updateHouseRules(
    propertyId: string,
    request: UpdatePropertyHouseRulesRequest
  ): Observable<PropertyHouseRulesResponse> {
    return this.http
      .put(`${this.apiUrl}/${propertyId}/house-rules`, request, {
        responseType: 'text',
      })
      .pipe(
        map(responseText => {
          return JSON.parse(responseText) as PropertyHouseRulesResponse;
        })
      );
  }

  getAllAmenities(): Observable<AmenityResponse[]> {
    return this.http
      .get(this.amenitiesUrl, {
        responseType: 'text',
      })
      .pipe(
        map(responseText => {
          if (!responseText) {
            return [];
          }

          return JSON.parse(responseText) as AmenityResponse[];
        })
      );
  }

  getPropertyAmenities(
    propertyId: string
  ): Observable<PropertyAmenitiesResponse> {
    return this.http
      .get(`${this.apiUrl}/${propertyId}/amenities`, {
        responseType: 'text',
      })
      .pipe(
        map(responseText => {
          return JSON.parse(responseText) as PropertyAmenitiesResponse;
        })
      );
  }

  updateAmenities(
    propertyId: string,
    amenityIds: string[]
  ): Observable<PropertyAmenitiesResponse> {
    const request: UpdatePropertyAmenitiesRequest = {
      amenityIds,
    };

    return this.http
      .put(`${this.apiUrl}/${propertyId}/amenities`, request, {
        responseType: 'text',
      })
      .pipe(
        map(responseText => {
          return JSON.parse(responseText) as PropertyAmenitiesResponse;
        })
      );
  }

  getImages(propertyId: string): Observable<PropertyImagesResponse> {
    return this.http
      .get(`${this.apiUrl}/${propertyId}/images`, {
        responseType: 'text',
      })
      .pipe(
        map(responseText => {
          return JSON.parse(responseText) as PropertyImagesResponse;
        })
      );
  }

  uploadImages(
    propertyId: string,
    files: File[]
  ): Observable<PropertyImagesResponse> {
    const formData = new FormData();

    files.forEach(file => {
      formData.append('Files', file);
    });

    return this.http
      .post(`${this.apiUrl}/${propertyId}/images`, formData, {
        responseType: 'text',
      })
      .pipe(
        map(responseText => {
          return JSON.parse(responseText) as PropertyImagesResponse;
        })
      );
  }

  deleteImage(
    propertyId: string,
    imageId: string
  ): Observable<PropertyImagesResponse> {
    return this.http
      .delete(`${this.apiUrl}/${propertyId}/images/${imageId}`, {
        responseType: 'text',
      })
      .pipe(
        map(responseText => {
          return JSON.parse(responseText) as PropertyImagesResponse;
        })
      );
  }

  setCoverImage(
    propertyId: string,
    imageId: string
  ): Observable<PropertyImagesResponse> {
    return this.http
      .put(`${this.apiUrl}/${propertyId}/images/${imageId}/cover`, {}, {
        responseType: 'text',
      })
      .pipe(
        map(responseText => {
          return JSON.parse(responseText) as PropertyImagesResponse;
        })
      );
  }

  updateImageOrder(
    propertyId: string,
    imageIds: string[]
  ): Observable<PropertyImagesResponse> {
    const request: UpdatePropertyImageOrderRequest = {
      imageIds,
    };

    return this.http
      .put(`${this.apiUrl}/${propertyId}/images/order`, request, {
        responseType: 'text',
      })
      .pipe(
        map(responseText => {
          return JSON.parse(responseText) as PropertyImagesResponse;
        })
      );
  }

  getVerificationDocument(
    propertyId: string
  ): Observable<PropertyVerificationDocumentResponse> {
    return this.http
      .get(`${this.apiUrl}/${propertyId}/verification-document`, {
        responseType: 'text',
      })
      .pipe(
        map(responseText => {
          return JSON.parse(responseText) as PropertyVerificationDocumentResponse;
        })
      );
  }

  uploadVerificationDocument(
    propertyId: string,
    documentType: number,
    files: File[]
  ): Observable<PropertyVerificationDocumentResponse> {
    const formData = new FormData();

    formData.append('DocumentType', String(documentType));

    files.forEach(file => {
      formData.append('Files', file);
    });

    return this.http
      .post(`${this.apiUrl}/${propertyId}/verification-document`, formData, {
        responseType: 'text',
      })
      .pipe(
        map(responseText => {
          return JSON.parse(responseText) as PropertyVerificationDocumentResponse;
        })
      );
  }

  deleteVerificationDocument(propertyId: string): Observable<void> {
    return this.http
      .delete(`${this.apiUrl}/${propertyId}/verification-document`, {
        responseType: 'text',
      })
      .pipe(
        map(() => {
          return undefined;
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

  submitProperty(propertyId: string): Observable<PropertySubmissionResponse> {
    return this.http
      .post(`${this.apiUrl}/${propertyId}/submit`, {}, {
        responseType: 'text',
      })
      .pipe(
        map(responseText => {
          return JSON.parse(responseText) as PropertySubmissionResponse;
        })
      );
  }

  private getEmptySummary(): ListingSummary {
    return {
      totalProperties: 0,
      draftProperties: 0,
      pendingProperties: 0,
      publishedProperties: 0,
      rejectedProperties: 0,
      unpublishedProperties: 0,
    };
  }
}