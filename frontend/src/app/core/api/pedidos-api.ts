import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import {
  CriarPedidoRequest,
  FiltroPedidos,
  PaginaResponse,
  PedidoResponse,
  PedidoResumoResponse,
} from './pedidos.models';

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

  /** `POST /pedidos` (RN-14): a mesma `Idempotency-Key` deve ser reenviada em toda nova tentativa. */
  criar(pedido: CriarPedidoRequest, idempotencyKey: string): Observable<PedidoResponse> {
    const headers = new HttpHeaders({ 'Idempotency-Key': idempotencyKey });
    return this.http.post<PedidoResponse>('/api/pedidos', pedido, { headers });
  }
}
