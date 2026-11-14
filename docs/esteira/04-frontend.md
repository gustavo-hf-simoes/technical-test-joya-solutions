# Relatório da etapa 4 — Frontend (`frontend`)

## Rodada 1 — 2026-11-14 — US-14

### Tarefas

| Tarefa | Status | Mensagem de commit prevista | Arquivos (separáveis por tarefa) |
|---|---|---|---|
| T-14.1 | Pronta | `chore(frontend): cria o projeto Angular 19 com Tailwind e pt-BR` | `frontend/package.json`, `package-lock.json`, `angular.json`, `eslint.config.js`, `.postcssrc.json`, `proxy.conf.json`, `tsconfig*.json`, `.gitignore`, `.editorconfig`, `public/`, `src/index.html`, `src/main.ts`, `src/styles.css`, `src/app/app.config.ts`, `src/app/app.routes.ts`, `src/app/app.component.ts`, `src/app/app.component.spec.ts` |
| T-14.2 | Pronta, com pendência (ver Desvios) | `feat(frontend): adiciona os clientes HTTP de pedidos e clientes` | `frontend/src/app/core/api/*` (`pedidos-api.ts`, `clientes-api.ts`, `pedidos.models.ts`, `clientes.models.ts`, `problema-api.model.ts`, `pedidos-api.spec.ts`, `contrato.spec.ts`) |
| T-14.3 | Pronta | `feat(frontend): cria a tela de listagem de pedidos com filtros (CA-28)` | `frontend/src/app/pedidos/pedido-lista.component.ts`, `.html` (formulário de filtros, tabela e paginação), `.spec.ts` |
| T-14.4 | Pronta | `feat(frontend): mostra estados de carregamento, vazio e erro (CA-29)` | Mesmos arquivos da T-14.3. Trechos da T-14.4: no `.ts`, o tipo `EstadoLista`, o `signal estado`, `tentarNovamente$`/`tentarNovamente()` e o `catchError`/`startWith` do `combineLatest`; no `.html`, os `@case ('carregando')`, `@case ('erro')` e o ramo `Nenhum pedido encontrado`, marcados por comentário. Testes da T-14.4 no `.spec.ts`: `ConsultaEmAndamento`, `RespostaVazia`, `FalhaNaApi` |
| T-14.5 | Pronta | `feat(frontend): empacota o frontend em Nginx com proxy para a API` | `frontend/Dockerfile`, `frontend/nginx.conf`, `frontend/.dockerignore` |

Observação para o commit: `node_modules/` e `dist/` já estão no `.gitignore` do projeto (criado pelo CLI). O `package-lock.json` entra na T-14.1.

### Testes criados ou alterados

| Teste | Tipo | Cobre |
|---|---|---|
| `AppComponent_Inicializacao_UsaLocalePtBr` | componente | T-14.1, RNF-08 |
| `PedidosApi_Listar_EnviaFiltrosNaQueryString`, `PedidosApi_ListarSemFiltros_NaoEnviaParametros`, `ClientesApi_Listar_UsaCaminhoRelativo` | unit (`HttpTestingController`) | T-14.2, §5.3 e §5.5 |
| `Contrato_ModelosDoFrontend_BatemComOsExemplosDaSpec` | unit | T-14.2 (parcial, ver Desvios) |
| `PedidoListaComponent_FiltrosAplicados_AtualizaQueryString` | componente | CA-28 |
| `PedidoListaComponent_QueryStringNaEntrada_PreencheFiltrosEConsulta` | componente | CA-28 (sobrevive a recarregamento) |
| `PedidoListaComponent_PeriodoInvertido_NaoConsultaEMostraErro` | componente | RN-15 |
| `PedidoListaComponent_Valores_FormataMoedaEDataEmPtBr` | componente | CA-28, RNF-08 |
| `PedidoListaComponent_ConsultaEmAndamento_MostraCarregando`, `_RespostaVazia_MostraNenhumPedidoEncontrado`, `_FalhaNaApi_MostraErroComTentarNovamente` | componente | CA-29 |

### Verificações executadas

Todas com `npm_config_cache=D:/.cache/npm` e `CHROME_BIN` apontando para o Chrome já instalado (`C:\Program Files\Google\Chrome\Application\chrome.exe`, 157). Nenhum navegador foi baixado.

| Comando | Resultado | Resumo da saída |
|---|---|---|
| `npm --prefix frontend run lint` | Passou | `All files pass linting.` |
| `npm --prefix frontend run build` | Passou | Sem warnings; inicial 288 kB (82 kB transferidos) |
| `npm --prefix frontend test -- --watch=false --browsers=ChromeHeadless` | Passou | `TOTAL: 12 SUCCESS` |
| `docker build -t joya-frontend frontend` | Passou | Imagem criada. A primeira tentativa falhou no `npm ci` dentro do contêiner e a repetição passou sem mudança (provável falha de rede); não investiguei mais |
| Manual T-14.5 (contêiner com `--add-host api:127.0.0.1`) | Parcial | `/` e `/pedidos` (fallback da SPA) respondem 200; processo roda como `uid=101(nginx)`. O proxy real para a API **não** foi provado: o alias aponta para o próprio Nginx. Conferir `/api/health/live` na US-15, no compose |
| Greps FE-01, FE-02, FE-03, FE-06, FE-09 | Passaram | Vazio |

### Desvios em relação à spec

| PROP | Texto atual da spec | Proposta | Motivo |
|---|---|---|---|
| — | — | — | Nenhum desvio de spec. |

Pendência de contrato (não é divergência da spec): `docs/contratos/openapi.json` ainda não existe (T-11.2). Os modelos vieram da spec §5 e `contrato.spec.ts` hoje só confere exemplos com `satisfies` e as chaves esperadas. Falta o teste que lê o `openapi.json` e compara chaves (`properties`/`required`) e tipos (ADR-0005). A T-14.2 só fecha de fato depois disso. A conferir com o contrato: os nomes dos schemas e o campo `valorTotal`/`preco` como `number`; o schema genérico `PaginaResponse<PedidoResumoResponse>` (o Swashbuckle costuma publicá-lo como `PedidoResumoResponsePaginaResponse`); `ClienteResponse` e `ProdutoResponse`; as extensões do `ProblemaApi` (que o OpenAPI não descreve). `ProdutoResponse` e `CriarPedidoRequest` ainda não foram modelados: pertencem à tela de criação (US-13).

### Decisões de UX e de implementação

- Filtros de cliente (select com "Todos os clientes", inclui inativos, pois o histórico pode ser deles), data inicial e data final. "Aplicar" grava na query string (`clienteId`, `dataInicio`, `dataFim`, `page`); a lista consulta a API a partir da URL, então recarregar ou compartilhar o link mantém o filtro (CA-28). "Limpar" remove os parâmetros. Parâmetros inválidos na URL são ignorados em vez de enviados à API.
- `dataFim < dataInicio` é barrado no cliente com mensagem em `role="alert"`, sem chamar a API (RN-15).
- Paginação com Anterior/Próxima e "Página x de y", `page` na URL, tamanho fixo de 20. Mudar o filtro volta à página 1.
- Estado como união discriminada num `signal` (`carregando`, `erro`, `sucesso`); "Tentar novamente" refaz a consulta com os filtros atuais, sem recarregar a página.
- Datas via `DatePipe` `dd/MM/yyyy` (fuso local) e moeda via `CurrencyPipe` `BRL`, com `LOCALE_ID` `pt-BR` e `registerLocaleData`.
- Tailwind 4 (`@tailwindcss/postcss`, `.postcssrc.json`), sem CSS por componente. ESLint 9 com `angular-eslint` 19 (fixado na versão do Angular; a 22 é a mais recente no npm), regras de acessibilidade de template ligadas, `no-explicit-any` e `prefer-on-push` como erro.
- Dependências além das de Angular 19: `tailwindcss`, `@tailwindcss/postcss`, `postcss`, `eslint`, `angular-eslint`, `typescript-eslint`, todas de desenvolvimento e todas previstas no pedido (Tailwind e ESLint). Nenhuma biblioteca de UI ou de estado. Testes em Karma + Jasmine, o runner padrão do CLI 19.
- `AppComponent` expõe `locale` só para o teste exigido pelo backlog. É um detalhe pequeno e fácil de remover.
- Nginx: `client_max_body_size 64k` e `X-Forwarded-For` (ADR-0002), `proxy_pass http://api:8080/;` (FE: DK-06), fallback da SPA, cabeçalhos `nosniff`, `X-Frame-Options` e `Referrer-Policy`, `server_tokens off`. Imagem `nginxinc/nginx-unprivileged:1.27-alpine`; o Nginx só sobe se o host `api` resolver (no compose, `depends_on: api`).
- `proxy.conf.json` (dev) encaminha `/api` para `localhost:8080` removendo o prefixo.

### Acessibilidade (checklist, RNF-08)

Verificado nesta rodada: `<label for>` em cada campo; `<th scope="col">` e `<th scope="row">`; `<caption class="sr-only">`; `role="status"` e `aria-live="polite"` para carregamento e resultado; `role="alert"` para erro e período inválido; `aria-invalid`/`aria-describedby` nas datas; `<nav aria-label="Paginação">`; `lang="pt-BR"`; foco visível nos botões; controles nativos (teclado completo); botões Anterior/Próxima com `disabled`; contraste: texto slate-900/700 sobre branco, botões azul 700 e vermelho 700 com texto branco (acima de 4,5:1 pelas tabelas do Tailwind, não medido com ferramenta). Não verificado ainda: foco no primeiro erro (vale para o formulário de pedido, US-13) e auditoria automática (axe) no E2E (US-16).

## Resposta a achados

| Achado | Rodada | O que foi feito | Onde (arquivo:linha) | Teste que prova | Status declarado |
|---|---|---|---|---|---|
| — | — | Sem achados nesta rodada | — | — | — |
