import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { ProdutoResponse } from './produtos.models';

@Injectable({ providedIn: 'root' })
export class ProdutosApi {
  private readonly http = inject(HttpClient);

  listar(ativo?: boolean): Observable<ProdutoResponse[]> {
    let params = new HttpParams();
    if (ativo !== undefined) params = params.set('ativo', ativo);
    return this.http.get<ProdutoResponse[]>('/api/produtos', { params });
  }
}
