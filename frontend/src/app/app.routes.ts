import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'pedidos' },
  {
    path: 'pedidos',
    loadComponent: () =>
      import('./pedidos/pedido-lista.component').then((m) => m.PedidoListaComponent),
  },
  { path: '**', redirectTo: 'pedidos' },
];
