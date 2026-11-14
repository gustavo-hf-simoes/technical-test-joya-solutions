# Mini Gestão de Pedidos — Joya Solutions

[![CI](https://github.com/gustavo-hf-simoes/technical-test-joya-solutions/actions/workflows/ci.yml/badge.svg)](https://github.com/gustavo-hf-simoes/technical-test-joya-solutions/actions/workflows/ci.yml)

Cadastro e consulta de pedidos de clientes: .NET 8, Angular 19 e SQL Server 2022, tudo em Docker. O projeto foi construído com o **Claude Code** numa esteira *Spec-Driven Development* com agentes por papel.

## Como rodar com Docker

Pré-requisito: Docker com Compose v2. Nada mais: não é preciso .NET, Node nem `.env`.

```bash
docker compose up
```

| Serviço | O que faz | Endereço |
|---|---|---|
| `db` | SQL Server 2022, com os dados num volume nomeado | `127.0.0.1:1433` (só no loopback) |
| `db-init` | Contêiner efêmero: aplica `database/*.sql` (schema, índices, logins e carga) e sai com 0. Rodar de novo não duplica a carga | — |
| `api` | API .NET 8 sem root, conectada como `joya_app` (nunca `sa`) | http://localhost:8080/swagger |
| `frontend` | Angular servido por Nginx sem root, com proxy de `/api` para a API | **http://localhost:4200** |

A ordem é garantida por `depends_on` com healthcheck: o `db` só fica saudável com todos os bancos `ONLINE`, o `db-init` espera o `db`, a `api` espera o `db-init` terminar e o `frontend` espera a `api` responder em `/health/ready`.

```bash
# Depois de mudar o código: reconstrói as imagens
docker compose up --build

# Só o banco (para rodar a API pelo dotnet ou usar o MCP)
docker compose up -d db db-init

# Recriar o banco do zero (APAGA o volume de dados)
docker compose down -v
```

As senhas do compose são valores de **desenvolvimento**, sobrescrevíveis por variável de ambiente (modelo em `.env.example`).

## Como rodar os testes

Execute a partir da raiz do repositório.

| Suíte | Comando | O que exige | Quantidade |
|---|---|---|---|
| Unitários (regras, validações, arquitetura) | `dotnet test backend/tests/Joya.Pedidos.UnitTests` | .NET 8 SDK | 109 |
| Integração (endpoints contra SQL Server real) | `dotnet test backend/tests/Joya.Pedidos.IntegrationTests --filter "Categoria!=Medicao"` | .NET 8 SDK e Docker em execução | 125 |
| Medição da Parte 4 (carga de 800 mil linhas, 2 a 3 min) | `dotnet test backend/tests/Joya.Pedidos.IntegrationTests --filter "Categoria=Medicao"` | Os mesmos | 1 |
| Frontend (unitários) | `npm --prefix frontend ci` e depois `npm --prefix frontend test -- --watch=false --browsers=ChromeHeadless` | Node 22 e Chrome | 24 |

- **Integração:** o Testcontainers sobe um SQL Server 2022 descartável e aplica os **mesmos** scripts `database/*.sql` do compose, pelo mesmo `init.sh` ([ADR-0003](docs/adr/0003-banco-real-nos-testes-de-integracao.md)). O Respawn limpa as tabelas entre os testes. Não há banco em memória: o schema, os índices, a baixa atômica de estoque e os logins são testados de verdade.
- **Arquitetura:** os testes unitários incluem regras de dependência (ArchUnitNET). Exemplos: o `Core` não referencia EF, ASP.NET nem SqlClient; DTOs são `sealed record`; métodos `Async` recebem `CancellationToken` por último.
- **Nomes:** os testes seguem `Metodo_Cenario_ResultadoEsperado` e, quando provam um critério da spec, levam o `CA-xx` no traço. A [matriz de rastreabilidade](docs/esteira/rastreabilidade.md) liga cada linha do enunciado ao teste e ao commit, e o CI recusa teste ou hash inexistente.

```bash
# Build sem nenhum aviso (TreatWarningsAsErrors) e formatação
dotnet build backend/Joya.Pedidos.sln
dotnet format backend/Joya.Pedidos.sln --verify-no-changes

# Lint e build do frontend
npm --prefix frontend run lint
npm --prefix frontend run build
```

## Links de entrega

| O quê | Link |
|---|---|
| Repositório | https://github.com/gustavo-hf-simoes/technical-test-joya-solutions |
| Vídeo (até 10 min) | A publicar. Roteiro em [docs/ia/roteiro-video.md](docs/ia/roteiro-video.md) |
| Aplicação publicada | A publicar (US-17) |

## Onde está cada coisa

| Para entender | Leia |
|---|---|
| O que se pede (texto literal da vaga, com linhas numeradas) | [docs/requisitos/enunciado.md](docs/requisitos/enunciado.md) |
| O que é construído: regras, contratos, modelo de dados, critérios de aceite | [docs/spec.md](docs/spec.md) |
| O plano da IA e o que o desenvolvedor mudou e por quê (`HL-xx`) | [docs/plan.md](docs/plan.md) |
| Qual teste e qual commit atendem cada linha do enunciado (validado no CI) | [docs/esteira/rastreabilidade.md](docs/esteira/rastreabilidade.md) |
| Como é construído: a esteira, os agentes e os gates | [docs/esteira/README.md](docs/esteira/README.md) |
| Como o custo de IA é controlado (modelo por complexidade) | [docs/adr/0001-roteamento-de-modelos.md](docs/adr/0001-roteamento-de-modelos.md) |
| A linha do tempo do trabalho com a IA, com datas, prompts e erros corrigidos | [docs/ia/diario.md](docs/ia/diario.md) |
| Convenções para a IA (rules) | [CLAUDE.md](CLAUDE.md) e [.claude/](.claude/) |

## Histórico de commits

A spec, o plano e a configuração `.claude/` foram escritos antes do `git init`. Para não juntar tudo num commit único, eles entraram como **commits lógicos de bootstrap**, na ordem real em que foram criados. Os commits têm a data real em que foram feitos: nenhuma data foi retroagida. A evolução de cada documento antes do repositório existir está registrada no [diário](docs/ia/diario.md) (`HL-06`).

A partir daí, cada história do backlog tem uma branch, com um commit por tarefa. A branch é integrada por PR com merge commit, sem squash e sem rebase, e o CI roda em cada PR.
