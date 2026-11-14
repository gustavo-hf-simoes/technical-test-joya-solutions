# Revisão segurança — rodada 3 (re-revisão) — 2026-11-14

> Relatório do agente `ciberseguranca` (etapa 5), no modelo de [templates/relatorio-revisao.md](templates/relatorio-revisao.md). O revisor não corrige. Cada achado tem um único dono e volta para este revisor na re-revisão, que confere o relatório do dono **e** o código.
>
> A rodada 1 abriu SEC-01 a SEC-07. A rodada 2 confere as correções e registra os aceites da `HL-29`. Também abre a SEC-08, encontrada ao escanear a imagem corrigida do frontend. A rodada 3 confere a segunda correção da SEC-02 e a primeira da SEC-08.

## Escopo

- **Commits revisados:** árvore de trabalho da branch `us-03-ci` em 2026-11-14, `HEAD` = `181d359` (main + T-11.1, `GET /pedidos/{id}`). O `ci.yml` foi lido como está no `HEAD` (`git show HEAD:.github/workflows/ci.yml`) porque outro agente o altera agora.
- **Histórias cobertas:** US-01 a US-10, US-11 (T-11.1), US-13 a US-16 e US-18. Fora: US-12 (idempotência), US-17 (deploy) e T-11.2, todas pendentes.
- **Superfícies:** API .NET (`backend/src`), scripts SQL e logins (`database/`), SPA Angular e Nginx (`frontend/`), `e2e/`, Dockerfiles, `docker-compose.yml`, `.env.example`, `.github/` (workflows, dependabot), `.mcp.json` e `.claude/mcp/`, `.claude/hooks/` e `.claude/skills/VENDORED.md`.
- **Ferramentas e comandos usados:**
  - `dotnet list backend/Joya.Pedidos.sln package --vulnerable --include-transitive`
  - `npm --prefix frontend audit --omit=dev` (e com `--json`), `npm --prefix frontend audit` (com dev), o mesmo em `e2e/` (`npm_config_cache=D:/.cache/npm`)
  - `rg`/`git grep` por SQL bruto, *sinks* de XSS, segredos e arquivos versionados
  - `curl` contra a aplicação que já estava no ar em `http://localhost:4200` (sem tocar no Docker). Só `GET`, um `OPTIONS` e `POST`s rejeitados antes do banco (`413` e `415`); nenhum dado foi gravado
  - MCP `mssql` para `sys.database_permissions`: **falhou com `AUTH_FAILED`** (pendência `CG-05` já conhecida). Os privilégios foram conferidos pelo script `database/03-logins.sql` e pelos testes `Banco/LoginsTests.cs`
  - Não foram rodadas suítes de teste nem comandos Docker, por restrição da delegação

### Rodada 2 (re-revisão)

- **Base conferida:** árvore de trabalho da branch `chore/achados-revisao-1` (correções sem commit sobre `181d359`): `git diff` de `frontend/nginx.conf`, `frontend/seguranca.inc` (novo), `frontend/Dockerfile`, `frontend/angular.json`, `e2e/package*.json`, `Core/Common/PeriodoRegras.cs`, `Core/Common/Periodo.cs`, testes dos validadores, `docker-compose.yml`, `.github/dependabot.yml` e `.github/workflows/codeql.yml`. As respostas dos donos foram lidas em `04-frontend.md` ("Correção, rodada 1 (HL-29)"), no fim de `03-backend.md` e na `HL-29` de `docs/plan.md`.
- **Aplicação no ar** (compose reconstruído pelo orquestrador; não subi nem derrubei nada): `curl -D -` em `127.0.0.1:4200` nos caminhos `/`, `/pedidos`, `/pedidos/novo`, `main-*.js`, `/naoexiste.js` (404), `/api/clientes`, `/api/swagger/index.html`, `/api/pedidos?dataFim=9999-12-31`, `/api/relatorios/top-clientes?dataInicio=0001-01-01&dataFim=9999-12-31`, `/api/pedidos/999999999` e um `POST` de 100 KB (413 do Nginx). `docker compose ps`; `docker compose exec frontend nginx -v` e `id`.
- **Dependências:** `npm --prefix e2e audit` e `dotnet list backend/Joya.Pedidos.sln package --vulnerable --include-transitive`.
- **Imagens:** `docker scout cves --only-severity critical,high` na imagem local `joya-pedidos-frontend:local` e no registro (`registry://nginxinc/nginx-unprivileged:1.28-alpine` e `:1.30-alpine`, `--platform linux/amd64`).
- O gate do orquestrador (build, 113 unitários, 130 de integração, format, lint, build e 27 testes do frontend, E2E 1/1) foi aceito como evidência e não foi repetido.

### Rodada 3 (re-revisão)

- **Base conferida:** `e2e/cabecalhos-seguranca.spec.ts` (novo), `frontend/Dockerfile:10` e a seção "Correção, rodada 2: SEC-02 (teste de regressão) e SEC-08" de `04-frontend.md`. Também conferi que o job `e2e` do `.github/workflows/ci.yml` roda `npm test` (linhas 152-154), que executa todo `*.spec.ts` (`playwright.config.ts`, `testMatch`).
- **Aplicação no ar** (imagem reconstruída com `--pull` pelo orquestrador; não subi nem derrubei nada): `docker compose exec frontend nginx -v` → `nginx/1.30.6`, `id` → `uid=101(nginx)`; `docker compose ps` → três serviços em `127.0.0.1`, frontend `healthy`; `curl -D -` em `/`, `/pedidos` e `/api/clientes` → os quatro cabeçalhos em cada um.
- **Imagem:** `docker scout cves --only-severity critical,high joya-pedidos-frontend:local`.
- Evidência do orquestrador aceita: `nginx -t` ok e `npm --prefix e2e test` → 2 passed (SEC-02 e CA-30).

## Resumo

Situação depois da rodada 3:

| Severidade | Abertos | Resolvidos | Aceitos (HL) |
|---|---|---|---|
| Crítica | 0 | 0 | 0 |
| Alta | 0 | 0 | 0 |
| Média | 0 | 1 (SEC-08) | 1 (SEC-01, HL-29) |
| Baixa | 0 | 5 (SEC-02, SEC-03, SEC-04, SEC-06, SEC-07) | 1 (SEC-05, HL-29) |

| Dono | Achados |
|---|---|
| frontend | SEC-01 (Média, Aceito HL-29), SEC-02 (Baixa, Resolvido na rodada 3), SEC-07 (Baixa, Resolvido), SEC-08 (Média, Resolvido na rodada 3) |
| backend | SEC-03 (Baixa, Resolvido), SEC-04 (Baixa, Resolvido), SEC-05 (Baixa, Aceito HL-29) |
| orquestrador | SEC-06 (Baixa, Resolvido) |

**Gate:** a etapa **passa**, sem nenhum achado em aberto. Ficam dois aceites da `HL-29`, SEC-01 e SEC-05, e o resíduo da base Alpine aceito na `HL-30`, depois de remover o `curl` (ver "Riscos aceitos").

O núcleo de risco do domínio está sólido: SQL só parametrizado, baixa de estoque atômica com `CHECK (Estoque >= 0)`, preço vindo do banco, ordem de *lock* determinística, limites de corpo e de taxa, Problem Details sem detalhe de exceção e contêineres sem root.

## Achados

### SEC-01 — Angular 19.2.25 fora de suporte, com advisories publicados e sem correção na linha 19

| Campo | Valor |
|---|---|
| Severidade | Média |
| Dono | frontend |
| Regra ou requisito | CWE-1104 (componente sem manutenção), CWE-1395 (dependência com vulnerabilidade conhecida) |
| Local | `frontend/package.json:13-19` (`@angular/*` `^19.2.0`); `frontend/package-lock.json:1275` (`@angular/core` 19.2.25) |
| Status | Aceito (HL-29) |

**Evidência.** `npm --prefix frontend audit --omit=dev` → `7 vulnerabilities (3 moderate, 4 high)`, todas em `@angular/*` 19.2.25, faixa afetada `<=19.2.25`. O `fixAvailable` é sempre `22.2.6` (`isSemVerMajor: true`): **não há correção na linha 19**. O Angular 19 saiu do LTS em 2026. Advisories (runtime):

| Pacote | Advisory | Severidade | Aplica a este app? |
|---|---|---|---|
| `@angular/common` | GHSA-48r7-hpm6-gfxm (OOM em `formatDate`) | Alta | Não hoje: o único uso é `date: 'dd/MM/yyyy'` (formato constante) sobre `dataPedido` gerado pelo servidor (`pedido-lista.component.html:128`) |
| `@angular/common` | GHSA-39pv-4j6c-2g6v, GHSA-jhpw-976m-542j, GHSA-p297-fm68-3q8c (`HttpTransferCache`) | Alta/Média | Não: sem SSR nem `provideClientHydration` (`app.config.ts`) |
| `@angular/common` | GHSA-jfgx-762w-99vr (vazamento de token XSRF) | Média | Não: não há cookie XSRF |
| `@angular/core` | GHSA-rgjc-h3x7-9mwg (hidratação) | Alta | Não: sem hidratação |
| `@angular/core`/`compiler` | GHSA-jj27-h5hq-8x99 (XSS via i18n) | Alta | Não: sem i18n |
| `@angular/core`/`compiler` | GHSA-hh8m-fm6v-7cvg, GHSA-58w9-8g37-x9v5 (bypass de sanitização) | Média | Não hoje: nenhum `[href]`/`[src]`/`host` com dado externo; formulários reativos, sem *two-way binding* em propriedade sensível |
| `@angular/router` | GHSA-ff3f-86qr-9cv3, GHSA-62vg-58rm-qff7 (DoS em SSR) | Alta | Não: sem SSR |

**Cenário.** Nenhum advisory é explorável no código atual. Esse é o motivo da severidade Média, e não Alta. O risco é estrutural: o framework de UI não recebe mais correções. Um template novo que use `[href]`, *host binding* ou i18n herda bypasses de sanitização já publicados, e o próximo advisory de XSS do Angular não terá correção para a 19. O `npm audit` completo (com dev) mostra ainda `49 vulnerabilities (2 low, 31 moderate, 14 high, 2 critical)` na cadeia de build (`tar`, `piscina`, `vite`, `webpack-dev-middleware`, `http-proxy-middleware`, `postcss`, `serialize-javascript`). Elas afetam a máquina de desenvolvimento (`ng serve`) e o estágio de build da imagem, não o *bundle* servido. A maioria só sai do ar com a mesma troca de *major* do `@angular-devkit/build-angular`.

**Correção esperada.** Uma de duas:
1. Atualizar `@angular/*`, `@angular/cli` e `@angular-devkit/build-angular` para uma linha suportada (21 LTS ou 22), regenerar o `package-lock.json` e deixar `npm --prefix frontend audit --omit=dev` sem `high`/`critical`. Lint, build, testes de unidade e o E2E devem continuar verdes.
2. Ou registrar o aceite por `HL`, com esta tabela de não aplicabilidade e a restrição explícita de não usar SSR, hidratação, i18n, `[innerHTML]` e *bindings* de URL com dado externo até a atualização.

**Histórico de re-revisão.**

| Rodada | Resposta do dono | Conferência (relatório + código) | Status |
|---|---|---|---|
| 1 | — | — | Aberto |
| 2 | Aceite de risco pelo desenvolvedor (`HL-29` em `docs/plan.md`): só a linha 22 corrige os advisories, e nenhum é explorável no código atual | `frontend/package.json` não mudou (Angular 19.2.25, como esperado num aceite). A `HL-29` cita o motivo da não aplicabilidade. **Condição do aceite:** não usar SSR, hidratação, i18n, `[innerHTML]` nem *bindings* de URL com dado externo enquanto a linha 19 continuar. O Dependabot `npm` (SEC-06) vai abrir o PR da *major* em separado | Aceito (HL-29) |

### SEC-02 — `X-Frame-Options` e `Referrer-Policy` somem do `index.html` (herança do `add_header` no Nginx); sem CSP

| Campo | Valor |
|---|---|
| Severidade | Baixa |
| Dono | frontend |
| Regra ou requisito | CWE-1021 (clickjacking), CWE-693; checklist "Cabeçalhos e CORS" |
| Local | `frontend/nginx.conf:13-14` (no `server`), `:27-28` e `:34-35` (`add_header` dentro de `location`) |
| Status | Resolvido (rodada 3) |

**Evidência.** No Nginx, uma `location` que declara qualquer `add_header` **não herda** os `add_header` do `server`. Na aplicação no ar:

```text
curl -D - http://localhost:4200/pedidos       → Cache-Control: no-cache, X-Content-Type-Options: nosniff
                                                 (sem X-Frame-Options, sem Referrer-Policy)
curl -D - http://localhost:4200/main-*.js     → idem
curl -D - http://localhost:4200/api/pedidos   → X-Frame-Options: DENY, Referrer-Policy: no-referrer
                                                 (a location /api/ não tem add_header e herda)
```

Não há `Content-Security-Policy` em nenhuma resposta.

**Cenário.** A proteção contra *framing* foi declarada, mas não vale justamente para o documento que pode ser emoldurado, o `index.html` da SPA. Uma página maliciosa aberta por um usuário da rede interna carrega `http://<host>:4200/pedidos/novo` num `iframe` transparente e induz cliques que criam um pedido e debitam estoque. A API em si não aceita CSRF (`POST` *form* → `415`, sem CORS → preflight `405`), mas o *clickjacking* passa por cima disso porque a ação sai da própria origem. O impacto é limitado, porque o formulário exige vários passos, por isso a severidade é Baixa. A falta de CSP elimina a segunda camada contra XSS.

**Correção esperada.** Os cabeçalhos de segurança precisam chegar em **todas** as respostas. Por exemplo: um `include` com os `add_header` repetido em cada `location`, ou o módulo `headers-more`. Junto, uma CSP compatível com o build do Angular, ao menos `default-src 'self'; frame-ancestors 'none'; object-src 'none'; base-uri 'self'`, com `style-src`/`script-src` ajustados ao que o Angular 19 e o Tailwind exigem. Para não regredir: uma verificação automatizada (passo do E2E ou script de CI) que confere `X-Frame-Options` (ou `frame-ancestors`), `Referrer-Policy` e `Content-Security-Policy` em `/`, numa rota da SPA e num ativo `.js`.

**Histórico de re-revisão.**

| Rodada | Resposta do dono | Conferência (relatório + código) | Status |
|---|---|---|---|
| 1 | — | — | Aberto |
| 2 | `04-frontend.md`: os cabeçalhos foram para `seguranca.inc`, incluído em todas as `location`, com CSP, Nginx 1.28 e `inlineCritical: false`. "Não incluí o teste automatizado de cabeçalhos sugerido pelo achado" | **Código:** `add_header` saíram do `server`, e `include /etc/nginx/conf.d/seguranca.inc` está em `nginx.conf:15`, `:24` e `:32` (as três `location`). O `.inc` não casa com o `include conf.d/*.conf` padrão da imagem, então não é carregado duas vezes. A CSP não tem `unsafe-eval`, e `script-src` é só `'self'`. O `'unsafe-inline'` em `style-src` está justificado pelo `<style>` que o Angular injeta. **No ar:** `curl -D -` em `/`, `/pedidos`, `/pedidos/novo`, `main-ZFQLPNBN.js`, `/naoexiste.js` (404), `/api/clientes` e `/api/...` com 400 devolve `X-Content-Type-Options`, `X-Frame-Options: DENY`, `Referrer-Policy: no-referrer` e a CSP completa com `frame-ancestors 'none'`. O 413 do Nginx traz `X-Frame-Options` e CSP. O `index.html` só tem `<script src=... type="module">`, sem *script* inline nem `onload`, e o orquestrador viu o console limpo e o CSS sem `media=print`. **Teste:** nenhum. `rg "Content-Security\|X-Frame\|Referrer-Policy" e2e .github/workflows` não encontra nada. O defeito original vinha justamente de uma regra pouco visível do Nginx (`add_header` numa `location` anula os do `server`). Uma `location` nova sem o `include` reabre o problema sem nenhum aviso | **Parcial**: a vulnerabilidade está corrigida. Falta o teste pedido na correção esperada, por exemplo um `test` do Playwright com `request.get()` em `/`, `/pedidos` e no primeiro `.js` do `index.html`, que confira os três cabeçalhos. Opção alternativa: o desenvolvedor aceita a ausência do teste |
| 3 | `04-frontend.md` ("Correção, rodada 2"): teste de regressão `e2e/cabecalhos-seguranca.spec.ts` em `/`, `/pedidos`, um `.js` e `/api/clientes` | **Teste:** `CabecalhosSeguranca_RotasDaSpaEAtivos_TrazemCspEDemais` usa `request` (sem navegador). Tira o `.js` do próprio `index.html` e checa as três `location` do `nginx.conf` (SPA, ativos por regex e `/api/`). Em cada rota confere `nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: no-referrer`, a CSP com `default-src 'self'`, `frame-ancestors 'none'` e `object-src 'none'`, e a ausência de `unsafe-eval`. Também exige que o `index.html` tenha um `.js`, então não passa sem verificar nada. Uma `location` nova que não seja um destes caminhos não é coberta. É uma limitação aceitável, porque o defeito original estava justamente nestas três. **CI:** o job `e2e` roda `npm test` contra `docker compose up --build`, então o teste impede a regressão nos PRs. **Execução:** 2 passed no gate do orquestrador. **No ar:** os quatro cabeçalhos aparecem em `/`, `/pedidos` e `/api/clientes` com a imagem nova | Resolvido |

### SEC-03 — `dataFim=9999-12-31` derruba a consulta em `500` (exceção não tratada a partir da entrada)

| Campo | Valor |
|---|---|
| Severidade | Baixa |
| Dono | backend |
| Regra ou requisito | CWE-20, CWE-248; RN-15, spec 5.1 (entrada inválida é `400 VALIDACAO`) |
| Local | `backend/src/Joya.Pedidos.Core/Common/Periodo.cs:32` (`dataFim?.AddDays(1)`); validação em `Core/Common/PeriodoRegras.cs` |
| Status | Resolvido (rodada 2) |

**Evidência.**

```text
GET /api/pedidos?dataFim=9999-12-31                                        → 500 ERRO_INTERNO
GET /api/relatorios/top-clientes?dataInicio=0001-01-01&dataFim=9999-12-31  → 500 ERRO_INTERNO
GET /api/relatorios/top-clientes?dataInicio=2026-01-01&dataFim=9999-12-30  → 200
```

`DateOnly.MaxValue.AddDays(1)` lança `ArgumentOutOfRangeException`. O validator aceita a data, porque o formato está correto, e a exceção estoura no serviço.

**Cenário.** Qualquer cliente, sem autenticação e sem limite de taxa nos `GET`, provoca `500` à vontade. Cada um grava um `LogError` com *stack trace* (`ErroInternoExceptionHandler`). Um laço de requisições inunda o log e mascara erros reais. Não há vazamento: o corpo é o `ERRO_INTERNO` genérico. O defeito também viola o contrato (deveria ser `400`).

**Correção esperada.** A regra de período rejeita, com `400 VALIDACAO` no campo `dataFim`, a data cujo dia seguinte não é representável. Pode ser uma faixa de anos plausível, por exemplo 2000 a 2100, a ser decidida com o `arquiteto` se mudar a spec. Para não regredir: testes unitários do validator (`dataFim = 9999-12-31` → erro em `dataFim`) e um teste de integração em `GET /pedidos` e em `GET /relatorios/top-clientes` que espera `400`.

**Histórico de re-revisão.**

| Rodada | Resposta do dono | Conferência (relatório + código) | Status |
|---|---|---|---|
| 1 | — | — | Aberto |
| 2 | `03-backend.md` ("Resposta a achados", rodada 1): o validator rejeita `9999-12-31` em `DataFim`, e `Periodo.Criar` devolve `Error.Validation` em vez de deixar `AddDays(1)` lançar exceção. Testes unitários e de integração novos | **Código:** em `PeriodoRegras.cs:27-31`, `fim == DateOnly.MaxValue` gera `AddFailure("DataFim", ...)`, antes da comparação início/fim. Em `Periodo.cs:30-33`, a guarda devolve `Error.Validation`, que serve de segunda barreira para outros chamadores. É a única data cujo dia seguinte não existe, então a correção cobre todo o domínio da entrada. **Testes:** dois `Validate_DataFimNoMaximoDoCalendario_RetornaErroNoCampoDataFim` (listagem e ranking) e `[InlineData]` novos em `GetPedidos_ParametrosInvalidos_Retorna400` e `GetTopClientes_SemPeriodo_Retorna400`. Os 113 unitários e os 130 de integração passaram no gate do orquestrador. **No ar:** as duas URLs da evidência original agora dão `400` com `code: VALIDACAO` e `errors.dataFim`, sem `500` | Resolvido |

### SEC-04 — Frontend publicado em todas as interfaces expõe a API aberta e o Swagger à rede local

| Campo | Valor |
|---|---|
| Severidade | Baixa |
| Dono | backend |
| Regra ou requisito | CWE-668 (recurso exposto à esfera errada), CWE-200; spec 5.1 (Swagger) |
| Local | `docker-compose.yml:88` (`"4200:8080"`), `:74` (`ASPNETCORE_ENVIRONMENT: Docker`); `backend/src/Joya.Pedidos.Api/Program.cs:86` |
| Status | Resolvido (rodada 2) |

**Evidência.** O comentário do compose (linha 11) diz "Swagger só no loopback (127.0.0.1:8080)". Mas o `frontend` publica `4200` em `0.0.0.0`, e o Nginx repassa `/api/*` inteiro:

```text
GET http://localhost:4200/api/swagger/index.html     → 200
GET http://localhost:4200/api/swagger/v1/swagger.json → 200
GET http://localhost:4200/api/health/ready            → Healthy
```

**Cenário.** Quem roda `docker compose up` numa rede compartilhada (escritório, Wi-Fi de evento) deixa qualquer máquina dessa rede criar pedidos e esgotar estoque pela API sem autenticação. Isso é risco aceito por escopo, mas só vale para uso local. Essa máquina também lê o contrato completo pelo Swagger. O mesmo `nginx.conf` vai para o deploy (US-17): se o ambiente lá não for `Production`, o Swagger fica público.

**Correção esperada.** Publicar o frontend só no loopback (`"127.0.0.1:4200:8080"`), coerente com `db` e `api`, e corrigir o comentário. Para a US-17, deixar registrado que o deploy roda com `ASPNETCORE_ENVIRONMENT=Production`, sem Swagger. Opcionalmente, o dono do `nginx.conf` (`frontend`) bloqueia `/api/swagger` com `return 404`. A verificação é por leitura do compose e por `curl` em `/api/swagger` no deploy.

**Histórico de re-revisão.**

| Rodada | Resposta do dono | Conferência (relatório + código) | Status |
|---|---|---|---|
| 1 | — | — | Aberto |
| 2 | `03-backend.md`: frontend em `127.0.0.1:4200:8080` e comentário do cabeçalho atualizado. Para a US-17, deploy com `ASPNETCORE_ENVIRONMENT=Production`, sem Swagger | **Código:** o `docker-compose.yml:90` publica `"127.0.0.1:4200:8080"`, e as linhas 13-14 explicam o motivo. **No ar:** `docker compose ps` mostra `frontend 127.0.0.1:4200->8080/tcp`, `api 127.0.0.1:8080` e `db 127.0.0.1:1433`, ou seja, nenhuma porta em `0.0.0.0`. Teste automatizado não se aplica a uma porta do compose; a leitura do arquivo e o `ps` bastam. O Swagger continua acessível por `127.0.0.1:4200/api/swagger` (200), mas só da própria máquina, o mesmo alcance da `127.0.0.1:8080`. A parte de produção fica como pendência da US-17 (ver "Precisa verificação") | Resolvido |

### SEC-05 — `joya_app` pode alterar pedidos e itens (UPDATE/INSERT no schema inteiro)

| Campo | Valor |
|---|---|
| Severidade | Baixa (defesa em profundidade) |
| Dono | backend |
| Regra ou requisito | CWE-250, CWE-272 (menor privilégio); RN-12 (pedido imutável); spec 6.5 |
| Local | `database/03-logins.sql:66` (`GRANT SELECT, INSERT, UPDATE ON SCHEMA::dbo TO joya_app`) |
| Status | Aceito (HL-29) |

**Evidência.** A API só faz `UPDATE` em `Produtos.Estoque` (`EstoqueRepository.cs`; nenhum outro `Update`/`ExecuteUpdate` em `backend/src`) e só faz `INSERT` em `Pedidos` e `ItensPedido`. O *grant* no schema inteiro permite também `UPDATE dbo.Pedidos SET ValorTotal = ...`, `UPDATE dbo.ItensPedido SET PrecoUnitario = ...`, `UPDATE dbo.Produtos SET Preco = ...` e `INSERT` em `Clientes`/`Produtos`. O `DENY DELETE` existe e está testado (`LoginsTests.LoginApp_TentaDelete_PermissaoNegada`). A consulta em `sys.database_permissions` pelo MCP não rodou (`AUTH_FAILED`, `CG-05`).

**Cenário.** Não há injeção SQL na API, por isso a severidade é Baixa. Se a credencial do `joya_app` vazar, porém, quem a tiver adultera pedidos fechados e preços praticados, quebrando a RN-12, sem precisar de `DELETE`. O privilégio está de acordo com a spec 6.5: a correção exige uma `PROP`.

**Correção esperada.** Propor a mudança da spec 6.5 (`PROP` ao `arquiteto`/desenvolvedor) e aplicar: `GRANT SELECT ON SCHEMA::dbo`, `GRANT INSERT ON dbo.Pedidos, dbo.ItensPedido`, `GRANT UPDATE (Estoque) ON dbo.Produtos`, mantendo o `DENY DELETE`. Considerar a coluna de idempotência da US-12. Para não regredir: casos novos em `LoginsTests` (`UPDATE dbo.Pedidos` e `UPDATE dbo.Produtos SET Preco` negados para `joya_app`). Ou, alternativamente, o aceite por `HL`.

**Histórico de re-revisão.**

| Rodada | Resposta do dono | Conferência (relatório + código) | Status |
|---|---|---|---|
| 1 | — | — | Aberto |
| 2 | Aceite de risco pelo desenvolvedor (`HL-29`): `UPDATE`/`INSERT` do `joya_app` de acordo com a spec 6.5; logins não alterados (`03-backend.md`) | `git diff database/` vazio, como esperado num aceite. O `DENY DELETE` e a ausência de DDL continuam testados (`LoginsTests`). Recomendação para produção: os *grants* por objeto e por coluna descritos na correção esperada, por `PROP` | Aceito (HL-29) |

### SEC-06 — Dependabot e CodeQL não cobrem NuGet, npm, Docker, C# e TypeScript

| Campo | Valor |
|---|---|
| Severidade | Baixa |
| Dono | orquestrador |
| Regra ou requisito | CWE-1104, CWE-1395; HL-08 |
| Local | `.github/dependabot.yml:7` (só `github-actions`); `.github/workflows/codeql.yml:37` (`linguagem: [actions, python]`) |
| Status | Resolvido (rodada 2) |

**Evidência.** Os próprios comentários dos arquivos dizem que `nuget`, `npm` e `docker` (Dependabot) e `csharp` e `javascript-typescript` (CodeQL) "entram com o esqueleto". O esqueleto já existe: `backend/`, `frontend/`, `e2e/` e os dois Dockerfiles. Nenhum job do `ci.yml` no `HEAD` roda `npm audit` ou `dotnet list package --vulnerable`.

**Cenário.** Os advisories da SEC-01 e da SEC-07 e a imagem `nginx-unprivileged:1.27-alpine`, de um ramo do Nginx que não recebe mais atualizações (ver "Precisa verificação"), entraram sem nenhum alerta. Uma CVE nova num pacote NuGet ou npm do projeto continuaria invisível até uma revisão manual.

**Correção esperada.** Incluir no `dependabot.yml` os ecossistemas `nuget` (`/backend`), `npm` (`/frontend` e `/e2e`) e `docker` (`/backend`, `/frontend`). Acrescentar `csharp` (com *build* manual ou `autobuild`) e `javascript-typescript` à matriz do CodeQL. Opcionalmente, um passo de CI com `dotnet list package --vulnerable --include-transitive` e `npm audit --omit=dev --audit-level=high`. A coordenação fica com o agente que altera o `ci.yml` agora (US-03).

**Histórico de re-revisão.**

| Rodada | Resposta do dono | Conferência (relatório + código) | Status |
|---|---|---|---|
| 1 | — | — | Aberto |
| 2 | Orquestrador (delegação da rodada 2): `dependabot.yml` com `nuget` (`/backend`), `npm` (`/frontend`, `/e2e`) e `docker` (`/backend`, `/frontend`), com *minor* e *patch* agrupados. `codeql.yml` com `csharp` e `javascript-typescript`, `build-mode: none`. Sem passo de `npm audit` nem de `dotnet list --vulnerable` no CI (era opcional) | **Código:** o `dependabot.yml` tem os três ecossistemas novos, com `directories`, `schedule` semanal e `open-pull-requests-limit: 5`. As *majors* ficam fora dos grupos e saem em PRs separados, o que é coerente com a SEC-01. O `codeql.yml:37` tem a matriz `[actions, python, csharp, javascript-typescript]`, com `build-mode: none` e `queries: security-extended`. As permissões do job não mudaram (`contents: read`, `actions: read`, `security-events: write`) e as actions continuam fixadas por SHA. Sem `pull_request_target` e sem expressão em `run:`. **Observação, não bloqueia:** o `db` usa `mcr.microsoft.com/mssql/server:2022-latest` no compose, que nenhum ecossistema cobre. Como a tag é flutuante, a imagem se atualiza a cada *pull*. O passo de auditoria no CI segue como recomendação. O efeito da cobertura nova já aparece na SEC-08: o Dependabot `docker` deve propor `1.28-alpine` → `1.30-alpine` assim que o arquivo estiver na `main` | Resolvido |

### SEC-07 — Playwright 1.49.1 baixa navegadores sem validar o certificado TLS

| Campo | Valor |
|---|---|
| Severidade | Baixa |
| Dono | frontend |
| Regra ou requisito | CWE-295, CWE-494; GHSA-7mvr-c777-76hp |
| Local | `e2e/package.json:9` (`"@playwright/test": "1.49.1"`) |
| Status | Resolvido (rodada 2) |

**Evidência.** `npm --prefix e2e audit` → `playwright <1.55.1 — Playwright downloads and installs browsers without verifying the authenticity of the SSL certificate`, `2 high severity vulnerabilities`. Com `--omit=dev` → `0`, porque é dependência de desenvolvimento.

**Cenário.** Ao rodar `npx playwright install` numa rede com interceptação (proxy corporativo, Wi-Fi hostil) ou no CI da T-03.5, um atacante em posição de MITM troca o binário do navegador e ganha execução de código na máquina do desenvolvedor ou no *runner*. A severidade é Baixa porque exige posição de rede privilegiada.

**Correção esperada.** Fixar `@playwright/test` em `>= 1.55.1` (versão exata, como hoje), regenerar o `e2e/package-lock.json`, deixar `npm --prefix e2e audit` com 0 vulnerabilidades e manter o E2E verde.

**Histórico de re-revisão.**

| Rodada | Resposta do dono | Conferência (relatório + código) | Status |
|---|---|---|---|
| 1 | — | — | Aberto |
| 2 | `04-frontend.md`: `@playwright/test` 1.49.1 → 1.65.1 (versão exata), *lock* atualizado, `npm audit` com 0 vulnerabilidades | **Código:** `e2e/package.json:9` tem `"1.65.1"` exata. No `e2e/package-lock.json`, `node_modules/@playwright/test` e `node_modules/playwright` estão em 1.65.1, acima da 1.55.1 que corrige o GHSA-7mvr-c777-76hp. **Execução:** `npm --prefix e2e audit` → `found 0 vulnerabilities`. O E2E 1/1 passou no gate do orquestrador. O *lock* com versão exata é o que impede a regressão, e o Dependabot `npm` em `/e2e` (SEC-06) vigia o resto | Resolvido |

### SEC-08 — A imagem corrigida do frontend (`nginx-unprivileged:1.28-alpine`) também está parada, com CVEs críticas no Nginx e no OpenSSL

| Campo | Valor |
|---|---|
| Severidade | Média |
| Dono | frontend |
| Regra ou requisito | CWE-1104 (componente sem manutenção), CWE-1395 (dependência com vulnerabilidade conhecida); checklist "Contêineres" |
| Local | `frontend/Dockerfile:10` (`FROM nginxinc/nginx-unprivileged:1.28-alpine`) |
| Status | Resolvido (rodada 3) |

**Evidência.** Este achado fecha o item da rodada 1 em "Precisa verificação". A correção da SEC-02 trocou a `1.27-alpine` pela `1.28-alpine`, mas no registro a tag `1.28-alpine` também parou no tempo:

```text
docker compose exec frontend nginx -v                          → nginx/1.28.2
docker scout cves --only-severity critical,high joya-pedidos-frontend:local
  nginx   1.28.2-r1  → 4C 8H  (correções em 1.28.3-r0 a 1.28.3-r6: CVE-2026-9256, -42945, -42533, -42055 críticas)
  openssl 3.5.5-r0   → 4C 20H (correções em 3.5.6-r0 a 3.5.8-r0)
  zlib 1.3.1-r2 1C 1H · pcre2 10.47-r0 3H · curl 8.17.0-r1 17C 36H · expat 1C 9H · util-linux 6H · ...
docker scout cves --platform linux/amd64 registry://nginxinc/nginx-unprivileged:1.28-alpine
  → mesmo resultado (nginx 1.28.2-r1, openssl 3.5.5-r0): o problema não é cache local, a tag está parada
docker scout cves --platform linux/amd64 registry://nginxinc/nginx-unprivileged:1.30-alpine
  → nginx e openssl sem CVE crítica ou alta; restam curl 7C 17H, expat 4H, zlib 1C 1H, libxslt 3H, brotli 1C, musl 1H
```

**Cenário.** O Nginx é o único processo que recebe tráfego do usuário: serve a SPA e faz o proxy de `/api`. Os pacotes `nginx` e `pcre2` (usado nas `location ~*`) estão no caminho de cada requisição. O `scout` não traz a descrição das CVEs, então não confirmei quais são alcançáveis com esta configuração (HTTP puro na 8080, sem TLS, HTTP/2, HTTP/3 nem módulos dinâmicos carregados). Por isso o achado é Médio, e não Alto: o componente é conhecidamente vulnerável e não recebe mais atualizações, mas a exploração concreta não foi demonstrada. Hoje o alcance é só o loopback (SEC-04). Na US-17, a mesma imagem fica atrás do Caddy e recebe tráfego da internet: se for publicada assim, a severidade sobe para **Alta**. O `curl`, o `openssl` usado como biblioteca de cliente e o `expat` não estão no caminho da requisição. Com eles, o risco é pós-exploração, não entrada.

**Correção esperada.** Em `frontend/Dockerfile:10`, trocar a imagem pela linha estável mantida: `nginxinc/nginx-unprivileged:1.30-alpine`, ou a mais recente que o `scout` mostrar sem CVE crítica ou alta em `nginx`, `openssl` e `pcre2`. Reconstruir com `--pull`. Manter `seguranca.inc` e o usuário 101. Para não regredir: o Dependabot `docker` em `/frontend` (SEC-06), já configurado, propõe a próxima tag. Opcionalmente, um passo de CI com `docker scout cves --only-severity critical,high --exit-code` ou `trivy image --severity CRITICAL,HIGH --exit-code 1` na imagem do frontend. Conferência: `nginx -v` no contêiner e o `scout` sem crítica ou alta em `nginx`, `openssl` e `pcre2`. Os pacotes que sobram na 1.30 (`curl`, `expat` etc.) vêm da Alpine base e não ficam no caminho da requisição; eles podem ficar registrados como risco residual ou sair com `apk del curl`, porque o `HEALTHCHECK` (`frontend/Dockerfile:15-16`) usa `wget`, do BusyBox, e não o `curl`.

**Histórico de re-revisão.**

| Rodada | Resposta do dono | Conferência (relatório + código) | Status |
|---|---|---|---|
| 2 | — | Aberto nesta re-revisão, a partir do item "Imagem `nginx-unprivileged:1.27-alpine`" de "Precisa verificação" | Aberto |
| 3 | `04-frontend.md` ("Correção, rodada 2"): `FROM nginxinc/nginx-unprivileged:1.30-alpine`; `nginx.conf` e `seguranca.inc` sem mudança | **Código:** `frontend/Dockerfile:10` usa `1.30-alpine`. O usuário 101, a porta 8080, o `HEALTHCHECK` com `wget` e o `COPY` do `seguranca.inc` não mudaram. **No ar:** `nginx/1.30.6`, `uid=101(nginx)`, contêiner `healthy`, e o orquestrador rodou `nginx -t` com sucesso. **Scan** (`docker scout cves --only-severity critical,high joya-pedidos-frontend:local`): `nginx`, `openssl` e `pcre2` saíram da lista, ou seja, nenhuma CVE crítica ou alta no caminho da requisição. Restam pacotes da base Alpine: `curl` 8.22.0 (7C 17H), `expat` (4H), `libxslt` (3H), `libxml2` (1H), `musl` (1H), `brotli` (1C) e `zlib` 1.3.2 (1C 1H). O `curl`, o `expat`, a `libxml2` e a `libxslt` só são usados por ferramentas e módulos dinâmicos que esta configuração não carrega. O `brotli` não está ativo no `nginx.conf`. O `zlib` só entraria com `gzip on`, que também não está ligado. Esses pacotes ficam como risco residual. Depois, o `curl` foi removido e o resto foi aceito na `HL-30` (ver "Riscos aceitos"). **Regressão:** o Dependabot `docker` em `/frontend` (SEC-06) vigia a tag | Resolvido |

## Dependências

| Pacote | Versão | Onde | CVE / advisory | Ação |
|---|---|---|---|---|
| `@angular/core`, `common`, `compiler`, `router`, `forms`, `platform-browser(-dynamic)` | 19.2.25 | `frontend` (runtime) | GHSA-48r7-hpm6-gfxm, GHSA-39pv-4j6c-2g6v, GHSA-jhpw-976m-542j, GHSA-p297-fm68-3q8c, GHSA-jfgx-762w-99vr, GHSA-58w9-8g37-x9v5, GHSA-jj27-h5hq-8x99, GHSA-hh8m-fm6v-7cvg, GHSA-rgjc-h3x7-9mwg, GHSA-ff3f-86qr-9cv3, GHSA-62vg-58rm-qff7 | SEC-01: Aceito (HL-29); não explorável hoje |
| `@angular-devkit/build-angular`, `@angular/build`, `@angular/cli` e transitivos (`tar`, `piscina`, `vite`, `webpack-dev-middleware`, `http-proxy-middleware`, `postcss`, `serialize-javascript`, `pacote`, `sigstore`) | 19.2.x | `frontend` (dev/build) | 49 no total (2 critical: `tar`, `piscina`) | SEC-01: some com a troca de *major*; afeta só a máquina de desenvolvimento e o estágio de build |
| `rxjs` | 7.8.2 | `frontend` | — | Nenhuma |
| `@playwright/test` / `playwright` | 1.49.1 → **1.65.1** (rodada 2) | `e2e` (dev) | GHSA-7mvr-c777-76hp (corrigido); `npm --prefix e2e audit` → 0 | SEC-07 Resolvido |
| `Microsoft.EntityFrameworkCore.SqlServer` | 8.0.34 | backend | — (`--vulnerable --include-transitive`: nenhum) | Nenhuma |
| `FluentValidation` | 11.11.0 | backend | — | Nenhuma |
| `Swashbuckle.AspNetCore` | 6.9.0 | backend | — | Nenhuma |
| Pacotes de teste (`xunit`, `NSubstitute`, `Shouldly`, `Respawn`, `Testcontainers.MsSql` 4.16.0, `ArchUnitNET`, `Mvc.Testing` 8.0.11) | fixados | testes | — | Nenhuma |
| Runtime .NET 8 (`aspnet:8.0`, SDK `8.0.400` *rollForward* `latestFeature`) | 8.0 | imagem da API | Fim do suporte em 2026-11-10 | Risco aceito proposto (abaixo) |
| `nginxinc/nginx-unprivileged` | `1.27-alpine` → `1.28-alpine` (rodada 2) → **`1.30-alpine`** (rodada 3; Nginx 1.30.6) | imagem do frontend | Na 1.28: nginx 4C 8H, openssl 4C 20H. Na 1.30: nginx, openssl e pcre2 sem C/H; base Alpine com curl 7C 17H, expat 4H, libxslt 3H, brotli 1C, zlib 1C 1H, libxml2 1H, musl 1H | SEC-08 Resolvido. `curl` removido; o resíduo da base Alpine foi aceito na `HL-30` |
| `mcr.microsoft.com/mssql/server` | `2022-latest` (flutuante) | `db` do compose | Não escaneado | Fora do Dependabot; atualiza a cada *pull*. Sem ação |
| `@bytebase/dbhub` | 1.6.1 (fixado em `.claude/mcp/dbhub.mjs`) | ferramenta de desenvolvimento | não auditado por `npm audit` (via `npx`) | Nenhuma imediata |

Todas as versões NuGet são exatas nos `.csproj`. No npm, os *ranges* `^` do `frontend/package.json` ficam fixados pelo `package-lock.json` (o Dockerfile usa `npm ci`), e o `e2e` fixa a versão exata.

## Verificado sem achado

| Item do checklist | Como foi verificado | Evidência |
|---|---|---|
| Injeção SQL | `rg "FromSqlRaw\|ExecuteSqlRaw\|SqlQueryRaw\|FromSqlInterpolated\|SqlQuery\|ExecuteSql"` em todo o repositório; leitura de `EstoqueRepository`, `PedidoConsultas` e `RelatorioConsultas` | Único SQL bruto: `Database.SqlQuery<decimal>($"""UPDATE ... {quantidade} ... {produtoId}""")`, um `FormattableString` que vira `@p0..@p2`. O resto é LINQ, sem ordenação nem filtro montados a partir de texto. Scripts SQL sem `EXEC(@sql)`/`sp_executesql`. As variáveis `$(APP_SQL_PASSWORD)` do `sqlcmd` são controladas pelo operador, e o `init.sh` recusa aspa simples |
| Validação e abuso: itens e quantidade | `CriarPedidoValidator.cs` | `ClienteId > 0`, itens não vazios, `<= Pedido.MaximoItens`, quantidade entre mínimo e máximo, produto sem duplicata, tudo antes da action (`ValidationFilter`) |
| Validação e abuso: paginação | `ListarPedidosQueryValidator.cs`, `PedidoConsultas.cs` | `pageSize` de 1 a 100, `page >= 1`, deslocamento em `long` (sem estouro do `Skip`) |
| Validação e abuso: corpo | `Program.cs` (`MaxRequestBodySize = 32 KiB` + `LimiteDeCorpoFilter`), `nginx.conf` (`client_max_body_size 64k`) | `POST` com 40 KB → `413` Problem Details da API. Com 100 KB → `413` do Nginx. Nenhum gravou dado |
| Validação e abuso: taxa | `LimiteCriarPedidoPolicy.cs` | Janela fixa por IP (10/10 s), `QueueLimit = 0`, `429` com `Retry-After`. O IP vem do `X-Forwarded-For` com `ForwardLimit = 1`, e o Nginx usa `$proxy_add_x_forwarded_for`, então a API pega o IP que o Nginx viu. Um `X-Forwarded-For` forjado pelo cliente fica à esquerda e é ignorado. Só dá para forjar indo direto em `127.0.0.1:8080`, o que exige acesso local |
| Validação e abuso: timeout de consulta | `DependencyInjection.cs` | Sem `CommandTimeout` explícito: vale o padrão de 30 s do SqlClient. `CancellationToken` repassado até o EF (cancelamento do cliente interrompe a consulta) |
| XSS e cliente | `rg "innerHTML\|outerHTML\|bypassSecurityTrust\|DomSanitizer\|[innerHtml]\|eval(\|document.write\|localStorage"` em `frontend/src`; leitura dos *bindings* | Nenhuma ocorrência. Textos da API (`problema.detail`, nomes) só por interpolação `{{ }}`, que o Angular escapa. Nenhum `[href]`/`[src]` dinâmico (`routerLink` só com constantes) |
| URL da API e segredo no *bundle* | `pedidos-api.ts`, `clientes-api.ts`, `produtos-api.ts`, `index.html` | Só caminhos relativos `/api/...`. Sem `environment` com URL ou chave. `Idempotency-Key` gerada com `crypto.randomUUID()` |
| CORS e CSRF | `Program.cs` (sem `AddCors`); `curl` | Preflight de `http://evil.example` → `405` sem `Access-Control-*`. `POST` *form-urlencoded* → `415`. A API só aceita JSON |
| Cabeçalhos (parcial) | `curl -D -` | `X-Content-Type-Options: nosniff` em todas as respostas; `server_tokens off` (`Server: nginx`, sem versão). O resto está na SEC-02. **Rodada 2:** os quatro cabeçalhos e a CSP chegam em SPA, ativos, `404`, `413` e `/api` (ver SEC-02) |
| Exposição de erro | `ErroInternoExceptionHandler.cs`, `RequisicaoHttpInvalidaExceptionHandler.cs`; respostas `400`/`500` reais | `500` com `title`, `detail` genérico, `code` e `traceId`, sem mensagem de exceção nem *stack trace*. A exceção vai só ao log JSON. Erros de *binding* devolvem a mensagem do framework ("The value 'abc' is not valid."), sem detalhe interno |
| Swagger fora de desenvolvimento | `Program.cs:86` | Só em `Development`, `Docker` e `Testing`; em `Production` não sobe. A exposição pelo `4200` está na SEC-04 |
| Health checks | `/health/live`, `/health/ready` | Devolvem só `Healthy`/`Unhealthy`, sem detalhe do banco |
| Menor privilégio: API não usa `sa` | `docker-compose.yml:76`, `appsettings.json` | A API conecta como `joya_app`. O `appsettings.json` não tem connection string. `sa` só no `db` e no `db-init` |
| Menor privilégio: `claude_mcp` | `03-logins.sql:79-91`, `LoginsTests.cs` (escrita e DDL negadas) | `db_datareader` + `VIEW DEFINITION`, `SHOWPLAN`, `VIEW DATABASE STATE`; `DENY INSERT, UPDATE, DELETE`. O `dbhub.toml` tem `readonly = true` e `max_rows = 200`. A consulta ao vivo pelo MCP falhou (`AUTH_FAILED`) |
| Menor privilégio: `joya_app` | `03-logins.sql:66-70` | `DENY DELETE` e sem DDL (testados). O excesso de `UPDATE`/`INSERT` está na SEC-05 |
| Segredos | `git ls-files` + `git log --diff-filter=A` por arquivos de ambiente; `git grep -i "password\|senha\|pwd=\|secret\|api_key"` | Nenhum arquivo de ambiente real versionado ou no histórico. O `.gitignore` cobre `.env`, `.env.*`, `*.pfx` e `*.key`. Senhas só como padrões de desenvolvimento no `docker-compose.yml`, no `.env.example` e no `.mcp.json`, todas identificadas como tal. Nenhuma no código C#/TS nem nos scripts SQL (chegam por `-v`) |
| Contêineres: usuário | `backend/Dockerfile` (`USER app`), `frontend/Dockerfile` (`nginx-unprivileged`, uid 101, porta 8080) | Sem root nos dois |
| Contêineres: `HEALTHCHECK` | Dockerfiles e compose | Presente na API (`/health/ready`), no frontend e no `db` (sem `-P` na linha de comando) |
| Contêineres: portas | `docker-compose.yml` | `db` em `127.0.0.1:1433` e `api` em `127.0.0.1:8080`. O `frontend` em `0.0.0.0` está na SEC-04. **Rodada 2:** `docker compose ps` mostra os três serviços em `127.0.0.1`, e o frontend roda como `uid=101(nginx)` |
| Contêineres: contexto de build | `backend/.dockerignore`, `frontend/.dockerignore` | `bin/`, `obj/`, `tests/`, `node_modules`, `dist` fora. Multi-stage: SDK e Node não vão para a imagem final |
| Contêineres: imagens base | Dockerfiles, compose | Fixadas por tag de versão (`8.0`, `22-alpine`, `1.27-alpine`, `2022-latest`), não por *digest*. Aceitável com Dependabot `docker` (SEC-06) |
| CI: gatilhos e permissões | `git show HEAD:.github/workflows/ci.yml`, `codeql.yml` (skill `gha-security-review`) | Só `push` na `main` e `pull_request`, sem `pull_request_target`/`workflow_run`. `permissions: contents: read` no topo. `security-events: write` só no job do CodeQL |
| CI: injeção de expressão | Leitura dos `run:` | Nenhum `${{ github.event.* }}` em `run:`. Só `${{ matrix.linguagem }}` (constante) em `with:` |
| CI: *pinning* | `uses:` | Todas as actions fixadas por SHA com comentário de versão. `checkout` com `persist-credentials: false`. `pip install "pyyaml==6.0.2"` fixado |
| Cadeia de ferramentas IA: hooks | `.claude/hooks/*.sh` | Leem o JSON do `stdin` com `node`/`jq` e usam o caminho só em `case` e `echo`, sem `eval` nem execução do conteúdo. O `post-edit.sh` executa apenas `dotnet build`/`test` em caminhos fixos |
| Cadeia de ferramentas IA: MCP | `.mcp.json`, `.claude/mcp/dbhub.mjs`, `dbhub.toml` | Credencial do `claude_mcp` (só leitura), `readonly = true`, versão do DBHub fixada (`1.6.1`). O `spawn` com `shell: true` só recebe o caminho do próprio arquivo, sem entrada externa |
| Cadeia de ferramentas IA: skills vendorizadas | `VENDORED.md`, `git log -- .claude/skills` | Depois do commit de vendorização (`0e9dc59`), só a skill própria `esteira` foi alterada. Nenhuma skill de terceiro mudou sem registro |
| Integridade: preço (RN-04/RN-09) | `CriarPedidoItemRequest.cs`, `EstoqueRepository.cs`, `PedidoService.cs` | O DTO só tem `ProdutoId` e `Quantidade`. O preço vem de `OUTPUT inserted.Preco` da mesma instrução que debita. Um campo extra no JSON é ignorado |
| Integridade: estoque negativo (RN-08) | `EstoqueRepository.cs`, `01-schema.sql:63`, `ConcorrenciaTests.cs` | `UPDATE ... WHERE Estoque >= @q` atômico, exige transação aberta, `ThrowIfNegativeOrZero(quantidade)`; `CK_Produtos_Estoque CHECK (Estoque >= 0)` como última barreira. O CA-10 está coberto por `ConcorrenciaTests` (`[InlineData(5, 1, 5, 0)]`) |
| Integridade: *deadlock* | `PedidoService.cs` (`OrderBy(i => i.ProdutoId)`), `ConcorrenciaTests.PostPedidos_DoisProdutosEmOrdensOpostas_SemDeadlockNemEstoqueNegativo` | Ordem de *lock* determinística, com teste de ordens opostas |
| `GET /pedidos/{id}` (T-11.1) | `PedidosController.cs`, `PedidoConsultas.ObterPorIdAsync` | `{id:int}` na rota, LINQ parametrizado, `AsNoTracking`, sem dado além do contrato. A enumeração de ids é consequência da falta de autenticação (risco aceito) |

## Riscos aceitos

| Risco | Motivo | Recomendação para produção | Decisão (HL-xx) |
|---|---|---|---|
| Ausência de autenticação e autorização: qualquer cliente com acesso à rede cria pedidos, lista todos e lê qualquer pedido por id (ids sequenciais) | Fora do escopo do enunciado (spec §2.2) | Gateway com OIDC/JWT, autorização por perfil, e `GET /pedidos/{id}` e listagem restritos ao escopo do usuário | Declarado no README e na spec §2.2; reafirmado na `HL-29` |
| Angular 19.2.25 fora de suporte, com advisories sem correção na linha 19 (SEC-01) | Nenhum advisory é explorável no código atual (tabela da SEC-01); só a linha 22 corrige | Migrar para Angular 21 LTS ou 22. Até lá: sem SSR, hidratação, i18n, `[innerHTML]` nem *binding* de URL com dado externo | `HL-29` |
| `joya_app` com `UPDATE`/`INSERT` no schema `dbo` inteiro (SEC-05) | De acordo com a spec 6.5; não há injeção SQL na API | *Grants* por objeto e por coluna (`UPDATE (Estoque) ON dbo.Produtos`), por `PROP` | `HL-29` |
| Senhas padrão de desenvolvimento versionadas no `docker-compose.yml`, no `.env.example` e no `.mcp.json` | Permitir `docker compose up` sem configuração. Valores identificados como desenvolvimento; os três serviços só no loopback | Segredos por *secret store* ou variáveis do provedor, senhas fortes e distintas, sem padrão no compose de produção (US-17) | `HL-29` |
| `TrustServerCertificate=True` na connection string e `sslmode = "require"` sem validação no MCP | Certificado autoassinado do SQL Server em contêiner, na rede interna do compose | Certificado válido e `TrustServerCertificate=False` no deploy | `HL-29` |
| .NET 8 fora de suporte desde 2026-11-10 (imagens `sdk:8.0`/`aspnet:8.0`) | A spec fixou .NET 8 (o enunciado aceita ".NET 8 ou superior"). Hoje `dotnet list --vulnerable` não aponta nada (conferido de novo na rodada 2). Migrar às vésperas da entrega tem custo e risco | Migrar para .NET 10 LTS (EF Core 10) logo depois da entrega, por `PROP` na spec | `HL-29` |
| Limite de taxa só no `POST /pedidos`; `GET` sem limite e relatório sem período máximo | Decisão do ADR-0002 (o `GET` é barato, indexado e paginado). O timeout padrão de 30 s do SqlClient limita a consulta mais longa | Limite global por IP no gateway e período máximo no relatório (ex.: 366 dias), se o volume crescer | `HL-29` (e ADR-0002) |
| Pacotes da base Alpine com CVE crítica ou alta na imagem do frontend, fora do caminho da requisição (resíduo da SEC-08): `brotli` 1C, `expat` 4H, `libxml2` 1H, `libxslt` 3H, `musl` 1H, `zlib` 1C/1H | O `curl` foi removido: `frontend/Dockerfile` faz `USER root` / `RUN apk del --no-cache curl` / `USER 101`. Conferi no contêiner que `id -u` = 101 e que `command -v curl` vem vazio. O orquestrador viu `nginx -t` ok, o contêiner `healthy`, o E2E com 2 passed e o `scout` com só a lista ao lado. O Nginx não usa nenhum desses pacotes com este `nginx.conf` (sem `gzip`, `brotli`, `xslt` nem módulos dinâmicos), e o `HEALTHCHECK` usa o `wget` do BusyBox. O Dependabot `docker` traz a próxima tag | Imagem distroless ou *scratch* com Nginx estático, ou *scan* com `--exit-code` no CI | Aceito (`HL-30`) |

## Precisa verificação

- ~~**Imagem `nginxinc/nginx-unprivileged:1.27-alpine`**~~ **Encerrado na rodada 2: virou a SEC-08.** A troca para a `1.28-alpine` não resolveu, porque essa tag também está parada no registro, com Nginx 1.28.2 e OpenSSL 3.5.5.
- **Swagger e `ASPNETCORE_ENVIRONMENT` no deploy (US-17).** No compose local, `127.0.0.1:4200/api/swagger` responde `200`, aceitável porque só alcança o loopback (SEC-04). No deploy, conferir que a API roda com `ASPNETCORE_ENVIRONMENT=Production` e que `curl https://<host>/api/swagger/index.html` dá `404`, como o `backend` registrou em `03-backend.md`.
- **`Proxy__ForwardedLimit=2` no deploy (US-17).** O cálculo só fica correto se o Caddy substituir o `X-Forwarded-For` recebido do cliente, que é o padrão sem `trusted_proxies`, e o Nginx acrescentar o IP do Caddy. Conferir no deploy com um `X-Forwarded-For` forjado: o limite de taxa precisa continuar valendo por IP real.
- **Hooks de limite como barreira.** O `guard-bash.sh` casa `git commit` por regex, mas `git -C . commit` e variações não casam. Ele protege contra engano do modelo, não contra um adversário; a proteção efetiva é a regra de que só o orquestrador faz commit, com autorização. Não é achado da aplicação; fica como observação para o `orquestrador`.
- ~~**`404` de `GET /api/pedidos/999999999` no ambiente no ar**~~ **Encerrado na rodada 2.** Com o compose reconstruído, a resposta é `404` com `code: PEDIDO_NAO_ENCONTRADO` e `detail` "O pedido 999999999 não existe.". Era mesmo a imagem antiga.
