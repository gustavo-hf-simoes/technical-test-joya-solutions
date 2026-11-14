import { HttpHeaders, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting, TestRequest } from '@angular/common/http/testing';
import { LOCALE_ID } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';

import { ClienteResponse } from '../core/api/clientes.models';
import { ProdutoResponse } from '../core/api/produtos.models';
import { PedidoFormComponent } from './pedido-form.component';
import { PedidoListaComponent } from './pedido-lista.component';

const CLIENTES: ClienteResponse[] = [
  { id: 7, nome: 'Mercado Bom Preço Ltda', ativo: true },
  { id: 8, nome: 'Padaria Central', ativo: true },
];
const PRODUTOS: ProdutoResponse[] = [
  { id: 12, sku: 'TEC-1', nome: 'Teclado Mecânico', preco: 149.9, estoque: 5 },
  { id: 31, sku: 'MP-1', nome: 'Mouse Pad', preco: 29.95, estoque: 10 },
];

describe('PedidoFormComponent', () => {
  let http: HttpTestingController;
  let harness: RouterTestingHarness;
  let el: HTMLElement;

  async function abrir(): Promise<void> {
    harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/pedidos/novo', PedidoFormComponent);
    http.expectOne((r) => r.url === '/api/clientes').flush(CLIENTES);
    http.expectOne((r) => r.url === '/api/produtos').flush(PRODUTOS);
    harness.detectChanges();
    el = harness.routeNativeElement as HTMLElement;
  }

  const botaoConfirmar = () =>
    el.querySelector<HTMLButtonElement>('button[type="submit"]')!;

  function escolher(id: string, valor: string): void {
    const s = el.querySelector<HTMLSelectElement>(id)!;
    s.value = valor;
    s.dispatchEvent(new Event('change'));
    harness.detectChanges();
  }

  function digitar(id: string, valor: string): void {
    const i = el.querySelector<HTMLInputElement>(id)!;
    i.value = valor;
    i.dispatchEvent(new Event('input'));
    harness.detectChanges();
  }

  function adicionarItem(): void {
    Array.from(el.querySelectorAll<HTMLButtonElement>('button'))
      .find((b) => b.textContent?.includes('Adicionar item'))!
      .click();
    harness.detectChanges();
  }

  function preencherValido(): void {
    escolher('#pedido-cliente', '7');
    adicionarItem();
    escolher('#item-produto-0', '12');
    digitar('#item-quantidade-0', '2');
  }

  function confirmar(): TestRequest {
    botaoConfirmar().click();
    harness.detectChanges();
    return http.expectOne((r) => r.url === '/api/pedidos' && r.method === 'POST');
  }

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([
          { path: 'pedidos/novo', component: PedidoFormComponent },
          { path: 'pedidos', component: PedidoListaComponent },
        ]),
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: LOCALE_ID, useValue: 'pt-BR' },
      ],
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('PedidoFormComponent_SemItens_DesabilitaConfirmar', async () => {
    await abrir();
    escolher('#pedido-cliente', '7');

    expect(botaoConfirmar().disabled).toBeTrue();
    expect(el.textContent).toContain('Adicione ao menos um item');
  });

  it('PedidoFormComponent_SemCliente_MostraMotivo', async () => {
    await abrir();
    escolher('#pedido-cliente', '7');
    escolher('#pedido-cliente', '');

    expect(botaoConfirmar().disabled).toBeTrue();
    expect(el.querySelector('#erro-cliente')?.textContent).toContain('Selecione um cliente');
  });

  it('PedidoFormComponent_QuantidadeInvalida_MostraMotivo', async () => {
    await abrir();
    preencherValido();
    expect(botaoConfirmar().disabled).toBeFalse();

    digitar('#item-quantidade-0', '0');
    expect(botaoConfirmar().disabled).toBeTrue();
    expect(el.querySelector('#erro-quantidade-0')?.textContent).toContain('entre 1 e 10000');
    expect(el.querySelector('#item-quantidade-0')?.getAttribute('aria-invalid')).toBe('true');

    digitar('#item-quantidade-0', '1.5');
    expect(el.querySelector('#erro-quantidade-0')?.textContent).toContain('inteiro');

    digitar('#item-quantidade-0', '10001');
    expect(botaoConfirmar().disabled).toBeTrue();
  });

  it('PedidoFormComponent_ProdutoRepetido_MostraMotivo', async () => {
    await abrir();
    preencherValido();
    adicionarItem();
    escolher('#item-produto-1', '12');

    expect(botaoConfirmar().disabled).toBeTrue();
    expect(el.querySelector('#erro-produto-1')?.textContent).toContain('já foi adicionado');
  });

  it('PedidoFormComponent_TotalEstimado_SomaPrecoVezesQuantidade', async () => {
    await abrir();
    preencherValido();

    expect(el.textContent).toMatch(/Total estimado: R\$\s299,80/);
  });

  it('PedidoFormComponent_Confirmar_RedirecionaParaListagemComSucesso', async () => {
    await abrir();
    preencherValido();

    const req = confirmar();
    expect(botaoConfirmar().disabled).toBeTrue();
    expect(req.request.body).toEqual({ clienteId: 7, itens: [{ produtoId: 12, quantidade: 2 }] });
    req.flush({ id: 99, clienteId: 7, clienteNome: 'x', dataPedido: '2026-11-14T10:00:00Z', valorTotal: 299.8, itens: [] });
    await harness.fixture.whenStable();

    expect(TestBed.inject(Router).url).toBe('/pedidos');
    http.expectOne((r) => r.url === '/api/clientes').flush(CLIENTES);
    http.expectOne((r) => r.url === '/api/pedidos').flush({ items: [], page: 1, pageSize: 20, totalItems: 0, totalPages: 0 });
    harness.detectChanges();
    expect((harness.routeNativeElement as HTMLElement).textContent).toContain('Pedido #99 criado com sucesso');
  });

  it('PedidoFormComponent_Reenvio_ReutilizaIdempotencyKey', async () => {
    await abrir();
    preencherValido();

    const primeira = confirmar();
    const chave = primeira.request.headers.get('Idempotency-Key');
    expect(chave).toMatch(/^[0-9a-f-]{36}$/);
    primeira.error(new ProgressEvent('error'), { status: 0 });
    harness.detectChanges();

    expect(botaoConfirmar().disabled).toBeFalse();
    expect(el.textContent).toContain('Seus dados foram mantidos');
    const segunda = confirmar();
    expect(segunda.request.headers.get('Idempotency-Key')).toBe(chave!);
    segunda.flush({ id: 1, clienteId: 7, clienteNome: 'x', dataPedido: '2026-11-14T10:00:00Z', valorTotal: 1, itens: [] });
    await harness.fixture.whenStable();
    http.match(() => true);
  });

  it('PedidoFormComponent_Resposta409_DestacaItensEPreservaDados', async () => {
    await abrir();
    preencherValido();

    confirmar().flush(
      {
        status: 409,
        code: 'ESTOQUE_INSUFICIENTE',
        itens: [{ produtoId: 12, produtoNome: 'Teclado Mecânico', quantidadeSolicitada: 2, estoqueDisponivel: 1 }],
      },
      { status: 409, statusText: 'Conflict', headers: new HttpHeaders({ 'Content-Type': 'application/problem+json' }) },
    );
    harness.detectChanges();

    // produtos recarregados com o estoque novo
    http.expectOne((r) => r.url === '/api/produtos').flush([{ ...PRODUTOS[0], estoque: 1 }, PRODUTOS[1]]);
    harness.detectChanges();

    expect(el.querySelector('#erro-produto-0')?.textContent).toContain('disponível 1');
    expect(el.querySelector('#item-produto-0')?.getAttribute('aria-invalid')).toBe('true');
    expect(el.querySelector<HTMLSelectElement>('#pedido-cliente')!.value).toBe('7');
    expect(el.querySelector<HTMLInputElement>('#item-quantidade-0')!.value).toBe('2');
    expect(el.textContent).toContain('estoque 1');
    expect(el.querySelector('[role="alert"]')?.textContent).toContain('Estoque insuficiente');
    expect(botaoConfirmar().disabled).toBeFalse();
  });

  it('PedidoFormComponent_Resposta422Cliente_MostraNoCampoCliente', async () => {
    await abrir();
    preencherValido();

    confirmar().flush(
      { status: 422, code: 'CLIENTE_INATIVO', detail: 'O cliente 7 está inativo.' },
      { status: 422, statusText: 'Unprocessable Content' },
    );
    harness.detectChanges();

    expect(el.querySelector('#erro-cliente')?.textContent).toContain('inativo');
    expect(el.querySelector<HTMLInputElement>('#item-quantidade-0')!.value).toBe('2');
  });

  it('PedidoFormComponent_ErroAoCarregarApoio_PermiteTentarNovamente', async () => {
    harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/pedidos/novo', PedidoFormComponent);
    el = harness.routeNativeElement as HTMLElement;
    http.expectOne((r) => r.url === '/api/clientes').flush('x', { status: 500, statusText: 'Err' });
    harness.detectChanges();
    expect(el.textContent).toContain('Não foi possível carregar clientes e produtos');

    Array.from(el.querySelectorAll<HTMLButtonElement>('button'))
      .find((b) => b.textContent?.includes('Tentar novamente'))!
      .click();
    http.expectOne((r) => r.url === '/api/clientes').flush(CLIENTES);
    // o forkJoin cancelou o primeiro GET de produtos quando clientes falhou
    http.match((r) => r.url === '/api/produtos').find((r) => !r.cancelled)!.flush(PRODUTOS);
    harness.detectChanges();
    expect(el.querySelector('#pedido-cliente')).not.toBeNull();
  });
});
