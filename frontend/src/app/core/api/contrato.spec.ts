import { ClienteResponse } from './clientes.models';
import { PaginaResponse, PedidoResponse, PedidoResumoResponse } from './pedidos.models';
import { ProblemaApi } from './problema-api.model';

/**
 * Exemplos da spec §5 verificados pelo compilador (`satisfies`: propriedade a
 * mais ou a menos não compila). A comparação com docs/contratos/openapi.json
 * (ADR-0005) entra quando a T-11.2 publicar o arquivo.
 */
describe('Contrato', () => {
  it('Contrato_ModelosDoFrontend_BatemComOsExemplosDaSpec', () => {
    const resumo = {
      id: 1042,
      clienteId: 7,
      clienteNome: 'Mercado Bom Preço Ltda',
      dataPedido: '2026-11-13T14:32:07.123Z',
      valorTotal: 359.7,
      quantidadeItens: 2,
    } satisfies PedidoResumoResponse;
    const pagina = {
      items: [resumo],
      page: 1,
      pageSize: 20,
      totalItems: 137,
      totalPages: 7,
    } satisfies PaginaResponse<PedidoResumoResponse>;
    const pedido = {
      id: 1042,
      clienteId: 7,
      clienteNome: 'Mercado Bom Preço Ltda',
      dataPedido: '2026-11-13T14:32:07.123Z',
      valorTotal: 359.7,
      itens: [
        { produtoId: 12, produtoNome: 'Teclado', quantidade: 2, precoUnitario: 149.9, subtotal: 299.8 },
      ],
    } satisfies PedidoResponse;
    const cliente = { id: 1, nome: 'Cliente', ativo: true } satisfies ClienteResponse;
    const problema = {
      status: 409,
      code: 'ESTOQUE_INSUFICIENTE',
      itens: [{ produtoId: 12, produtoNome: 'Teclado', quantidadeSolicitada: 2, estoqueDisponivel: 1 }],
    } satisfies ProblemaApi;

    expect(Object.keys(resumo).sort()).toEqual([
      'clienteId',
      'clienteNome',
      'dataPedido',
      'id',
      'quantidadeItens',
      'valorTotal',
    ]);
    expect(Object.keys(pagina).sort()).toEqual(['items', 'page', 'pageSize', 'totalItems', 'totalPages']);
    expect(Object.keys(pedido).sort()).toEqual([
      'clienteId',
      'clienteNome',
      'dataPedido',
      'id',
      'itens',
      'valorTotal',
    ]);
    expect(Object.keys(cliente).sort()).toEqual(['ativo', 'id', 'nome']);
    expect(problema.code).toBe('ESTOQUE_INSUFICIENTE');
  });
});
