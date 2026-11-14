/** Spec §5.5: `GET /clientes`. */
export interface ClienteResponse {
  id: number;
  nome: string;
  ativo: boolean;
}
