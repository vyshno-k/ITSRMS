import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { switchMap } from 'rxjs';
import { ServiceRequestService } from '../../../service-requests/services/service-request.service';
import { ServiceRequest, Employee } from '../../../service-requests/models/service-request.model';

@Component({
  selector: 'app-resolution-closure',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './resolution-closure.component.html',
  styleUrls: ['./resolution-closure.component.scss']
})
export class ResolutionClosureComponent implements OnInit {
  tickets: ServiceRequest[] = [];
  employees: Employee[] = [];
  filter = 'all';
  loading = true;
  savingId: number | null = null;
  errorMessage = '';
  successMessage = '';
  pageSize = 10; currentPage = 1;
  readonly Math = Math;
  selectedTicket: ServiceRequest | null = null;
  closureDescription = '';
  closureCategory = 'Software Issue';
  closureNotes = '';
  selectedAssignedTicketIds: number[] = [];

  constructor(private srService: ServiceRequestService, private changeDetector: ChangeDetectorRef) {}

  ngOnInit(): void {
    this.srService.getActiveEmployees().subscribe({ next: items => { this.employees = items; this.changeDetector.detectChanges(); } });
    this.load();
  }

  load(): void {
    this.loading = true;
    this.errorMessage = '';
    this.srService.getAll().subscribe({
      next: items => {
        this.tickets = items ?? []; this.currentPage = 1;
        this.loading = false;
        this.changeDetector.detectChanges();
      },
      error: err => {
        this.tickets = [];
        this.loading = false;
        this.errorMessage = this.message(err, 'Resolution and closure data could not be loaded.');
        this.changeDetector.detectChanges();
      }
    });
  }

  get visibleTickets(): ServiceRequest[] {
    if (this.filter === 'all') return this.tickets;
    if (this.filter === 'Closed') return this.tickets.filter(t => t.status === 'Resolved' || t.status === 'Closed');
    return this.tickets.filter(t => t.status === this.filter);
  }
  get pagedTickets(): ServiceRequest[] { const items = this.visibleTickets; return items.slice((this.currentPage - 1) * this.pageSize, this.currentPage * this.pageSize); }
  get totalPages(): number { return Math.max(1, Math.ceil(this.visibleTickets.length / this.pageSize)); }
  get assignedTickets(): ServiceRequest[] { return this.tickets.filter(ticket => ticket.status === 'Assigned' || ticket.status === 'In Progress'); }
  goToPage(page: number): void { this.currentPage = Math.max(1, Math.min(page, this.totalPages)); }

  toggleAssignedTicket(ticketId: number, checked: boolean): void {
    this.selectedAssignedTicketIds = checked
      ? [...this.selectedAssignedTicketIds, ticketId]
      : this.selectedAssignedTicketIds.filter(id => id !== ticketId);
  }

  markSelectedResolved(): void {
    if (!this.selectedAssignedTicketIds.length) {
      const ticket = this.selectedTicket && this.assignedTickets.some(item => item.id === this.selectedTicket?.id)
        ? this.selectedTicket
        : this.assignedTickets[0];
      if (ticket) {
        this.selectedAssignedTicketIds = [ticket.id];
        this.selectTicket(ticket);
        this.scrollToClosureForm();
      }
      this.errorMessage = ticket
        ? 'Enter the resolution details for the selected ticket, then click Mark Resolved again.'
        : 'No assigned tickets are available to resolve.';
      return;
    }
    const tickets = this.selectedAssignedTicketIds.map(id => this.tickets.find(item => item.id === id)).filter((ticket): ticket is ServiceRequest => !!ticket);
    if (tickets.some(ticket => !ticket.engineerId)) {
      this.errorMessage = 'Every selected ticket must have an assigned support engineer.';
      return;
    }
    const ticket = tickets[0];
    if (this.selectedTicket !== ticket) this.selectTicket(ticket);
    if (!this.closureDescription.trim()) {
      this.errorMessage = 'Enter a resolution description in the form before marking the ticket resolved.';
      return;
    }
    const resolvedBy = this.employees[0]?.id;
    if (!resolvedBy) {
      this.errorMessage = 'No active employee is available to record the resolution.';
      return;
    }
    this.savingId = ticket.id;
    this.errorMessage = '';
    this.resolveSelectedTickets(tickets, 0, resolvedBy);
  }

  private resolveSelectedTickets(tickets: ServiceRequest[], index: number, resolvedBy: number): void {
    if (index >= tickets.length) {
      this.savingId = null;
      this.successMessage = 'Selected tickets marked resolved.';
      this.selectedAssignedTicketIds = [];
      this.selectedTicket = null;
      this.changeDetector.detectChanges();
      this.load();
      return;
    }
    const ticket = tickets[index];
    this.savingId = ticket.id;
    const resolution = {
      investigationNotes: this.closureNotes.trim() || this.closureDescription.trim(),
      resolutionNotes: this.closureDescription.trim(),
      resolvedBy,
      engineerId: ticket.engineerId!
    };
    const request = ticket.status === 'Assigned'
      ? this.srService.updateStatus(ticket.id, { status: 'In Progress', performedBy: resolvedBy }).pipe(switchMap(() => this.srService.resolve(ticket.id, resolution)))
      : this.srService.resolve(ticket.id, resolution);
    request.subscribe({
      next: resolvedTicket => {
        ticket.status = 'Resolved';
        ticket.resolvedAt = resolvedTicket?.resolvedAt || new Date().toISOString();
        ticket.latestResolution = resolvedTicket?.latestResolution || {
          id: 0,
          serviceRequestId: ticket.id,
          investigationNotes: resolution.investigationNotes,
          resolutionNotes: resolution.resolutionNotes,
          resolvedAt: ticket.resolvedAt,
          slaResult: ticket.resolutionSlaStatus || 'On Track'
        };
        this.resolveSelectedTickets(tickets, index + 1, resolvedBy);
      },
      error: err => {
        this.savingId = null;
        this.errorMessage = this.message(err, 'The ticket could not be marked resolved.');
        this.changeDetector.detectChanges();
      }
    });
  }

  selectTicket(ticket: ServiceRequest): void {
    this.selectedTicket = ticket;
    this.closureDescription = ticket.latestResolution?.resolutionNotes || '';
    this.closureNotes = ticket.latestResolution?.investigationNotes || '';
  }

  closeTicket(ticket: ServiceRequest): void {
    if (!ticket) {
      const resolvedTicket = this.tickets.find(item => item.status === 'Resolved');
      if (!resolvedTicket) {
        this.errorMessage = 'No resolved tickets are available to close.';
        return;
      }
      this.selectTicket(resolvedTicket);
      this.scrollToClosureForm();
      this.errorMessage = 'Review the selected resolution, then click Close Ticket again.';
      return;
    }
    if (ticket.status === 'Closed') return;
    if (ticket.status !== 'Resolved') {
      this.selectedAssignedTicketIds = [ticket.id];
      this.markSelectedResolved();
      return;
    }
    if (!window.confirm(`Close ${ticket.ticketNumber}? Confirm that the resolution is complete.`)) return;
    if (!this.closureDescription.trim()) {
      this.errorMessage = 'Resolution description is required before closing the ticket.';
      this.selectTicket(ticket);
      return;
    }
    this.savingId = ticket.id;
    this.successMessage = '';
    this.srService.addComment(ticket.id, { commentText: `Resolution (${this.closureCategory}): ${this.closureDescription.trim()}${this.closureNotes.trim() ? ` Notes: ${this.closureNotes.trim()}` : ''}` }).subscribe({
      next: () => this.srService.updateStatus(ticket.id, { status: 'Closed' }).subscribe({
      next: () => {
        this.savingId = null;
        this.successMessage = 'Ticket closed successfully.';
        this.load();
      },
      error: err => {
        this.savingId = null;
        this.errorMessage = this.message(err, 'The ticket could not be closed.');
      }
      }),
      error: err => {
        this.savingId = null;
        this.errorMessage = this.message(err, 'The resolution could not be saved.');
      }
    });
  }

  reopenTicket(ticket: ServiceRequest): void {
    if (ticket.status !== 'Resolved') return;
    const reason = window.prompt('Why is this ticket being reopened?') ?? '';
    if (!reason.trim()) return;
    const reopenedBy = this.employees[0]?.id;
    this.savingId = ticket.id;
    this.successMessage = '';
    this.srService.reopen(ticket.id, { reopenedBy, reason: reason.trim() }).subscribe({
      next: () => {
        this.savingId = null;
        this.successMessage = `${ticket.ticketNumber} was reopened and returned to the workflow.`;
        this.load();
      },
      error: err => {
        this.savingId = null;
        this.errorMessage = this.message(err, 'The ticket could not be reopened.');
      }
    });
  }

  private message(err: any, fallback: string): string {
    if (typeof err?.error === 'string' && err.error.trim()) return err.error;
    if (typeof err?.error?.error === 'string' && err.error.error.trim()) return err.error.error;
    return fallback;
  }

  private scrollToClosureForm(): void {
    setTimeout(() => document.getElementById('closure-form')?.scrollIntoView({ behavior: 'smooth', block: 'start' }));
  }
}
