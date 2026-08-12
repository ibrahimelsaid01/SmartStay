// host-application.service.ts
import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import {
  HostApplication,
  HostApplicationBasicInfo,
} from '../models/host-application.models';
import { environment } from '../../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class HostApplicationService {
  private http = inject(HttpClient);

  /** Base API path — change here if your backend is hosted elsewhere */
  private readonly baseUrl = `${environment.baseApi}/api/host-applications`;



  createDraft(payload: HostApplicationBasicInfo): Observable<HostApplication> {
    return this.http.post<HostApplication>(`${this.baseUrl}/draft`, payload);
  }

  getCurrent(): Observable<HostApplication> {
    return this.http.get<HostApplication>(`${this.baseUrl}/current`);
  }

  updateCurrent(payload: HostApplicationBasicInfo): Observable<HostApplication> {
    return this.http.put<HostApplication>(`${this.baseUrl}/current`, payload);
  }

  uploadProfileImage(file: File): Observable<HostApplication> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<HostApplication>(
      `${this.baseUrl}/current/profile-image`,
      formData
    );
  }

  uploadNationalId(frontFile: File, backFile: File): Observable<HostApplication> {
    const formData = new FormData();
    formData.append('FrontFile', frontFile);
    formData.append('BackFile', backFile);
    return this.http.post<HostApplication>(
      `${this.baseUrl}/current/national-id`,
      formData
    );
  }

  submit(): Observable<HostApplication> {
    return this.http.post<HostApplication>(`${this.baseUrl}/current/submit`, {});
  }

  
}
