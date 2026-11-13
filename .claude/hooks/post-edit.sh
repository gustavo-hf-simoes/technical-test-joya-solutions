#!/usr/bin/env bash
# Hook PostToolUse (Edit | MultiEdit | Write)
#
# Sempre que o Claude altera um arquivo do backend, compila a solução (warnings
# são erros) e roda os testes unitários. Se algo quebrar, sai com código 2 e
# escreve o diagnóstico no stderr — o Claude Code devolve esse texto ao modelo,
# que corrige antes de continuar.
#
# Não roda os testes de integração: dependem de Docker e levam dezenas de
# segundos; ficam para `dotnet test` explícito e para o CI.
#
# Códigos de saída:
#   0  nada a verificar, ou build e testes passaram
#   2  build ou teste falhou (stderr é entregue ao Claude)
#
# Desligar temporariamente:  SKIP_POST_EDIT_HOOK=1

set -u

[ "${SKIP_POST_EDIT_HOOK:-0}" = "1" ] && exit 0

ROOT="${CLAUDE_PROJECT_DIR:-$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)}"
SOLUTION="$ROOT/backend/Joya.Pedidos.sln"
UNIT_TESTS="$ROOT/backend/tests/Joya.Pedidos.UnitTests"
MAX_LINES=60

# --- 1. Qual arquivo foi editado? (o payload do hook chega como JSON no stdin) ---
payload="$(cat)"

extract_file_path() {
  if command -v node >/dev/null 2>&1; then
    printf '%s' "$payload" | node -e '
      let raw = "";
      process.stdin.on("data", (chunk) => (raw += chunk));
      process.stdin.on("end", () => {
        try {
          const input = JSON.parse(raw).tool_input ?? {};
          process.stdout.write(String(input.file_path ?? input.path ?? ""));
        } catch {
          /* payload inesperado: sem caminho, o hook não faz nada */
        }
      });'
  elif command -v jq >/dev/null 2>&1; then
    printf '%s' "$payload" | jq -r '.tool_input.file_path // .tool_input.path // empty'
  else
    printf '%s' "$payload" | sed -n 's/.*"file_path"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' | head -n 1
  fi
}

file="$(extract_file_path)"
file="${file//\\\\//}"   # barras duplas escapadas (fallback do sed em caminhos Windows)
file="${file//\\//}"     # barras invertidas do Windows

[ -z "$file" ] && exit 0

# --- 2. É um arquivo que afeta o build do backend? ---
case "$file" in
  */backend/*|backend/*) ;;
  *) exit 0 ;;
esac

case "$file" in
  */bin/*|*/obj/*) exit 0 ;;
esac

case "$file" in
  *.cs|*.csproj|*.sln|*.props|*.targets|*.editorconfig|*/appsettings*.json|*/global.json) ;;
  *) exit 0 ;;
esac

# --- 3. Há o que compilar? (antes da Fase 2 do plano a solução ainda não existe) ---
[ -f "$SOLUTION" ] || exit 0

if ! command -v dotnet >/dev/null 2>&1 || [ -z "$(dotnet --list-sdks 2>/dev/null)" ]; then
  echo "[post-edit] .NET SDK não encontrado: build e testes NÃO foram verificados para $file." >&2
  exit 0
fi

# --- 4. Build ---
if ! build_output="$(dotnet build "$SOLUTION" --nologo --verbosity quiet -warnaserror -clp:NoSummary 2>&1)"; then
  {
    echo "[post-edit] BUILD FALHOU após editar $file"
    echo "Corrija os erros abaixo antes de continuar (warnings são tratados como erros):"
    echo
    diagnostics="$(printf '%s\n' "$build_output" | grep -E '(error|warning) [A-Z]+[0-9]+' | sort -u | head -n "$MAX_LINES")"
    # Falha sem linha de diagnóstico reconhecível (ex.: SDK ou restore): mostra o fim da saída.
    printf '%s\n' "${diagnostics:-$(printf '%s\n' "$build_output" | tail -n "$MAX_LINES")}"
  } >&2
  exit 2
fi

# --- 5. Testes unitários (regressão rápida) ---
if [ -d "$UNIT_TESTS" ]; then
  if ! test_output="$(dotnet test "$UNIT_TESTS" --no-build --nologo --verbosity quiet 2>&1)"; then
    {
      echo "[post-edit] TESTES UNITÁRIOS FALHARAM após editar $file"
      echo "Se a regra mudou de propósito, atualize o teste; caso contrário, corrija a regressão:"
      echo
      printf '%s\n' "$test_output" | tail -n "$MAX_LINES"
    } >&2
    exit 2
  fi
fi

echo "[post-edit] build e testes unitários OK ($file)"
exit 0
