import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'pedidos' },
  {
    path: 'pedidos/novo',
    loadComponent: () => import('./pedidos/pedido-form.component').then((m) => m.PedidoFormComponent),
  },
  {
    path: 'pedidos',
    loadComponent: () =>
      import('./pedidos/pedido-lista.component').then((m) => m.PedidoListaComponent),
  },
  { path: '**', redirectTo: 'pedidos' },
];
