import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface DatabricksRequest { employeeId?: number | null; categoryId?: number | null; serviceTypeId?: number | null; search?: string | null; from?: string | null; to?: string | null; }
export interface DatabricksResponse { correlationId: string; runId: number; state: string; result: unknown; }

@Injectable({ providedIn: 'root' })
export class DatabricksService {
  private readonly baseUrl = `${environment.apiBaseUrl}/api/databricks`;
  constructor(private readonly http: HttpClient) {}
  status(): Observable<{ configured: boolean; message: string }> { return this.http.get<{ configured: boolean; message: string }>(`${this.baseUrl}/status`); }
  process(request: DatabricksRequest): Observable<DatabricksResponse> { return this.http.post<DatabricksResponse>(`${this.baseUrl}/process`, request); }
  prepare(request: DatabricksRequest): Observable<unknown> { return this.http.post(`${this.baseUrl}/prepare`, request); }
  runStatus(runId: number): Observable<unknown> { return this.http.get(`${this.baseUrl}/runs/${runId}`); }
}
