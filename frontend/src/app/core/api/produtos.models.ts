/** Spec §5.5: `GET /produtos`. `preco` e `estoque` são informativos na tela (a verdade é do `POST`). */
export interface ProdutoResponse {
  id: number;
  sku: string;
  nome: string;
  preco: number;
  estoque: number;
}
