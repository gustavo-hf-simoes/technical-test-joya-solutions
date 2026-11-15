interface ClienteIdentificavel {
  id: number;
  nome: string;
}

function chave(nome: string): string {
  return nome.trim().toLocaleLowerCase('pt-BR');
}

/** Nomes que aparecem mais de uma vez na lista de clientes carregados. */
export function nomesRepetidos(clientes: readonly { nome: string }[]): ReadonlySet<string> {
  const vistos = new Set<string>();
  const repetidos = new Set<string>();
  for (const { nome } of clientes) {
    const k = chave(nome);
    if (vistos.has(k)) repetidos.add(k);
    vistos.add(k);
  }
  return repetidos;
}

/** `Nome (#id)` só quando o nome se repete entre os clientes (QA-06); senão, só o nome. */
export function rotuloCliente(cliente: ClienteIdentificavel, repetidos: ReadonlySet<string>): string {
  return repetidos.has(chave(cliente.nome)) ? `${cliente.nome} (#${cliente.id})` : cliente.nome;
}
