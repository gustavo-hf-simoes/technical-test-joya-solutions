#!/usr/bin/env bash
# Hook PreToolUse (Edit | MultiEdit | Write | NotebookEdit) declarado no frontmatter
# de cada agente da esteira (.claude/agents/*.md).
#
# Garante, pelo harness e não só pelo prompt, que um agente escreve apenas nas
# pastas que são dele. Ex.: o agente `backend` não altera `frontend/`; os revisores
# (`ciberseguranca`, `qa`) só escrevem o próprio relatório.
#
# Uso:   guard-paths.sh <prefixo-permitido> [<prefixo-permitido> ...]
#        prefixos relativos à raiz do repositório, ex.: backend/ database/ docs/esteira/
#
# Códigos de saída:
#   0  caminho permitido (ou payload sem caminho)
#   2  caminho fora do escopo: a escrita é bloqueada e o motivo volta ao agente

set -u

ROOT="${CLAUDE_PROJECT_DIR:-$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)}"
payload="$(cat)"

if command -v node >/dev/null 2>&1; then
  file="$(printf '%s' "$payload" | node -e '
    let raw = "";
    process.stdin.on("data", (c) => (raw += c));
    process.stdin.on("end", () => {
      try {
        const i = JSON.parse(raw).tool_input ?? {};
        process.stdout.write(String(i.file_path ?? i.notebook_path ?? i.path ?? ""));
      } catch { /* sem caminho */ }
    });')"
else
  file="$(printf '%s' "$payload" | sed -n 's/.*"file_path"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' | head -n 1)"
  file="${file//\\\\//}"
fi

[ -z "$file" ] && exit 0

norm() { local p="${1//\\//}"; p="${p%/}"; printf '%s' "$p" | tr '[:upper:]' '[:lower:]'; }

f="$(norm "$file")"
root="$(norm "$ROOT")"
# Git Bash expõe D:\x como /d/x; o payload do Claude Code usa d:/x. Compara as duas formas.
root_alt="$root"
case "$root" in
  /[a-z]/*) root_alt="${root:1:1}:${root:2}" ;;
  [a-z]:/*) root_alt="/${root:0:1}${root:2}" ;;
esac

rel=""
for r in "$root" "$root_alt"; do
  case "$f" in "$r"/*) rel="${f#"$r"/}" ;; esac
done
# Caminho relativo já veio no payload.
[ -z "$rel" ] && case "$f" in /*|[a-z]:/*) ;; *) rel="$f" ;; esac

if [ -z "$rel" ]; then
  echo "[guard-paths] BLOQUEADO: $file está fora do repositório." >&2
  exit 2
fi

case "$rel" in
  *../*|../*) echo "[guard-paths] BLOQUEADO: caminho com '..' ($file)." >&2; exit 2 ;;
esac

for allowed in "$@"; do
  a="$(norm "$allowed")"
  case "$rel" in "$a"|"$a"/*) exit 0 ;; esac
done

{
  echo "[guard-paths] BLOQUEADO: este agente não pode escrever em '$rel'."
  echo "Escopo permitido: $*"
  echo "Se a mudança é necessária, registre-a como achado no seu relatório com o agente dono e devolva ao orquestrador."
} >&2
exit 2
