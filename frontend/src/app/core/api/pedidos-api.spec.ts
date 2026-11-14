import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { ClientesApi } from './clientes-api';
import { PedidosApi } from './pedidos-api';

describe('PedidosApi e ClientesApi', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('PedidosApi_Listar_EnviaFiltrosNaQueryString', () => {
    TestBed.inject(PedidosApi)
      .listar({ clienteId: 7, dataInicio: '2026-01-01', dataFim: '2026-12-31', page: 2, pageSize: 20 })
      .subscribe();

    const req = http.expectOne((r) => r.url === '/api/pedidos');
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('clienteId')).toBe('7');
    expect(req.request.params.get('dataInicio')).toBe('2026-01-01');
    expect(req.request.params.get('dataFim')).toBe('2026-12-31');
    expect(req.request.params.get('page')).toBe('2');
    expect(req.request.params.get('pageSize')).toBe('20');
    req.flush({ items: [], page: 2, pageSize: 20, totalItems: 0, totalPages: 0 });
  });

  it('PedidosApi_ListarSemFiltros_NaoEnviaParametros', () => {
    TestBed.inject(PedidosApi).listar({}).subscribe();

    const req = http.expectOne('/api/pedidos');
    expect(req.request.params.keys()).toEqual([]);
    req.flush({ items: [], page: 1, pageSize: 20, totalItems: 0, totalPages: 0 });
  });

  it('ClientesApi_Listar_UsaCaminhoRelativo', () => {
    TestBed.inject(ClientesApi).listar().subscribe();

    const req = http.expectOne('/api/clientes');
    expect(req.request.params.keys()).toEqual([]);
    req.flush([]);
  });
});
