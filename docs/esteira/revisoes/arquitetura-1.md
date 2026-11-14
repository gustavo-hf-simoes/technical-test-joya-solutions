# Revisão arquitetura — rodada 1 — 2026-11-14

> Revisor: `arquiteto` (modo REVISAO). O revisor **não corrige**: cada achado tem um único dono e é re-revisado pelo mesmo revisor que o abriu.

## Escopo

- Commits ou diff revisados: árvore de trabalho em 2026-11-14, branch `us-03-ci` (= `main` + T-11.1, HEAD `eee7dc1`). Fora do escopo por delegação: `.github/workflows/ci.yml` (T-03.3 a T-03.5 em andamento).
- Histórias cobertas: US-01 a US-10, US-13 a US-16, US-18 e T-11.1 (`GET /pedidos/{id}`).
- Não aplicável nesta rodada: T-11.2 (contrato OpenAPI, depois da US-12), US-12, US-17.
- Referência: [02-arquitetura.md](../02-arquitetura.md) §1 a §8, ADR-0002 a ADR-0005, `CLAUDE.md`, spec §4 e §6. Decisões vigentes: `HL-27` e `HL-28`.
- Ferramentas e comandos usados:
  - todos os itens do checklist §8 rodados com `git grep --untracked` (Git Bash, raiz do repositório);
  - leitura de `Program.cs`, controllers, `ProblemasHttp`, `LimitesHttp`, `PedidoService`, entidades, `UnitOfWork`, repositórios, `*Consultas.cs`, `02-indices.sql`, `init.sh`, `docker-compose.yml`, Dockerfiles, `nginx.conf`, modelos e serviços `*Api` do frontend, `RegrasDeArquiteturaTests.cs`;
  - revisão das consultas EF com a skill `optimizing-ef-core-queries`.
- Restrição da delegação: sem Docker e sem rodar suítes. Itens que exigem execução (`BE-01` a `BE-03`, `BE-11` parte de teste, `DB-01`/`DB-02` parte de teste, `DB-06`, `DK-01`, `DK-02`, `FE-11`) citam a evidência do último gate aprovado, sem reexecução.

## Resumo

| Severidade | Abertos | Resolvidos | Aceitos (HL) |
|---|---|---|---|
| Crítica | 0 | 0 | 0 |
| Alta | 0 | 0 | 0 |
| Média | 1 | 0 | 0 |
| Baixa | 3 | 0 | 0 |

| Dono | Achados |
|---|---|
| backend | ARQ-02 |
| frontend | ARQ-03, ARQ-04 |
| orquestrador | ARQ-01 |

**Re-revisão, rodada 2 (2026-11-14, `HL-29`, branch `chore/achados-revisao-1`, correções sem commit).** Conferi o relatório de cada dono e o código. O gate do orquestrador estava verde e não foi reexecutado. Sem Docker. Para o `ARQ-02` rodei uma sonda descartável do ArchUnitNET fora do repositório; ela já foi apagada.

| Severidade | Abertos | Resolvidos | Aceitos (HL) |
|---|---|---|---|
| Crítica | 0 | 0 | 0 |
| Alta | 0 | 0 | 0 |
| Média | 1 (`ARQ-02`, Parcial, reclassificado de Baixa) | 1 (`ARQ-04`) | 0 |
| Baixa | 0 | 2 (`ARQ-01`, `ARQ-03`) | 0 |

**Re-revisão, rodada 3 (2026-11-14).** O `ARQ-02` foi resolvido, e os quatro achados estão fechados: Média 0 abertos e 2 resolvidos (`ARQ-02`, `ARQ-04`); Baixa 0 abertos e 2 resolvidos (`ARQ-01`, `ARQ-03`). Por dono: backend 1, frontend 2 e orquestrador 1, todos resolvidos.

Ajustes do arquiteto no próprio caminho (autorizados pelo orquestrador, rodada 3), sem achado novo, alinhando os resíduos apontados no `ARQ-01` (rodada 2):

| Local | Ajuste | Motivo |
|---|---|---|
| `docs/adr/0002-limites-contra-abuso.md:25` | `web` → `frontend` | Mesmo nome do serviço do compose (`ARQ-01`) |
| `docs/esteira/02-arquitetura.md:73` (tabela §2) | `4200 → 8080` → `127.0.0.1:4200 → 8080` (só *loopback*, `SEC-04`) | O compose publica `"127.0.0.1:4200:8080"` (`docker-compose.yml:90`) desde a correção do `SEC-04` |
| `docs/esteira/02-arquitetura.md:83` (§2 item 6) | Inclui o `4200` entre as portas no *loopback* e explica por quê: o Nginx repassa `/api` inteiro, inclusive o Swagger | Idem |
| `docs/esteira/02-arquitetura.md:470` (`DK-04`) | Busca passa a `"?(1433\|4200\|8080):"`. A anterior não pegava o `4200` e a linha "`4200:8080` público, como o desenho permite" da rodada 1 deixa de valer | Hoje a busca devolve 3 linhas, todas com `127.0.0.1:` |

Ajustes feitos pelo arquiteto no próprio checklist (§8 de `02-arquitetura.md`), sem achado:

| Item | Ajuste | Motivo |
|---|---|---|
| BE-15 | Três buscas novas, testadas (vazio / 1 linha / revisar) | Pendência (b). O comando antigo acusava o próprio SQL da baixa (`EstoqueRepository.cs:30,34`), `PedidoErrors.cs:26` e um comentário XML em `ProdutoResponse.cs:3` |
| BE-23 | Cinco buscas novas, testadas | Pendência (b). O código usa `[EnableRateLimiting(LimitesHttp.PoliticaCriarPedido)]`, não o literal; a ordem dos *middlewares* agora é verificável por comando |
| BE-09 | Parte do banco restrita a `database/*.sql` sem linhas de comentário | Falso positivo: a palavra "real" em prosa (`database/parte4/00-carga-volume.sql:20,96`, `investigacao.md`) |
| BE-10 | Filtra `context.Result` | Falso positivo: `ValidationFilter.cs:42` atribui a propriedade do filtro MVC |
| DK-01, DK-02 | `web` → `frontend` | Pendência (a), ver `ARQ-01` |
| DK-05 | `sleep` vazio no compose; no `init.sh` só em laço de espera limitado | `init.sh:73` está no laço de 60 x 3 s do passo 1b (erro 904 num segundo `up`), não é espera fixa de ordem de subida |

## Achados

### ARQ-01 — Nome do serviço do frontend: `web` no desenho, `frontend` no código

| Campo | Valor |
|---|---|
| Severidade | Baixa |
| Dono | orquestrador |
| Regra ou requisito | Rastreabilidade desenho ↔ código; `R-13`, `R-15`; checklist `DK-01`, `DK-02` |
| Local | `docs/esteira/02-arquitetura.md:27` (R-13), `:50`, `:59-61` (diagrama §2), `:73` (tabela §2), `:78` (§2 item 1), `:145` (participante do diagrama §4); `:258` (§5, nome de schema, ver `ARQ-03`) |
| Status | Resolvido |

**Evidência.** `docker-compose.yml:80-88` declara o serviço `frontend`. O mesmo nome está em `README.md:34`, no backlog (T-13.2), no CI (`--wait frontend`) e no registro do gate da US-13 ("api e frontend saudáveis, uid 1654 (api) e 101 (frontend)"). `web` só aparece em `02-arquitetura.md` e em `docs/plan.md:190` (tarefa histórica 6.3).

**Decisão do arquiteto (pendência a).** Fica **`frontend`**. É o nome usado pelo código, pela documentação de uso, pelo CI e pelas evidências de gate; trocar para `web` mexeria em quatro lugares sem ganho. O checklist já foi corrigido (`DK-01`, `DK-02`). O corpo do desenho (§1, §2, §4) não pode ser reescrito pelo arquiteto nesta rodada (escopo da delegação: só o checklist; regra do documento: rodadas antigas não são reescritas).

**Cenário.** Quem lê o desenho procura `web` no compose e não encontra; um comando copiado do desenho (`docker compose exec web ...`) falha.

**Correção esperada.** O orquestrador escolhe uma das duas: (1) autoriza o arquiteto a trocar `web` por `frontend` nas linhas citadas e `ItemPedidoResponse` por `PedidoItemResponse` em §5 (`:258`); ou (2) registra uma errata no topo da §2 de `02-arquitetura.md` ("o serviço `web` do desenho é o serviço `frontend` do compose; os schemas da §5 seguem os nomes das classes do backend"). `docs/plan.md:190` é histórico e não muda. Resolvido quando `git grep -nw "web" -- docs/esteira/02-arquitetura.md` não retornar o serviço sem a errata.

**Histórico de re-revisão.**

| Rodada | Resposta do dono | Conferência (relatório + código) | Status |
|---|---|---|---|
| 1 | — | — | Aberto |
| 2 | Orquestrador (HL-29): `web` → `frontend` em `02-arquitetura.md:27,50,59-61,73,78,80,145` e `ItemPedidoResponse` → `PedidoItemResponse` em `:258`; resposta em `02-arquitetura.md:532` | **Relatório:** linha de resposta presente, com local e comando. **Código (doc):** `git diff` confirma as 10 trocas; `git grep -nw web -- docs/esteira/02-arquitetura.md` → só `:532`, que é a própria resposta citando a troca; `git grep -n "ItemPedido(Request\|Response)"` no desenho → só `:532`. `docker-compose.yml:82` declara `frontend`. Critério de aceite do achado atendido. **Resíduos fora do local do achado, sem reabrir:** (a) `docs/adr/0002-limites-contra-abuso.md:25` ainda diz "no `web`"; (b) divergência **nova**, vinda da correção da SEC (`05-seguranca.md:155`): o compose agora publica `"127.0.0.1:4200:8080"` (`docker-compose.yml:90`), mas o desenho ainda diz `4200 → 8080` (`02-arquitetura.md:73`), o item 6 da §2 cita só `1433` e `8080` no *loopback* e o `DK-04` diz "`4200:8080` público, como o desenho permite". Os dois ficam para o arquiteto alinhar no próprio caminho quando o orquestrador autorizar | Resolvido |

### ARQ-02 — Testes de arquitetura com regras que passam vazias e comentários desatualizados

| Campo | Valor |
|---|---|
| Severidade | Baixa → **Média** (reclassificado na rodada 2: duas regras da §3 não provavam nada, ver histórico) |
| Dono | backend |
| Regra ou requisito | `02-arquitetura.md` §3 ("Como a regra é provada"), `HL-08`, regra de ouro 2 (entidades com setters privados), checklist `BE-03` |
| Local | `backend/tests/Joya.Pedidos.UnitTests/Arquitetura/RegrasDeArquiteturaTests.cs:14-16`, `:46-47`, `:70`, `:80-84`, `:99`, `:110`, `:123-125`, `:169`, `:180` (rodada 1); rodada 2: `:14-16`, `:68-77`, `:86-93`, `:103-106`, `:185-193` |
| Status | Resolvido |

**Evidência.**

- `.WithoutRequiringPositiveResults()` continua nas regras das linhas 70, 84, 99, 169 e 180. Os comentários dizem "Infrastructure ainda não tem tipos" (`:70`, `:80`, `:180`) e "Ainda não há controllers". Hoje há `PedidosController`, `ClientesController`, `ProdutosController`, `RelatoriosController`, `AppDbContext`, repositórios e consultas.
- `:46` "ainda não existem no Core"; `:123` "a lista fica vazia e o teste passa sem verificar nada por ora"; `:110` "Hoje: PaginaResponse<T>". Os três estão errados: `Pedido` e `ItemPedido` existem no Core e há dezenas de DTOs.
- `Entidades_Propriedades_NaoTemSetterPublico` (`:121-134`) só varre `_core`. `Cliente` e `Produto` são modelos de leitura em `Joya.Pedidos.Infrastructure` e nunca são verificados. O teste também não afirma que encontrou alguma entidade.

**Cenário.** Se uma refatoração mover os controllers para outro *namespace* ou renomear `Pedido`, as regras continuam verdes sem verificar nada: o seletor fica vazio e `WithoutRequiringPositiveResults` aceita o vazio. A prova da §3 vira decorativa. Hoje as regras passam de verdade (os tipos existem e estão conformes), por isso a severidade é Baixa.

**Correção esperada.**

1. Remover `WithoutRequiringPositiveResults()` das regras cujo conjunto avaliado já existe (controllers, Core, Infrastructure). Manter só onde o vazio é legítimo, com comentário que explique por quê.
2. No teste de entidades, varrer `_core` e `_infrastructure` e afirmar o conjunto encontrado (por exemplo `entidades.Select(t => t.Name).ShouldBe(["Cliente", "ItemPedido", "Pedido", "Produto"], ignoreOrder: true)`) antes de checar os setters.
3. Atualizar os comentários das linhas citadas para o estado atual.
4. `dotnet test backend/tests/Joya.Pedidos.UnitTests --filter "FullyQualifiedName~Arquitetura"` → 9 testes verdes; `git grep -n "ainda não\|ainda sem\|Hoje" -- backend/tests/Joya.Pedidos.UnitTests/Arquitetura` → vazio.

**Histórico de re-revisão.**

| Rodada | Resposta do dono | Conferência (relatório + código) | Status |
|---|---|---|---|
| 1 | — | — | Aberto |
| 2 | Backend (`03-backend.md:940`): tirou `WithoutRequiringPositiveResults()` de controllers e de "classes são sealed"; **manteve** nas três regras `NotDependOnAny` alegando que "regra negativa não gera resultado positivo no ArchUnitNET"; acrescentou `ShouldNotBeEmpty` sobre os assemblies; teste de entidades varre Core + Infrastructure e exige as quatro; comentários corrigidos | **Resolvidos:** item 2 (`:130-134`, `ShouldBe(_entidades, ignoreOrder: true)`), item 3 (`git grep -n "ainda não\|ainda sem\|Hoje"` na pasta → vazio). **Não resolvido:** item 1, e a causa é outra. **A alegação do dono é falsa.** Sonda descartável (projeto *console* fora do repositório, ArchUnitNET 0.13.5, mesmos DLLs de `UnitTests/bin/Debug/net8.0`, já apagada): (1) `Types().That().ResideInNamespace(Regex(...))` seleciona **0** tipos para Core, Infrastructure, Api, UnitTests e EF Core. Na 0.13.x, `ResideInNamespace` compara o nome **exato**; expressão regular é `ResideInNamespaceMatching` e prefixo é `ResideInNamespaceOrBelow` (este dá 44 tipos no Core e 17 na Infrastructure). O `WithoutRequiringPositiveResults` era necessário porque **o conjunto avaliado é vazio**, não porque a regra é negativa. (2) Com `ResideInNamespaceOrBelow`, a regra Core → Infrastructure/Api **sem** `WithoutRequiringPositiveResults` passa (`HasNoViolations = True`); o próprio controller (`:103-106`, regra negativa sem o *opt-out*) já contradiz a alegação. (3) Mutação: `Controllers NotDependOnAny(Core)` com o seletor atual → `True` (não detecta), com `OrBelow` → `False` (detecta). O **alvo** da regra dos controllers (`:104-105`) é vazio, logo a regra nunca reprova. (4) `NotDependOnAny(Types().That()...("Microsoft.EntityFrameworkCore"))` também é vazio mesmo com `OrBelow`: `Types()` só contém os assemblies carregados. A Infrastructure, que usa EF, passa nessa regra; com `NotDependOnAnyTypesThat().ResideInNamespaceOrBelow("Microsoft.EntityFrameworkCore")` ela reprova, como deveria. (5) Os `ShouldNotBeEmpty` novos (`:70-72`, `:87-88`, `:186-187`) olham o assembly ou `FullName.StartsWith`, **não o seletor da regra**, então não pegam o vazio. **Efeito real:** Core → Infra/Api e Infra → Api continuam provados pela parte de reflexão (`GetReferencedAssemblies`, `:79-80`, `:95-97`). **Controllers → EF/Infrastructure** (§3, T-03.2) e **testes de regra → Infra/Api** (`:189-193`) não têm prova nenhuma. O comentário XML `:14-16` ("o ArchUnitNET reprova a regra se o conjunto ficar vazio") e os de `:68-69`, `:86` e `:185` afirmam o contrário do que acontece. **Correção do revisor:** na rodada 1 escrevi "hoje as regras passam de verdade"; estava errado, e por isso a severidade sobe para Média. **Correção esperada (rodada 3):** (a) trocar `ResideInNamespace(Regex(x))` e `DoNotResideInNamespace(Regex(x))` por `ResideInNamespaceOrBelow(x)` e `DoNotResideInNamespaceOrBelow(x)`, e remover o *helper* `Regex`; (b) remover `WithoutRequiringPositiveResults()` das três regras; (c) na regra dos controllers, usar `.Should().NotDependOnAnyTypesThat().ResideInNamespaceOrBelow("Microsoft.EntityFrameworkCore").AndShould().NotDependOnAnyTypesThat().ResideInNamespaceOrBelow(_nsInfra)`, com `AndShould` e não `OrShould`; (d) remover os `ShouldNotBeEmpty` sobre assemblies, que ficam redundantes com o padrão de exigir positivos, e corrigir os comentários. **Verificação:** `git grep -nE "ResideInNamespace\(Regex\|WithoutRequiringPositiveResults" -- backend/tests` → vazio; os 9 testes de arquitetura verdes; e uma prova de mutação descrita na resposta: trocar temporariamente o alvo da regra dos controllers por `_nsCore` e ver o teste ficar vermelho. Se a rodada 3 não resolver, o achado vai para o desenvolvedor | Parcial |
| 3 | Backend (`03-backend.md:945-951`): seletores trocados por `ResideInNamespaceOrBelow`, *helper* `Regex` removido; `WithoutRequiringPositiveResults()` removido das três regras; `ShouldNotBeEmpty` redundantes removidos; controllers com `NotDependOnAnyTypesThat()` para Infrastructure e, com `AndShould`, para `Microsoft.EntityFrameworkCore`; assembly do EF Core carregado no modelo; prova de mutação com `MutacaoTempController` (propriedade `Cliente`) → vermelho, arquivo removido | **Relatório:** a resposta reconhece a alegação errada da rodada 2, lista as quatro correções pedidas e descreve a mutação com o resultado. **Código:** `git grep -nE "ResideInNamespace\(\|Regex\(\|WithoutRequiringPositiveResults\|ShouldNotBeEmpty\(\)" -- backend/tests/Joya.Pedidos.UnitTests/Arquitetura` → só o comentário XML `:15`, que agora descreve o comportamento real. As regras `:68-71` (Core), `:80-82` (Infrastructure), `:92-95` (controllers, sujeito restrito a `ResideInNamespaceOrBelow(_nsApi)`) e `:174-178` (testes de regra) usam `OrBelow` + `NotDependOnAnyTypesThat` + `AndShould`, exatamente os seletores que a sonda da rodada 2 mostrou não vazios: 44 tipos no Core, 17 na Infrastructure, 4 controllers e 11 testes de regra. A parte de reflexão das regras de camada foi mantida. `:31` carrega `typeof(DbContext).Assembly`, sem pacote novo: o EF chega por transitividade da referência à Infrastructure, e `git status` não mostra mudança em `*.csproj` nem em `Directory.Packages.props`. `git grep --untracked -n Mutacao -- backend` → vazio, o arquivo temporário foi removido. **Mutação:** a do dono (`Cliente`) e a independente do orquestrador (`PedidosDbContext`) deixaram `Controllers_Dependencias_NaoAcessamDbContext` vermelho e voltaram a 9/9. As duas exercitam a cláusula da Infrastructure. A cláusula do EF Core é a mesma construção que, na sonda da rodada 2, reprovou a Infrastructure, que depende de EF. Gate do orquestrador: build com 0 aviso, 113/113, `format` limpo | Resolvido |

### ARQ-03 — Nomes das interfaces do frontend diferem dos schemas do contrato

| Campo | Valor |
|---|---|
| Severidade | Baixa |
| Dono | frontend |
| Regra ou requisito | ADR-0005 (modelos do frontend com o mesmo nome dos schemas do OpenAPI), checklist `FE-12` |
| Local | `frontend/src/app/core/api/pedidos.models.ts:13` (`ItemPedidoResponse`), `:49` (`ItemPedidoRequest`) |
| Status | Resolvido |

**Evidência.** O backend, que é a fonte do contrato (o Swashbuckle gera o schema com o nome da classe), declara `PedidoItemResponse` (`backend/src/Joya.Pedidos.Core/Pedidos/PedidoItemResponse.cs:4`) e `CriarPedidoItemRequest` (`backend/src/Joya.Pedidos.Core/Pedidos/CriarPedidoItemRequest.cs:4`). O frontend usa `ItemPedidoResponse` e `ItemPedidoRequest`. Os campos coincidem; só os nomes divergem. O texto da §5 do desenho (`:258`) também usa `ItemPedidoResponse`; a errata está em `ARQ-01`.

**Cenário.** Na T-11.2 o `contrato.spec.ts` vai comparar os modelos com `docs/contratos/openapi.json`. Com nomes diferentes, a comparação por nome de schema falha, ou o teste precisa de um mapa de apelidos que esconde divergências. Corrigir agora custa duas renomeações.

**Correção esperada.** Renomear `ItemPedidoResponse` → `PedidoItemResponse` e `ItemPedidoRequest` → `CriarPedidoItemRequest` em `pedidos.models.ts` e em todos os usos. `git grep --untracked -nE "\bItemPedido(Response|Request)\b" -- frontend/src` → vazio; `FE-11` verde (lint, build, testes).

**Histórico de re-revisão.**

| Rodada | Resposta do dono | Conferência (relatório + código) | Status |
|---|---|---|---|
| 1 | — | — | Aberto |
| 2 | Frontend (`04-frontend.md:128`): renomeou para `PedidoItemResponse` e `CriarPedidoItemRequest` em `pedidos.models.ts` (sem outros usos) | **Relatório:** linha presente, com local e prova (build + `contrato.spec.ts`). **Código:** `git grep --untracked -nE "\bItemPedido(Response\|Request)\b" -- frontend/src e2e` → vazio; `pedidos.models.ts:13,27,49,56` usam os nomes novos, iguais aos `record` do backend (`Core/Pedidos/PedidoItemResponse.cs:4`, `CriarPedidoItemRequest.cs:4`); campos inalterados. `FE-11` verde no gate do orquestrador (lint, build, 27 testes) | Resolvido |

### ARQ-04 — `crypto.randomUUID()` quebra o formulário fora de contexto seguro

| Campo | Valor |
|---|---|
| Severidade | Média |
| Dono | frontend |
| Regra ou requisito | `02-arquitetura.md` §2 (porta `4200` publicada em todas as interfaces, `DK-04`), spec §2 (`Idempotency-Key`), RNF de disponibilidade da tela de cadastro |
| Local | `frontend/src/app/pedidos/pedido-form.component.ts:93` |
| Status | Resolvido |

**Evidência.** `private readonly idempotencyKey = crypto.randomUUID();` é inicializado na construção do componente. `crypto.randomUUID` só existe em *secure context* (HTTPS, `localhost` ou `127.0.0.1`). `docker-compose.yml:88` publica `"4200:8080"` sem *loopback*, como o desenho permite. Não há alternativa no código (`git grep -n "randomUUID\|getRandomValues" -- frontend/src` → 1 linha).

**Cenário.** Alguém abre a aplicação por `http://<ip-da-máquina>:4200` (outra máquina da rede, VM, WSL com IP próprio). Ao navegar para "Novo pedido", a construção do componente lança `TypeError: crypto.randomUUID is not a function` e a tela de cadastro não renderiza. É a tela principal do enunciado. Por `localhost` funciona, por isso não é Alta.

**Correção esperada.** Uma das duas:

1. Gerar o UUID v4 com uma função pequena em `core/` que usa `crypto.randomUUID` quando existe e, caso contrário, `crypto.getRandomValues(new Uint8Array(16))` com os bits de versão e variante ajustados (`getRandomValues` existe fora de contexto seguro). Teste unitário que remove `randomUUID` do `crypto` e verifica o formato `^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$`.
2. Ou o orquestrador aceita o risco com um `HL` e o README passa a dizer "abra por `http://localhost:4200`".

Nas duas, `FE-11` verde.

**Histórico de re-revisão.**

| Rodada | Resposta do dono | Conferência (relatório + código) | Status |
|---|---|---|---|
| 1 | — | — | Aberto |
| 2 | Frontend (`04-frontend.md:127`): `gerarUuid()` em `core/uuid.ts`, com `crypto.randomUUID` quando existe e, sem ele, `getRandomValues` com versão e variante ajustadas; teste em `core/uuid.spec.ts` | **Relatório:** linha presente, com local e teste. **Código:** `uuid.ts:3-10` testa `typeof c.randomUUID === 'function'`, senão faz `b[6] = (b[6] & 0x0f) \| 0x40` e `b[8] = (b[8] & 0x3f) \| 0x80` e formata 8-4-4-4-12; o `Crypto` injetável por parâmetro permite testar sem *mock* global. `uuid.spec.ts:13-20` remove `randomUUID`, preenche com `0xff` (exercita as duas máscaras) e verifica a expressão regular v4 do achado e o valor exato `ffffffff-ffff-4fff-bfff-ffffffffffff`; `:7-9` cobre o caminho nativo e `:24-25` o padrão. `pedido-form.component.ts:94` usa `gerarUuid()`; `git grep --untracked -n "randomUUID" -- frontend/src` → só `uuid.ts` e `uuid.spec.ts`. Sem `any` (`as unknown as Crypto` só no teste). `FE-11` verde no gate (27 testes). Observação: a correção da SEC passou a publicar `4200` só no *loopback* (`docker-compose.yml:90`), o que reduz o cenário local, mas a correção continua valendo para qualquer acesso por HTTP fora de `localhost` (por exemplo, uma porta republicada em outra interface). O deploy atrás do Caddy é HTTPS e já era contexto seguro | Resolvido |

## Verificado sem achado

| Item do checklist | Como foi verificado | Evidência |
|---|---|---|
| BE-01 | `git grep` + gate T-11.1 | `<TreatWarningsAsErrors>true` em `Directory.Build.props`; build com 0 warnings no gate T-11.1 (`03-backend.md:909`) |
| BE-02, BE-03 | Gate T-11.1 (sem reexecução) | `dotnet format` limpo; 111 testes unitários verdes, incluindo os 9 de arquitetura (ressalva em `ARQ-02`) |
| BE-04, BE-05, BE-06 | `git grep` | Vazio. O `Core` só referencia `FluentValidation`; a `Infrastructure` só `Microsoft.EntityFrameworkCore.SqlServer` |
| BE-07 | `git grep` (src e tests) | Vazio; nenhum `!` de supressão de nulo |
| BE-08 | `git grep` | Vazio; `TimeProvider.System` registrado uma vez (`Program.cs:26`) |
| BE-09 | `git grep` (comando ajustado) | Vazio no backend e nos scripts |
| BE-10 | `git grep` (comando ajustado) | Vazio |
| BE-11 | `git grep` | Vazio |
| BE-12 | `git grep -L` | Só `EstoqueRepository.cs` (`SqlQuery`, sem rastreamento) |
| BE-13 | `git grep` + leitura de `*Consultas.cs` | Sem `Include`; nenhum `await` em laço. `PedidoConsultas.ListarAsync` e `ObterPorIdAsync` projetam direto para o DTO; a contagem de itens vira subconsulta correlata |
| BE-14 | `git grep` | SQL à mão só em `EstoqueRepository.cs`, interpolado e parametrizado; nenhum `*Raw` |
| BE-15 | Comandos novos | (1) vazio; (2) 1 linha; (3) `TentarBaixarAsync` em `PedidoService.cs:84`, dentro da transação; `ObterEstoquesAsync` em `:110`, depois do *rollback*, só para a mensagem do `409` |
| BE-16 | `git grep` | Vazio |
| BE-17 | `git grep` | Subconjunto da lista aprovada |
| BE-18, BE-19, BE-20 | `git grep` | Vazio |
| BE-21 | `git grep` + leitura | Só guardas de invariante (`Result.cs`, `ArgumentException`/`ArgumentOutOfRangeException` em `Pedido.cs` e `ItemPedido.cs`) |
| BE-22 | `git grep` + leitura de `UnitOfWork` | Sem *retry*; único `catch (DbUpdateException` em `PedidoRepository.cs:31` (2601/2627 em `UX_Pedidos_ChaveIdempotencia`); *rollback* em `Failure` e limpeza do *change tracker* |
| BE-23 | Comandos novos | 5 linhas; 1 linha; `app.UseForwardedHeaders( app.UseRateLimiter(` nesta ordem; 3 chaves no `appsettings.json`; testes `LimitesHttpTests.cs:42` e `:83` |
| BE-25, BE-27 | `git grep` | Vazio; `Created($"/pedidos/{pedido.Id}", ...)` relativo |
| BE-26 | `git grep` | `PedidoServiceTests.cs:142` |
| Fluxo `POST /pedidos` (§4) | Leitura de `PedidoService` | Ordena por `ProdutoId`, valida RN-05 a RN-07, baixa atômica com `OUTPUT inserted.Preco` dentro de `ExecutarEmTransacaoAsync`, preço do banco (RN-09), `Result<PedidoResponse>`; `Pedido.Criar` trunca a data em milissegundos (`PROP-11`/`HL-20`) |
| Mapa de erros | Leitura | `ProblemasHttp.ToProblem` é o único mapa `Error` → HTTP |
| Camadas (§3) | Leitura + `git grep` | `Api` usa a `Infrastructure` só em `Program.cs` (`AddInfrastructure()`); *health check* passa pela porta `IVerificadorDeBanco` do `Core`. `IClienteConsultas`/`IProdutoConsultas` do desenho foram fundidas em `IClienteRepository.ListarAsync`/`IProdutoRepository.ListarAsync`: simplificação aceita (um método de leitura por recurso) |
| Consultas EF (skill `optimizing-ef-core-queries`) | Leitura | Sem N+1; predicados sargáveis (intervalo semiaberto sobre a coluna crua); `AsNoTracking` em toda leitura; paginação com ordem estável (`DataPedido DESC, Id DESC`) e curto-circuito quando o *offset* passa do total; relatório agrupa antes do *join* e não lê `ItensPedido`; `Contains` traduzido para `OPENJSON` |
| Modelo de dados (§6) | Leitura de `02-indices.sql` e do mapeamento | Índices iguais à §6; mapeamento sem `HasIndex`; `decimal(18,2)` por convenção; conversor de data UTC |
| DB-01 (parte estática), DB-03, DB-04, DB-05, DB-07, DB-08 | `git grep` | `init.sh` no *fixture* e no compose, mesma imagem `mssql/server:2022-latest`; `init.sh:96` com `-C -I -b`; demais vazios ou presentes conforme o item |
| FE-01 a FE-06, FE-08 a FE-10, FE-13, FE-14 | `git grep` | Vazio ou presente conforme o item; HTTP só nos 3 `*-api.ts`; `no-explicit-any` como erro; inclui `e2e/` no FE-01 |
| FE-07 | `git grep` + leitura | Dois `constructor()` sem parâmetros (`pedido-form.component.ts:136`, `pedido-lista.component.ts:49`), usados para `effect`; sem injeção por construtor |
| FE-11 | Gates US-14/15 e US-16 (sem reexecução) | Lint limpo, 24/24 testes |
| DK-03, DK-06 | `git grep` | Vazio; as três diretivas do Nginx presentes (`client_max_body_size 64k`) |
| DK-04 | `git grep` | `1433` e `8080` no *loopback*; `4200:8080` público, como o desenho permite (ver `ARQ-04`) |
| DK-05 | Comando ajustado + leitura | Compose sem `sleep`; `init.sh:73` dentro do laço limitado do passo 1b; `service_healthy`/`service_completed_successfully` presentes |
| DK-01, DK-02 | Gate US-13 (sem reexecução, Docker proibido nesta rodada) | 8/8 rotas `200`; uid 1654 (`api`) e 101 (`frontend`) |
| Dockerfiles | Leitura | *Multi-stage*; `USER app` na API; `nginx-unprivileged` no frontend; *healthchecks* sem ferramentas extras; `.dockerignore` exclui `bin`, `obj`, `node_modules`, `dist`, `.angular` |

## Riscos aceitos

| Risco | Motivo | Recomendação para produção | Decisão (HL-xx) |
|---|---|---|---|
| Ausência de autenticação | Fora do escopo do enunciado (spec §2.2) | Autenticação JWT/OIDC e autorização por perfil | Declarado no README |
| Paginação por *offset* | 3 endpoints e volume de teste; ordem estável garante consistência entre páginas | *Keyset pagination* em `(DataPedido, Id)` se o volume crescer | Desenho §6 |
| Gates de integração sem a categoria `Medicao` | Tempo de execução | Rodar `Medicao` antes de release | `HL-27` |

## Precisa verificação

| Item | Motivo da dúvida |
|---|---|
| DB-06 (planos de execução) | O MCP `mssql` responde `AUTH_FAILED` (`CG-05`); os planos foram coletados pelos testes da Parte 4, não por `explain_sql` nesta rodada |
| BE-24, FE-12 (contrato) | Não aplicável: `docs/contratos/openapi.json` não existe; depende da T-11.2, depois da US-12. `contrato.spec.ts` hoje só usa `satisfies` contra os exemplos da spec |
| `Idempotency-Key` | O frontend envia o cabeçalho (`pedidos-api.ts`) e o backend o ignora até a US-12. Sem a US-12, um duplo clique gera dois pedidos. Coberto pelo planejamento da US-12, não é achado |
| Resposta `429` | `LimiteCriarPedidoPolicy` devolve Problem Details sem `type` e com título próprio, diferente do formato de `ProblemasHttp`. Consistência de contrato; fica para o QA confirmar contra a spec §2 |
| `413` com corpo *chunked* | Coberto por teste com `Content-Length`; o comportamento com `Transfer-Encoding: chunked` no Kestrel real fica para o QA |
| `Proxy__ForwardedLimit` no deploy | Depende da topologia da US-17 (proxy na frente do Nginx exige `2`) |
| BE-01 a BE-03, FE-11, DK-01, DK-02 | Não reexecutados por restrição da delegação; valem os gates citados. Reexecutar no gate desta etapa |
