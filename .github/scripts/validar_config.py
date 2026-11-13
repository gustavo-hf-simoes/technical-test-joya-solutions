"""Valida a configuração AI-first versionada no repositório.

Roda no CI (job `config`) e localmente:  python .github/scripts/validar_config.py

Confere:
  1. Frontmatter YAML de cada agente (.claude/agents/*.md) e skill (.claude/skills/*/SKILL.md),
     com os campos obrigatórios; nos agentes, `model` e `effort` válidos (ADR-0001).
  2. Arquivos JSON de configuração (.mcp.json, .claude/settings.json).
  3. Links relativos em Markdown fora das skills vendorizadas: o alvo precisa existir.
Sai com código 1 se encontrar qualquer problema.
"""

from __future__ import annotations

import json
import re
import sys
from pathlib import Path

import yaml

ROOT = Path(__file__).resolve().parents[2]
MODELOS = {"haiku", "sonnet", "opus", "fable", "inherit"}
ESFORCOS = {"low", "medium", "high", "xhigh", "max"}
FRONTMATTER = re.compile(r"\A---\r?\n(.*?)\r?\n---", re.S)
LINK = re.compile(r"\]\(([^)\s#]+)(?:#[^)]*)?\)")

erros: list[str] = []


def frontmatter(caminho: Path) -> dict:
    m = FRONTMATTER.match(caminho.read_text(encoding="utf-8"))
    if not m:
        erros.append(f"{caminho.relative_to(ROOT)}: sem frontmatter YAML")
        return {}
    try:
        dados = yaml.safe_load(m.group(1)) or {}
    except yaml.YAMLError as exc:
        erros.append(f"{caminho.relative_to(ROOT)}: YAML inválido: {exc}")
        return {}
    return dados


for agente in sorted((ROOT / ".claude/agents").glob("*.md")):
    d = frontmatter(agente)
    rel = agente.relative_to(ROOT)
    for campo in ("name", "description", "model", "effort"):
        if campo not in d:
            erros.append(f"{rel}: campo obrigatório ausente: {campo}")
    if d.get("model") not in MODELOS:
        erros.append(f"{rel}: model inválido: {d.get('model')!r}")
    if d.get("effort") not in ESFORCOS:
        erros.append(f"{rel}: effort inválido: {d.get('effort')!r}")
    if d.get("name") and d["name"] != agente.stem:
        erros.append(f"{rel}: name '{d['name']}' difere do nome do arquivo")

for skill in sorted((ROOT / ".claude/skills").glob("*/SKILL.md")):
    d = frontmatter(skill)
    for campo in ("name", "description"):
        if campo not in d:
            erros.append(f"{skill.relative_to(ROOT)}: campo obrigatório ausente: {campo}")

for arquivo in (".mcp.json", ".claude/settings.json"):
    try:
        json.loads((ROOT / arquivo).read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as exc:
        erros.append(f"{arquivo}: JSON inválido: {exc}")

# Skills vendorizadas são cópias de terceiros: os links delas apontam para os repositórios de origem.
vendorizadas = {
    p.parent for p in (ROOT / ".claude/skills").glob("*/LICENSE")
}
for md in sorted(ROOT.rglob("*.md")):
    if ".git" in md.parts or "node_modules" in md.parts:
        continue
    if any(v in md.parents for v in vendorizadas):
        continue
    for alvo in LINK.findall(md.read_text(encoding="utf-8")):
        if re.match(r"^[a-z]+:", alvo):
            continue
        if not (md.parent / alvo).resolve().exists():
            erros.append(f"{md.relative_to(ROOT)}: link quebrado: {alvo}")

if erros:
    print("Configuração inválida:")
    for e in erros:
        print(f"  - {e}")
    sys.exit(1)

print("Configuração AI-first válida: agentes, skills, JSON e links.")
