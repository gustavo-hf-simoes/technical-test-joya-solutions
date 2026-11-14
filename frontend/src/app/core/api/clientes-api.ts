import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { ClienteResponse } from './clientes.models';

@Injectable({ providedIn: 'root' })
export class ClientesApi {
  private readonly http = inject(HttpClient);

  listar(ativo?: boolean): Observable<ClienteResponse[]> {
    let params = new HttpParams();
    if (ativo !== undefined) params = params.set('ativo', ativo);
    return this.http.get<ClienteResponse[]>('/api/clientes', { params });
  }
}
