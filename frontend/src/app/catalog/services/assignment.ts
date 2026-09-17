import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
export interface Assignment { id?: number; serviceRequestId: number; supportEngineerId: number; assignedTo: string; assignedDate?: string; assignedAt?: string; status: string; supportEngineer?: any; }
@Injectable({ providedIn: 'root' })
export class AssignmentService {
  private apiUrl = 'http://localhost:5103/api/ticket-assignments';
  constructor(private http: HttpClient) {}
  getAssignments(): Observable<Assignment[]> { return this.http.get<Assignment[]>(this.apiUrl); }
  getAssignment(id: number): Observable<Assignment> { return this.http.get<Assignment>(`${this.apiUrl}/${id}`); }
  createAssignment(a: Assignment): Observable<Assignment> { return this.http.post<Assignment>(this.apiUrl, a); }
  updateAssignment(id: number, a: Assignment): Observable<Assignment> { return this.http.put<Assignment>(`${this.apiUrl}/${id}`, a); }
  deleteAssignment(id: number): Observable<void> { return this.http.delete<void>(`${this.apiUrl}/${id}`); }
}
