import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { finalize, timeout } from 'rxjs';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService, LoggedInUser } from '../services/auth.service';

interface EmployeeForm {
  firstName: string;
  lastName: string;
  email: string;
  phone: string;
  department: string;
  designation: string;
  dateOfJoining: string;
  isActive: boolean;
}

@Component({
  selector: 'app-add-employee',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './add-employee.html',
  styleUrl: './add-employee.css'
})
export class AddEmployee implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly auth = inject(AuthService);
  private readonly changeDetector = inject(ChangeDetectorRef);

  readonly apiUrl = 'http://localhost:5103/api/employees';
  readonly limits = { firstName: 50, lastName: 50, email: 100, phone: 15, department: 50, designation: 80 };

  user: LoggedInUser | null = null;
  saving = false;
  errorMessage = '';
  toastMessage = '';
  editingId: number | null = null;

  employee: EmployeeForm = {
    firstName: '', lastName: '', email: '', phone: '', department: '', designation: '', dateOfJoining: '', isActive: true
  };

  ngOnInit(): void {
    this.user = this.auth.getUser();
    if (!this.user) { this.router.navigate(['/login']); return; }
    if (!this.auth.isAdmin()) { this.router.navigate(['/dashboard']); return; }
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (id) {
      this.editingId = id;
      this.http.get<EmployeeForm & { id: number }>(`${this.apiUrl}/${id}`).subscribe({
        next: employee => {
          this.employee = { firstName: employee.firstName, lastName: employee.lastName, email: employee.email, phone: employee.phone, department: employee.department, designation: employee.designation, dateOfJoining: employee.dateOfJoining?.slice(0, 10) || '', isActive: employee.isActive };
          this.changeDetector.detectChanges();
        },
        error: () => {
          this.errorMessage = 'Unable to load this employee.';
          this.changeDetector.detectChanges();
        }
      });
    }
  }

  saveEmployee(): void {
    // Do not allow a second click while the current request is running.
    if (this.saving) return;

    this.errorMessage = '';
    this.toastMessage = '';

    const e = this.employee;

    const fields: Array<[string, string, number]> = [
      ['First name', e.firstName, this.limits.firstName],
      ['Last name', e.lastName, this.limits.lastName],
      ['Email', e.email, this.limits.email],
      ['Phone', e.phone, this.limits.phone],
      ['Department', e.department, this.limits.department],
      ['Designation', e.designation, this.limits.designation]
    ];

    for (const [label, value, limit] of fields) {
      const trimmed = value.trim();
      if (!trimmed) {
        this.errorMessage = `${label} is required.`;
        return;
      }
      if (trimmed.length > limit) {
        this.errorMessage = `${label} must be ${limit} characters or less.`;
        return;
      }
    }

    if (!e.dateOfJoining) {
      this.errorMessage = 'Date of joining is required.';
      return;
    }

    if (!/^\S+@\S+\.\S+$/.test(e.email.trim())) {
      this.errorMessage = 'Please enter a valid email address.';
      return;
    }

    if (!/^[0-9+()\-\s]{7,15}$/.test(e.phone.trim())) {
      this.errorMessage = 'Phone must contain 7 to 15 valid characters.';
      return;
    }

    const payload = {
      firstName: e.firstName.trim(),
      lastName: e.lastName.trim(),
      email: e.email.trim(),
      phone: e.phone.trim(),
      department: e.department.trim(),
      designation: e.designation.trim(),
      dateOfJoining: e.dateOfJoining,
      isActive: e.isActive
    };

    // Start the loading state immediately after validation.
    this.saving = true;

    const request = this.editingId ? this.http.put<any>(`${this.apiUrl}/${this.editingId}`, payload, { observe: 'response' }) : this.http.post<any>(this.apiUrl, payload, { observe: 'response' });
    request.pipe(timeout(15000),
      finalize(() => {
        this.saving = false;
      })
    ).subscribe({
      next: (response) => {
        console.log('Employee saved:', response.body);
        this.saving = false;

        if (response.status < 200 || response.status >= 300) {
          this.errorMessage = 'Employee could not be saved.';
          return;
        }

        const successMessage = 'All changes saved';
        this.toastMessage = successMessage;
        this.errorMessage = '';
        this.changeDetector.detectChanges();

        // Keep the user on this page; clear the form for the next employee.
        if (this.editingId) { setTimeout(() => this.toastMessage = '', 3500); return; }
        this.employee = {
          firstName: '',
          lastName: '',
          email: '',
          phone: '',
          department: '',
          designation: '',
          dateOfJoining: '',
          isActive: true
        };

        setTimeout(() => {
          this.toastMessage = '';
        }, 3500);
      },
      error: (err: HttpErrorResponse) => {
        console.error('Employee save failed:', err);
        this.saving = false;

        if (err.status === 0) {
          this.errorMessage = 'Cannot connect to backend. Make sure the .NET API is running on port 5103.';
        } else if (err.status === 409) {
          this.errorMessage = err.error?.message || 'Employee email already exists.';
        } else if (err.status === 400) {
          this.errorMessage = err.error?.message || 'Please check the employee details.';
        } else {
          this.errorMessage = err.error?.message || 'Unable to save employee. Please try again.';
        }
        this.changeDetector.detectChanges();
      }
    });
  }

  goBack(): void { this.router.navigate(['/employees']); }
  logout(): void { this.auth.logout(); this.router.navigate(['/login']); }
}
