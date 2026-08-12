import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';

export type ExternalAuthProvider = 'Google' | 'Facebook';

export type AuthNextStep =
  | 'complete_profile'
  | 'properties'
  | 'host_dashboard'
  | 'admin_dashboard';

export interface AuthenticatedUserResponse {
  id: string;
  email: string;
  firstName?: string | null;
  lastName?: string | null;
  profileImageUrl?: string | null;
  isProfileCompleted: boolean;
  roles: string[];
}

export interface AuthResponse {
  accessToken: string;
  accessTokenExpiresAt: string;
  isNewUser: boolean;
  nextStep: AuthNextStep;
  user: AuthenticatedUserResponse;
}

export interface SendOtpResponse {
  resendAvailableAfterSeconds: number;
  expiresAt: string;
}

@Injectable({
  providedIn: 'root',
})
export class Auth {
  private readonly apiUrl = `${environment.baseApi}/api/auth`;

  constructor(private readonly http: HttpClient) {}

  sendOtp(email: string): Observable<SendOtpResponse> {
    return this.http.post<SendOtpResponse>(
      `${this.apiUrl}/otp/send`,
      { email },
      { withCredentials: true },
    );
  }

  verifyOtp(email: string, code: string): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(
      `${this.apiUrl}/otp/verify`,
      { email, code },
      { withCredentials: true },
    );
  }

  resendOtp(email: string): Observable<SendOtpResponse> {
    return this.sendOtp(email);
  }

  completeProfile(
    firstName: string,
    lastName: string,
  ): Observable<AuthenticatedUserResponse> {
    return this.http.patch<AuthenticatedUserResponse>(
      `${this.apiUrl}/complete-profile`,
      { firstName, lastName },
      { withCredentials: true },
    );
  }

  externalLogin(
    provider: ExternalAuthProvider,
    token: string,
  ): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(
      `${this.apiUrl}/external-login`,
      {
        provider,
        token,
      },
      { withCredentials: true },
    );
  }

  refresh(): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(
      `${this.apiUrl}/refresh`,
      {},
      { withCredentials: true },
    );
  }

  logout(): Observable<void> {
    return this.http.post<void>(
      `${this.apiUrl}/logout`,
      {},
      { withCredentials: true },
    );
  }

  logoutAllDevices(): Observable<void> {
    return this.http.post<void>(
      `${this.apiUrl}/logout-all-devices`,
      {},
      { withCredentials: true },
    );
  }
}