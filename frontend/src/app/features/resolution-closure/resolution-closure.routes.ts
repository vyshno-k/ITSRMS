import { Routes } from '@angular/router';

export const RESOLUTION_CLOSURE_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./components/resolution-closure/resolution-closure.component')
        .then(m => m.ResolutionClosureComponent)
  }
];
