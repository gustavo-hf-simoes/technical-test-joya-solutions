import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { FiltroPedidos, PaginaResponse, PedidoResumoResponse } from './pedidos.models';

@Injectable({ providedIn: 'root' })
export class PedidosApi {
  private readonly http = inject(HttpClient);

  listar(filtro: FiltroPedidos): Observable<PaginaResponse<PedidoResumoResponse>> {
    let params = new HttpParams();
    if (filtro.clienteId !== undefined) params = params.set('clienteId', filtro.clienteId);
    if (filtro.dataInicio) params = params.set('dataInicio', filtro.dataInicio);
    if (filtro.dataFim) params = params.set('dataFim', filtro.dataFim);
    if (filtro.page !== undefined) params = params.set('page', filtro.page);
    if (filtro.pageSize !== undefined) params = params.set('pageSize', filtro.pageSize);

    return this.http.get<PaginaResponse<PedidoResumoResponse>>('/api/pedidos', { params });
  }
}
