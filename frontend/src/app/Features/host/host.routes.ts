import { Routes } from '@angular/router';
import { HostApplicationComponent } from './pages/host-application/host-application';

export const HOST_ROUTES: Routes = [
  {
    path: 'become-a-host',
    component: HostApplicationComponent,
  },
];

// In app.routes.ts:
// import { HOST_ROUTES } from './features/host/host.routes';
// export const routes: Routes = [
//   ...HOST_ROUTES,
//   // ...
// ];
