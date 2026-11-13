# Matriz de rastreabilidade

> Liga cada linha do [enunciado](../requisitos/enunciado.md) ao requisito, à história, à tarefa, ao teste e ao commit que a atendem (`HL-08`). O CI valida esta tabela em todo push e PR com [validar_rastreabilidade.py](../../.github/scripts/validar_rastreabilidade.py).
>
> - **Quem preenche:** o `product-owner` preenche as colunas até **Testes** na etapa 1. O orquestrador preenche **Commits** quando a tarefa é commitada.
> - **Enquanto a tabela não tem linhas**, o validador só avisa. A partir da primeira linha, todas as regras abaixo valem.

## Regras que o CI confere

1. Toda linha não vazia do enunciado aparece em alguma linha da matriz, mesmo as de classe `Contexto`.
2. **Classe** é `Obrigatório`, `Diferencial` ou `Contexto`.
3. As linhas `Obrigatório` e `Diferencial` têm **Requisito** (`RF-xx`, `RNF-xx` ou `ENT-xx`), **US**, **Tarefas** e **Testes** preenchidos. Use `—` para "vazio", o que só é aceito em `Contexto`.
4. Toda `US-xx` e toda `T-xx.n` citada existe em `docs/esteira/backlog.md`, gerado na etapa 1.
5. **Testes** traz nomes de teste entre crases (ex.: `` `CriarAsync_EstoqueInsuficiente_RetornaConflito` ``) ou uma verificação manual no formato `Manual: <como verificar>`, para entregáveis como o vídeo.
6. Quando **Commits** tem um hash:
   - o commit precisa existir no histórico;
   - cada teste citado entre crases precisa existir no código de teste (`backend/tests/`, `frontend/src/`, `e2e/`).

## Matriz

| ENU | Classe | Requisito | US | Tarefas | Testes | Commits |
|---|---|---|---|---|---|---|
