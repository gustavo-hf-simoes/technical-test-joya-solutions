import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { LOCALE_ID } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';

import { ClienteResponse } from '../core/api/clientes.models';
import { PaginaResponse, PedidoResumoResponse } from '../core/api/pedidos.models';
import { PedidoListaComponent } from './pedido-lista.component';

const CLIENTES: ClienteResponse[] = [
  { id: 7, nome: 'Mercado Bom Preço Ltda', ativo: true },
  { id: 8, nome: 'Padaria Central', ativo: true },
];

function pagina(items: PedidoResumoResponse[], extra: Partial<PaginaResponse<unknown>> = {}) {
  return { items, page: 1, pageSize: 20, totalItems: items.length, totalPages: items.length ? 1 : 0, ...extra };
}

describe('PedidoListaComponent', () => {
  let http: HttpTestingController;
  let harness: RouterTestingHarness;

  async function abrir(url = '/pedidos') {
    harness = await RouterTestingHarness.create();
    await harness.navigateByUrl(url, PedidoListaComponent);
    http.expectOne((r) => r.url === '/api/clientes').flush(CLIENTES);
    return harness.routeNativeElement as HTMLElement;
  }

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([{ path: 'pedidos', component: PedidoListaComponent }]),
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: LOCALE_ID, useValue: 'pt-BR' },
      ],
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('PedidoListaComponent_FiltrosAplicados_AtualizaQueryString', async () => {
    const el = await abrir();
    http.expectOne((r) => r.url === '/api/pedidos').flush(pagina([]));
    harness.detectChanges();

    const select = el.querySelector<HTMLSelectElement>('#filtro-cliente');
    const inicio = el.querySelector<HTMLInputElement>('#filtro-data-inicio');
    const fim = el.querySelector<HTMLInputElement>('#filtro-data-fim');
    select!.value = '7';
    select!.dispatchEvent(new Event('change'));
    inicio!.value = '2026-01-01';
    inicio!.dispatchEvent(new Event('input'));
    fim!.value = '2026-12-31';
    fim!.dispatchEvent(new Event('input'));
    el.querySelector<HTMLButtonElement>('button[type="submit"]')!.click();
    await harness.fixture.whenStable();

    const url = TestBed.inject(Router).url;
    expect(url).toContain('clienteId=7');
    expect(url).toContain('dataInicio=2026-01-01');
    expect(url).toContain('dataFim=2026-12-31');
    const req = http.expectOne((r) => r.url === '/api/pedidos');
    expect(req.request.params.get('clienteId')).toBe('7');
    expect(req.request.params.get('dataInicio')).toBe('2026-01-01');
    expect(req.request.params.get('dataFim')).toBe('2026-12-31');
    req.flush(pagina([]));
  });

  it('PedidoListaComponent_QueryStringNaEntrada_PreencheFiltrosEConsulta', async () => {
    const el = await abrir('/pedidos?clienteId=7&dataInicio=2026-01-01&dataFim=2026-12-31&page=2');
    const req = http.expectOne((r) => r.url === '/api/pedidos');
    expect(req.request.params.get('clienteId')).toBe('7');
    expect(req.request.params.get('page')).toBe('2');
    req.flush(pagina([], { page: 2 }));
    harness.detectChanges();

    expect(el.querySelector<HTMLSelectElement>('#filtro-cliente')!.value).toBe('7');
    expect(el.querySelector<HTMLInputElement>('#filtro-data-inicio')!.value).toBe('2026-01-01');
    expect(el.querySelector<HTMLInputElement>('#filtro-data-fim')!.value).toBe('2026-12-31');
  });

  it('PedidoListaComponent_PeriodoInvertido_NaoConsultaEMostraErro', async () => {
    const el = await abrir('/pedidos?dataInicio=2026-12-31');
    http.expectOne((r) => r.url === '/api/pedidos').flush(pagina([]));
    harness.detectChanges();

    const fim = el.querySelector<HTMLInputElement>('#filtro-data-fim')!;
    fim.value = '2026-01-01';
    fim.dispatchEvent(new Event('input'));
    el.querySelector<HTMLButtonElement>('button[type="submit"]')!.click();
    harness.detectChanges();

    expect(el.querySelector('#erro-periodo')!.textContent).toContain('posterior');
    http.expectNone((r) => r.url === '/api/pedidos');
  });

  it('PedidoListaComponent_Valores_FormataMoedaEDataEmPtBr', async () => {
    const el = await abrir();
    http.expectOne((r) => r.url === '/api/pedidos').flush(
      pagina([
        {
          id: 1042,
          clienteId: 7,
          clienteNome: 'Mercado Bom Preço Ltda',
          dataPedido: '2026-11-13T14:32:07.123Z',
          valorTotal: 1234.5,
          quantidadeItens: 2,
        },
      ]),
    );
    harness.detectChanges();

    const texto = (el.querySelector('tbody')!.textContent ?? '').replace(/\s+/g, ' ');
    expect(texto).toContain('13/11/2026');
    expect(texto).toContain('R$ 1.234,50');
    expect(texto).toContain('Mercado Bom Preço Ltda');
    expect(el.querySelectorAll('th[scope="col"]').length).toBe(4);
  });

  it('PedidoListaComponent_ConsultaEmAndamento_MostraCarregando', async () => {
    const el = await abrir();
    harness.detectChanges();

    expect(el.textContent).toContain('Carregando pedidos');
    http.expectOne((r) => r.url === '/api/pedidos').flush(pagina([]));
  });

  it('PedidoListaComponent_RespostaVazia_MostraNenhumPedidoEncontrado', async () => {
    const el = await abrir();
    http.expectOne((r) => r.url === '/api/pedidos').flush(pagina([]));
    harness.detectChanges();

    expect(el.textContent).toContain('Nenhum pedido encontrado');
    expect(el.querySelector('table')).toBeNull();
  });

  it('PedidoListaComponent_FalhaNaApi_MostraErroComTentarNovamente', async () => {
    const el = await abrir();
    http
      .expectOne((r) => r.url === '/api/pedidos')
      .flush({ status: 500 }, { status: 500, statusText: 'Server Error' });
    harness.detectChanges();

    const alertas = Array.from(el.querySelectorAll('[role="alert"]')).map((a) => a.textContent);
    expect(alertas.join(' ')).toContain('Não foi possível carregar');
    const botao = Array.from(el.querySelectorAll('button')).find((b) =>
      b.textContent?.includes('Tentar novamente'),
    );
    expect(botao).toBeDefined();

    botao!.click();
    http.expectOne((r) => r.url === '/api/pedidos').flush(pagina([]));
    harness.detectChanges();

    expect(el.textContent).toContain('Nenhum pedido encontrado');
  });
});
