# Mini Gestão de Pedidos — Joya Solutions

[![CI](https://github.com/gustavo-hf-simoes/technical-test-joya-solutions/actions/workflows/ci.yml/badge.svg)](https://github.com/gustavo-hf-simoes/technical-test-joya-solutions/actions/workflows/ci.yml)

Cadastro e consulta de pedidos de clientes: .NET 8, Angular 19 e SQL Server 2022, tudo em Docker. O projeto foi construído com o **Claude Code** numa esteira *Spec-Driven Development* com agentes por papel.

> **Em construção.** As seções de execução, testes, decisões técnicas e Parte 4 serão preenchidas conforme a esteira avança. O estado atual está em [docs/esteira/status.md](docs/esteira/status.md).

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
