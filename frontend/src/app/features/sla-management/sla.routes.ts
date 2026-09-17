import { Routes } from '@angular/router';

export const SLA_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./components/sla-management/sla-management.component').then(m => m.SlaManagementComponent)
  }
];
