#!/usr/bin/env bash
# Testa o validar_rastreabilidade.py contra repositórios de exemplo criados num diretório temporário.
set -euo pipefail

SCRIPT="$(cd "$(dirname "$0")" && pwd)/validar_rastreabilidade.py"
TMP="$(mktemp -d)"
trap 'rm -rf "$TMP"' EXIT
falhas=0

montar() { # $1 = diretório; cria enunciado (3 linhas, uma vazia), backlog e um teste com commit
  mkdir -p "$1/docs/requisitos" "$1/docs/esteira" "$1/backend/tests"
  printf '```text\n001  Titulo\n002  \n003  Crie a API\n```\n' > "$1/docs/requisitos/enunciado.md"
  printf '## US-01\n- T-01.1 cria a API\n' > "$1/docs/esteira/backlog.md"
  printf 'public void CriarAsync_Valido_RetornaCriado() {}\n' > "$1/backend/tests/T.cs"
  local g=(git -C "$1" -c core.autocrlf=false -c user.name=t -c user.email=t@t)
  git -C "$1" init -q && "${g[@]}" add -A && "${g[@]}" commit -q -m t
}

matriz() { # $1 = diretório, demais = linhas da tabela
  local d="$1"; shift
  { echo '| ENU | Classe | Requisito | US | Tarefas | Testes | Commits |'
    echo '|---|---|---|---|---|---|---|'
    printf '%s\n' "$@"; } > "$d/docs/esteira/rastreabilidade.md"
}

caso() { # $1 = esperado (0|1), $2 = descrição, $3 = diretório
  local obtido=0
  python3 "$SCRIPT" --raiz "$3" > "$3.out" 2>&1 || obtido=$?
  if [[ "$obtido" == "$1" ]]; then echo "ok   $2"; else echo "FALHA $2 (saída $obtido)"; cat "$3.out"; falhas=$((falhas+1)); fi
}

n=0
novo() { n=$((n+1)); D="$TMP/r$n"; montar "$D"; HASH="$(git -C "$D" rev-parse --short HEAD)"; }

novo; caso 0 "matriz sem linhas só avisa" "$D"
novo; matriz "$D" "| ENU-L001 | Contexto | — | — | — | — | — |" \
  "| ENU-L003 | Obrigatório | RF-01 | US-01 | T-01.1 | \`CriarAsync_Valido_RetornaCriado\` | $HASH |"
caso 0 "matriz completa, teste e commit existentes" "$D"
novo; matriz "$D" "| ENU-L001 | Contexto | — | — | — | — | — |"
caso 1 "linha obrigatória do enunciado sem cobertura" "$D"
novo; matriz "$D" "| ENU-L001 | Contexto | — | — | — | — | — |" "| ENU-L003 | Obrigatório | RF-01 | US-01 | T-01.1 | — | — |"
caso 1 "requisito obrigatório sem teste" "$D"
novo; matriz "$D" "| ENU-L001 a L003 | Contexto | — | — | — | — | — |" "| ENU-L003 | Obrigatório | RF-01 | US-09 | T-09.1 | Manual: ver | — |"
caso 1 "história e tarefa fora do backlog" "$D"
novo; matriz "$D" "| ENU-L001 | Contexto | — | — | — | — | — |" \
  "| ENU-L003 | Obrigatório | RF-01 | US-01 | T-01.1 | \`TesteQueNaoExiste\` | $HASH |"
caso 1 "teste citado não existe no código" "$D"
novo; matriz "$D" "| ENU-L001 | Contexto | — | — | — | — | — |" \
  "| ENU-L003 | Obrigatório | RF-01 | US-01 | T-01.1 | \`CriarAsync_Valido_RetornaCriado\` | deadbeef |"
caso 1 "commit citado não existe" "$D"
novo; matriz "$D" "| ENU-L001 | Contexto | — | — | — | — | — |" "| ENU-L099 | Talvez | — | — | — | — | — |" \
  "| ENU-L003 | Diferencial | RF-01 | US-01 | T-01.1 | Manual: ok | — |"
caso 1 "ENU inexistente e classe inválida" "$D"
novo; matriz "$D" "| ENU-L001–L003 | Obrigatório | ENT-01 | US-01 | T-01.1 | Manual: conferir README | — |"
caso 0 "intervalo de linhas e verificação manual" "$D"

if [[ $falhas -gt 0 ]]; then echo "$falhas caso(s) falharam"; exit 1; fi
echo "Todos os casos de rastreabilidade passaram."
