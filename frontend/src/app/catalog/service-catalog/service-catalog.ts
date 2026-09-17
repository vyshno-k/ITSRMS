import { Component, OnInit } from '@angular/core';
import { timeout } from 'rxjs';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { ServiceCatalogService, ServiceCatalogItem } from '../services/service-catalog';

@Component({
  selector: 'app-service-catalog',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './service-catalog.html',
  styleUrl: './service-catalog.css'
})
export class ServiceCatalogComponent implements OnInit {
  services: ServiceCatalogItem[] = [];
  filteredServices: ServiceCatalogItem[] = [];
  showAddForm = false;
  isEditMode = false;
  selectedCategory = '';
  selectedStatus = '';
  saving = false;
  errorMessage = '';
  successMessage = '';
  pageSize = 10; currentPage = 1; readonly Math = Math;

  newService: ServiceCatalogItem = this.emptyService();

  constructor(private serviceCatalogService: ServiceCatalogService) {}

  ngOnInit(): void { this.loadServices(); }

  private emptyService(): ServiceCatalogItem {
    return { id: 0, serviceName: '', description: '', category: '', requestType: '', serviceOwner: '', estimatedDeliveryTime: '', defaultPriority: '', sla: '', isActive: true };
  }

  loadServices(): void {
    this.serviceCatalogService.getServices().subscribe({
      next: data => { this.services = Array.isArray(data) ? data : []; this.applyFilters(); },
      error: err => this.errorMessage = this.errorMessageFrom(err, 'Could not load service catalog.')
    });
  }

  openAddForm(): void { this.isEditMode = false; this.newService = this.emptyService(); this.errorMessage = ''; this.successMessage = ''; this.showAddForm = true; }
  closeForm(): void { this.showAddForm = false; this.saving = false; }

  editService(service: ServiceCatalogItem): void {
    this.isEditMode = true;
    this.newService = { ...service };
    this.errorMessage = '';
    this.showAddForm = true;
    setTimeout(() => document.getElementById('service-form')?.scrollIntoView({ behavior: 'smooth', block: 'start' }));
  }

  saveService(): void {
    this.errorMessage = '';
    this.successMessage = '';
    if (!this.newService.serviceName.trim() || !this.newService.category || !this.newService.description.trim() || !this.newService.requestType || !this.newService.defaultPriority || !this.newService.sla.trim()) { this.errorMessage = 'Complete all required service details before saving.'; return; }
    if (this.saving) return;
    this.saving = true;
    const request = { ...this.newService, serviceName: this.newService.serviceName.trim(), description: this.newService.description.trim(), sla: this.newService.sla.trim() };
    const call = this.isEditMode && this.newService.id ? this.serviceCatalogService.updateService(this.newService.id, request) : this.serviceCatalogService.createService(request);
    call.pipe(timeout(5000)).subscribe({
      next: () => { this.saving = false; this.successMessage = 'All changes saved'; this.closeForm(); this.loadServices(); },
      error: err => { this.saving = false; this.errorMessage = this.errorMessageFrom(err, 'Could not save service.'); }
    });
  }

  toggleServiceStatus(service: ServiceCatalogItem): void {
    if (!service.id) return;
    this.serviceCatalogService.updateService(service.id, { ...service, isActive: !service.isActive }).subscribe({
      next: updated => { const i = this.services.findIndex(x => x.id === service.id); if (i >= 0) this.services[i] = updated; this.applyFilters(); },
      error: err => this.errorMessage = this.errorMessageFrom(err, 'Could not change service status.')
    });
  }

  applyFilters(): void {
    this.filteredServices = this.services.filter(s =>
      (!this.selectedCategory || s.category === this.selectedCategory) &&
      (!this.selectedStatus || (this.selectedStatus === 'Active' ? s.isActive : !s.isActive))
    ); this.currentPage = 1;
  }

  get totalPages(): number { return Math.max(1, Math.ceil(this.filteredServices.length / this.pageSize)); }
  get pagedServices(): ServiceCatalogItem[] { return this.filteredServices.slice((this.currentPage - 1) * this.pageSize, this.currentPage * this.pageSize); }
  goToPage(page: number): void { this.currentPage = Math.max(1, Math.min(page, this.totalPages)); }

  clearFilters(): void { this.selectedCategory = ''; this.selectedStatus = ''; this.applyFilters(); }

  private errorMessageFrom(err: any, fallback: string): string {
    return typeof err?.error === 'string' && err.error.trim() ? err.error : (err?.error?.error || err?.message || fallback);
  }
}
