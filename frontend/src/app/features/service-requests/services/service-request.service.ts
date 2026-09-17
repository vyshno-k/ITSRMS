import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, catchError, of, switchMap } from 'rxjs';
import { environment } from '../../../../environments/environment';
import {
  ServiceRequest, CreateServiceRequest, UpdateServiceRequest, AssignTicket,
  ResolveTicket, AddComment, TicketHistoryEntry, ServiceRequestFilter,
  TicketComment, SupportEngineer, Employee, ReopenTicket, UpdateStatusRequest,
  ResolutionView
} from '../models/service-request.model';

@Injectable({ providedIn: 'root' })
export class ServiceRequestService {
  private readonly baseUrl = `${environment.apiBaseUrl}/api/service-requests`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<ServiceRequest[]> {
    return this.http.get<ServiceRequest[]>(this.baseUrl);
  }

  getActiveEmployees(): Observable<Employee[]> {
    return this.http.get<Employee[]>(`${this.baseUrl}/employees`).pipe(
      switchMap((employees) => {
        if (Array.isArray(employees) && employees.length) {
          return of(employees);
        }

        return this.http.get<Employee[]>(`${environment.apiBaseUrl}/api/employees`);
      }),
      catchError(() => this.http.get<Employee[]>(`${environment.apiBaseUrl}/api/employees`))
    );
  }

  search(filter: ServiceRequestFilter): Observable<ServiceRequest[]> {
    let params = new HttpParams();
    if (filter.search) params = params.set('search', filter.search);
    if (filter.status) params = params.set('status', filter.status);
    if (filter.priorityId) params = params.set('priorityId', filter.priorityId);
    if (filter.categoryId) params = params.set('categoryId', filter.categoryId);
    if (filter.employeeId) params = params.set('employeeId', filter.employeeId);
    return this.http.get<ServiceRequest[]>(`${this.baseUrl}/search`, { params });
  }

  getById(id: number): Observable<ServiceRequest> {
    return this.http.get<ServiceRequest>(`${this.baseUrl}/${id}`);
  }

  create(request: CreateServiceRequest): Observable<ServiceRequest> {
    return this.http.post<ServiceRequest>(this.baseUrl, request);
  }

  update(id: number, request: UpdateServiceRequest): Observable<ServiceRequest> {
    return this.http.put<ServiceRequest>(`${this.baseUrl}/${id}`, request);
  }

  updateStatus(id: number, request: UpdateStatusRequest): Observable<ServiceRequest> {
    return this.http.put<ServiceRequest>(`${this.baseUrl}/${id}/status`, request);
  }

  assign(id: number, request: AssignTicket): Observable<any> {
    return this.http.post(`${this.baseUrl}/${id}/assign`, request);
  }

  addComment(id: number, request: AddComment): Observable<TicketComment> {
    return this.http.post<TicketComment>(`${this.baseUrl}/${id}/comments`, request);
  }

  getComments(id: number): Observable<TicketComment[]> {
    return this.http.get<TicketComment[]>(`${this.baseUrl}/${id}/comments`);
  }

  resolve(id: number, request: ResolveTicket): Observable<ServiceRequest> {
    return this.http.post<ServiceRequest>(`${this.baseUrl}/${id}/resolve`, request);
  }

  reopen(id: number, request: ReopenTicket = {}): Observable<ServiceRequest> {
    return this.http.post<ServiceRequest>(`${this.baseUrl}/${id}/reopen`, request);
  }

  getHistory(id: number): Observable<TicketHistoryEntry[]> {
    return this.http.get<TicketHistoryEntry[]>(`${this.baseUrl}/${id}/history`);
  }

  getResolution(id: number): Observable<ResolutionView | null> {
    return this.http.get<ResolutionView | null>(`${this.baseUrl}/${id}/resolution`);
  }

  getEngineers(): Observable<SupportEngineer[]> {
    return this.http.get<SupportEngineer[]>(`${environment.apiBaseUrl}/api/support-engineers`);
  }
}
