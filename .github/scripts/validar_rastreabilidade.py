#!/usr/bin/env python3
"""Valida a matriz de rastreabilidade ENU -> requisito -> US -> tarefa -> teste -> commit (HL-08).

As regras estão descritas em docs/esteira/rastreabilidade.md. Uso:
    python3 .github/scripts/validar_rastreabilidade.py [--raiz CAMINHO]
Sai com 1 se houver erro. Enquanto a matriz não tem linhas (antes da etapa 1), só avisa.
"""
from __future__ import annotations

import argparse
import re
import subprocess
import sys
from pathlib import Path

CLASSES = {"Obrigatório", "Diferencial", "Contexto"}
VAZIO = {"", "—", "-"}
CABECALHO = ["ENU", "Classe", "Requisito", "US", "Tarefas", "Testes", "Commits"]
PASTAS_DE_TESTE = [("backend/tests", "*.cs"), ("frontend/src", "*.spec.ts"), ("e2e", "*.ts")]

RE_LINHA_ENU = re.compile(r"^(\d{3})  ?(.*)$")
RE_ENU = re.compile(r"ENU-L(\d{3})(?:\s*(?:–|a|\.\.)\s*(?:ENU-)?L(\d{3}))?")
RE_REQ = re.compile(r"\b(?:RF|RNF|ENT)-\d+\b")
RE_US = re.compile(r"\bUS-\d+\b")
RE_TAREFA = re.compile(r"\bT-\d+\.\d+\b")
RE_TESTE = re.compile(r"`([^`]+)`")
RE_HASH = re.compile(r"\b[0-9a-f]{7,40}\b")


def linhas_do_enunciado(raiz: Path) -> dict[int, str]:
    texto = (raiz / "docs/requisitos/enunciado.md").read_text(encoding="utf-8")
    linhas: dict[int, str] = {}
    for bruta in texto.splitlines():
        m = RE_LINHA_ENU.match(bruta)
        if m:
            linhas[int(m.group(1))] = m.group(2).strip()
    return linhas


def ler_matriz(raiz: Path) -> list[list[str]] | None:
    caminho = raiz / "docs/esteira/rastreabilidade.md"
    if not caminho.exists():
        return None
    linhas: list[list[str]] = []
    dentro = False
    for bruta in caminho.read_text(encoding="utf-8").splitlines():
        if not bruta.startswith("|"):
            dentro = False
            continue
        celulas = [c.strip() for c in bruta.strip().strip("|").split("|")]
        if celulas == CABECALHO:
            dentro = True
            continue
        if dentro and not all(set(c) <= set("-: ") for c in celulas):
            linhas.append(celulas)
    return linhas


def ids_enu(celula: str) -> set[int]:
    ids: set[int] = set()
    for m in RE_ENU.finditer(celula):
        inicio = int(m.group(1))
        fim = int(m.group(2)) if m.group(2) else inicio
        ids.update(range(inicio, fim + 1))
    return ids


def codigo_de_teste(raiz: Path) -> str:
    partes = []
    for pasta, padrao in PASTAS_DE_TESTE:
        base = raiz / pasta
        if base.is_dir():
            partes += [p.read_text(encoding="utf-8", errors="ignore") for p in base.rglob(padrao)
                       if "node_modules" not in p.parts]
    return "\n".join(partes)


def commit_existe(raiz: Path, hash_: str) -> bool:
    r = subprocess.run(["git", "-C", str(raiz), "cat-file", "-e", f"{hash_}^{{commit}}"],
                       capture_output=True, check=False)
    return r.returncode == 0


def validar(raiz: Path) -> list[str]:
    matriz = ler_matriz(raiz)
    if not matriz:
        print("Aviso: a matriz de rastreabilidade ainda não tem linhas (etapa 1 pendente).")
        return []

    erros: list[str] = []
    enunciado = linhas_do_enunciado(raiz)
    backlog_path = raiz / "docs/esteira/backlog.md"
    backlog = backlog_path.read_text(encoding="utf-8") if backlog_path.exists() else ""
    us_backlog, tarefas_backlog = set(RE_US.findall(backlog)), set(RE_TAREFA.findall(backlog))
    testes_codigo: str | None = None
    cobertas: set[int] = set()

    for n, celulas in enumerate(matriz, start=1):
        if len(celulas) != len(CABECALHO):
            erros.append(f"linha {n} da matriz: {len(celulas)} colunas, esperado {len(CABECALHO)}")
            continue
        enu, classe, req, us, tarefas, testes, commits = celulas
        rotulo = f"linha {n} ({enu or 'sem ENU'})"

        ids = ids_enu(enu)
        if not ids:
            erros.append(f"{rotulo}: coluna ENU sem ID no formato ENU-Lxxx")
        for i in sorted(ids):
            if i not in enunciado:
                erros.append(f"{rotulo}: ENU-L{i:03d} não existe no enunciado")
        cobertas |= ids

        if classe not in CLASSES:
            erros.append(f"{rotulo}: classe '{classe}' inválida (use {', '.join(sorted(CLASSES))})")
            continue
        if classe == "Contexto":
            continue

        if not RE_REQ.search(req):
            erros.append(f"{rotulo}: requisito {classe.lower()} sem RF/RNF/ENT")
        if not RE_US.search(us):
            erros.append(f"{rotulo}: sem história (US-xx)")
        if not RE_TAREFA.search(tarefas):
            erros.append(f"{rotulo}: sem tarefa (T-xx.n)")
        for item in sorted(set(RE_US.findall(us)) - us_backlog):
            erros.append(f"{rotulo}: {item} não existe no backlog")
        for item in sorted(set(RE_TAREFA.findall(tarefas)) - tarefas_backlog):
            erros.append(f"{rotulo}: {item} não existe no backlog")

        nomes = RE_TESTE.findall(testes)
        if not nomes and not testes.startswith("Manual:"):
            erros.append(f"{rotulo}: sem teste (nome entre crases ou 'Manual: ...')")

        if commits not in VAZIO:
            for hash_ in RE_HASH.findall(commits):
                if not commit_existe(raiz, hash_):
                    erros.append(f"{rotulo}: commit {hash_} não existe no histórico")
            if testes_codigo is None:
                testes_codigo = codigo_de_teste(raiz)
            for nome in nomes:
                if nome not in testes_codigo:
                    erros.append(f"{rotulo}: teste '{nome}' não encontrado no código de teste")

    for i, texto in sorted(enunciado.items()):
        if texto and i not in cobertas:
            erros.append(f"ENU-L{i:03d} sem linha na matriz: \"{texto[:60]}\"")
    return erros


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--raiz", type=Path, default=Path(__file__).resolve().parents[2])
    erros = validar(parser.parse_args().raiz)
    for e in erros:
        print(f"ERRO: {e}")
    print(f"Rastreabilidade: {len(erros)} erro(s).")
    return 1 if erros else 0


if __name__ == "__main__":
    sys.exit(main())
