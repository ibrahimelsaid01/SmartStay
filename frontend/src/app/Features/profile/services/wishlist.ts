import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, catchError, map, of, switchMap, throwError } from 'rxjs';
import { environment } from '../../../../environments/environment';

export interface WishListsResponse {
  items: WishListSummaryResponse[];
  totalCount: number;
}

export interface WishListSummaryResponse {
  id: string;
  name: string;
  itemsCount: number;
  previewImageUrls: string[];
  containsProperty: boolean;
  createdAt: string;
  updatedAt?: string | null;
}

export interface WishListDetailsResponse {
  id: string;
  name: string;
  items: WishListItemResponse[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  createdAt: string;
  updatedAt?: string | null;
}

export interface WishListItemResponse {
  propertyId: string;
  title: string;
  propertyType: string;
  spaceType: string;
  country: string;
  city: string;
  pricePerNight?: number | null;
  currency: string;
  coverImageUrl?: string | null;
  maxGuests?: number | null;
  averageRating: number;
  reviewsCount: number;
  isAvailable: boolean;
  note?: string | null;
  addedAt: string;
}

@Injectable({
  providedIn: 'root',
})
export class WishlistService {
  private readonly apiUrl = `${environment.baseApi}/api/wishlists`;
  private readonly defaultWishListName = 'Favorites';

  constructor(private readonly http: HttpClient) {}

  getWishLists(propertyId?: string): Observable<WishListsResponse> {
    let params = new HttpParams();

    if (propertyId) {
      params = params.set('propertyId', propertyId);
    }

    return this.http
      .get(this.apiUrl, {
        params,
        responseType: 'text',
        withCredentials: true,
      })
      .pipe(map((response) => this.parseJsonResponse<WishListsResponse>(response)));
  }

  getWishListById(
    wishListId: string,
    page = 1,
    pageSize = 50,
  ): Observable<WishListDetailsResponse> {
    const params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    return this.http
      .get(`${this.apiUrl}/${wishListId}`, {
        params,
        responseType: 'text',
        withCredentials: true,
      })
      .pipe(map((response) => this.parseJsonResponse<WishListDetailsResponse>(response)));
  }

  createWishList(name = this.defaultWishListName): Observable<WishListSummaryResponse> {
    return this.http
      .post(
        this.apiUrl,
        { name },
        {
          responseType: 'text',
          withCredentials: true,
        },
      )
      .pipe(map((response) => this.parseJsonResponse<WishListSummaryResponse>(response)));
  }

  getOrCreateDefaultWishList(): Observable<WishListSummaryResponse> {
    return this.getWishLists().pipe(
      switchMap((response) => {
        const defaultWishList = this.findDefaultWishList(response.items ?? []);

        if (defaultWishList) {
          return of(defaultWishList);
        }

        return this.createWishList().pipe(
          catchError((error: unknown) => {
            if (!(error instanceof HttpErrorResponse) || error.status !== 409) {
              return throwError(() => error);
            }

            return this.getWishLists().pipe(
              map((latestResponse) => {
                const existingDefaultWishList = this.findDefaultWishList(
                  latestResponse.items ?? [],
                );

                if (!existingDefaultWishList) {
                  throw error;
                }

                return existingDefaultWishList;
              }),
            );
          }),
        );
      }),
    );
  }

  getDefaultWishListItems(page = 1, pageSize = 50): Observable<WishListDetailsResponse> {
    return this.getOrCreateDefaultWishList().pipe(
      switchMap((wishList) => this.getWishListById(wishList.id, page, pageSize)),
    );
  }

  addToDefaultWishList(propertyId: string): Observable<WishListItemResponse> {
    return this.getOrCreateDefaultWishList().pipe(
      switchMap((wishList) =>
        this.http
          .post(
            `${this.apiUrl}/${wishList.id}/items`,
            { propertyId },
            {
              responseType: 'text',
              withCredentials: true,
            },
          )
          .pipe(map((response) => this.parseJsonResponse<WishListItemResponse>(response))),
      ),
    );
  }

  removeFromDefaultWishList(propertyId: string): Observable<void> {
    return this.getOrCreateDefaultWishList().pipe(
      switchMap((wishList) =>
        this.http
          .delete(`${this.apiUrl}/${wishList.id}/items/${propertyId}`, {
            responseType: 'text',
            withCredentials: true,
          })
          .pipe(map(() => undefined)),
      ),
    );
  }

  isPropertyWishlisted(propertyId: string): Observable<boolean> {
    return this.getWishLists(propertyId).pipe(
      map((response) => {
        const defaultWishList = this.findDefaultWishList(response.items ?? []);

        return defaultWishList?.containsProperty ?? false;
      }),
    );
  }

  private findDefaultWishList(
    wishLists: WishListSummaryResponse[],
  ): WishListSummaryResponse | undefined {
    return wishLists.find(
      (wishList) =>
        wishList.name.trim().toLowerCase() ===
        this.defaultWishListName.toLowerCase(),
    );
  }

  private parseJsonResponse<T>(response: string): T {
    const normalizedResponse = response.replace(/^\uFEFF/, '').trim();

    if (!normalizedResponse) {
      throw new Error('The server returned an empty response.');
    }

    return JSON.parse(normalizedResponse) as T;
  }
}