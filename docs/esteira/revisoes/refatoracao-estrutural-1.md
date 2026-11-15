# Refatoração estrutural: diagnóstico e proposta (rodada 1)

| Campo | Valor |
|---|---|
| Etapa | 2, Arquitetura, modo DESENHO (refatoração estrutural) |
| Rodada | 1 |
| Agente | `arquiteto` |
| Base | branch `chore/refatoracao-estrutural`, árvore limpa (`main` com as correções QA-01 a QA-08, PR #31) |
| Escopo | Fases 1 (diagnóstico) e 2 (proposta) do pedido do desenvolvedor. **Nada foi implementado.** |
| Decisões vigentes | HL-28 (US-17, US-12 e T-11.2 fora do escopo), HL-32 e as regras de ouro do `CLAUDE.md` |

## Conclusão

A estrutura está **saudável**. Os namespaces batem 100% com as pastas nos 5 projetos .NET. As dependências apontam para o `Core` e o teste de arquitetura prova isso. O `Core` é organizado por feature (D2), e o `Infrastructure` espelha essas features. O frontend segue o style guide do Angular.

Não há mudança de arquitetura a propor. Sobram seis ajustes locais (`EST-01` a `EST-06`), todos sem efeito em comportamento, contrato HTTP, SQL ou telas. Eles atacam três coisas:

- **Arquivos que escondem tipos.** O nome do arquivo não revela tudo o que ele contém.
- **Lógica no lugar errado.** Há adaptação de erro HTTP dentro do `Program.cs`.
- **Ajudantes de teste espalhados** em pastas que não dizem o que guardam.

Além disso, quatro propostas (`PROP-15` a `PROP-18`) alinham a documentação e o ferramental ao estado real.

Não há exclusão de arquivo, projeto novo, dependência nova ou pasta vazia.

## 1. Árvore atual (resumida)

A árvore ignora `bin/`, `obj/`, `node_modules/`, `dist/` e `.angular/`.

```text
/ (raiz)
  .claude/  .github/  backend/  database/  docs/  e2e/  frontend/
  .env.example  .gitattributes  .gitignore  .mcp.json  CLAUDE.md  README.md  docker-compose.yml

backend/
  Joya.Pedidos.sln  Directory.Build.props  global.json  .editorconfig  Dockerfile
  src/
    Joya.Pedidos.Core/
      Clientes/   ClienteResponse, ClienteResumo, IClienteRepository,
                  IClienteService.cs  (interface IClienteService + class ClienteService)
      Common/     Error, IUnitOfWork, IVerificadorDeBanco, PaginaResponse, Periodo,
                  PeriodoRegras, Result, TipoErro
      Estoque/    IEstoqueRepository
      Pedidos/    CriarPedidoItemRequest, CriarPedidoRequest, CriarPedidoValidator,
                  IPedidoConsultas, IPedidoRepository, IPedidoService, ItemPedido, ItemSemEstoque,
                  ListarPedidosQuery, ListarPedidosQueryValidator, Pedido,
                  PedidoConsultaService.cs  (interface IPedidoConsultaService + class),
                  PedidoErrors, PedidoItemResponse, PedidoResponse, PedidoResumoResponse, PedidoService
      Produtos/   IProdutoRepository,
                  IProdutoService.cs  (interface IProdutoService + class ProdutoService),
                  ProdutoResponse, ProdutoResumo
      Relatorios/ IRelatorioConsultas, RelatorioErrors,
                  RelatorioService.cs  (interface IRelatorioService + class),
                  TopClienteResponse, TopClientesQuery, TopClientesQueryValidator
    Joya.Pedidos.Infrastructure/
      DependencyInjection.cs
      Clientes/      Cliente (modelo de leitura), ClienteRepository
      Estoque/       EstoqueRepository
      Pedidos/       PedidoConsultas, PedidoRepository
      Persistencia/  PedidosDbContext, UnitOfWork, ConversorDataUtc,
                     Configuracoes/ {Cliente,ItemPedido,Pedido,Produto}Configuracao
      Produtos/      Produto (modelo de leitura), ProdutoRepository
      Relatorios/    RelatorioConsultas
      Saude/         VerificadorDeBanco
    Joya.Pedidos.Api/
      Program.cs  (composição + funções locais ErrosDoModelState e NormalizarChave)
      appsettings.json
      Controllers/ {Clientes,Pedidos,Produtos,Relatorios}Controller
      Erros/       ErroInternoExceptionHandler, ProblemasHttp, RequisicaoHttpInvalidaExceptionHandler,
                   ValidationFilter
      Limites/     LimiteCriarPedidoPolicy, LimiteDeCorpoFilter, LimiteDeTaxaOptions, LimitesHttp
      Saude/       BancoDeDadosHealthCheck
  tests/
    Joya.Pedidos.UnitTests/
      Apoio/        RelogioFalso, UnidadeDeTrabalhoFalsa
      Arquitetura/  RegrasDeArquiteturaTests
      Common/       PeriodoTests, ResultTests
      Pedidos/      CriarPedidoValidatorTests, ListarPedidosQueryValidatorTests,
                    PedidoConsultaServiceTests, PedidoServiceTests, PedidoTests
      Relatorios/   RelatorioServiceTests, TopClientesQueryValidatorTests
    Joya.Pedidos.IntegrationTests/
      Api/          ApiFactory*, RotasDeTesteController*, HealthTests, LimitesHttpTests, PipelineDeErrosTests
      Banco/        CargaTests, IndicesTests, LoginsTests, SchemaTests
      Catalogo/     CatalogoTests
      Infra/        Sql*, SqlServerCollection*, SqlServerFixture*
      Parte4/       InvestigacaoTests, PlanoDeExecucao, QueryRankingTests, ScriptsParte4
      Pedidos/      ConcorrenciaTests, GetPedidoPorIdTests, GetPedidosTests, PostPedidosTests
      Persistencia/ Arranjo*, InfraestruturaReal, EstoqueRepositoryTests, LeiturasTests,
                    MapeamentoTests, UnitOfWorkTests
      Relatorios/   GetTopClientesTests, RelatorioConsultasTests
      (* = ajudante usado por várias pastas)

frontend/
  Dockerfile  nginx.conf  seguranca.inc  angular.json  package.json  tsconfig*.json  eslint.config.js
  src/  main.ts  index.html  styles.css
    app/
      app.component.ts (+spec)  app.config.ts  app.routes.ts
      core/
        api/      clientes-api.ts, clientes.models.ts, pedidos-api.ts (+spec), pedidos.models.ts,
                  produtos-api.ts, produtos.models.ts, problema-api.model.ts, contrato.spec.ts
        rotulo-cliente.ts (+spec)   (usado só por pedidos/)
        uuid.ts (+spec)
      pedidos/    pedido-form.component.{ts,html,spec.ts}, pedido-lista.component.{ts,html,spec.ts}
      testing/    dom.ts

database/  01-schema.sql  02-indices.sql  03-logins.sql  04-carga.sql  parte4/ ...
e2e/       Playwright (smoke)
```

### Evidências coletadas

- **Namespaces e pastas.** Todos os arquivos de `backend/src` e `backend/tests` declaram o namespace igual ao caminho. A única exceção é `Program.cs`, que usa *top-level statements* e não tem namespace.
- **Arquivos com mais de um tipo de topo** em `backend/src`:
  - `Core/Clientes/IClienteService.cs`
  - `Core/Produtos/IProdutoService.cs`
  - `Core/Pedidos/PedidoConsultaService.cs`
  - `Core/Relatorios/RelatorioService.cs`

  Nos testes também há casos: `RotasDeTesteController.cs`, `Parte4/PlanoDeExecucao.cs` e `Parte4/ScriptsParte4.cs`. Esses ficam (ver §3).
- **Referências entre projetos.**

  | Projeto | Referencia |
  |---|---|
  | `Infrastructure` | `Core` e EF SqlServer |
  | `Api` | `Core` e `Infrastructure` |
  | `UnitTests` | os 3 projetos (por causa de `Arquitetura/`) |
  | `IntegrationTests` | `Api` |
- **Projetos SDK-style com *globbing*.** Mover um arquivo dentro de um projeto não altera o `.csproj` nem o `.sln`.
- **Hooks.** `guard-paths.sh` filtra por prefixo de diretório de topo: `backend/` para o agente `backend`, `frontend/` e `e2e/` para o `frontend`. Movimentos internos não exigem mudança nos hooks.
- **Rastreabilidade.** `.github/scripts/validar_rastreabilidade.py` procura **nomes** de teste via `rglob` em `backend/tests`, `frontend/src` e `e2e`. Mover arquivos dentro dessas raízes não o afeta.
- **CI e Dockerfiles.** `ci.yml` só cita caminhos de topo. O Dockerfile do backend copia os `.csproj` e `src/`. O do frontend faz `COPY . .`, mais `nginx.conf` e `seguranca.inc`. Nenhum dos movimentos propostos os afeta.
- **ArchUnitNET** (`UnitTests/Arquitetura/RegrasDeArquiteturaTests.cs`). As regras usam `ResideInNamespaceOrBelow` sobre os namespaces raiz (`Joya.Pedidos.Core`, `.Infrastructure`, `.Api`), o sufixo `.Arquitetura` na exclusão, os nomes de entidade (`Pedido`, `ItemPedido`, `Cliente`, `Produto`) e os sufixos `Request`/`Query`/`Response`. A âncora do assembly é `typeof(Core.Common.TipoErro)`. **Nenhum movimento proposto altera esses seletores.**
- **Checklist do `02-arquitetura.md` §8.** Os itens que citam caminho (BE-14, BE-15, BE-16, BE-20, FE-05, FE-12 e DK-06) apontam para caminhos que **não se movem**.

## 2. Diagnóstico

As fontes citadas são:

- **[MS-CA]** Microsoft, *Common web application architectures*: o *Application Core* guarda entidades, interfaces e serviços de domínio, e as dependências apontam para ele.
- **[MS-AP]** Microsoft, *Architectural principles*: separação de responsabilidades e responsabilidade única.
- **[MS-MVC]** Microsoft, *Develop ASP.NET Core MVC apps*: organização por feature, *filters* e *middleware* para preocupações transversais.
- **[NG-SG]** angular.dev, *Style guide*: organizar por área de feature, evitar subpastas por tipo, manter juntos os arquivos relacionados, um conceito por arquivo e nomes hifenizados.
- **[PROJ]** Decisões do projeto: `CLAUDE.md`, `plan.md` D-xx, ADRs e `02-arquitetura.md`.

| ID | Problema | Caminho atual | Caminho proposto | Justificativa (fonte) | Impacto | Risco | Referências a atualizar |
|---|---|---|---|---|---|---|---|
| EST-01 | Quatro arquivos guardam interface e implementação juntas. `ClienteService` e `ProdutoService` não são encontráveis pelo nome do arquivo, e `IPedidoConsultaService` e `IRelatorioService` ficam escondidos dentro da implementação. O padrão destoa de `Pedidos/IPedidoService.cs` + `PedidoService.cs`. | `Core/Clientes/IClienteService.cs`<br>`Core/Produtos/IProdutoService.cs`<br>`Core/Pedidos/PedidoConsultaService.cs`<br>`Core/Relatorios/RelatorioService.cs` | A implementação sai para um arquivo próprio: `Core/Clientes/ClienteService.cs` e `Core/Produtos/ProdutoService.cs`. A interface sai para um arquivo próprio: `Core/Pedidos/IPedidoConsultaService.cs` e `Core/Relatorios/IRelatorioService.cs`. Os 4 arquivos atuais ficam com um tipo cada. | [PROJ] skill `new-endpoint`, etapa 1: "um arquivo por tipo"; padrão já usado em `Pedidos/`. [NG-SG] e [MS-AP]: um conceito por arquivo. | 4 arquivos novos, 4 editados. Namespaces e `using` **inalterados**. | Baixo: só divide arquivos e é coberto pelo build. | Nenhuma. O ArchUnit não depende do arquivo. |
| EST-02 | `IVerificadorDeBanco` está em `Common`, mas é a porta da feature de saúde, que já tem pasta `Saude/` no `Api` e no `Infrastructure`. O `Common` do `CLAUDE.md` descreve o núcleo compartilhado (`Result`, `Error`, `PaginaResponse`, `Periodo`), não portas de feature. | `Core/Common/IVerificadorDeBanco.cs`<br>namespace `Joya.Pedidos.Core.Common` | `Core/Saude/IVerificadorDeBanco.cs`<br>namespace `Joya.Pedidos.Core.Saude` | [PROJ] D2 (pasta por feature no Core) e `CLAUDE.md` §Arquitetura. [MS-CA]: as portas ficam no Core, junto da feature. | 1 arquivo movido e 3 `using` ajustados: `Api/Saude/BancoDeDadosHealthCheck.cs`, `Infrastructure/Saude/VerificadorDeBanco.cs` e `Infrastructure/DependencyInjection.cs`, que mantém `Core.Common` por causa do `IUnitOfWork`. Nenhum teste referencia o tipo. | Baixo. Muda o namespace de um tipo público, mas o uso é interno à solução e não aparece em contrato HTTP. | `CLAUDE.md`, árvore do Core (PROP-15). |
| EST-03 | `Program.cs` mistura composição com lógica de adaptação de erro: `ErrosDoModelState` e `NormalizarChave` (QA-04, linhas 107-147) moldam o corpo `400` do *model binding*. A tradução para Problem Details já mora em `Api/Erros/ProblemasHttp.cs`. | `Api/Program.cs` (funções locais estáticas) | `Api/Erros/ProblemasHttp.cs`, como métodos `static` (`ErrosDoModelState` público, `NormalizarChave` privado). `Program.cs` passa a chamar `ProblemasHttp.ErrosDoModelState(contexto)`. Nenhum arquivo novo. | [PROJ] `02-arquitetura.md` §3: um único lugar traduz erro em HTTP. [MS-MVC]: o *composition root* só compõe. [MS-AP]: responsabilidade única. | 2 arquivos editados. `Program.cs` cai de cerca de 150 para cerca de 110 linhas. | Baixo a médio. É **movimento de código**, não só de arquivo. A cobertura vem de `Api/PipelineDeErrosTests`, que testa o formato do `400` com as chaves normalizadas. O código deve ser copiado sem reescrita. | Nenhuma. |
| EST-04 | Os ajudantes compartilhados dos testes de integração estão em três pastas que não dizem o que guardam. `Infra/` lembra o projeto `Infrastructure`, mas guarda o contêiner e o SQL utilitário. `ApiFactory` mora em `Api/`, junto dos testes do pipeline. `Arranjo` mora em `Persistencia/`, mas é usado por 5 pastas. Por isso cada teste de feature precisa de 3 `using` só para ajudantes. | `IntegrationTests/Infra/{Sql, SqlServerCollection, SqlServerFixture}.cs`<br>`IntegrationTests/Api/{ApiFactory, RotasDeTesteController}.cs`<br>`IntegrationTests/Persistencia/Arranjo.cs` | `IntegrationTests/Apoio/` com os 6 arquivos, namespace `Joya.Pedidos.IntegrationTests.Apoio`. `RotasDeTesteController` vai junto porque é registrado pelo `ApiFactory` (`AddApplicationPart`). `InfraestruturaReal` **fica** em `Persistencia/`, que é o único usuário. | [PROJ] espelha `UnitTests/Apoio/`, que já existe. [NG-SG] e [MS-AP]: agrupar o que é relacionado e nomear pelo papel. | 6 arquivos movidos, com o namespace trocado. Cerca de 22 arquivos de teste com `using` ajustado: os 20 que hoje importam `.Infra`, `.Api` ou `.Persistencia`, mais `Api/LimitesHttpTests` e `Api/PipelineDeErrosTests`, que usam `ApiFactory` pelo namespace e passam a precisar de `using ...Apoio`. | Baixo. É só compilação. A descoberta de testes do xUnit não depende de pasta, e a `[CollectionDefinition]` continua sendo achada pelo nome. | Nenhuma, em CI, hooks e rastreabilidade (ver §1). |
| EST-05 | `RelatorioConsultasTests` testa o `Infrastructure` direto (ServiceCollection própria e interceptador EF, sem HTTP), mas fica em `Relatorios/`, junto do teste HTTP. Os outros testes de persistência sem HTTP ficam em `Persistencia/`. | `IntegrationTests/Relatorios/RelatorioConsultasTests.cs` | `IntegrationTests/Persistencia/RelatorioConsultasTests.cs` (namespace `.Persistencia`) | [PROJ] regra de organização (PROP-15): `<Feature>/` é HTTP via `ApiFactory`; `Persistencia/` é Infrastructure sem HTTP; `Banco/` são os scripts; `Api/` é o pipeline; `Parte4/` é a investigação. | 1 arquivo movido e 1 namespace. | Baixo. **Opcional:** só vale se a regra entrar no `CLAUDE.md`. | Nenhuma. |
| EST-06 | `core/rotulo-cliente.ts` (QA-06) é regra de apresentação da feature de pedidos e só é usado por `pedido-form` e `pedido-lista`. Em `core/`, ele parece infraestrutura de aplicação. | `frontend/src/app/core/rotulo-cliente.ts` (+ `.spec.ts`) | `frontend/src/app/pedidos/rotulo-cliente.ts` (+ `.spec.ts`) | [NG-SG]: organizar por área de feature e manter juntos os arquivos relacionados. | 2 arquivos movidos e 2 imports ajustados (`'../core/rotulo-cliente'` passa a ser `'./rotulo-cliente'` nos 2 componentes). O import do spec é relativo e não muda. | Baixo: é coberto por `lint`, `build` e `test`. | Nenhuma. `angular.json`, `tsconfig` e `eslint` não citam subcaminhos de `app/`. |

## 3. Itens que ficam como estão

| Item | Motivo |
|---|---|
| Os 3 projetos `Core`, `Infrastructure` e `Api` | D1. Para 3 endpoints e 2 telas, um projeto `Application` separado não resolve nenhum problema real. A regra de dependência já é provada pelo ArchUnit. |
| Pastas por feature no `Core`, incluindo `Estoque/` | D2. `Estoque/` isola a porta da baixa atômica (D7, regra de ouro 4, spec 4.3), e os greps BE-14 e BE-15 dependem desse caminho. |
| `Core/Pedidos` sem subpastas (17 arquivos) | Os prefixos `Criar*` e `Listar*` já agrupam. Subdividir por caso de uso criaria pastas de 2 ou 3 arquivos sem ganho de navegação. |
| `Common` com `Result`, `Error`, `TipoErro`, `PaginaResponse`, `Periodo`, `PeriodoRegras` e `IUnitOfWork` | São de uso compartilhado entre Pedidos e Relatórios. `IUnitOfWork` é a porta transversal de transação (ADR-0004). `TipoErro` é a âncora do assembly no ArchUnit. |
| `Infrastructure`: features espelhadas + `Persistencia/` + `DependencyInjection.cs` na raiz | O espelho Core↔Infrastructure por feature facilita a navegação. Separar em `Persistence/` e `Repositories/` (árvore de referência do pedido) quebraria esse espelho e criaria pastas por tipo, que [NG-SG] e [MS-MVC] desaconselham. `DependencyInjection.cs` é o ponto de entrada do assembly. |
| Modelos de leitura `Cliente` e `Produto` no `Infrastructure` | Decisão registrada no `03-backend.md`: não são entidades de domínio. O teste de arquitetura os confere pelo nome. |
| `Api/` agrupado por responsabilidade (`Controllers`, `Erros`, `Limites`, `Saude`) | Os filtros já estão junto do assunto: `ValidationFilter` em `Erros`, `LimiteDeCorpoFilter` em `Limites`. Uma pasta `Filters/` por tipo separaria preocupações que andam juntas. |
| Nomes de pasta mistos (PT/EN) | Seguem a convenção do `CLAUDE.md`: domínio em português, sufixos técnicos em inglês. Renomear seria mudança estética, proibida pelo pedido. |
| `IntegrationTests/Catalogo/CatalogoTests` | Assunto coeso (GET `/clientes` e `/produtos`, HL-18), com uma *theory* compartilhada entre as duas rotas. |
| `IntegrationTests/Parte4/`, incluindo os arquivos com vários tipos | Espelha `database/parte4/`. `PlanoDeExecucao` + `OperadorDoPlano` e os registros de `ScriptsParte4` são ajudantes privados do mesmo teste. |
| `RotasDeTesteController.cs` com `CorpoDeTeste` | O DTO só existe para essa rota de teste. Ele vai junto em EST-04. |
| Frontend `pedidos/` sem invólucro `features/` | Só existe uma feature com telas. Reavaliar quando houver a segunda. |
| Frontend `core/api/` com todos os `*Api` e modelos | Regra do `CLAUDE.md` ("HTTP só nos serviços `*Api` em `core/api/`"), FE-05, FE-12 e ADR-0005. `contrato.spec.ts` confere todos os modelos juntos. Clientes e produtos não têm telas próprias, então não justificam features. |
| `core/uuid.ts` | Utilitário genérico de plataforma (`crypto`), não regra de pedidos. |
| `frontend/src/app/testing/` | Ajudante exclusivo de specs (QA-08), já isolado. |
| `frontend/nginx.conf` e `seguranca.inc` na raiz do frontend | O Dockerfile e o DK-06 dependem desse caminho. |
| `database/`, `e2e/`, `.github/`, `.claude/`, `docs/` e os arquivos da raiz | Já organizados. A raiz não tem código de implementação. Os hooks filtram por prefixo de topo. |
| Documentos históricos que citam caminhos internos | `03-backend.md`, `04-frontend.md`, `05-seguranca.md`, `06-qa.md`, `backlog.md`, `revisoes/arquitetura-1.md`, `ia/diario.md` e `ia/prompts.md` (encerrado). São registros datados e **não** devem ser reescritos. |
| `Directory.Packages.props` | Não existe, embora a árvore do `plan.md` o cite. Criá-lo é gerenciamento central de pacotes, que está fora do escopo estrutural (§7). |
| `docs/contratos/` | Não será criado: T-11.2 está fora do escopo (HL-28) e o pedido proíbe pasta vazia. |

## 4. Árvore proposta

Legenda: `[M]` movido, `[N]` arquivo novo por divisão, `[E]` editado, `[ns]` namespace ajustado. O que não aparece fica igual à §1.

```text
backend/src/
  Joya.Pedidos.Core/
    Clientes/
      IClienteService.cs              [E]  só a interface
      ClienteService.cs               [N]  EST-01
    Common/                           (sem IVerificadorDeBanco)
    Pedidos/
      IPedidoConsultaService.cs       [N]  EST-01
      PedidoConsultaService.cs        [E]  só a classe
    Produtos/
      IProdutoService.cs              [E]  só a interface
      ProdutoService.cs               [N]  EST-01
    Relatorios/
      IRelatorioService.cs            [N]  EST-01
      RelatorioService.cs             [E]  só a classe
    Saude/
      IVerificadorDeBanco.cs          [M][ns] Core.Common -> Core.Saude  (EST-02)
  Joya.Pedidos.Infrastructure/
    DependencyInjection.cs            [E]  + using Joya.Pedidos.Core.Saude
    Saude/VerificadorDeBanco.cs       [E]  using Core.Common -> Core.Saude
  Joya.Pedidos.Api/
    Program.cs                        [E]  sem as funções locais; chama ProblemasHttp.ErrosDoModelState (EST-03)
    Erros/ProblemasHttp.cs            [E]  + ErrosDoModelState (public static), NormalizarChave (private static)
    Saude/BancoDeDadosHealthCheck.cs  [E]  using Core.Common -> Core.Saude

backend/tests/
  Joya.Pedidos.IntegrationTests/
    Apoio/                                       (EST-04, namespace ...IntegrationTests.Apoio)
      ApiFactory.cs                   [M][ns]  de Api/
      RotasDeTesteController.cs       [M][ns]  de Api/
      Arranjo.cs                      [M][ns]  de Persistencia/
      Sql.cs                          [M][ns]  de Infra/
      SqlServerCollection.cs          [M][ns]  de Infra/
      SqlServerFixture.cs             [M][ns]  de Infra/
    Api/          HealthTests, LimitesHttpTests, PipelineDeErrosTests          [E] using
    Banco/        CargaTests, IndicesTests, LoginsTests, SchemaTests           [E] using
    Catalogo/     CatalogoTests                                                [E] using
    Parte4/       InvestigacaoTests, QueryRankingTests, ScriptsParte4 [E] using; PlanoDeExecucao
    Pedidos/      ConcorrenciaTests, GetPedidoPorIdTests, GetPedidosTests, PostPedidosTests [E] using
    Persistencia/ InfraestruturaReal, EstoqueRepositoryTests, LeiturasTests,
                  MapeamentoTests, UnitOfWorkTests                             [E] using
                  RelatorioConsultasTests          [M][ns] de Relatorios/ (EST-05, opcional)
    Relatorios/   GetTopClientesTests                                          [E] using
    (Infra/ deixa de existir, porque todos os arquivos foram movidos; nenhum arquivo é apagado)

frontend/src/app/
  core/
    api/  (igual)
    uuid.ts (+spec)
  pedidos/
    pedido-form.component.{ts,html,spec.ts}   [E] import './rotulo-cliente'
    pedido-lista.component.{ts,html,spec.ts}  [E] import './rotulo-cliente'
    rotulo-cliente.ts (+spec)                 [M] de core/  (EST-06)
  testing/dom.ts
```

## 5. Propostas de alteração (PROP-xx)

Nenhuma proposta altera spec, regras, contrato ou modelo de dados.

### Baixo risco, puramente estrutural

**PROP-15: atualizar a árvore "Arquitetura" do `CLAUDE.md`** (dono: desenvolvedor/orquestrador)

- **Texto atual.**
  - Core: `Common/ Result<T>, Error, PaginaResponse<T>, Periodo` e `Pedidos/ Relatorios/ Clientes/ Produtos/`.
  - Infrastructure: "EF Core 8 (DbContext, configurações, repositórios, UnitOfWork)".
  - Api: "Controllers, filtros, Problem Details, composição (Program.cs)".
  - IntegrationTests: "Endpoints contra SQL Server real".
  - O frontend não tem árvore.
- **Texto proposto.**
  - **Core.**
    - `Common/`: `Result<T>`, `Error`, `TipoErro`, `PaginaResponse<T>`, `Periodo`, `PeriodoRegras` e `IUnitOfWork`.
    - Features: `Pedidos/ Relatorios/ Clientes/ Produtos/ Estoque/ Saude/`, com a nota "`Estoque/` = porta da baixa atômica (D7)".
  - **Infrastructure.** Uma pasta por feature, espelhando o Core, mais `Persistencia/` (DbContext, configurações, UnitOfWork) e `DependencyInjection.cs`.
  - **Api.** `Controllers/`, `Erros/` (Problem Details, ExceptionHandlers, ValidationFilter), `Limites/` (rate limit e tamanho de corpo), `Saude/` e `Program.cs` (só composição).
  - **UnitTests.** `Apoio/` (falsos) e `Arquitetura/` (ArchUnitNET).
  - **IntegrationTests.** "Contra SQL Server real", com a regra de pastas: `Apoio/` = fixture e ajudantes; `<Feature>/` = HTTP via `ApiFactory`; `Persistencia/` = Infrastructure sem HTTP; `Banco/` = scripts `database/`; `Api/` = pipeline HTTP; `Parte4/` = investigação.
  - **Frontend.** `core/api/` (HTTP e modelos), `core/` (utilitários de plataforma), `pedidos/` (telas e regras de apresentação da feature) e `testing/` (ajudantes de spec).
- **Benefício.** O mapa de entrada do repositório passa a refletir o código. Hoje ele omite `Estoque/`, `IUnitOfWork`, `TipoErro` e as pastas do `Api`. A regra de pastas de teste deixa de ser implícita.
- **Custo.** Cerca de 25 linhas no `CLAUDE.md`. Nenhum código.

**PROP-16: corrigir a deriva do `02-arquitetura.md` §3** (dono: arquiteto)

- **Texto atual.** O §3 cita as portas `IClienteConsultas` e `IProdutoConsultas`, que foram fundidas em `IClienteRepository` e `IProdutoRepository`. A tabela dos testes de arquitetura lista as entidades sem `Cliente` e `Produto`.
- **Texto proposto.** Os nomes reais das portas. A linha "Entidades" da tabela passa a ter `Pedido`, `ItemPedido`, `Cliente` e `Produto`, como em `RegrasDeArquiteturaTests`.
- **Benefício.** O documento de desenho volta a bater com o teste que o prova.
- **Custo.** 2 ou 3 linhas. Nenhum código.

### Precisa de aprovação

**PROP-17: guardar "namespace = pasta" no build (IDE0130)** (dono da execução: `backend`; `.editorconfig` está no caminho dele)

- **Texto atual.** `backend/.editorconfig` não configura `dotnet_style_namespace_match_folder`. A coerência que existe hoje é 100%, mas é só convenção.
- **Texto proposto.** Na seção `[*.cs]`:

  ```ini
  dotnet_style_namespace_match_folder = true
  dotnet_diagnostic.IDE0130.severity = warning
  ```

  Com `EnforceCodeStyleInBuild=true` e `TreatWarningsAsErrors` (já no `Directory.Build.props`), qualquer divergência quebra o build.
- **Benefício.** Torna verificável a premissa que sustenta o ArchUnit, que seleciona por namespace. Um arquivo movido sem ajustar o namespace (justamente o risco de EST-02, EST-04 e EST-05) passa a falhar no BE-01/BE-02 em vez de escapar silenciosamente das regras.
- **Custo.** 2 linhas. O diff no código hoje é zero.
- **Ressalva.** No gate, confirmar que a regra dispara na linha de comando: um namespace trocado de propósito numa cópia descartável, fora da árvore versionada, deve falhar no `dotnet build`. Se não disparar, a PROP é retirada e registrada como não verificável.

**PROP-18: alinhar a skill `/new-endpoint` e o checklist ao padrão "um tipo por arquivo"** (donos: `.claude/skills/new-endpoint/SKILL.md` é do desenvolvedor/orquestrador; `02-arquitetura.md` é do arquiteto)

- **Texto atual.**
  - **Skill, etapa 4.** "Interface + implementação `sealed`", com as duas num único bloco de código. Isso induz o padrão de 2 tipos por arquivo que EST-01 corrige.
  - **Skill, linhas 18 e 232.** Citam `database/01_schema.sql`, um nome antigo. Os arquivos reais são `01-schema.sql` e `02-indices.sql`.
  - **Checklist do `02-arquitetura.md` §8.** Não tem regra de um tipo por arquivo.
- **Texto proposto.**
  - **Skill, etapa 4.** "Interface `I<Recurso>Service.cs` e implementação `<Recurso>Service.cs` em arquivos separados", com dois blocos de código.
  - **Nomes dos scripts.** `database/01-schema.sql` (tabelas) e `database/02-indices.sql` (índices).
  - **Novo item BE-28 no checklist.** "Um tipo de topo por arquivo em `backend/src`". O comando de verificação:

    ```bash
    for f in $(rg -l --glob '*.cs' '^\s*(public|internal)\s' backend/src); do
      n=$(rg -c '^(public|internal)\s+(sealed\s+|static\s+|abstract\s+)?(partial\s+)?(class|record|interface|enum|struct)\b' "$f")
      [ "$n" -gt 1 ] && echo "$f"
    done
    ```

    A saída esperada é vazia. `Program.cs` sai naturalmente, porque `public partial class Program;` é o único tipo de topo.
- **Benefício.** Quem gera endpoints novos não reintroduz o problema, e a revisão passa a ter um critério objetivo.
- **Custo.** Cerca de 15 linhas de documentação. A regra vale só para `backend/src`; os testes ficam de fora (§3).

## 6. Tamanho, ordem e gates

### Tamanho estimado

| Área | Novos | Movidos | Editados | Itens |
|---|---|---|---|---|
| `backend/src` | 4 | 1 | 10 (4 divididos, 3 `using`, `Program.cs`, `ProblemasHttp.cs`, `DependencyInjection.cs`) | EST-01, EST-02, EST-03 |
| `backend/tests` | 0 | 6 (+1 opcional) | cerca de 22 (`using`/namespace) | EST-04, EST-05 |
| `frontend` | 0 | 2 | 2 | EST-06 |
| Transversais | 0 | 0 | 4: `CLAUDE.md`, `backend/.editorconfig`, `SKILL.md`, `02-arquitetura.md` | PROP-15 a PROP-18 |
| **Total** | **4** | **9 (+1)** | **cerca de 38** | |

Nenhum arquivo é apagado. Os movimentos devem usar `git mv` para preservar o histórico. O orquestrador ou o desenvolvedor executa, porque os agentes não alteram a árvore pelo `git`.

### Ordem e gate de cada etapa

Cada etapa é uma tarefa com seu commit próprio, só com autorização.

1. **Backend `src`** (agente `backend`): EST-01, depois EST-02, depois EST-03. Se aprovada, a PROP-17 entra **antes** dos movimentos, para que o build já os guarde.
   - **Gate:**
     - `dotnet build backend/Joya.Pedidos.sln` (zero warnings)
     - `dotnet format backend/Joya.Pedidos.sln --verify-no-changes`
     - `dotnet test backend/tests/Joya.Pedidos.UnitTests` (inclui `Arquitetura/`)
     - `dotnet test backend/tests/Joya.Pedidos.IntegrationTests --filter "Categoria!=Medicao"` (em especial `PipelineDeErrosTests` para EST-03 e `HealthTests` para EST-02)
     - os greps do `02-arquitetura.md` §8 (BE-xx) e, se aprovado, o BE-28
2. **Backend `tests`** (agente `backend`): EST-04, depois EST-05 (opcional).
   - **Gate:**
     - `dotnet build`
     - `dotnet format --verify-no-changes`
     - os testes unitários e de integração completos
     - a contagem de testes descobertos igual à de antes. Comparar o total de `dotnet test --list-tests` antes e depois. Nenhum teste pode sumir por *collection* não encontrada.
3. **Frontend** (agente `frontend`): EST-06.
   - **Gate:**
     - `npm --prefix frontend ci`
     - `npm --prefix frontend run lint`
     - `npm --prefix frontend run build`
     - `npm --prefix frontend test -- --watch=false --browsers=ChromeHeadless`
4. **Transversais** (orquestrador e arquiteto): PROP-15, PROP-16 e PROP-18.
   - **Gate:**
     - `python .github/scripts/validar_rastreabilidade.py`
     - `bash .github/scripts/testar_hooks.sh`
     - `docker compose config`
     - `git diff --check`
     - re-revisão do arquiteto (`docs/esteira/revisoes/refatoracao-estrutural-2.md`), aplicando o checklist §8 ao diff

## 7. Recomendações fora do escopo (só listadas)

1. **Gerenciamento central de pacotes** (`Directory.Packages.props`). As versões hoje estão por `.csproj`. Mudaria o ferramental de build, não a estrutura.
2. **Deriva histórica do `plan.md`.** A árvore cita `Directory.Packages.props` e os scripts `01_schema.sql`, `02_seguranca.sql` e `03_carga.sql`; as tarefas 1.1 a 1.3, 2.1 e 5.1 também. O `plan.md` é registro de decisão e não deve ser reescrito: no máximo, uma nota de errata, se o desenvolvedor quiser.
3. **T-11.2** (`docs/contratos/openapi.json` e o BE-24). Continua fora do escopo pela HL-28. O BE-24 segue sem objeto até lá.
4. **Testes unitários** para `ClienteService` e `ProdutoService`. Hoje esses serviços são só repasse e estão cobertos por `Catalogo/CatalogoTests`, na integração.
5. **Uma feature `clientes/` ou `produtos/` no frontend.** Só faz sentido se surgirem telas próprias.
