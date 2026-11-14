#!/usr/bin/env bash
# =============================================================================
# init.sh — aplica os scripts de database/ em ordem, com o sqlcmd e o login sa.
#
# Executor ÚNICO do schema (ADR-0003): o contêiner db-init do compose e o fixture
# dos testes de integração (Testcontainers) rodam este mesmo arquivo, dentro da
# imagem mcr.microsoft.com/mssql/server:2022-latest, que já traz o sqlcmd.
#
# Uso:
#   init.sh                          aplica todos os database/NN-*.sql, em ordem
#   init.sh 01-schema 02-indices     aplica só as etapas informadas (sempre em ordem)
#
# Ambiente:
#   MSSQL_SA_PASSWORD   obrigatória. Senha do sa (vai ao sqlcmd por SQLCMDPASSWORD,
#                       nunca pela linha de comando).
#   MSSQL_HOST          servidor (padrão: localhost).
#   APP_SQL_PASSWORD    senha do login joya_app  } obrigatórias só para os scripts
#   MCP_SQL_PASSWORD    senha do login claude_mcp } que as usam (03-logins.sql)
#
# Idempotente: cada script só cria o que falta, então rodar duas vezes não falha.
# Sai com código diferente de zero no primeiro erro (set -e e sqlcmd -b).
# Precisa de final de linha LF (.gitattributes força eol=lf em *.sh).
# =============================================================================
set -euo pipefail
shopt -s nullglob
export LC_ALL=C

DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
HOST="${MSSQL_HOST:-localhost}"
VARIAVEIS_DE_SENHA=(APP_SQL_PASSWORD MCP_SQL_PASSWORD)

falhar() {
  echo "init.sh: $*" >&2
  exit 2
}

[ -n "${MSSQL_SA_PASSWORD:-}" ] || falhar "defina MSSQL_SA_PASSWORD (senha do sa)."
export SQLCMDPASSWORD="$MSSQL_SA_PASSWORD"

# --- 1. Quais scripts aplicar, sempre em ordem numérica ----------------------
scripts=()
if [ "$#" -eq 0 ]; then
  for caminho in "$DIR"/[0-9][0-9]-*.sql; do
    scripts+=("$(basename "$caminho")")
  done
else
  for etapa in "$@"; do
    case "$etapa" in
      */* | .*) falhar "etapa inválida: '$etapa' (use o nome do arquivo, ex.: 01-schema)." ;;
    esac
    nome="${etapa%.sql}.sql"
    [ -f "$DIR/$nome" ] || falhar "etapa desconhecida: '$etapa' (não existe $DIR/$nome)."
    scripts+=("$nome")
  done
  mapfile -t scripts < <(printf '%s\n' "${scripts[@]}" | sort -u)
fi

[ "${#scripts[@]}" -gt 0 ] || falhar "nenhum script NN-*.sql encontrado em $DIR."

# --- 2. Aplica cada script ----------------------------------------------------
for nome in "${scripts[@]}"; do
  arquivo="$DIR/$nome"

  # Senhas só vão ao sqlcmd (por -v) quando o script as usa. Uma aspa simples
  # quebraria o literal N'...' do script; por isso é recusada aqui.
  variaveis=()
  for var in "${VARIAVEIS_DE_SENHA[@]}"; do
    if grep -qF "\$($var)" "$arquivo"; then
      valor="${!var:-}"
      [ -n "$valor" ] || falhar "$nome usa \$($var); defina a variável de ambiente $var."
      case "$valor" in
        *\'*) falhar "$var não pode conter aspa simples." ;;
      esac
      variaveis+=(-v "$var=$valor")
    fi
  done

  echo "init.sh: aplicando $nome em $HOST"
  /opt/mssql-tools18/bin/sqlcmd -S "$HOST" -U sa -C -I -b -l 30 -i "$arquivo" "${variaveis[@]}"
done

echo "init.sh: ${#scripts[@]} script(s) aplicado(s) com sucesso."
