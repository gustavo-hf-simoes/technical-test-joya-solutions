# Revisão <arquitetura | segurança | QA> — rodada <r> — <data>

> Modelo de relatório de revisão, usado por `arquiteto` (REVISAO), `ciberseguranca` e `qa`. O revisor **não corrige**: cada achado tem um único dono e é re-revisado pelo mesmo revisor que o abriu.

## Escopo

- Commits ou diff revisados: `<hash>..<hash>` ou "árvore de trabalho em <data>"
- Histórias cobertas: US-xx
- Ferramentas e comandos usados:

## Resumo

| Severidade | Abertos | Resolvidos | Aceitos (HL) |
|---|---|---|---|
| Crítica | 0 | 0 | 0 |
| Alta | 0 | 0 | 0 |
| Média | 0 | 0 | 0 |
| Baixa | 0 | 0 | 0 |

## Achados

### <PREFIXO>-01 — <título curto>

| Campo | Valor |
|---|---|
| Severidade | Crítica / Alta / Média / Baixa |
| Dono | backend / frontend / orquestrador |
| Regra ou requisito | `RN-xx`, `CA-xx`, `ENU-Lxxx`, regra de ouro n, CWE-xxx |
| Local | `caminho/arquivo.cs:42` |
| Status | Aberto / Resolvido / Parcial / Não resolvido / Aceito (HL-xx) |

**Evidência.** O comando, a consulta ou o trecho, e a saída resumida.

**Cenário.** O que acontece, como reproduzir e qual é o impacto. Para segurança, inclua o cenário de exploração concreto.

**Correção esperada.** O critério objetivo para o achado ser considerado resolvido, incluindo o teste que impede a regressão.

**Histórico de re-revisão.**

| Rodada | Resposta do dono | Conferência (relatório + código) | Status |
|---|---|---|---|
| 1 | — | — | Aberto |

## Verificado sem achado

| Item do checklist | Como foi verificado | Evidência |
|---|---|---|
| ... | ... | ... |

## Riscos aceitos

| Risco | Motivo | Recomendação para produção | Decisão (HL-xx) |
|---|---|---|---|
| Ausência de autenticação | Fora do escopo do enunciado (spec §2.2) | Autenticação JWT/OIDC e autorização por perfil | Declarado no README |

## Precisa verificação

Itens de confiança média ou baixa que não viraram achado, cada um com o motivo da dúvida.
