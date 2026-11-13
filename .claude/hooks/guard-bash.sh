#!/usr/bin/env bash
# Hook PreToolUse (Bash) declarado no frontmatter dos agentes da esteira.
#
# Subagentes não fazem commit, não mexem no histórico e não leem segredos: o
# commit é responsabilidade do orquestrador (sessão principal), com autorização
# do desenvolvedor. As regras de deny do settings.json continuam valendo; este
# hook acrescenta o que é específico dos agentes.
#
# Códigos de saída: 0 permitido · 2 bloqueado (motivo volta ao agente)

set -u

payload="$(cat)"
if command -v node >/dev/null 2>&1; then
  cmd="$(printf '%s' "$payload" | node -e '
    let raw = "";
    process.stdin.on("data", (c) => (raw += c));
    process.stdin.on("end", () => {
      try { process.stdout.write(String(JSON.parse(raw).tool_input?.command ?? "")); } catch {}
    });')"
else
  cmd="$payload"
fi

block() {
  echo "[guard-bash] BLOQUEADO para agentes da esteira: $1" >&2
  echo "Registre a necessidade no seu relatório; o orquestrador decide com o desenvolvedor." >&2
  exit 2
}

if printf '%s' "$cmd" | grep -Eq '(^|[;&|[:space:]])git[[:space:]]+(commit|push|rebase|reset|checkout|switch|merge|cherry-pick|revert|tag|stash|clean|restore|branch[[:space:]]+-[dDmM])'; then
  block "comandos git que alteram histórico, branch ou working tree"
fi

if printf '%s' "$cmd" | grep -Eq '(^|[^[:alnum:]_.-])\.env($|[^.[:alnum:]]|\.local)'; then
  block "acesso a arquivos .env"
fi

if printf '%s' "$cmd" | grep -Eq '(curl|wget|iwr|Invoke-WebRequest)[^|]*\|[[:space:]]*(ba|z)?sh'; then
  block "download seguido de execução (pipe para shell)"
fi

if printf '%s' "$cmd" | grep -Eq 'npm[[:space:]]+(i|install)[[:space:]]+(-g|--global)|dotnet[[:space:]]+tool[[:space:]]+install[[:space:]]+(-g|--global)'; then
  block "instalação global de ferramentas (use dependência versionada do projeto)"
fi

exit 0
