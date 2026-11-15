# Revisão arquitetura — rodada 2 — 2026-11-15 (refatoração estrutural, re-revisão)

> Re-revisão de [refatoracao-estrutural-1.md](refatoracao-estrutural-1.md), sob a `HL-33`. O revisor **não corrige**: cada achado tem um único dono.

## Escopo

- Diff revisado: árvore de trabalho em 2026-11-15, branch `chore/refatoracao-estrutural`, sobre `HEAD` `1da52e3`. Inclui os movimentos já preparados no índice (`git mv`) e os 4 arquivos novos não rastreados de `EST-01`.
- Itens cobertos: `EST-01` a `EST-06` e `PROP-15` a `PROP-18`.
- Relatórios dos donos conferidos: `03-backend.md`, seção "Refatoração estrutural (HL-33)"; `04-frontend.md`, seção "Refatoração estrutural (HL-33): EST-06"; `02-arquitetura.md`, "Rodada 2"; `CLAUDE.md`; `.claude/skills/new-endpoint/SKILL.md`; `backend/.editorconfig`; `plan.md` (`HL-33`).
- Ferramentas: `git status`, `git diff` (com `-M`), `git grep --untracked`, `awk`, `diff`, `comm`, `ls`. **Não** rodei `dotnet build`, `dotnet format`, `dotnet test` nem `npm`: o orquestrador roda esses comandos em paralelo, e BE-01, BE-02, BE-03, BE-11 (parte de teste), BE-26 e FE-11 dependem dessa saída.
- Skill `optimizing-ef-core-queries`: não há objeto. O diff da `Infrastructure` só troca `using` (`DependencyInjection.cs` e `Saude/VerificadorDeBanco.cs`). Nenhuma consulta EF, predicado, projeção ou paginação mudou.

## Conclusão

A refatoração saiu **como proposta**. Os 6 `EST` e as 4 `PROP` estão **Resolvidos**:

- os movimentos têm 100% de similaridade;
- o código movido ou dividido é idêntico ao do `HEAD`;
- não sobrou namespace antigo;
- o BE-28 sai vazio;
- `database/`, as rotas, os templates, o compose, os Dockerfiles, o CI e o `e2e/` estão intactos.

Há **1 achado novo**, de severidade Baixa: um `using` redundante deixado pela troca mecânica de namespace (`ARQ-01`). Ele não bloqueia o gate e foi resolvido na rodada 3 (ver "Re-revisão do ARQ-01").

## Resumo

| Severidade | Abertos | Resolvidos | Aceitos (HL) |
|---|---|---|---|
| Crítica | 0 | 0 | 0 |
| Alta | 0 | 0 | 0 |
| Média | 0 | 0 | 0 |
| Baixa | 0 | 1 | 0 |

## Veredito por item

| Item | Dono | Veredito | Evidência (comandos em §Verificações) |
|---|---|---|---|
| `EST-01` | backend | **Resolvido** | Os 4 arquivos novos (`ClienteService.cs`, `ProdutoService.cs`, `IPedidoConsultaService.cs` e `IRelatorioService.cs`) têm 1 tipo cada, e os 4 originais também. V9: nenhuma linha de código do `HEAD` se perdeu. V1: o BE-28 sai vazio. V1d: contra o `HEAD`, o BE-28 lista exatamente esses 4 arquivos. |
| `EST-02` | backend | **Resolvido** | `Core/Common/IVerificadorDeBanco.cs` foi para `Core/Saude/IVerificadorDeBanco.cs` (rename 100%) com o namespace `Joya.Pedidos.Core.Saude`. Os 3 `using` foram ajustados. V2: nenhum uso pelo namespace antigo. `Common/` ficou com `Error`, `IUnitOfWork`, `PaginaResponse`, `Periodo`, `PeriodoRegras`, `Result` e `TipoErro`. |
| `EST-03` | backend | **Resolvido** | `ErrosDoModelState` (`public static`) e `NormalizarChave` (`private static`) estão em `Api/Erros/ProblemasHttp.cs:103-143` (comentário na 103, métodos nas linhas 105 e 139). V7: o corpo é idêntico ao do `HEAD` (36 linhas contra 36, `diff` vazio, ignorando a indentação). `Program.cs:23` chama `ProblemasHttp.ErrosDoModelState(contexto)`, e o arquivo caiu de cerca de 150 para 108 linhas. O `using Microsoft.AspNetCore.Mvc` continua necessário em `Program.cs` (`MvcOptions`, linha 53). |
| `EST-04` | backend | **Resolvido** (o `ARQ-01` foi resolvido na rodada 3) | Os 6 ajudantes estão em `IntegrationTests/Apoio/` (renames 100%), com o namespace `...IntegrationTests.Apoio`. `Infra/` não existe mais (V5). V2: nenhum `IntegrationTests.Infra`, nenhum `using ...IntegrationTests.Api;` e nenhum `using ...IntegrationTests.Persistencia;`. `LimitesHttpTests` e `PipelineDeErrosTests` ganharam o `using ...Apoio`. `InfraestruturaReal` continua em `Persistencia/`. |
| `EST-05` | backend | **Resolvido** | `RelatorioConsultasTests` foi para `Persistencia/` (rename 100%) com o namespace `.Persistencia`. `Relatorios/` ficou só com `GetTopClientesTests` (V5). O backend declara a mesma contagem de testes antes e depois (`--list-tests`: Unit 113, Integration 131); o orquestrador confirma no gate. |
| `EST-06` | frontend | **Resolvido** | `rotulo-cliente.ts` e o `.spec.ts` foram para `pedidos/` (renames 100%). Os 2 imports agora são `'./rotulo-cliente'`. V10: nenhuma referência a `core/rotulo-cliente` em `frontend/` ou `e2e/`. `rotulo-cliente.ts` não importa nada, então o movimento não quebra caminhos relativos. |
| `PROP-15` | orquestrador | **Resolvido** | A árvore do `CLAUDE.md` bate com `ls` (V5 e V11): `Common/` completo, `Estoque/`, `Saude/`, as pastas do `Infrastructure`, as do `Api`, `Apoio/` e `Arquitetura/`, a regra de pastas dos testes de integração e as pastas do frontend. Acrescenta "Um tipo de topo por arquivo, e o namespace segue a pasta (IDE0130 no build)". Ver a observação O-1. |
| `PROP-16` | arquiteto | **Resolvido** | `02-arquitetura.md` §3, item 1, cita `IClienteRepository` e `IProdutoRepository`. A linha `Entidades_Propriedades_NaoTemSetterPublico` traz `Pedido`, `ItemPedido`, `Cliente` e `Produto`. V12: nenhuma menção restante a `IClienteConsultas` ou `IProdutoConsultas`. |
| `PROP-17` | backend | **Resolvido** | `backend/.editorconfig` tem `dotnet_style_namespace_match_folder = true` e `dotnet_diagnostic.IDE0130.severity = warning`. O `03-backend.md` traz o erro real `IDE0130` no `dotnet build` de linha de comando, que apareceu com o namespace antigo num arquivo já movido: a condição da ressalva foi cumprida. V3: namespace igual à pasta em todos os `.cs` de `backend/src` e `backend/tests`; a única exceção é `Program.cs`, que usa *top-level statements*. **Não** reproduzi o erro de forma independente, para não concorrer com o build do gate. |
| `PROP-18` | orquestrador (skill) e arquiteto (BE-28) | **Resolvido** | A etapa 4 da skill separa `I<Recurso>Service.cs` e `<Recurso>Service.cs`, em dois blocos, e cita o BE-28. As linhas 18 e 238 citam `02-indices.sql` e `01-schema.sql`, e não sobrou nenhum `01_schema` (V13). O BE-28 está no §8, usando `git grep` + `awk` porque o `rg` não existe no Git Bash. Ele sai vazio (V1), e o `-L` também sai vazio sobre os 75 arquivos (V1b e V1c): não há arquivo escapando da regex. |

## Achados

### ARQ-01 — `Arranjo.cs` importa o próprio namespace

| Campo | Valor |
|---|---|
| Severidade | Baixa |
| Dono | backend |
| Regra ou requisito | `EST-04` (troca de namespace sem resíduo); higiene de `using`. Não é regra de ouro. |
| Local | `backend/tests/Joya.Pedidos.IntegrationTests/Apoio/Arranjo.cs:2` |
| Status | Resolvido (rodada 3) |

**Evidência.** V4 procura, em cada `.cs` de `backend/`, um `using` igual ao próprio `namespace`:

```text
backend/tests/Joya.Pedidos.IntegrationTests/Apoio/Arranjo.cs:2:using Joya.Pedidos.IntegrationTests.Apoio;
```

O arquivo declara `namespace Joya.Pedidos.IntegrationTests.Apoio;` na linha 5. O diff mostra a origem: `using ...Infra;` virou `using ...Apoio;` por substituição mecânica. No `HEAD`, o `using ...Infra;` era necessário, porque `Arranjo` estava em `.Persistencia` e usava `Sql`.

**Cenário.** Não há efeito de compilação nem de comportamento. O C# aceita a diretiva redundante, e o `IDE0005` não dispara no build porque `GenerateDocumentationFile` está `false` no `Directory.Build.props`. Sobra ruído no arquivo, e ele contradiz o próprio objetivo do `EST-04`, que era tirar `using` desnecessários dos ajudantes.

**Correção esperada.** Remover a linha 2 de `Arranjo.cs`. Critério: o V4 sai vazio, e o `dotnet build` e o `dotnet format --verify-no-changes` continuam limpos. Não exige teste novo, porque é só higiene.

**Histórico de re-revisão.**

| Rodada | Resposta do dono | Conferência (relatório + código) | Status |
|---|---|---|---|
| 2 | — | — | Aberto |
| 3 | `03-backend.md`, "Resposta a achados", linha ARQ-01: removeu a linha 2; build 0 aviso e format exit 0 declarados | Código: `Arranjo.cs` começa com `using System.Globalization;` e `using Microsoft.Data.SqlClient;`, namespace na linha 4. V4 sai vazio. Ver "Re-revisão do ARQ-01" | Resolvido |

## Verificações (comandos e saída)

Todos os comandos rodaram da raiz do repositório, no Git Bash. `g` é um alias para `git grep --untracked`.

| # | Comando | Saída |
|---|---|---|
| V0 | `git status --porcelain=v1 -uall` e `git diff --stat -M` | 46 arquivos, +183/−127. São 10 renames no índice (`IVerificadorDeBanco`, 6 ajudantes, `RelatorioConsultasTests` e os 2 `rotulo-cliente`) e 4 novos não rastreados em `Core`. Também aparece `docs/esteira/revisoes/refatoracao-estrutural-1.md` não rastreado. Nenhum arquivo apagado sem destino. |
| V1 (BE-28) | `g -cE '<regex BE-28>' -- 'backend/src/*.cs' \| awk -F: '$NF > 1 { print $1 }'` | vazio |
| V1b | `g -LE '<regex BE-28>' -- 'backend/src/*.cs'` | vazio: todo arquivo tem ao menos 1 tipo reconhecido |
| V1c | `git ls-files --cached --others --exclude-standard -- 'backend/src/*.cs' \| wc -l` | `75` |
| V1d | `git grep -cE '<regex BE-28>' HEAD -- 'backend/src/*.cs' \| awk -F: '$NF > 1 { print $2 }'` | `Core/Clientes/IClienteService.cs`, `Core/Pedidos/PedidoConsultaService.cs`, `Core/Produtos/IProdutoService.cs` e `Core/Relatorios/RelatorioService.cs`. São os 4 do `EST-01`, o que mostra que a regra detecta o problema original. |
| V2 | `g -nE "IntegrationTests\.Infra\b" -- backend`; `g -nE "using Joya\.Pedidos\.IntegrationTests\.(Api\|Persistencia);" -- backend`; `g -n IVerificadorDeBanco -- backend` | As duas primeiras saem vazias. A terceira dá 4 linhas, todas em `Core/Saude`, `Api/Saude`, `Infrastructure/Saude` e `DependencyInjection.cs`, e nenhuma `Core.Common` as importa por esse tipo. |
| V3 | Laço: o `namespace` da 1ª declaração de cada `.cs` de `backend/src` e `backend/tests`, comparado com o caminho da pasta (`/` → `.`) | Só `SEM NAMESPACE: backend/src/Joya.Pedidos.Api/Program.cs` (esperado). Nenhum `DIVERGE`. |
| V4 | Laço: `grep "^using $ns;"` em cada `.cs` de `backend/`, onde `$ns` é o próprio namespace do arquivo | `Apoio/Arranjo.cs:2` → `ARQ-01` |
| V5 | `ls` nas pastas de `IntegrationTests`, `Apoio/`, `Relatorios/`, `Api/`, `Core/Common` e `Core/Saude`; `test -e .../Infra` | Pastas: `Api`, `Apoio`, `Banco`, `Catalogo`, `Parte4`, `Pedidos`, `Persistencia` e `Relatorios`. `Apoio` tem os 6 ajudantes, `Relatorios` só `GetTopClientesTests.cs`, `Api` só os 3 testes do pipeline. `Infra ausente`. |
| V6 | `git status --porcelain -uall -- database docs/contratos frontend/src/app/app.routes.ts 'frontend/src/**/*.html' frontend/src/app/app.config.ts backend/src/Joya.Pedidos.Api/Controllers docker-compose.yml backend/Dockerfile frontend/Dockerfile frontend/nginx.conf .github e2e` | vazio. `docs/contratos/` não existe: o T-11.2 está fora do escopo pela `HL-28`, então não há `openapi.json` a comparar. |
| V7 | Corpo das 2 funções em `git show HEAD:.../Program.cs` contra o trecho novo de `ProblemasHttp.cs`, sem indentação, sem linhas vazias e com `static`/`public static`/`private static` normalizados | 36 linhas contra 36, `diff` vazio, `IDENTICO` |
| V8 | `git diff -M --summary` e `git diff --cached -M --summary` | 10 renames, todos com `(100%)` |
| V9 | Para cada arquivo dividido do `EST-01`, as linhas de código do `HEAD` (sem `using`, `namespace` e linhas vazias, ordenadas), comparadas por `comm -23` com o par de arquivos novo | vazio nos 4 |
| V10 | `g -nE "core/rotulo-cliente" -- frontend e2e`; `g -nE "Core\.Common.*IVerificador\|Common/IVerificadorDeBanco" -- backend CLAUDE.md .claude` | vazio |
| V11 | `ls` de `Core`, `Infrastructure`, `Infrastructure/Persistencia`, `Api`, `UnitTests` e `frontend/src/app` | Bate com a árvore do `CLAUDE.md` (ver O-1) |
| V12 | `g -nE "IVerificadorDeBanco\|ApiFactory\|SqlServerFixture\|IntegrationTests/(Infra\|Api)/\|rotulo-cliente\|Arranjo\|IClienteConsultas\|IProdutoConsultas" -- docs/esteira/02-arquitetura.md README.md docs/adr CLAUDE.md docs/spec.md` | Só `CLAUDE.md:45-46`, que estão corretas (`Apoio/` e `<Feature>/` via `ApiFactory`) |
| V13 | `g -nE "01_schema\|02_\|IntegrationTests\.(Infra\|Api\|Apoio\|Persistencia)\|ApiFactory\|..." -- .claude/skills/new-endpoint`; `g -nE "IntegrationTests(\.\|/)(Infra\|Api)\b\|Infra/Sql\|core/rotulo\|Core\.Common\.IVerificador" -- .claude .github docs/esteira/README.md docs/esteira/status.md` | vazio nas duas |
| V14 | `git diff --check`; `git diff --cached --check` | exit 0 nas duas |

## Verificado sem achado (checklist §8, só greps; build e testes ficam com o orquestrador)

Comparei o resultado de cada item com o da revisão de arquitetura anterior. A refatoração não alterou nenhum deles.

| Item | Evidência |
|---|---|
| BE-01 (parte grep) | `Directory.Build.props:7` `<TreatWarningsAsErrors>true` |
| BE-04, BE-05, BE-06 | vazio nos três: o `Core` não tem EF, ASP.NET Core nem `ProjectReference`, e a `Infrastructure` não conhece a `Api`. A troca de `IVerificadorDeBanco` para `Core.Saude` não muda a direção das dependências. |
| BE-07 | supressões vazio; `!` vazio |
| BE-08 | relógio direto vazio; `TimeProvider.System` só em `Program.cs:26` |
| BE-09, BE-10, BE-11 (grep) | vazio |
| BE-12 | só `EstoqueRepository.cs` sem `AsNoTracking` (esperado) |
| BE-13, BE-16, BE-18, BE-19, BE-20, BE-25, BE-27 | vazio |
| BE-14 | só `EstoqueRepository.cs`; `*Raw` vazio |
| BE-15 | 15.1 vazio; 15.2 em `EstoqueRepository.cs:34` |
| BE-17 | 12 pacotes, todos da lista aprovada. A refatoração não adicionou nenhum. |
| BE-21 | só guardas de invariante (`Result.cs`, `ItemPedido.cs`, `Pedido.cs`), as mesmas de antes |
| BE-22 | sem *retry*; `catch (DbUpdateException` só em `PedidoRepository.cs:31` |
| BE-23 | 23.1 dá 5 linhas; 23.3 dá `app.UseForwardedHeaders( app.UseRateLimiter(` |
| BE-26 | 1 teste |
| BE-28 | vazio (V1) |
| FE-01 a FE-04, FE-06, FE-08 | vazio |
| FE-05 | só `core/api/{clientes,pedidos,produtos}-api.ts` |
| FE-12 | `core/api/contrato.spec.ts` presente |
| Caminhos citados no §8 (BE-14, BE-15, BE-16, BE-20, FE-05, FE-12, DK-06) | Nenhum se moveu: os greps acima acham os arquivos onde o checklist espera |

## Observações (sem achado)

- **O-1. A árvore do `CLAUDE.md` para `UnitTests` cita só `Apoio/` e `Arquitetura/`.** As pastas por feature (`Common/`, `Pedidos/` e `Relatorios/`) não aparecem. Isso segue o texto aprovado da `PROP-15`, e o padrão "pasta por feature" já fica implícito pela linha do `Core`. Não é desvio. Se o desenvolvedor quiser simetria com `IntegrationTests`, basta uma linha `<Feature>/ Regras e validações por feature`.
- **O-2. `BE-24` continua sem objeto.** Não existe `docs/contratos/openapi.json` (`HL-28`, T-11.2). A conferência "contrato inalterado" foi feita pelos controllers, rotas e DTOs intactos (V6) e pelo `contrato.spec.ts` do frontend, que não mudou.
- **O-3. `docs/esteira/revisoes/refatoracao-estrutural-1.md` não está rastreado.** Ele precisa entrar no commit de documentação da refatoração, junto deste relatório.

## Precisa verificação (gate do orquestrador)

- BE-01, BE-02, BE-03 e a parte de teste do BE-11: `dotnet build` com 0 aviso, `dotnet format --verify-no-changes` com exit 0 e os 9 testes de `Arquitetura/` verdes.
- `EST-04` e `EST-05`: a contagem de `dotnet test --list-tests` precisa ser igual à da linha de base (Unit 113, Integration 131, segundo o `03-backend.md`), sem *collection* perdida.
- `EST-03` e `EST-02`: `PipelineDeErrosTests` e `HealthTests` verdes na integração.
- FE-11: `lint`, `build` e `test` do frontend verdes (34/34 declarados).
- `PROP-17`, se o orquestrador quiser reproduzir a ressalva da rodada 1: trocar de propósito um namespace numa cópia descartável fora da árvore versionada e conferir o `error IDE0130` no `dotnet build`.

## Re-revisão do ARQ-01 (rodada 3, 2026-11-15)

**Veredito: Resolvido.** Conferi o relatório e o código.

- **Relatório.** O `03-backend.md` tem uma linha `ARQ-01` em "Resposta a achados". Ela diz que a linha 2 foi removida e que o build saiu com 0 aviso e o `format` com exit 0. O texto bate com o código.
- **Código.** `git diff -M -- backend/tests/Joya.Pedidos.IntegrationTests/Apoio/Arranjo.cs` mostra que, em relação ao `HEAD`, saíram o `using ...Infra;` e o namespace `.Persistencia`, e entrou o namespace `.Apoio`. Não entrou nenhum `using` novo. O arquivo agora abre com `using System.Globalization;` e `using Microsoft.Data.SqlClient;`. `Sql` continua resolvido, porque está no mesmo namespace `Apoio`.
- **V4.** O laço que procura `^using <namespace-próprio>;` em todos os `.cs` de `backend/` sai **vazio**.
- **Escopo.** `git status --porcelain -uall` não mostra arquivo novo nem alterado fora do que já estava revisado. O único arquivo não rastreado novo é este relatório. O `git diff --stat -M` continua em 46 arquivos: o `Arranjo.cs` perdeu 1 inserção e o `03-backend.md` ganhou 1 linha.
- **Fica com o orquestrador:** `dotnet build` com 0 aviso e `dotnet format --verify-no-changes` com exit 0.

Com isso, a rodada fecha com **0 achados abertos**. Todos os `EST-01` a `EST-06` e `PROP-15` a `PROP-18` estão Resolvidos.

