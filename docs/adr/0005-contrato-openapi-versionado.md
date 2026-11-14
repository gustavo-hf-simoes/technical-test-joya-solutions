# ADR-0005 — Contrato OpenAPI versionado e verificado nos dois lados

- **Status:** Proposto (etapa 2, arquitetura)
- **Data:** 2026-11-13
- **Referências:** [plan.md](../plan.md) `D8`, `D12`; spec §5; [02-arquitetura.md](../esteira/02-arquitetura.md) §5; [ADR-0003](0003-banco-real-nos-testes-de-integracao.md) (ambiente `Testing`)

## Contexto

O contrato JSON está escrito na spec §5 em prosa e em JSON Schema. O backend o implementa em C#, e o frontend o consome em TypeScript. Sem um artefato comum, um campo renomeado no backend (`quantidadeItens` → `qtdItens`) passa pelo build dos dois lados e só aparece na tela, ou no E2E, se houver um que leia esse campo.

A etapa 2 pede que o backend exporte `docs/contratos/openapi.json`, que o frontend se tipe a partir dele e que um teste compare o arquivo com o que a API publica. Falta decidir como gerar, como comparar e como o frontend usa o arquivo sem dependência nova (regra de ouro 12).

## Decisão

1. **Gerador: Swashbuckle**, que já vem no modelo `webapi` do .NET 8 e serve o `/swagger` exigido pela spec §5.1. Não se adiciona `Microsoft.AspNetCore.OpenApi` nem NSwag.
2. **Ajustes sem pacote:**
   - `ObrigatoriosSchemaFilter` (`ISchemaFilter`, cerca de 20 linhas) marca em `required` toda propriedade cujo tipo não é anulável, de acordo com `NullabilityInfoContext`. Assim, o `required` do OpenAPI fica igual ao da spec;
   - `SupportNonNullableReferenceTypes()`;
   - `MapType<decimal>(() => new OpenApiSchema { Type = "number", Format = "decimal" })`, para que o contrato não anuncie `double` em dinheiro;
   - `CustomSchemaIds` com nomes estáveis para genéricos (`PaginaResponseDePedidoResumoResponse`).
3. **Arquivo versionado:** `docs/contratos/openapi.json`, com indentação de 2 espaços e final de linha `LF`. É gerado, nunca editado à mão.
4. **Verificação no backend:** o teste de integração `Contrato_OpenApiPublicado_IgualAoVersionado` faz `GET /swagger/v1/swagger.json` na `WebApplicationFactory`, interpreta as duas versões como `JsonNode` e compara com `JsonNode.DeepEquals`. Se divergirem, falha com a mensagem "contrato mudou: rode com ATUALIZAR_CONTRATO=1 e revise o diff". Com a variável de ambiente `ATUALIZAR_CONTRATO=1`, o teste regrava o arquivo em vez de comparar. A raiz do repositório é achada subindo diretórios a partir de `AppContext.BaseDirectory` até encontrar `docs/contratos`.
5. **Tipos no frontend:** interfaces escritas à mão em `frontend/src/app/core/api/`, com os nomes dos schemas. Modelos de resposta não têm propriedades opcionais. O erro é a interface `ProblemaApi` (spec §5.1), com as extensões opcionais.
6. **Verificação no frontend:** `contrato.spec.ts` importa o `openapi.json` (`resolveJsonModule`). Para cada modelo, declara um exemplo com `satisfies <Modelo>`, e o compilador garante que o exemplo tem exatamente as propriedades da interface. Em seguida compara as chaves do exemplo com `properties` e `required` do schema e o tipo de cada valor com o `type`. Se o Angular não aceitar a importação de fora de `frontend/`, o caminho alternativo é um script `pretest` que copia o arquivo para `frontend/src/app/core/api/openapi.json`, ignorado pelo Git. O `frontend` registra no relatório o caminho escolhido.

## Alternativas consideradas

| Alternativa | Por que não |
|---|---|
| Gerar os tipos TypeScript (`openapi-typescript`, `ng-openapi-gen`, NSwag) | Dependência nova e passo de geração no build, para 7 modelos. O teste de chaves detecta a mesma classe de erro |
| `Microsoft.AspNetCore.OpenApi` | Pacote novo no .NET 8. O documento nativo só amadureceu no .NET 9 |
| Exportar o arquivo com a CLI `Swashbuckle.AspNetCore.Cli` | Ferramenta global a mais. O próprio teste já sobe a API e consegue gravar o arquivo |
| Comparar texto em vez de `JsonNode` | Quebra por espaço e ordem de serialização, sem mudança de contrato |
| Testes de *snapshot* das respostas, sem OpenAPI | Não dá ao frontend um artefato para comparar e não documenta o contrato |

## Consequências

- **Positivas:** uma mudança de contrato aparece como diff em `docs/contratos/openapi.json` no PR e quebra o teste do lado que não acompanhou. A spec continua sendo a fonte normativa, e o arquivo é a evidência do que foi implementado.
- **Negativas:**
  - O teste de contrato depende do Swagger ligado no ambiente `Testing` (`PROP-12`).
  - Os limites numéricos e os cabeçalhos ficam fora da comparação e continuam a cargo dos testes de integração.
  - Atualizar o contrato exige rodar o teste de integração, que precisa do Docker.
- **Ações:**
  - Uma tarefa nova de backend (C2) cria o filtro, o arquivo e o teste, depois que os 3 endpoints e os de apoio existirem.
  - A T-14.2 do frontend passa a incluir o `contrato.spec.ts`.
  - O hook `guard-paths.sh` precisa permitir que o `backend` escreva em `docs/contratos/`. Hoje não há como saber se permite, e isso é uma pergunta ao orquestrador.
