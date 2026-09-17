import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { SlaService } from '../../services/sla.service';
import { SlaConfiguration, SlaTicket, UpdateSlaConfiguration } from '../../models/sla.model';

@Component({
  selector: 'app-sla-management',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './sla-management.component.html',
  styleUrls: ['./sla-management.component.scss']
})
export class SlaManagementComponent implements OnInit {
  configurations: SlaConfiguration[] = [];
  tickets: SlaTicket[] = [];
  pauseStatuses: string[] = [];
  loading = true;
  saving = false;
  errorMessage = '';
  successMessage = '';
  filter = 'all';
  pageSize = 10; currentPage = 1;
  readonly Math = Math;

  readonly workflowStatuses = ['New', 'Assigned', 'In Progress', 'Resolved', 'Closed'];
  readonly priorityNames: Record<number, string> = { 1: 'Critical', 2: 'High', 3: 'Medium', 4: 'Low' };

  form;

  constructor(private fb: FormBuilder, private sla: SlaService) {
    this.form = this.fb.group({
      response1: this.fb.control(1, [Validators.required, Validators.min(0.1)]),
      resolution1: this.fb.control(4, [Validators.required, Validators.min(1)]),
      unit1: this.fb.control('hours'),
      response2: this.fb.control(1, [Validators.required, Validators.min(1)]),
      resolution2: this.fb.control(8, [Validators.required, Validators.min(1)]),
      unit2: this.fb.control('hours'),
      response3: this.fb.control(4, [Validators.required, Validators.min(1)]),
      resolution3: this.fb.control(2, [Validators.required, Validators.min(1)]),
      unit3: this.fb.control('hours'),
      response4: this.fb.control(8, [Validators.required, Validators.min(1)]),
      resolution4: this.fb.control(5, [Validators.required, Validators.min(1)]),
      unit4: this.fb.control('hours')
    });
  }

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading = true;
    this.errorMessage = '';
    this.sla.getConfigurations().subscribe({
      next: configs => {
        this.configurations = configs;
        this.patchForm(configs);
        this.sla.getPauseStatuses().subscribe({
          next: statuses => { this.pauseStatuses = statuses; this.loadTickets(); },
          error: err => { this.errorMessage = this.message(err, 'Could not load SLA pause statuses.'); this.loadTickets(); }
        });
      },
      error: err => { this.errorMessage = this.message(err, 'Could not load SLA configuration.'); this.loading = false; }
    });
  }

  patchForm(configs: SlaConfiguration[]): void {
    for (const c of configs) {
      const resolution = c.resolutionTargetBusinessDays > 0 ? c.resolutionTargetBusinessDays * 8 : Math.round(c.resolutionTargetMinutes / 60);
      this.form.patchValue({ [`response${c.priorityId}`]: c.responseTargetMinutes / 60, [`resolution${c.priorityId}`]: resolution, [`unit${c.priorityId}`]: 'hours' });
    }
  }

  loadTickets(): void {
    this.sla.getTickets(this.filter === 'all' ? undefined : this.filter).subscribe({
      next: items => { this.tickets = items; this.currentPage = 1; this.loading = false; },
      error: err => { this.errorMessage = this.message(err, 'Could not load SLA tickets.'); this.tickets = []; this.loading = false; }
    });
  }

  saveConfiguration(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    const raw: any = this.form.getRawValue();
    const items: UpdateSlaConfiguration[] = [1,2,3,4].map(priorityId => {
      const responseValue = Number(raw[`response${priorityId}`]);
      const resolutionValue = Number(raw[`resolution${priorityId}`]);
      return {
        priorityId,
        responseTargetMinutes: Math.round(responseValue * 60),
        resolutionTargetMinutes: Math.round(resolutionValue * 60),
        resolutionTargetBusinessDays: 0
      };
    });
    this.saving = true; this.errorMessage = ''; this.successMessage = '';
    this.sla.saveConfigurations(items).subscribe({
      next: configs => { this.configurations = configs; this.saving = false; this.successMessage = 'SLA targets saved.'; this.loadTickets(); },
      error: err => { this.saving = false; this.errorMessage = this.message(err, 'Could not save SLA configuration.'); }
    });
  }

  isPaused(status: string): boolean { return this.pauseStatuses.includes(status); }

  togglePause(status: string, checked: boolean): void {
    const next = checked ? [...this.pauseStatuses, status] : this.pauseStatuses.filter(x => x !== status);
    this.pauseStatuses = [...new Set(next)];
    this.sla.savePauseStatuses(this.pauseStatuses).subscribe({
      next: statuses => { this.pauseStatuses = statuses; this.successMessage = 'SLA pause statuses saved.'; this.loadTickets(); },
      error: err => { this.errorMessage = this.message(err, 'Could not save SLA pause statuses.'); }
    });
  }

  setFilter(value: string): void {
    this.filter = this.normalizeFilter(value);
    this.loadTickets();
  }

  private normalizeFilter(value: string | null | undefined): string {
    const normalized = (value ?? 'all').trim().toLowerCase();
    if (normalized === 'all' || normalized === 'all tickets') return 'all';
    if (normalized === 'breached') return 'breached';
    if (normalized === 'paused') return 'paused';
    if (normalized === 'ontrack' || normalized === 'on track') return 'ontrack';
    return 'all';
  }
  get selectedPauseStatusesText(): string {
    const selected = this.workflowStatuses.filter(status => this.pauseStatuses.includes(status));
    return selected.length ? selected.join(', ') : 'No pause statuses selected';
  }
  displayStatus(status: string): string {
    return status;
  }
  get totalPages(): number { return Math.max(1, Math.ceil(this.tickets.length / this.pageSize)); }
  get pagedTickets(): SlaTicket[] { return this.tickets.slice((this.currentPage - 1) * this.pageSize, this.currentPage * this.pageSize); }
  goToPage(page: number): void { this.currentPage = Math.max(1, Math.min(page, this.totalPages)); }

  formatTarget(c: SlaConfiguration): string {
    const responseHours = c.responseTargetMinutes / 60;
    const response = `${responseHours} ${this.hourLabel(responseHours)}`;
    const resolutionHours = c.resolutionTargetBusinessDays > 0 ? c.resolutionTargetBusinessDays * 8 : c.resolutionTargetMinutes / 60;
    const resolution = `${resolutionHours} ${this.hourLabel(resolutionHours)}`;
    return `${response} response / ${resolution} resolution`;
  }

  private hourLabel(value: number): string { return value === 1 ? 'hour' : 'hours'; }

  private message(err: any, fallback: string): string {
    if (typeof err?.error === 'string' && err.error.trim()) return err.error;
    if (typeof err?.error?.error === 'string' && err.error.error.trim()) return err.error.error;
    return fallback;
  }
}
