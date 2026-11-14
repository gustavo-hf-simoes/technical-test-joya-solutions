/**
 * Problem Details (RFC 9457) com as extensões da spec §5.1 e §5.2.
 * As extensões são opcionais porque cada código de erro traz um subconjunto.
 */
export interface ItemSemEstoque {
  produtoId: number;
  produtoNome: string;
  quantidadeSolicitada: number;
  estoqueDisponivel: number;
}

export interface ProblemaApi {
  type?: string;
  title?: string;
  status: number;
  detail?: string;
  code?: string;
  traceId?: string;
  errors?: Record<string, string[]>;
  produtoIds?: number[];
  itens?: ItemSemEstoque[];
}
