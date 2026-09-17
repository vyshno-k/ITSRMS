import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface SupportEngineer { id: number; name?: string; fullName?: string; email?: string; isActive: boolean; }
@Injectable({ providedIn: 'root' })
export class SupportEngineerService {
  private apiUrl = 'http://localhost:5103/api/support-engineers';
  constructor(private http: HttpClient) {}
  getSupportEngineers(): Observable<SupportEngineer[]> { return this.http.get<SupportEngineer[]>(this.apiUrl); }
  updateSupportEngineer(id: number, engineer: SupportEngineer): Observable<SupportEngineer> { return this.http.put<SupportEngineer>(`${this.apiUrl}/${id}`, engineer); }
}
