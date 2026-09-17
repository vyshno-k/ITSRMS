import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ServiceRequestService } from '../../services/service-request.service';
import { Employee, ServiceRequest, TicketHistoryEntry, TicketComment, SupportEngineer } from '../../models/service-request.model';

@Component({
  selector: 'app-request-details',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './request-details.component.html',
  styleUrls: ['./request-details.component.scss']
})
export class RequestDetailsComponent implements OnInit {
  ticket?: ServiceRequest;
  history: TicketHistoryEntry[] = [];
  comments: TicketComment[] = [];
  engineers: SupportEngineer[] = [];
  employees: Employee[] = [];
  loading = true;
  saving = false;
  editing = false;
  errorMessage = '';
  historyError = '';
  actionError = '';
  private id = 0;
  historyPage = 1; commentsPage = 1; readonly detailPageSize = 10; readonly Math = Math;

  assignForm;
  commentForm;
  resolveForm;
  editForm;
  progressForm;
  reopenForm;

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

  constructor(
    private route: ActivatedRoute,
    private fb: FormBuilder,
    private srService: ServiceRequestService
  ) {
    this.assignForm = this.fb.group({
      engineerId: this.fb.control<number | null>(null, Validators.required),
      performedBy: this.fb.control<number | null>(null, Validators.required)
    });
    this.progressForm = this.fb.group({
      performedBy: this.fb.control<number | null>(null, Validators.required)
    });
    this.resolveForm = this.fb.group({
      investigationNotes: this.fb.control('', Validators.required),
      resolutionNotes: this.fb.control('', Validators.required),
      resolvedBy: this.fb.control<number | null>(null, Validators.required)
    });
    this.reopenForm = this.fb.group({
      reopenedBy: this.fb.control<number | null>(null),
      reason: this.fb.control('')
    });
    this.commentForm = this.fb.group({
      employeeId: this.fb.control<number | null>(null),
      commentText: this.fb.control('', Validators.required)
    });
    this.editForm = this.fb.group({
      categoryId: this.fb.control<number | null>(null, Validators.required),
      serviceTypeId: this.fb.control<number | null>(null, Validators.required),
      priorityId: this.fb.control<number | null>(null, Validators.required),
      subject: this.fb.control('', [Validators.required, Validators.maxLength(200)]),
      description: this.fb.control('', Validators.required)
    });
  }

  ngOnInit(): void {
    const rawId = this.route.snapshot.paramMap.get('id');
    this.id = Number(rawId);
    if (!Number.isInteger(this.id) || this.id <= 0) {
      this.loading = false;
      this.errorMessage = 'Invalid ticket ID.';
      return;
    }

    this.loadReferenceData();
    this.load();
  }

  private loadReferenceData(): void {
    this.srService.getEngineers().subscribe({
      next: items => this.engineers = Array.isArray(items) ? items : [],
      error: err => this.actionError = this.getErrorMessage(err, 'Active engineers could not be loaded.')
    });
    this.srService.getActiveEmployees().subscribe({
      next: items => {
        this.employees = Array.isArray(items) ? items : [];
        this.setDefaultActorValues();
      },
      error: err => this.actionError = this.getErrorMessage(err, 'Active employees could not be loaded.')
    });
  }

  load(): void {
    this.loading = true;
    this.errorMessage = '';
    this.historyError = '';
    this.actionError = '';
    this.ticket = undefined;
    this.history = [];
    this.comments = [];

    this.srService.getById(this.id).subscribe({
      next: ticket => {
        if (!ticket) {
          this.loading = false;
          this.errorMessage = 'Ticket was not found.';
          return;
        }

        this.ticket = ticket;
        this.loading = false;
        this.patchEditForm();
        this.patchResolutionForm();
        this.setDefaultActorValues();

        this.loadHistory();
        this.loadComments();
      },
      error: err => {
        this.ticket = undefined;
        this.loading = false;
        this.errorMessage = this.getErrorMessage(err, 'Ticket details could not be loaded. Please try again.');
      }
    });
  }

  private loadHistory(): void {
    this.srService.getHistory(this.id).subscribe({
      next: history => { this.history = Array.isArray(history) ? history : []; this.historyPage = 1; },
      error: err => {
        this.history = [];
        this.historyError = this.getErrorMessage(err, 'Ticket loaded, but history could not be loaded.');
      }
    });
  }

  private loadComments(): void {
    this.srService.getComments(this.id).subscribe({
      next: comments => { this.comments = Array.isArray(comments) ? comments : []; this.commentsPage = 1; },
      error: err => this.actionError = this.getErrorMessage(err, 'Comments could not be loaded.')
    });
  }

  private patchEditForm(): void {
    if (!this.ticket) return;
    this.editForm.patchValue({
      categoryId: this.ticket.categoryId,
      serviceTypeId: this.ticket.serviceTypeId,
      priorityId: this.ticket.priorityId,
      subject: this.ticket.subject,
      description: this.ticket.description
    });
  }

  private patchResolutionForm(): void {
    const resolution = this.ticket?.latestResolution;
    if (!resolution) {
      this.resolveForm.reset();
      return;
    }
    this.resolveForm.patchValue({
      investigationNotes: resolution.investigationNotes,
      resolutionNotes: resolution.resolutionNotes,
      resolvedBy: resolution.resolvedBy ?? null
    });
  }

  private setDefaultActorValues(): void {
    const employeeId = this.employees[0]?.id ?? null;
    if (employeeId == null) return;
    if (!this.assignForm.controls.performedBy.value) this.assignForm.controls.performedBy.setValue(employeeId);
    if (!this.progressForm.controls.performedBy.value) this.progressForm.controls.performedBy.setValue(employeeId);
    if (!this.resolveForm.controls.resolvedBy.value) this.resolveForm.controls.resolvedBy.setValue(employeeId);
    if (!this.reopenForm.controls.reopenedBy.value) this.reopenForm.controls.reopenedBy.setValue(employeeId);
  }

  startEdit(): void {
    if (!this.ticket || this.ticket.status === 'Closed') return;
    this.editing = true;
    this.actionError = '';
    this.patchEditForm();
  }

  cancelEdit(): void { this.editing = false; }
  get pagedHistory(): TicketHistoryEntry[] { return this.history.slice((this.historyPage - 1) * this.detailPageSize, this.historyPage * this.detailPageSize); }
  get historyPages(): number { return Math.max(1, Math.ceil(this.history.length / this.detailPageSize)); }
  get pagedComments(): TicketComment[] { return this.comments.slice((this.commentsPage - 1) * this.detailPageSize, this.commentsPage * this.detailPageSize); }
  get commentPages(): number { return Math.max(1, Math.ceil(this.comments.length / this.detailPageSize)); }
  setHistoryPage(page: number): void { this.historyPage = Math.max(1, Math.min(page, this.historyPages)); }
  setCommentsPage(page: number): void { this.commentsPage = Math.max(1, Math.min(page, this.commentPages)); }

  saveEdit(): void {
    if (this.editForm.invalid || !this.ticket) {
      this.editForm.markAllAsTouched();
      return;
    }
    this.saving = true;
    this.actionError = '';
    this.srService.update(this.id, this.editForm.getRawValue() as any).subscribe({
      next: () => {
        this.editing = false;
        this.saving = false;
        this.load();
      },
      error: err => {
        this.saving = false;
        this.actionError = this.getErrorMessage(err, 'The ticket could not be updated.');
      }
    });
  }

  assign(): void {
    if (this.assignForm.invalid) {
      this.assignForm.markAllAsTouched();
      return;
    }
    this.saving = true;
    this.actionError = '';
    this.srService.assign(this.id, this.assignForm.getRawValue() as any).subscribe({
      next: () => {
        this.saving = false;
        this.assignForm.controls.engineerId.reset();
        this.load();
      },
      error: err => {
        this.saving = false;
        this.actionError = this.getErrorMessage(err, 'Assignment failed.');
      }
    });
  }

  startProgress(): void {
    if (!this.ticket || !['Assigned', 'Reopened'].includes(this.ticket.status)) return;
    if (this.progressForm.invalid) {
      this.progressForm.markAllAsTouched();
      return;
    }
    const engineerId = this.ticket.engineerId;
    const performedBy = this.progressForm.controls.performedBy.value;
    if (!engineerId || !performedBy) {
      this.actionError = 'An assigned engineer and active employee actor are required.';
      return;
    }
    this.saving = true;
    this.actionError = '';
    this.srService.updateStatus(this.id, {
      status: 'In Progress',
      performedBy,
      engineerId
    }).subscribe({
      next: () => { this.saving = false; this.load(); },
      error: err => { this.saving = false; this.actionError = this.getErrorMessage(err, 'Could not start progress.'); }
    });
  }

  addComment(): void {
    if (this.commentForm.invalid) {
      this.commentForm.markAllAsTouched();
      return;
    }
    this.saving = true;
    this.actionError = '';
    this.srService.addComment(this.id, this.commentForm.getRawValue() as any).subscribe({
      next: () => {
        this.saving = false;
        this.commentForm.reset({ employeeId: null, commentText: '' });
        this.loadComments();
        this.loadHistory();
      },
      error: err => {
        this.saving = false;
        this.actionError = this.getErrorMessage(err, 'Comment could not be added.');
      }
    });
  }

  resolve(): void {
    if (this.resolveForm.invalid || !this.ticket || this.ticket.status !== 'In Progress') {
      this.resolveForm.markAllAsTouched();
      return;
    }
    this.saving = true;
    this.actionError = '';
    const engineerId = this.ticket.engineerId;
    const raw = this.resolveForm.getRawValue();
    if (!engineerId || !raw.resolvedBy) {
      this.actionError = 'The assigned engineer and an active employee actor are required.';
      return;
    }
    this.srService.resolve(this.id, {
      investigationNotes: raw.investigationNotes ?? '',
      resolutionNotes: raw.resolutionNotes ?? '',
      resolvedBy: raw.resolvedBy,
      engineerId
    }).subscribe({
      next: () => { this.saving = false; this.load(); },
      error: err => { this.saving = false; this.actionError = this.getErrorMessage(err, 'Could not resolve ticket.'); }
    });
  }

  close(): void {
    if (!this.ticket || this.ticket.status !== 'Resolved') return;
    if (!window.confirm(`Close ticket ${this.ticket.ticketNumber}? Confirm that the employee has confirmed the resolution.`)) return;

    this.saving = true;
    this.actionError = '';
    this.srService.updateStatus(this.id, { status: 'Closed' }).subscribe({
      next: () => { this.saving = false; this.load(); },
      error: err => { this.saving = false; this.actionError = this.getErrorMessage(err, 'Could not close ticket.'); }
    });
  }

  reopen(): void {
    if (!this.ticket || this.ticket.status !== 'Resolved') return;
    const reason = window.prompt('Why is this ticket being reopened?') ?? '';
    if (!reason.trim()) return;

    const reopenedBy = this.reopenForm.controls.reopenedBy.value ?? undefined;
    this.saving = true;
    this.actionError = '';
    this.srService.reopen(this.id, { reopenedBy, reason: reason.trim() }).subscribe({
      next: () => { this.saving = false; this.load(); },
      error: err => { this.saving = false; this.actionError = this.getErrorMessage(err, 'Could not reopen ticket.'); }
    });
  }

  categoryName(id: number): string { return this.categories.find(x => x.id === id)?.name ?? `ID ${id}`; }
  serviceTypeName(id: number): string { return this.serviceTypes.find(x => x.id === id)?.name ?? `ID ${id}`; }
  priorityName(id: number): string { return this.priorities.find(x => x.id === id)?.name ?? `ID ${id}`; }
  displayDate(value: string | null | undefined): string {
    if (!value) return '—';
    const timestamp = /(?:Z|[+-]\d{2}:?\d{2})$/i.test(value) ? value : `${value}Z`;
    const parsed = new Date(timestamp);
    return Number.isNaN(parsed.getTime()) ? '—' : new Intl.DateTimeFormat('en-GB', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
      hour12: true
    }).format(parsed);
  }
  employeeName(id: number | null | undefined): string {
    return id == null ? '—' : (this.employees.find(x => x.id === id)?.fullName ?? `Employee ${id}`);
  }

  private getErrorMessage(err: any, fallback: string): string {
    if (typeof err?.error === 'string' && err.error.trim()) return err.error;
    if (typeof err?.error?.error === 'string' && err.error.error.trim()) return err.error.error;
    if (typeof err?.message === 'string' && err.message.trim()) return err.message;
    return fallback;
  }
}
