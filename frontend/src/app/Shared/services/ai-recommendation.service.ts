import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';

export interface NearbyRecommendation {
  name?: string;
  title?: string;
  address?: string;
  location?: string;
  category?: string;
  distance?: string | number;
  rating?: number;
  [key: string]: unknown;
}

export interface NearbyRecommendationRequest {
  latitude: number;
  longitude: number;
  category?: string;
}

@Injectable({
  providedIn: 'root',
})
export class AiRecommendationService {
  private readonly apiUrl =
    'https://smartstayaifeatures-production.up.railway.app/api/restaurants/recommend';

  constructor(private http: HttpClient) {}

  getRecommendations(request: NearbyRecommendationRequest): Observable<NearbyRecommendation[]> {
    const params = new HttpParams().set('category', request.category?.trim() || 'coffee');

    return this.http
      .post<unknown>(
        this.apiUrl,
        {
          latitude: request.latitude,
          longitude: request.longitude,
        },
        { params },
      )
      .pipe(map((response) => this.extractRecommendations(response)));
  }

  private extractRecommendations(response: unknown): NearbyRecommendation[] {
    if (Array.isArray(response)) {
      return response as NearbyRecommendation[];
    }

    if (!response || typeof response !== 'object') {
      return [];
    }

    const record = response as Record<string, unknown>;
    const rawCandidates: unknown[] = [
      record['data'],
      record['recommendations'],
      record['places'],
      record['items'],
      record,
    ];

    for (const candidate of rawCandidates) {
      if (Array.isArray(candidate)) {
        return candidate as NearbyRecommendation[];
      }

      if (candidate && typeof candidate === 'object') {
        const nested = candidate as Record<string, unknown>;
        const nestedCandidates = [
          nested['recommendations'],
          nested['places'],
          nested['items'],
          nested['data'],
        ];

        for (const nestedCandidate of nestedCandidates) {
          if (Array.isArray(nestedCandidate)) {
            return nestedCandidate as NearbyRecommendation[];
          }
        }
      }
    }

    return [];
  }
}
