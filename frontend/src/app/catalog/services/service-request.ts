import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface ServiceRequest {
  id: number;
  title: string;
  description: string;
  priority: string;
  status: string;
  requestedBy: string;
  serviceCatalogId: number;
}

@Injectable({
  providedIn: 'root'
})
export class ServiceRequestService {

  private apiUrl = 'http://localhost:5103/api/service-requests';

  constructor(private http: HttpClient) {}

  createRequest(request: ServiceRequest): Observable<ServiceRequest> {
    return this.http.post<ServiceRequest>(this.apiUrl, request);
  }

  getRequests(): Observable<ServiceRequest[]> {
    return this.http.get<ServiceRequest[]>(this.apiUrl);
  }
}