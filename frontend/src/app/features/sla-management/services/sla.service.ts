import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { SlaConfiguration, SlaTicket, UpdateSlaConfiguration } from '../models/sla.model';

@Injectable({ providedIn: 'root' })
export class SlaService {
  private readonly baseUrl = `${environment.apiBaseUrl}/api/sla`;

  constructor(private http: HttpClient) {}

  getConfigurations(): Observable<SlaConfiguration[]> {
    return this.http.get<SlaConfiguration[]>(`${this.baseUrl}/configurations`);
  }

  saveConfigurations(items: UpdateSlaConfiguration[]): Observable<SlaConfiguration[]> {
    return this.http.put<SlaConfiguration[]>(`${this.baseUrl}/configurations`, items);
  }

  getPauseStatuses(): Observable<string[]> {
    return this.http.get<string[]>(`${this.baseUrl}/pause-statuses`);
  }

  savePauseStatuses(statuses: string[]): Observable<string[]> {
    return this.http.put<string[]>(`${this.baseUrl}/pause-statuses`, { statuses });
  }

  getTickets(filter?: string): Observable<SlaTicket[]> {
    const suffix = filter ? `?filter=${encodeURIComponent(filter)}` : '';
    return this.http.get<SlaTicket[]>(`${this.baseUrl}/tickets${suffix}`);
  }
}
