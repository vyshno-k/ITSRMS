import { ChangeDetectorRef, Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { timeout } from 'rxjs';
import { ServiceRequestService } from '../../services/service-request.service';
import { ServiceRequest } from '../../models/service-request.model';

@Component({
  selector: 'app-request-list-page',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: './request-list-page.component.html',
  styleUrls: ['./request-list-page.component.scss']
})
export class RequestListPageComponent {
  tickets: ServiceRequest[] = [];
  loading = true;
  errorMessage = '';
  successMessage = '';
  searchText = '';
  statusFilter = '';
  priorityFilter: number | null = null;
  categoryFilter: number | null = null;
  readonly priorities = [{id:1,name:'Critical'},{id:2,name:'High'},{id:3,name:'Medium'},{id:4,name:'Low'}];
  readonly categories = [{id:1,name:'Hardware'},{id:2,name:'Software'},{id:3,name:'Network'},{id:4,name:'Access'},{id:5,name:'Email'}];
  readonly statuses = ['New','Assigned','In Progress','Resolved','Closed','Reopened'];
  pageSize = 10; currentPage = 1;
  readonly Math = Math;

  constructor(
    private srService: ServiceRequestService,
    private changeDetector: ChangeDetectorRef,
    private route: ActivatedRoute
  ) {
    this.route.queryParamMap.subscribe(params => {
      const message = params.get('message');
      if (message === 'request-submitted') {
        this.showSuccess('Request submitted.');
      }
    });

    this.load();
  }

  load(): void {
    this.loading = true;
    this.errorMessage = '';

    const filter = {
      search: this.searchText.trim() || undefined,
      status: this.statusFilter || undefined,
      priorityId: this.priorityFilter ?? undefined,
      categoryId: this.categoryFilter ?? undefined
    };

    const request$ =
      filter.search || filter.status || filter.priorityId || filter.categoryId
        ? this.srService.search(filter)
        : this.srService.getAll();

    request$.pipe(timeout(15000)).subscribe({
      next: (data) => {
        setTimeout(() => {
          this.tickets = Array.isArray(data) ? data : [];
          this.currentPage = 1;
          this.loading = false;
          this.changeDetector.detectChanges();
        });
      },
      error: (err) => {
        this.tickets = [];
        setTimeout(() => {
          this.tickets = [];
          this.loading = false;
          this.errorMessage = this.getErrorMessage(err);
          this.changeDetector.detectChanges();
        });
      }
    });
  }

  private getErrorMessage(err: any): string {
    if (typeof err?.error === 'string' && err.error.trim()) return err.error;
    if (typeof err?.error?.error === 'string' && err.error.error.trim()) return err.error.error;
    if (typeof err?.message === 'string' && err.message.trim()) return err.message;
    return 'Requests could not be loaded within 15 seconds. Check that the API is running on http://localhost:5103.';
  }
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

  clearFilters(): void {
    this.searchText = '';
    this.statusFilter = '';
    this.priorityFilter = null;
    this.categoryFilter = null;
    this.load();
  }

  get totalPages(): number { return Math.max(1, Math.ceil(this.tickets.length / this.pageSize)); }
  get pagedTickets(): ServiceRequest[] { return this.tickets.slice((this.currentPage - 1) * this.pageSize, this.currentPage * this.pageSize); }
  goToPage(page: number): void { this.currentPage = Math.max(1, Math.min(page, this.totalPages)); }

  private showSuccess(message: string): void {
    this.successMessage = message;
    setTimeout(() => {
      this.successMessage = '';
      this.changeDetector.detectChanges();
    }, 3200);
  }

}