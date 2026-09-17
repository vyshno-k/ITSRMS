import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface ServiceCatalogItem {
  id?: number;
  serviceName: string;
  description: string;
  category: string;
  requestType: string;
  serviceOwner: string;
  estimatedDeliveryTime: string;
  defaultPriority: string;
  sla: string;
  isActive: boolean;
}

@Injectable({ providedIn: 'root' })
export class ServiceCatalogService {
  private apiUrl = 'http://localhost:5103/api/servicecatalog';
  constructor(private http: HttpClient) {}
  getServices(): Observable<ServiceCatalogItem[]> { return this.http.get<ServiceCatalogItem[]>(this.apiUrl); }
  getService(id: number): Observable<ServiceCatalogItem> { return this.http.get<ServiceCatalogItem>(`${this.apiUrl}/${id}`); }
  createService(service: ServiceCatalogItem): Observable<ServiceCatalogItem> { return this.http.post<ServiceCatalogItem>(this.apiUrl, service); }
  updateService(id: number, service: ServiceCatalogItem): Observable<ServiceCatalogItem> { return this.http.put<ServiceCatalogItem>(`${this.apiUrl}/${id}`, service); }
  deleteService(id: number): Observable<void> { return this.http.delete<void>(`${this.apiUrl}/${id}`); }
}
