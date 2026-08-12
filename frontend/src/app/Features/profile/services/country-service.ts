import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
export interface Country {
  name: string;
  code: string;
  flag: string;
  phoneCode: string
}
@Injectable({
  providedIn: 'root',
})
export class CountryService {
  private apiUrl = 'https://countriesnow.space/api/v0.1/countries/codes';

  constructor(private http: HttpClient) {}

getCountries(): Observable<any[]> {
    return this.http.get<any>(this.apiUrl).pipe(
      map(response => {
        if (response && response.data) {
          return response.data
            .filter((c: any) => c.code && c.code !== 'null' && c.code !== 'EL' && c.code !== 'AN') // طرد الأكواد المسببة للـ 404
            .map((country: any) => {
              let pCode = country.dial_code;
              if (pCode && !pCode.startsWith('+')) pCode = '+' + pCode;
              return {
                name: country.name,
                code: country.code.toLowerCase(),
                phoneCode: pCode || '+1'
              };
            })
            .sort((a: any, b: any) => a.name.localeCompare(b.name));
        }
        return [];
      })
    );
  }
}
