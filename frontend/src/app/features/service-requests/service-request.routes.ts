import { Routes } from '@angular/router';

export const SERVICE_REQUEST_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./components/request-list/request-list-page.component')
        .then((m) => m.RequestListPageComponent)
  },
  {
    path: 'new',
    loadComponent: () =>
      import('./components/create-request/create-request.component')
        .then((m) => m.CreateRequestComponent)
  },
  {
    path: ':id',
    loadComponent: () =>
      import('./components/request-details/request-details.component')
        .then((m) => m.RequestDetailsComponent)
  }
];
