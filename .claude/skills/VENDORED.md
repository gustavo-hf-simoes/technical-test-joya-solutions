# Skills de terceiros vendorizadas

Este repositório **não instala skills em tempo de execução** (`npx skills add` ou marketplace). As skills de terceiros foram copiadas para cá a partir de um commit fixo, depois de lidas e auditadas. Assim o conteúdo fica versionado e revisável no diff, e atualizar alguma delas é sempre um commit explícito.

**Por que tanto cuidado?** A auditoria ToxicSkills (Snyk, 2026) encontrou falhas de segurança em 36,8% das skills publicadas no ClawHub e no skills.sh, com 76 casos de malware confirmado. Em muitos deles, o ataque está escrito em linguagem natural no próprio `SKILL.md` (pesquisa da CSA sobre envenenamento de contexto via `SKILL.md`). Uma skill é, na prática, código executado pelo agente.

## Política de adoção

1. **Só publicadores oficiais.** Vale apenas a organização dona da tecnologia: Microsoft/.NET, Google/Angular, Testcontainers/Docker ou Sentry. Coleções genéricas da comunidade ficam de fora.
2. **Commit fixo**, nunca branch ou `@latest`.
3. **Auditoria antes de copiar**, com estes passos:
   - leitura do `SKILL.md`;
   - busca por comandos de rede ou instalação (`curl`, `wget`, `iwr`, `npm -g`, `pip install`), `base64`, `eval`/`exec`, frases de injeção ("ignore previous", "do not tell") e acesso a `.ssh` ou tokens;
   - busca por caracteres Unicode invisíveis ou bidirecionais (U+200B–U+200F, U+202A–U+202E, U+2060–U+2064, U+FEFF, U+E0000–U+E007F);
   - verificação de comentários HTML escondidos;
   - inventário dos domínios citados.
4. **Sem scripts executáveis.** Skills que trazem `scripts/` ficaram de fora, ou entraram só com as referências em Markdown.
5. **`allowed-tools` removido.** Nenhuma skill de terceiro ganha permissão de ferramenta pré-aprovada. Tudo passa pelas permissões de `.claude/settings.json`.
6. **Adaptação local explícita.** Cada `SKILL.md` ganhou, logo abaixo do frontmatter, um bloco `> **Adaptação local (Joya Solutions)**`. Esse bloco ajusta a skill às decisões do projeto e declara que, em caso de conflito, prevalecem o `CLAUDE.md` e o `docs/spec.md`. **Essa é a única alteração feita no texto original.**

Auditoria feita em 13/11/2026 pelo Claude Code (Opus 5.5), na sessão que montou a esteira, e registrada no `docs/plan.md` (seção Human-in-the-Loop).

## Inventário

| Skill local | Origem (repositório @ commit) | Caminho na origem | Licença | Usada por |
|---|---|---|---|---|
| `dotnet-webapi` | `dotnet/skills` @ `2877c712edc20643b72f64e2983b168e6661d607` | `plugins/dotnet-aspnetcore/skills/dotnet-webapi` | MIT | `backend` |
| `optimizing-ef-core-queries` | `dotnet/skills` @ `2877c712…` | `plugins/dotnet-data/skills/optimizing-ef-core-queries` | MIT | `backend`, `arquiteto` |
| `test-anti-patterns` | `dotnet/skills` @ `2877c712…` | `plugins/dotnet-test/skills/test-anti-patterns` | MIT | `qa` |
| `assertion-quality` | `dotnet/skills` @ `2877c712…` | `plugins/dotnet-test/skills/assertion-quality` | MIT | `qa` |
| `test-gap-analysis` | `dotnet/skills` @ `2877c712…` | `plugins/dotnet-test/skills/test-gap-analysis` | MIT | `qa` |
| `test-analysis-extensions` | `dotnet/skills` @ `2877c712…` | `plugins/dotnet-test/skills/test-analysis-extensions` | MIT | catálogo de referência (não é invocada) |
| `testcontainers-dotnet` | `testcontainers/claude-skills` @ `5263fe47160c3ef187b93de4b21d2d7380bce12e` | `plugins/testcontainers/skills/testcontainers-dotnet` | MIT | `backend` |
| `angular-developer` | `angular/angular` @ `189afa9ce5db12ba4b308afc999f05967b15c1ce` | `skills/dev-skills/angular-developer` | MIT | `frontend` |
| `security-review` | `getsentry/skills` @ `e94f22214528d69899d09c627b3315ddc3544816` | `skills/security-review` | Apache-2.0 (referências derivadas do OWASP Cheat Sheet Series, CC BY-SA 4.0) | `ciberseguranca` |
| `gha-security-review` | `getsentry/skills` @ `e94f2221…` | `skills/gha-security-review` | Apache-2.0 | `ciberseguranca` |

Skill própria do projeto, sem origem externa: `new-endpoint`.

## Avaliadas e não adotadas

| Candidata | Motivo |
|---|---|
| `microsoft/playwright-cli` → `playwright-cli` | Exige `npm install -g @playwright/cli@latest`: instalação global, sem versão fixa. Os testes E2E usam `@playwright/test` como dependência versionada do projeto. |
| `anthropics/skills` → `webapp-testing` | Depende de scripts Python (`with_server.py`) e repete o que o `@playwright/test` já resolve. |
| `dotnet/skills` → `coverage-analysis`, `crap-score` | Trazem scripts PowerShell executáveis. A cobertura sai do `dotnet test --collect "XPlat Code Coverage"`. |
| `dotnet/skills` → `run-tests`, `writing-mstest-tests`, `code-testing` | Voltadas para MSTest e Microsoft.Testing.Platform. O projeto usa xUnit, e os comandos já estão no `CLAUDE.md`. |
| `getsentry/skills` → `skill-scanner` | Ótima ideia, mas traz um script Python executável. A mesma checagem foi feita manualmente (passo 3 da política) e a execução local do script não foi autorizada. |
| Coleções genéricas (`wshobson/agents`, `dl-ezo/claude-code-sub-agents`, `valllabh/claude-agents`) | Genéricas demais para este escopo e de procedência variável. Serviram só como referência de formato. |
| BMAD Method, Spec Kit CLI | Avaliados na decisão sobre a esteira (`docs/esteira/README.md`). Adotamos as convenções do Spec Kit sem a CLI. |

## Achados da auditoria

- **Nenhum caractere invisível ou bidirecional** em nenhum arquivo.
- **Comentários HTML:** apenas exemplos de template Angular e o crédito ao OWASP. Nenhuma instrução escondida.
- **Domínios suspeitos** (`hackmoltrepeat.com`, `attacker.com`, `evil.com`): aparecem só em `gha-security-review/references/real-world-attacks.md` e em outros exemplos de vulnerabilidade, como indicadores documentados de ataques reais (campanha HackerBot Claw). São conteúdo didático e não são executados.
- **Comandos de instalação** nos textos:
  - `dotnet add package Testcontainers.*` na skill da Testcontainers, coerente com o propósito;
  - `npm install -g` na `playwright-cli`, motivo da exclusão.
- **Divergências com as decisões do projeto**, todas neutralizadas pelo bloco de adaptação local:
  - `dotnet-webapi` prefere minimal APIs; aqui usamos Controllers.
  - `optimizing-ef-core-queries` sugere migrations, que são proibidas aqui.
  - `angular-developer` recomenda a versão mais recente, Signal Forms e Vitest; aqui fixamos Angular 19 com Reactive Forms tipados.

## Como atualizar uma skill

1. Clone o repositório de origem numa pasta temporária, fora do projeto, e faça checkout do novo commit.
2. Rode o `git diff <commit-antigo>..<commit-novo> -- <caminho>` e repita o passo 3 da política só sobre o que mudou.
3. Copie a pasta, reaplique o bloco de adaptação local e remova `allowed-tools`.
4. Atualize esta tabela e faça um commit dedicado: `chore(claude): atualiza skill <nome> para <commit>`.
