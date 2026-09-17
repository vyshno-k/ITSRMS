import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';

export interface LoggedInUser {
  id: number;
  username: string;
  fullName: string;
  email: string;
  department: string;
  role: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  readonly api = 'http://localhost:5103/api';

  login(data: { username: string; password: string }): Observable<any> {
    return this.http.post<any>(`${this.api}/auth/login`, data).pipe(
      tap(response => {
        if (response?.user) localStorage.setItem('loggedInUser', JSON.stringify(response.user));
      })
    );
  }

  register(data: any): Observable<any> {
    return this.http.post<any>(`${this.api}/auth/register`, data);
  }

  getUser(): LoggedInUser | null {
    const raw = localStorage.getItem('loggedInUser');
    if (!raw) return null;
    try { return JSON.parse(raw) as LoggedInUser; } catch { return null; }
  }

  isLoggedIn(): boolean { return !!this.getUser(); }

  isAdmin(): boolean { return ['admin', 'administrator'].includes(this.getUser()?.role?.trim().toLowerCase() ?? ''); }

  logout(): void { localStorage.removeItem('loggedInUser'); }
}
