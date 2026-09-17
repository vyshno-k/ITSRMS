import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AssignmentService, Assignment as AssignmentModel } from '../services/assignment';
import { SupportEngineerService, SupportEngineer } from '../services/support-engineer';
import { ServiceRequestService } from '../../features/service-requests/services/service-request.service';
import { ServiceRequest } from '../../features/service-requests/models/service-request.model';

@Component({ selector: 'app-assignment', standalone: true, imports: [CommonModule, FormsModule], templateUrl: './assignment.html', styleUrl: './assignment.css' })
export class Assignment implements OnInit {
  assignments: AssignmentModel[] = [];
  supportEngineers: SupportEngineer[] = [];
  allSupportEngineers: SupportEngineer[] = [];
  serviceRequests: ServiceRequest[] = [];
  selectedServiceRequestId: number | null = null;
  selectedSupportEngineerId: number | null = null;
  selectedStatus = 'Assigned';
  reassigningAssignment: AssignmentModel | null = null;
  reassignSupportEngineerId: number | null = null;
  errorMessage = '';
  pageSize = 10; currentPage = 1; readonly Math = Math;

  constructor(private assignmentService: AssignmentService, private supportEngineerService: SupportEngineerService, private serviceRequestService: ServiceRequestService, private changeDetector: ChangeDetectorRef) {}

  ngOnInit(): void { this.loadAssignments(); this.loadSupportEngineers(); this.loadServiceRequests(); }

  loadServiceRequests(): void { this.serviceRequestService.getAll().subscribe({ next: items => { this.serviceRequests = Array.isArray(items) ? items : []; this.changeDetector.detectChanges(); }, error: err => { this.errorMessage = err?.error || 'Could not load service requests.'; this.changeDetector.detectChanges(); } }); }

  loadAssignments(): void { this.assignmentService.getAssignments().subscribe({ next: x => { this.assignments = Array.isArray(x) ? x.map(a => ({ ...a, assignedDate: a.assignedDate ?? a.assignedAt })) : []; this.currentPage = 1; }, error: err => this.errorMessage = err?.error || 'Could not load assignments.' }); }
  get totalPages(): number { return Math.max(1, Math.ceil(this.assignments.length / this.pageSize)); }
  get pagedAssignments(): AssignmentModel[] { return this.assignments.slice((this.currentPage - 1) * this.pageSize, this.currentPage * this.pageSize); }
  goToPage(page: number): void { this.currentPage = Math.max(1, Math.min(page, this.totalPages)); }

  loadSupportEngineers(): void {
    this.supportEngineerService.getSupportEngineers().subscribe({
      next: x => { this.allSupportEngineers = Array.isArray(x) ? x.map(e => ({ ...e, name: e.name || e.fullName || '' })) : []; this.supportEngineers = this.allSupportEngineers.filter(e => e.isActive); this.changeDetector.detectChanges(); },
      error: err => { this.errorMessage = err?.error || 'Could not load support engineers.'; this.changeDetector.detectChanges(); }
    });
  }

  createAssignment(): void {
    if (!this.selectedServiceRequestId || !this.selectedSupportEngineerId) { alert('Please select a service request and support engineer.'); return; }
    const engineer = this.supportEngineers.find(e => e.id === this.selectedSupportEngineerId);
    if (!engineer) { alert('Only active support engineers can receive assignments.'); return; }
    this.assignmentService.createAssignment({ serviceRequestId: this.selectedServiceRequestId, supportEngineerId: this.selectedSupportEngineerId, assignedTo: engineer.fullName || engineer.name || '', status: this.selectedStatus }).subscribe({
      next: () => { alert('Assignment created successfully.'); this.selectedServiceRequestId = null; this.selectedSupportEngineerId = null; this.loadAssignments(); },
      error: err => alert(err?.error || 'Failed to create assignment.')
    });
  }

  getEngineerWorkload(id: number): number { return this.assignments.filter(a => a.supportEngineerId === id && a.status === 'Assigned').length; }

  toggleEngineerStatus(engineer: SupportEngineer): void {
    this.supportEngineerService.updateSupportEngineer(engineer.id, { ...engineer, isActive: !engineer.isActive }).subscribe({ next: () => this.loadSupportEngineers(), error: err => alert(err?.error || 'Could not update engineer status.') });
  }

  startReassignment(assignment: AssignmentModel): void {
    this.reassigningAssignment = assignment;
    this.reassignSupportEngineerId = null;
    setTimeout(() => {
      document.getElementById('reassign-form')?.scrollIntoView({ behavior: 'smooth', block: 'start' });
      (document.getElementById('reassignEngineer') as HTMLSelectElement | null)?.focus();
    });
  }
  cancelReassignment(): void { this.reassigningAssignment = null; this.reassignSupportEngineerId = null; }

  confirmReassignment(): void {
    if (!this.reassigningAssignment || !this.reassignSupportEngineerId) return;
    const engineer = this.supportEngineers.find(e => e.id === this.reassignSupportEngineerId);
    if (!engineer) { alert('Select an active engineer.'); return; }
    this.assignmentService.createAssignment({ serviceRequestId: this.reassigningAssignment.serviceRequestId, supportEngineerId: engineer.id, assignedTo: engineer.fullName || engineer.name || '', status: 'Assigned' }).subscribe({
      next: () => { alert('Ticket reassigned successfully.'); this.cancelReassignment(); this.loadAssignments(); },
      error: err => alert(err?.error || 'Could not reassign ticket.')
    });
  }
}
