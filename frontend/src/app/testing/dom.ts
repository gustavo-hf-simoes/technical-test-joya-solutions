/** Helpers de teste: estreitam o tipo com verificação explícita, sem `!` (QA-08). */
export function obter<T extends Element>(raiz: ParentNode, seletor: string): T {
  const el = raiz.querySelector<T>(seletor);
  if (el === null) throw new Error(`Elemento não encontrado: ${seletor}`);
  return el;
}

export function botaoComTexto(raiz: ParentNode, texto: string): HTMLButtonElement {
  const botao = Array.from(raiz.querySelectorAll<HTMLButtonElement>('button')).find((b) =>
    b.textContent?.includes(texto),
  );
  if (botao === undefined) throw new Error(`Botão não encontrado: ${texto}`);
  return botao;
}
