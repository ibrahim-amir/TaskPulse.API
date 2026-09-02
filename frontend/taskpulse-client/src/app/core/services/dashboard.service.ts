import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CommonResponse } from '../models/common-response.model';
import { DashboardSummary } from '../models/dashboard.model';

@Injectable({ providedIn: 'root' })
export class DashboardService {
  constructor(private http: HttpClient) {}

  getSummary(): Observable<DashboardSummary> {
    return this.http
      .get<CommonResponse<DashboardSummary>>(`${environment.apiUrl}/dashboard/summary`)
      .pipe(map((res) => res.data!));
  }
}
