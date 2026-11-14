# ADR-0002 — Limites contra abuso: tamanho do corpo e taxa no `POST /pedidos`

- **Status:** Proposto (etapa 2, arquitetura). A decisão de **ter** os limites é humana (`HL-12`). Este ADR fixa os **valores** e o **mecanismo**.
- **Data:** 2026-11-13
- **Referências:** spec RNF-06 e §5.1 (catálogo `413` e `429`); [plan.md](../plan.md) `HL-03`, `HL-12`; [02-arquitetura.md](../esteira/02-arquitetura.md) §2 e §4

## Contexto

A API não tem autenticação (`HL-03`) e fica pública no deploy (ENU-L087). Os limites de 50 itens e 10 000 unidades (RN-01, RN-02) só valem depois que o corpo foi lido e desserializado. Eles não impedem um corpo de vários megabytes nem uma rajada de `POST`s que trava linhas de `Produtos`. A `HL-12` exige `413` para corpo grande e `429 LIMITE_DE_TAXA` por IP no `POST /pedidos`, com o *rate limiter* nativo e sem dependência nova. Os valores ficaram para a arquitetura.

**Tamanho do maior pedido válido.** Um item com os maiores valores possíveis, `{"produtoId":2147483647,"quantidade":10000}`, tem 44 bytes. Com 50 itens, o corpo compacto fica em cerca de 2,3 KB. Indentado e com campos extras, que são ignorados (RN-04), fica abaixo de 8 KB.

## Decisão

### 1. Tamanho máximo do corpo: **32 KiB** (32 768 bytes)

- **Valor.** Cerca de 4× o maior pedido válido indentado e cerca de 1/900 do padrão do Kestrel (30 MB). Fica numa constante única, `LimitesHttp.CorpoMaximoBytes`, na `Api`.
- **Mecanismo.** `KestrelServerOptions.Limits.MaxRequestBodySize = LimitesHttp.CorpoMaximoBytes`, global. Só o `POST /pedidos` recebe corpo. O limite global também cobre corpos *chunked*, sem `Content-Length`.
- **Resposta.** O Kestrel lança `Microsoft.AspNetCore.Http.BadHttpRequestException` com `StatusCode = 413` ao ler o corpo. O `IExceptionHandler` trata essa exceção **antes** do caso genérico: responde com o `StatusCode` da exceção (`413`, ou `400` para corpo malformado no nível HTTP) em Problem Details com `traceId`, e não com `500 ERRO_INTERNO`.
- **Teste.** `PostPedidos_CorpoAcimaDoLimite_Retorna413`. **Risco conhecido:** o `TestServer` do `WebApplicationFactory` (.NET 8) não passa pelo Kestrel e pode ignorar `MaxRequestBodySize`. O `backend` escreve esse teste **primeiro** (vermelho) e segue a primeira opção que o deixar verde:
  1. `[RequestSizeLimit(LimitesHttp.CorpoMaximoBytes)]` na action, que usa `IHttpMaxRequestBodySizeFeature`, junto com o limite do Kestrel;
  2. se o `TestServer` também não expuser essa *feature*, um filtro de recurso de ~10 linhas no `POST` que rejeita `Content-Length > CorpoMaximoBytes` com `413` antes da leitura. O limite do Kestrel continua como a proteção real contra *chunked*.

  O caminho escolhido fica registrado no relatório da etapa 3.
- **Nginx.** `client_max_body_size 64k` no `frontend`. É uma barreira externa mais larga que a da API, para que quem decida o `413`, e o devolva em Problem Details, seja a API. O padrão do Nginx (1 MB) também funcionaria, mas o valor explícito documenta a intenção.

### 2. Limite de taxa: janela fixa por IP, **10 requisições a cada 10 s**

| Parâmetro | Valor | Configuração |
|---|---|---|
| Algoritmo | `FixedWindowRateLimiter` (`AddRateLimiter` + `RateLimitPartition.GetFixedWindowLimiter`) | — |
| Política | `"criar-pedido"`, aplicada só no `POST /pedidos` com `[EnableRateLimiting("criar-pedido")]` | — |
| Partição | `HttpContext.Connection.RemoteIpAddress` depois de `UseForwardedHeaders`. Sem IP (caso do `TestServer`): partição `"desconhecido"` | — |
| Permissões por janela | 10 | `LimiteDeTaxa:CriarPedido:Permissoes` |
| Janela | 10 s | `LimiteDeTaxa:CriarPedido:JanelaSegundos` |
| Fila | 0: o excedente é rejeitado na hora, sem segurar conexão | fixo |
| Rejeição | `RejectionStatusCode = 429`. O `OnRejected` escreve Problem Details com `code = "LIMITE_DE_TAXA"` e `traceId`, e define `Retry-After` a partir de `MetadataName.RetryAfter` do *lease* | — |

**Por que esses números.** Um operador humano cria poucos pedidos por minuto. Dez em 10 s cobrem duplo clique, *retry* com a mesma `Idempotency-Key` e uso normal com folga. Um script, por outro lado, fica limitado a 1 pedido/s por IP. A janela fixa é a mais simples que devolve `Retry-After`. O pico na fronteira entre janelas (até 20 em 10 s) é aceitável para este escopo.

**Testes.** O fixture de integração sobe `LimiteDeTaxa:CriarPedido:Permissoes` para um valor alto (por exemplo, `100000`). Sem isso, o CA-10 (20 `POST`s simultâneos) e o CA-37 receberiam `429`. O teste `PostPedidos_RajadaAcimaDoLimite_Retorna429` usa uma `WebApplicationFactory` própria (`WithWebHostBuilder`) com `Permissoes = 2`. Ele confere o `429`, o `code` e a presença de `Retry-After`. A mudança na spec que deixa isso explícito está na `PROP-10` do relatório da etapa 2. A numeração continua a da etapa 1, que usou `PROP-01` a `PROP-07`.

### 3. IP do cliente atrás de proxy

- `app.UseForwardedHeaders()` **antes** do *rate limiter*, com `ForwardedHeaders = XForwardedFor`, `ForwardedLimit` vindo de `Proxy:ForwardedLimit` (padrão `1`) e `KnownNetworks`/`KnownProxies` vazios, ou seja, qualquer proxy é aceito.
- **Local:** navegador → Nginx → API. O Nginx usa `proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for`. Com `ForwardedLimit = 1`, a API pega o último IP, que é o que o Nginx viu.
- **Deploy:** navegador → Caddy → Nginx → API. Por padrão, o Caddy descarta o `X-Forwarded-For` vindo de cliente não confiável e grava o IP real. O Nginx acrescenta o IP do Caddy. Com `ForwardedLimit = 2` (no override de deploy), a API pega o IP real.
- **Por que aceitar qualquer proxy é seguro aqui:** no deploy, a porta da API não é publicada. Só o Nginx, dentro da rede interna, alcança a API. Localmente, a `8080` só escuta em `127.0.0.1`. Se a porta da API for publicada para fora, essa premissa cai, e o revisor `ciberseguranca` deve confirmá-la na etapa dele.

## Alternativas consideradas

| Alternativa | Por que não |
|---|---|
| Sem limite de taxa, declarado como risco aceito | Rejeitada pela `HL-12` |
| `TokenBucket` ou `SlidingWindow` | Suavizam o pico de fronteira, mas têm mais parâmetros e não mudam o resultado para este escopo |
| Limite global, sem partição por IP | Um único abusador bloqueia todos os usuários. A spec pede "por IP" |
| Limite de taxa só no Nginx (`limit_req`) | Devolve HTML, e não Problem Details com `LIMITE_DE_TAXA`. Também não é testável pelo `WebApplicationFactory` |
| Limite de corpo de 1 MB, igual ao padrão do Nginx | 400× o maior pedido válido, sem motivo |
| Aceitar `X-Forwarded-For` sem limite de saltos | Qualquer cliente forjaria o IP e escaparia do limite |

## Consequências

- **Positivas:** os dois limites usam só o framework (`Microsoft.AspNetCore.RateLimiting` já faz parte do ASP.NET Core 8). Os valores são configuráveis e testáveis, e as respostas seguem o mesmo formato de erro das demais.
- **Negativas e riscos:**
  - O teste de `413` pode exigir o caminho alternativo do item 1, e isso só se descobre no primeiro teste.
  - Usuários atrás do mesmo NAT dividem a mesma cota. Com 10 a cada 10 s, isso só pesa em uso intenso.
  - O estado do limitador fica em memória e por instância. Com mais de uma réplica, cada uma teria a própria cota. Hoje é uma réplica só.
  - Se a topologia de proxies mudar, o `ForwardedLimit` errado faz o limite valer para o IP do proxy, e todos dividem uma cota. Por isso a configuração fica explícita no override de deploy.
