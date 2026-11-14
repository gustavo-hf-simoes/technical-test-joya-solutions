/** Modelos de pedidos, com os nomes dos schemas da spec §5.2 e §5.3. */

export interface PedidoResumoResponse {
  id: number;
  clienteId: number;
  clienteNome: string;
  /** Instante em UTC (ISO 8601, sufixo Z). */
  dataPedido: string;
  valorTotal: number;
  quantidadeItens: number;
}

export interface ItemPedidoResponse {
  produtoId: number;
  produtoNome: string;
  quantidade: number;
  precoUnitario: number;
  subtotal: number;
}

export interface PedidoResponse {
  id: number;
  clienteId: number;
  clienteNome: string;
  dataPedido: string;
  valorTotal: number;
  itens: ItemPedidoResponse[];
}

/** Envelope de paginação (spec §5.3): `PaginaResponse<PedidoResumoResponse>`. */
export interface PaginaResponse<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}

/** Filtros de `GET /pedidos`. Datas no formato `yyyy-MM-dd`, inclusivas. */
export interface FiltroPedidos {
  clienteId?: number;
  dataInicio?: string;
  dataFim?: string;
  page?: number;
  pageSize?: number;
}
