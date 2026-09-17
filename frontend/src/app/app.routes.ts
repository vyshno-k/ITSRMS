import { Routes } from '@angular/router';
import { SERVICE_REQUEST_ROUTES } from './features/service-requests/service-request.routes';
import { SLA_ROUTES } from './features/sla-management/sla.routes';
import { RESOLUTION_CLOSURE_ROUTES } from './features/resolution-closure/resolution-closure.routes';
import { Login } from './employee-management/login/login';
import { Register } from './employee-management/register/register';
import { Employees } from './employee-management/employees/employees';
import { AddEmployee } from './employee-management/add-employee/add-employee';
import { ServiceCatalogComponent } from './catalog/service-catalog/service-catalog';
import { Assignment } from './catalog/assignment/assignment';
import { DashboardComponent } from './features/dashboard/dashboard.component';
import { adminGuard } from './guards/admin.guard';

export const routes: Routes = [
  { path: '', redirectTo: 'login', pathMatch: 'full' },
  { path: 'login', component: Login },
  { path: 'register', component: Register },
  { path: 'admin', component: DashboardComponent, canActivate: [adminGuard] },
  { path: 'employees/add', component: AddEmployee, canActivate: [adminGuard] },
  { path: 'employees/edit/:id', component: AddEmployee, canActivate: [adminGuard] },
  { path: 'employees', component: Employees, canActivate: [adminGuard] },
  { path: 'dashboard', component: DashboardComponent },
  { path: 'service-catalog', component: ServiceCatalogComponent },
  { path: 'assignment', component: Assignment },
  { path: 'service-requests', children: SERVICE_REQUEST_ROUTES },
  { path: 'sla', children: SLA_ROUTES },
  { path: 'resolution-closure', children: RESOLUTION_CLOSURE_ROUTES },
  { path: '**', redirectTo: 'login' }
];
