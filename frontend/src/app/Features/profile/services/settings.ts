import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { environment } from '../../../../environments/environment';

export interface AccountDeactivationResponse {
  isDeactivated: boolean;
  deactivatedAt: string;
  unpublishedPropertiesCount: number;
  message: string;
}

export interface EmailChangeUnavailableResponse {
  message: string;
}

@Injectable({
  providedIn: 'root',
})
export class SettingsService {
  private readonly accountApiUrl = `${environment.baseApi}/api/account`;

  constructor(private http: HttpClient) {}

  deleteAccount(): Observable<AccountDeactivationResponse> {
    return this.http.post<AccountDeactivationResponse>(
      `${this.accountApiUrl}/deactivate`,
      {
        confirmation: 'DEACTIVATE',
      }
    );
  }

  requestEmailChange(
    newEmail: string
  ): Observable<EmailChangeUnavailableResponse> {
    /*
     * ملاحظة:
     * الفرونت القديم كان ينادي:
     * /api/auth/request-email-change
     *
     * لكن الباك الحالي لا يحتوي على endpoint لتغيير الإيميل.
     * لذلك لا نرسل request وهمي حتى لا يظهر 404.
     */
    console.warn(
      `Email change request is not connected to backend yet. newEmail=${newEmail}`
    );

    return throwError(
      () =>
        new Error(
          'Email change backend endpoint is not implemented yet.'
        )
    );
  }

  confirmEmailChange(
    newEmail: string,
    otpCode: string
  ): Observable<EmailChangeUnavailableResponse> {
    /*
     * ملاحظة:
     * الباك الحالي لا يحتوي على:
     * /api/auth/confirm-email-change
     *
     * لذلك نوقف النداء مؤقتًا بدل استدعاء endpoint غير موجود.
     */
    console.warn(
      `Email change confirmation is not connected to backend yet. newEmail=${newEmail}, otpCode=${otpCode}`
    );

    return throwError(
      () =>
        new Error(
          'Email change confirmation endpoint is not implemented yet.'
        )
    );
  }
}