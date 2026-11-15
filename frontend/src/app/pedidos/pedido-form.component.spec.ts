import { HttpHeaders, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting, TestRequest } from '@angular/common/http/testing';
import { LOCALE_ID } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';

import { ClienteResponse } from '../core/api/clientes.models';
import { ProdutoResponse } from '../core/api/produtos.models';
import { botaoComTexto, obter } from '../testing/dom';
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

describe('PedidoFormComponent (CA-30, CA-31, CA-32)', () => {
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

  const botaoConfirmar = () => obter<HTMLButtonElement>(el, 'button[type="submit"]');

  function escolher(id: string, valor: string): void {
    const s = obter<HTMLSelectElement>(el, id);
    s.value = valor;
    s.dispatchEvent(new Event('change'));
    harness.detectChanges();
  }

  function digitar(id: string, valor: string): void {
    const i = obter<HTMLInputElement>(el, id);
    i.value = valor;
    i.dispatchEvent(new Event('input'));
    harness.detectChanges();
  }

  function adicionarItem(): void {
    botaoComTexto(el, 'Adicionar item').click();
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

  // CA-31: validacao antes do envio
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

  // CA-30: criar e voltar a listagem
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
    if (chave === null) throw new Error('Idempotency-Key ausente');
    expect(chave).toMatch(/^[0-9a-f-]{36}$/);
    primeira.error(new ProgressEvent('error'), { status: 0 });
    harness.detectChanges();

    expect(botaoConfirmar().disabled).toBeFalse();
    expect(el.textContent).toContain('Seus dados foram mantidos');
    const segunda = confirmar();
    expect(segunda.request.headers.get('Idempotency-Key')).toBe(chave);
    segunda.flush({ id: 1, clienteId: 7, clienteNome: 'x', dataPedido: '2026-11-14T10:00:00Z', valorTotal: 1, itens: [] });
    await harness.fixture.whenStable();
    http.match(() => true);
  });

  // CA-32: erros 409/422 no item afetado
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
    expect(obter<HTMLSelectElement>(el, '#pedido-cliente').value).toBe('7');
    expect(obter<HTMLInputElement>(el, '#item-quantidade-0').value).toBe('2');
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
    expect(obter<HTMLInputElement>(el, '#item-quantidade-0').value).toBe('2');
  });

  // CA-31 (UX, QA-07): produto sem estoque nao e selecionavel
  it('PedidoFormComponent_ProdutoSemEstoque_OpcaoDesabilitada', async () => {
    harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/pedidos/novo', PedidoFormComponent);
    http.expectOne((r) => r.url === '/api/clientes').flush(CLIENTES);
    http
      .expectOne((r) => r.url === '/api/produtos')
      .flush([...PRODUTOS, { id: 40, sku: 'AG-1', nome: 'Agenda', preco: 39.9, estoque: 0 }]);
    harness.detectChanges();
    el = harness.routeNativeElement as HTMLElement;
    escolher('#pedido-cliente', '7');
    adicionarItem();

    expect(obter<HTMLOptionElement>(el, '#item-produto-0 option[value="40"]').disabled).toBeTrue();
    expect(obter<HTMLOptionElement>(el, '#item-produto-0 option[value="12"]').disabled).toBeFalse();
  });

  // CA-31/CA-32 (UX, QA-07): aviso nao bloqueante; o 409 segue como garantia
  it('PedidoFormComponent_QuantidadeAcimaDoEstoque_AvisaSemBloquearEnvio', async () => {
    await abrir();
    preencherValido();
    expect(el.querySelector('#aviso-estoque-0')).toBeNull();

    digitar('#item-quantidade-0', '6'); // estoque exibido = 5
    expect(obter<HTMLElement>(el, '#aviso-estoque-0').textContent).toContain('acima do estoque exibido (5)');
    expect(obter<HTMLElement>(el, '#item-quantidade-0').getAttribute('aria-describedby')).toContain('aviso-estoque-0');
    expect(botaoConfirmar().disabled).toBeFalse();

    digitar('#item-quantidade-0', '5');
    expect(el.querySelector('#aviso-estoque-0')).toBeNull();
  });

  // CA-30 (QA-06): homonimos distinguidos por #id no seletor de cliente
  it('PedidoFormComponent_ClientesHomonimos_MostraIdNoRotulo', async () => {
    harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/pedidos/novo', PedidoFormComponent);
    http.expectOne((r) => r.url === '/api/clientes').flush([
      { id: 1, nome: 'Maria Silva', ativo: true },
      { id: 2, nome: 'Maria Silva', ativo: true },
      { id: 3, nome: 'João Souza', ativo: true },
    ]);
    http.expectOne((r) => r.url === '/api/produtos').flush(PRODUTOS);
    harness.detectChanges();
    el = harness.routeNativeElement as HTMLElement;

    const rotulos = Array.from(el.querySelectorAll('#pedido-cliente option')).map((o) => o.textContent?.trim());
    expect(rotulos).toEqual(['Selecione um cliente', 'Maria Silva (#1)', 'Maria Silva (#2)', 'João Souza']);
  });

  it('PedidoFormComponent_ErroAoCarregarApoio_PermiteTentarNovamente', async () => {
    harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/pedidos/novo', PedidoFormComponent);
    el = harness.routeNativeElement as HTMLElement;
    http.expectOne((r) => r.url === '/api/clientes').flush('x', { status: 500, statusText: 'Err' });
    harness.detectChanges();
    expect(el.textContent).toContain('Não foi possível carregar clientes e produtos');

    botaoComTexto(el, 'Tentar novamente').click();
    http.expectOne((r) => r.url === '/api/clientes').flush(CLIENTES);
    // o forkJoin cancelou o primeiro GET de produtos quando clientes falhou
    const vivo = http.match((r) => r.url === '/api/produtos').find((r) => !r.cancelled);
    if (vivo === undefined) throw new Error('GET /produtos nao reenviado');
    vivo.flush(PRODUTOS);
    harness.detectChanges();
    expect(el.querySelector('#pedido-cliente')).not.toBeNull();
  });
});
