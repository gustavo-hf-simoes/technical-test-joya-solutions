# ADR-0003 — Banco real nos testes de integração: mesmo executor de scripts e mesmo login da API

- **Status:** Proposto (etapa 2, arquitetura)
- **Data:** 2026-11-13
- **Referências:** [plan.md](../plan.md) `D6`, `D10`, `D15`; spec §6.5 e RNF-07; [02-arquitetura.md](../esteira/02-arquitetura.md) §1 (R-03, R-12) e §8 (`DB-01`, `DB-02`)

## Contexto

O `D10` já decide **com o quê** testar: Testcontainers, `WebApplicationFactory` e Respawn, aplicando `database/*.sql`. Faltava decidir **como** os scripts chegam ao contêiner de teste e **com qual login** a API testada se conecta. As duas escolhas determinam o que o teste prova.

- Se o fixture lê os `.sql` e os executa pelo `SqlClient`, ele precisa partir os scripts em `GO` sozinho e não liga `QUOTED_IDENTIFIER` como o `sqlcmd -I` liga. Um script pode passar no teste e falhar no `db-init`, ou o contrário. O índice filtrado `UX_Pedidos_ChaveIdempotencia` exige `QUOTED_IDENTIFIER ON` (spec §6.2).
- Se a API testada usa `sa`, os testes não percebem um `GRANT` faltando para `joya_app`. O erro só apareceria no `docker compose up`.

## Decisão

1. **Um executor só.** `database/init.sh` aplica os scripts em ordem (`01-schema.sql`, `02-indices.sql`, `03-logins.sql`, `04-carga.sql`) com `/opt/mssql-tools18/bin/sqlcmd -C -I -b`, e as senhas dos logins vão por `-v`. O `db-init` do compose roda esse script. O fixture de integração copia `database/` para o contêiner do Testcontainers (`WithResourceMapping`) e roda o **mesmo** script com `ExecAsync`. Um código de saída diferente de zero falha o fixture, com o `stderr` na mensagem.
2. **Escolha dos scripts por argumento.** `init.sh` aceita a lista de etapas e, sem argumento, roda todas. O fixture aplica schema, índices e logins. A carga de exemplo (`04-carga.sql`) fica de fora, para que cada teste monte só os dados de que precisa. Os testes `Carga_*` e `QueryOriginal_*`/`QueryCorrigida_*` aplicam a carga por conta própria, pelo mesmo executor.
3. **A API testada usa `joya_app`.** A `WebApplicationFactory` troca a *connection string* para o login `joya_app` com a senha passada ao `init.sh`. Assim, cada teste de endpoint também prova os `GRANT`s da spec §6.5.
4. **Arrange e Respawn usam `sa`.** O `joya_app` não tem `DELETE` (RN-12), então o reset do Respawn e a inserção de dados de teste usam uma segunda *connection string* com `sa`, que só existe no projeto de teste. O Respawn limpa só as quatro tabelas do schema `dbo` e reinicia o `IDENTITY` (`WithReseed = true`), para que os ids fiquem previsíveis.
5. **Um contêiner por execução.** Uma `ICollectionFixture` única, com os testes em série dentro dela. O CA-10 e o CA-37 fazem a própria concorrência por `Task.WhenAll`; o paralelismo do xUnit não traria ganho e abriria disputa entre testes e Respawn.
6. **Imagem fixa.** O fixture usa a mesma *tag* do compose (`mcr.microsoft.com/mssql/server:2022-latest`), declarada numa constante do projeto de teste. Uma divergência entre as duas aparece na revisão (`DB-01` da §8).
7. **Ambiente de teste.** A `WebApplicationFactory` usa o ambiente `Testing`, com Swagger ligado (o teste de contrato do [ADR-0005](0005-contrato-openapi-versionado.md) lê `/swagger/v1/swagger.json`) e o limite de taxa alto (ADR-0002).

## Alternativas consideradas

| Alternativa | Por que não |
|---|---|
| Executar os `.sql` pelo `SqlClient`, partindo em `GO` | Segundo executor com comportamento diferente do `sqlcmd` (`QUOTED_IDENTIFIER`, variáveis `$(...)`, `-b`) |
| Usar o SQL Server do compose nos testes | Depende de estado externo e da ordem de quem subiu antes. O CI teria de subir o compose para rodar testes de backend |
| API testada como `sa` | Esconde erros de permissão até o `docker compose up` |
| Aplicar a carga de exemplo no fixture | Os testes passariam a depender de dados que mudam com a carga. Asserções sobre contagens ficariam frágeis |
| Um contêiner por classe de teste | De 10 a 20 s por contêiner, sem ganho de isolamento sobre o Respawn |

## Consequências

- **Positivas:** um script que falha no teste falha também no `db-init`, e vice-versa. Os `GRANT`s de `joya_app` ficam cobertos por todos os testes de endpoint, sem teste dedicado além dos de T-04.3.
- **Negativas:**
  - O `init.sh` vira código de produção e de teste ao mesmo tempo: precisa de `set -eu`, de final de linha `LF` (o `.gitattributes` deve forçar `eol=lf` em `*.sh`) e de parâmetros explícitos.
  - Os testes em série são mais lentos que em paralelo. Com um contêiner e Respawn, a estimativa fica em poucos minutos no CI.
  - O projeto de teste guarda uma *connection string* com `sa`, que é aceitável porque o contêiner é efêmero e a senha é gerada no teste.
