import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ServiceRequestService } from '../../services/service-request.service';
import { Employee } from '../../models/service-request.model';

@Component({
  selector: 'app-create-request',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './create-request.component.html',
  styleUrls: ['./create-request.component.scss']
})
export class CreateRequestComponent implements OnInit {
  submitting = false;
  loadingEmployees = true;
  errorMessage = '';
  successMessage = '';
  employees: Employee[] = [];
  selectedEmployeeName = '';

  readonly categories = [
    { id: 1, name: 'Hardware' }, { id: 2, name: 'Software' }, { id: 3, name: 'Network' },
    { id: 4, name: 'Access' }, { id: 5, name: 'Email' }
  ];
  readonly serviceTypes = [
    { id: 1, name: 'Software' }, { id: 2, name: 'Hardware' }, { id: 3, name: 'Network' }
  ];
  readonly priorities = [
    { id: 1, name: 'Critical' }, { id: 2, name: 'High' }, { id: 3, name: 'Medium' }, { id: 4, name: 'Low' }
  ];

  form;

  constructor(
    private fb: FormBuilder,
    private srService: ServiceRequestService,
    private router: Router
  ) {
    this.form = this.fb.group({
      employeeId: this.fb.control<number | null>(null, Validators.required),
      categoryId: this.fb.control<number | null>(null, Validators.required),
      serviceTypeId: this.fb.control<number | null>(null, Validators.required),
      priorityId: this.fb.control<number | null>(null, Validators.required),
      subject: this.fb.control('', [Validators.required, Validators.maxLength(200)]),
      description: this.fb.control('', Validators.required)
    });
  }

  ngOnInit(): void {
    this.loadEmployees();
  }

  onEmployeeChange(): void {
    const selectedId = this.form.controls.employeeId.value;
    const selected = this.employees.find((employee) => employee.id === selectedId);
    this.selectedEmployeeName = selected ? selected.fullName : '';
  }

  private loadEmployees(): void {
    this.loadingEmployees = true;
    this.srService.getActiveEmployees().subscribe({
      next: (items) => {
        this.employees = Array.isArray(items) ? items : [];
        this.loadingEmployees = false;
        if (this.employees.length === 1) {
          this.form.controls.employeeId.setValue(this.employees[0].id);
          this.onEmployeeChange();
        }
      },
      error: (err) => {
        this.loadingEmployees = false;
        this.employees = [];
        this.errorMessage = this.getErrorMessage(err, 'Active employees could not be loaded.');
      }
    });
  }

  submit(): void {
    if (this.form.invalid || this.loadingEmployees || !this.employees.length) {
      this.form.markAllAsTouched();
      if (!this.employees.length && !this.loadingEmployees) {
        this.errorMessage = 'No active employees are available. Please check the seeded SQLite data.';
      }
      return;
    }

    const raw = this.form.getRawValue();
    if (raw.employeeId == null || raw.categoryId == null || raw.serviceTypeId == null || raw.priorityId == null) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.srService.create({
      employeeId: raw.employeeId,
      categoryId: raw.categoryId,
      serviceTypeId: raw.serviceTypeId,
      priorityId: raw.priorityId,
      subject: (raw.subject ?? '').trim(),
      description: (raw.description ?? '').trim()
    }).subscribe({
      next: () => {
        this.submitting = false;
        this.successMessage = 'Request submitted.';
        this.form.reset({
          employeeId: null,
          categoryId: null,
          serviceTypeId: null,
          priorityId: null,
          subject: '',
          description: ''
        });

        setTimeout(() => {
          this.successMessage = '';
          void this.router.navigate(['/service-requests'], {
            queryParams: { message: 'request-submitted' }
          });
        }, 900);
      },
      error: (err) => {
        this.submitting = false;
        this.errorMessage = this.getErrorMessage(err, 'Failed to create the ticket.');
      }
    });
  }

  private getErrorMessage(err: any, fallback: string): string {
    if (typeof err?.error === 'string' && err.error.trim()) return err.error;
    if (typeof err?.error?.error === 'string' && err.error.error.trim()) return err.error.error;
    if (typeof err?.message === 'string' && err.message.trim()) return err.message;
    return fallback;
  }
}
