#!/usr/bin/env bash
# Roteiro exploratorio do QA (etapa 6) contra a API em http://localhost:8080, com o compose no ar.
# Uso: bash docs/esteira/evidencias/qa-exploratorio-api.sh | tee docs/esteira/evidencias/qa-exploratorio-api.log
# O POST /pedidos tem limite de 10 requisicoes por 10 s (429): o roteiro faz uma pausa entre os blocos.
A=http://localhost:8080
post() { # $1 = rotulo, $2 = corpo, $3.. = cabecalhos extras
  local r="$1" b="$2"; shift 2
  local args=(-s -o /tmp/qa-body.txt -w '%{http_code}' -X POST "$A/pedidos") ctype='Content-Type: application/json'
  for h in "$@"; do case "$h" in Content-Type:*) ctype="$h" ;; *) args+=(-H "$h") ;; esac; done
  args+=(-H "$ctype")
  local code; code=$(curl "${args[@]}" -d "$b")
  printf '%-52s HTTP %s  %s\n' "$r" "$code" "$(cut -c1-230 /tmp/qa-body.txt | tr -d '\n')"
}
get() { # $1 = rotulo, $2 = caminho
  local code; code=$(curl -s -o /tmp/qa-body.txt -w '%{http_code}' "$A$2")
  printf '%-52s HTTP %s  %s\n' "$1" "$code" "$(cut -c1-260 /tmp/qa-body.txt | tr -d '\n')"
}
estoque() { curl -s "$A/produtos?ativo=true" | tr '{' '\n' | grep "\"id\":$1," | grep -o '"estoque":[0-9]*'; }

echo "== estoque do produto 5 antes: $(estoque 5)"
echo "== POST /pedidos: regras de entrada e de negocio (bloco 1)"
post "CA-05 cliente inexistente (9999)"                '{"clienteId":9999,"itens":[{"produtoId":5,"quantidade":1}]}'
post "CA-06 cliente inativo (11)"                      '{"clienteId":11,"itens":[{"produtoId":5,"quantidade":1}]}'
post "CA-07 produto inativo (13) e inexistente (9999)" '{"clienteId":1,"itens":[{"produtoId":13,"quantidade":1},{"produtoId":9999,"quantidade":1}]}'
post "CA-08 estoque insuficiente (qtd 100000)"         '{"clienteId":1,"itens":[{"produtoId":5,"quantidade":100000}]}'
post "CA-08 estoque insuficiente (produto 11, estoque 0)" '{"clienteId":1,"itens":[{"produtoId":11,"quantidade":1}]}'
post "CA-02 sem itens"                                 '{"clienteId":1,"itens":[]}'
post "CA-04 produto duplicado"                         '{"clienteId":1,"itens":[{"produtoId":5,"quantidade":1},{"produtoId":5,"quantidade":1}]}'
post "CA-03 quantidade 0"                              '{"clienteId":1,"itens":[{"produtoId":5,"quantidade":0}]}'
post "CA-03 quantidade negativa"                       '{"clienteId":1,"itens":[{"produtoId":5,"quantidade":-3}]}'
post "CA-14 quantidade fora do limite com cliente invalido" '{"clienteId":9999,"itens":[{"produtoId":8888,"quantidade":99999}]}'
echo "== estoque do produto 5 depois dos erros (deve ser igual ao de antes): $(estoque 5)"
sleep 11
echo "== POST /pedidos (bloco 2)"
post "CA-12 preco no corpo (0.01) e ignorado"          '{"clienteId":1,"itens":[{"produtoId":5,"precoUnitario":0.01,"quantidade":2}]}'
post "CA-38 Content-Type text/plain"                   '{"clienteId":1,"itens":[{"produtoId":5,"quantidade":1}]}' 'Content-Type: text/plain'
post "JSON malformado"                                 '{"clienteId":'
post "tipo invalido no corpo (clienteId texto)"        '{"clienteId":"abc","itens":[]}'
post "Idempotency-Key K (spec CA-37 pede 400)"         '{"clienteId":1,"itens":[{"produtoId":5,"quantidade":1}]}' 'Idempotency-Key: K'
post "Idempotency-Key valida, 1o envio (CA-13)"        '{"clienteId":1,"itens":[{"produtoId":5,"quantidade":1}]}' 'Idempotency-Key: 3f2b8c1e-7a4d-4e9f-9b21-5c6d8e0f1a2c'
post "Idempotency-Key valida, 2o envio (CA-13 pede 200)" '{"clienteId":1,"itens":[{"produtoId":5,"quantidade":1}]}' 'Idempotency-Key: 3f2b8c1e-7a4d-4e9f-9b21-5c6d8e0f1a2c'
echo "== estoque do produto 5 depois (2 + 1 + 1 + 1 = 5 unidades de 4 pedidos validos; o replay com a mesma chave debitou de novo): $(estoque 5)"
echo "== GET /pedidos: limites e fronteiras"
get "CA-15 pagina padrao"                              '/pedidos'
get "CA-19 pageSize 101"                               '/pedidos?pageSize=101'
get "CA-19 pageSize 0"                                 '/pedidos?pageSize=0'
get "CA-19 page 0"                                     '/pedidos?page=0'
get "CA-19 periodo invertido"                          '/pedidos?dataInicio=2026-12-31&dataFim=2026-01-01'
get "CA-19 data invalida"                              '/pedidos?dataInicio=31-12-2026'
get "CA-19 clienteId negativo"                         '/pedidos?clienteId=-1'
get "CA-20 cliente sem pedidos (9999)"                 '/pedidos?clienteId=9999'
get "fronteira: dia unico 2026-03-01 a 2026-03-01"     '/pedidos?dataInicio=2026-03-01&dataFim=2026-03-01&pageSize=100'
get "SEC-03 dataFim 9999-12-31"                        '/pedidos?dataFim=9999-12-31'
echo "== GET /relatorios/top-clientes"
get "CA-21 ranking de 2026 (5 linhas, decrescente)"    '/relatorios/top-clientes?dataInicio=2026-01-01&dataFim=2026-12-31'
get "CA-26 sem dataInicio"                             '/relatorios/top-clientes?dataFim=2026-12-31'
get "periodo invertido"                                '/relatorios/top-clientes?dataInicio=2026-12-31&dataFim=2026-01-01'
get "fronteira: 2026-12-31 a 2026-12-31"               '/relatorios/top-clientes?dataInicio=2026-12-31&dataFim=2026-12-31'
echo "== apoio"
get "CA-35 clientes ativos"                            '/clientes?ativo=true'
get "CA-36 health live"                                '/health/live'
get "CA-34 pedido por id inexistente"                  '/pedidos/99999999'
