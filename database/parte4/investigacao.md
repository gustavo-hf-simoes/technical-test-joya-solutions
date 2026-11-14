# Parte 4: investigação da query de ranking de 2026

O enunciado (ENU-L040 a L048) diz que a query "deveria retornar o ranking dos clientes que mais compraram em 2026, mas está lenta e retornando resultados incorretos". Este documento registra o que estava errado, como foi investigado e o que foi medido. Os números saem dos testes de `backend/tests/Joya.Pedidos.IntegrationTests/Parte4/`, rodados contra um SQL Server real. **Nenhum número aqui é estimado à mão.**

| Arquivo | Papel |
|---|---|
| `01-query-original.sql` | A query do enunciado, sem alteração. Reproduz os defeitos |
| `02-query-corrigida.sql` | A correção entregue (CA-33) |
| `03-variante-valor-total.sql` | Mesmo ranking sobre `Pedidos.ValorTotal`. Serve de comparação; é a forma da API |
| `00-carga-volume.sql` | Carga determinística de volume para medir planos (5 000 clientes, 200 000 pedidos, 600 000 itens) |

## 1. Resumo

| # | Defeito na original | Efeito | Correção em `02` | Prova |
|---|---|---|---|---|
| 1 | `GROUP BY c.Nome` | Homônimos viram uma linha só, com os totais somados. A saída nem traz o `Id` (RN-17) | `GROUP BY c.Id, c.Nome`, `ClienteId` na saída | `QueryOriginal_ClientesHomonimos_SomaIndevidamente`, `QueryCorrigida_ClientesHomonimos_UmaLinhaPorCliente` (CA-23) |
| 2 | `ORDER BY Total` | Ordem crescente: o "ranking" começa por quem **menos** comprou. Não há desempate | `ORDER BY Total DESC, c.Id` | `QueryOriginal_Ordenacao_RetornaMenorTotalPrimeiro`, `QueryCorrigida_Empate_OrdenaPorMenorClienteId` (CA-24) |
| 3 | `YEAR()` aplicada à coluna `DataPedido` | Não sargável: não há seek por data, todos os pedidos são lidos, e a estimativa de linhas vira chute (RN-15) | Intervalo semiaberto sobre a coluna: `>= '2026-01-01' AND < '2027-01-01'` | `QueryOriginal_Plano_LeTodosOsPedidosComFuncaoSobreDataPedido`, `QueryCorrigida_FronteirasDoAno_ContaSoPedidosDe2026` (CA-22), plano em `Investigacao_CargaDeVolume_MedePlanosELeituras` |
| 4 | `LEFT JOIN` + `WHERE` sobre a tabela da direita | O filtro descarta as linhas com `DataPedido` nulo: é um `INNER JOIN` disfarçado. No plano medido, o otimizador **manteve** a junção externa e juntou os 200 000 pedidos antes de filtrar | `INNER JOIN`, começando por `Pedidos`, que é o lado filtrado | Plano da seção 4 |

O resultado está em `02-query-corrigida.sql`. Ele traz o **ranking completo**, sem `TOP` (HL-10, PROP-01), porque a original também não limita. Os 5 primeiros coincidem com `GET /relatorios/top-clientes?dataInicio=2026-01-01&dataFim=2026-12-31` (`QueryCorrigida_Ranking2026_CoincideComTopClientes`, CA-33).

**Nenhum índice novo foi criado.** A seção 6 mostra por quê.

## 2. Como foi medido

**Ambiente.** SQL Server 2022 (build 16.0.4325.4, lido do Showplan) no contêiner do Testcontainers, com os mesmos scripts `database/01` a `03` do compose. Docker Desktop sobre WSL2, com 7,73 GB de memória. A máquina estava ocupada por outro build em paralelo, então **os tempos têm ruído**. As leituras lógicas e a forma dos planos são determinísticas, porque a carga não usa aleatoriedade.

**MCP `mssql`.** Ele estava fora do ar nesta rodada: o login do MCP falhou com `AUTH_FAILED` (CG-05). O caminho alternativo, autorizado pelo orquestrador, foi fazer no teste exatamente o que o MCP faria, usando o **mesmo login** `claude_mcp` (`db_datareader` + `SHOWPLAN` + `VIEW DATABASE STATE`, `database/03-logins.sql`):

| O quê | Como | Login |
|---|---|---|
| Plano estimado (o que o `explain_sql` do MCP devolve) | `SET SHOWPLAN_XML ON` | `claude_mcp` |
| Plano real, com linhas e leituras por operador | `SET STATISTICS XML ON` | `claude_mcp` |
| Leituras lógicas por tabela | `SET STATISTICS IO ON`, mensagens lidas pelo `InfoMessage` do SqlClient | `claude_mcp` |
| Tempo, CPU, leituras médias, DOP e memória concedida | `sys.dm_exec_query_stats`, filtrando pelo comentário `-- parte4:<nome>` de cada arquivo | `sa`, porque a DMV exige `VIEW SERVER STATE` e o `claude_mcp` não tem |

**Roteiro do teste `Investigacao_CargaDeVolume_MedePlanosELeituras`:**

1. Aplica `00-carga-volume.sql`, que leva entre 87 e 115 s.
2. Para cada arquivo, captura o plano estimado e depois executa uma vez com `STATISTICS IO` e `STATISTICS XML`. Essa execução também aquece o cache de dados.
3. Esvazia o cache de planos (`DBCC FREEPROCCACHE`) e executa cada consulta 10 vezes, intercaladas, sem as opções de diagnóstico.
4. Lê `sys.dm_exec_query_stats`.

As médias são, portanto, **com cache quente**. A primeira execução, que paga a compilação e a leitura física, não entra nelas.

**Volume gerado** (contagens lidas no banco):

| Clientes | Nomes distintos | Produtos | Pedidos | Itens | Pedidos de 2026 | Compradores em 2026 |
|---|---|---|---|---|---|---|
| 5 000 | 300 | 200 | 200 000 | 600 000 | 44 867 (22,4 %) | 5 000 |

## 3. Hipóteses e o que a medição disse

| Hipótese | Resultado | Evidência |
|---|---|---|
| H1. `YEAR()` sobre a coluna impede o seek e obriga a ler todos os pedidos | **Confirmada** | Original: *Index Scan* de `IX_Pedidos_DataPedido`, com 200 000 linhas reais e 1 151 leituras. Corrigida: *Index Seek* com 44 867 linhas e 248 leituras (−78 % em `Pedidos`) |
| H2. Com a função, a estimativa de cardinalidade é um chute | **Confirmada** | Filtro do ano na original: 33 335,7 estimadas contra 44 867 reais (−26 %). Seek com intervalo na corrigida: 44 871,9 estimadas contra 44 867 reais (erro de 0,01 %) |
| H3. `GROUP BY c.Nome` funde homônimos | **Confirmada** | Original: 300 linhas, igual ao número de nomes distintos. Corrigida: 5 000 linhas, igual ao número de compradores. Também confirmada com a carga de exemplo (dois "Maria Silva", dois "João Souza") |
| H4. O otimizador transforma sozinho o `LEFT JOIN` anulado em `INNER JOIN`, e o defeito 4 seria só de legibilidade | **Refutada** | O plano real da original tem *Hash Match (Left Outer Join)* de `Clientes` com os 200 000 pedidos e só **depois** um *Filter* (200 000 → 44 867). O defeito custa trabalho, não só clareza |
| H5. Falta índice | **Refutada** | `Pedidos` é coberta por `IX_Pedidos_DataPedido`, sem *Key Lookup*. Na corrigida, 91 % das leituras (3 278 de 3 589) estão em `ItensPedido`, lidas pela `PK_ItensPedido`. Um índice sobre colunas existentes não reduz isso (seção 6) |
| H6. Agregar por cliente antes de juntar `Clientes` reduz o trabalho, e convém reescrever a query nesse formato | **Parcialmente confirmada.** O ganho existe, mas a reescrita é desnecessária | O otimizador já agrega antes: na corrigida, o *Hash Match (Aggregate)* reduz 134 601 linhas para 5 000 **abaixo** da junção com `Clientes`. A `03` faz isso explicitamente |

## 4. Plano antes: `01-query-original.sql`

Plano real com o volume da seção 2. Os valores são: linhas estimadas, linhas reais e leituras lógicas do operador. O plano é paralelo (DOP 3), com custo estimado de 11,517.

```text
Parallelism (Gather Streams)                         est=285,7    reais=300
  Sort                                               est=285,7    reais=300
    Stream Aggregate (GROUP BY Nome)                 est=285,7    reais=300
      Sort                                           est=571,3    reais=900
        Parallelism (Repartition Streams)
          Hash Match (Partial Aggregate)             est=571,3    reais=900
            Hash Match (Left Outer Join) ← itens     est=100 007  reais=134 601
              Bitmap Create                          est=33 335,7 reais=44 867
                Filter (ano = 2026)                  est=33 335,7 reais=44 867   ← o filtro só vem aqui
                  Parallelism (Repartition Streams)                reais=200 000
                    Hash Match (Left Outer Join)     est=200 000  reais=200 000  ← junta TODOS os pedidos
                      Clustered Index Scan Clientes.PK_Clientes         reais=5 000    leituras=184
                      Index Scan Pedidos.IX_Pedidos_DataPedido          reais=200 000  leituras=1 151
              Clustered Index Scan ItensPedido.PK_ItensPedido           reais=135 596  leituras=3 444
```

Leituras de `STATISTICS IO`: Clientes 184 (scan count 4), Pedidos 1 151 (scan count 4) e ItensPedido 3 444 (scan count 4), num total de 4 779.

Como ler esse plano:

- `Pedidos` é lida **inteira**: 200 000 linhas, sem seek. A função sobre a coluna é calculada linha a linha, num *Compute Scalar* seguido do *Filter*.
- O filtro do ano só é aplicado **depois** da junção externa com `Clientes`. A junção processa os 200 000 pedidos, quando só 44 867 interessam.
- O erro de estimativa do filtro (33 335,7 contra 44 867) se propaga para a junção com os itens (100 007 contra 134 601). Concessões de memória e escolhas de junção ficam baseadas em números errados.
- O custo leva o otimizador a um plano paralelo. Isso explica a CPU acima do tempo decorrido na seção 5: 1 229 ms de CPU para 715 ms decorridos.

**Com a carga de exemplo**, de 34 pedidos, o plano é outro, mas o defeito é o mesmo. O otimizador prefere *Nested Loops* com *Index Seek* em `IX_Pedidos_ClienteId_DataPedido` **por `ClienteId`**: um seek por cliente, que no conjunto lê todos os pedidos de todos os clientes. O ano continua num *Filter* posterior. Por isso o teste da carga de exemplo verifica o que não depende do tamanho: há função sobre a coluna e **nenhum seek delimitado por `DataPedido`** (`PlanoDeExecucao.BuscaPelaColuna`, que lê os `SeekPredicates` do Showplan). O scan de `Pedidos` é verificado com volume.

## 5. Plano depois: `02-query-corrigida.sql`

Plano serial (DOP 1), com custo estimado de 4,335.

```text
Sort (Total DESC, Id)                                est=4 991,8  reais=5 000
  Adaptive Join → hash (Clientes)                    est=4 991,8  reais=5 000
    Hash Match (Aggregate por ClienteId)             est=4 991,8  reais=5 000    ← agrega antes de juntar Clientes
      Adaptive Join → hash (itens)                   est=134 616  reais=134 601
        Nested Loops
          Merge Interval (1 faixa)                   est=2        reais=1
          Index Seek Pedidos.IX_Pedidos_DataPedido   est=44 871,9 reais=44 867   leituras=248   ← seek pelo intervalo
        Clustered Index Scan ItensPedido.PK_ItensPedido          reais=134 601  leituras=3 278
    Clustered Index Scan Clientes.PK_Clientes                    reais=5 000    leituras=63
```

Leituras de `STATISTICS IO`: Clientes 63, Pedidos 248 e ItensPedido 3 278 (scan count 1 em cada), num total de 3 589.

Como ler esse plano:

- `Pedidos` agora é lida por um **Index Seek** em `IX_Pedidos_DataPedido`, delimitado pelo intervalo de 2026, sem *Key Lookup*. O índice inclui `ClienteId`.
- O *Merge Interval* sobre duas *Constant Scan* é a conversão dos literais para `DATETIME2(3)`. Ela produz uma única faixa (`reais=1`). A coluna fica intacta.
- A estimativa do seek é praticamente exata, e toda a cadeia acima herda isso.
- Os *Adaptive Join* (modo lote em rowstore do SQL Server 2022) decidiram pelo caminho *hash* nas duas junções. Para 44 867 pedidos, que são 22 % da tabela, ler `ItensPedido` uma vez é mais barato que 44 867 seeks. O CA-33 permite isso, porque restringe só o acesso a `Pedidos` (arquitetura Q12).
- O plano é serial. A CPU cai para perto do tempo decorrido.

A variante `03-variante-valor-total.sql` tem custo estimado de 0,822 e é serial:

```text
Sort → Hash Match (Inner Join) → Hash Match (Aggregate) → Index Seek Pedidos.IX_Pedidos_DataPedido (reais=44 867, leituras=248)
                               → Clustered Index Scan Clientes.PK_Clientes (reais=5 000, leituras=63)
```

Ela não toca em `ItensPedido`. As leituras são 63 + 248 = 311, e o resultado é **idêntico** ao da `02`, linha a linha. O teste verifica isso com `variante.Linhas.ShouldBe(corrigida.Linhas)`.

## 6. Medições: `sys.dm_exec_query_stats`

Médias por execução, com cache quente. Os tempos estão em milissegundos.

**Rodada final, 10 execuções de cada:**

| Consulta | Leituras lógicas | CPU | Decorrido | Menor | Maior | DOP | Memória concedida (KB) |
|---|---|---|---|---|---|---|---|
| `01` original | 4 779 | 1 229,0 | 714,6 | 639,4 | 867,0 | 3 | 34 320 |
| `02` corrigida | 3 589 | 351,4 | 352,4 | 295,2 | 416,8 | 1 | 81 072 |
| `03` variante `ValorTotal` | 311 | 84,5 | 84,6 | 62,5 | 119,6 | 1 | 5 120 |

**Rodada anterior, 5 execuções de cada**, numa máquina mais carregada. Serve para mostrar a dispersão:

| Consulta | Leituras lógicas | CPU | Decorrido | Menor | Maior | DOP | Memória concedida (KB) |
|---|---|---|---|---|---|---|---|
| `01` original | 4 779 | 1 685,5 | 891,5 | 545,2 | 1 165,9 | 3 | 23 112 |
| `02` corrigida | 3 589 | 471,1 | 481,1 | 330,8 | 800,3 | 1 | 81 072 |
| `03` variante `ValorTotal` | 311 | 101,8 | 102,3 | 73,5 | 137,1 | 1 | 5 120 |

**Original → corrigida (`02`), na rodada final:**

- leituras: −25 %;
- leituras em `Pedidos`: −78 %;
- CPU: −71 %;
- tempo decorrido: −51 %;
- plano paralelo virou serial.

**Original → variante (`03`):**

- leituras: −93 %;
- CPU: −93 %;
- tempo decorrido: −88 %.

As leituras lógicas são idênticas nas duas rodadas, porque a carga é determinística. Os tempos variam, mas a ordem e a proporção entre as três consultas se mantêm.

**Ponto de atenção da `02`: a memória concedida.** São 81 MB, mais que a original. A causa são as duas junções *hash*, a agregação *hash* e o *Sort* final, num plano serial. Com muitas execuções simultâneas, isso pesaria no *memory grant*. A `03` pede 5 MB.

## 7. Índices

Não há índice novo, e a medição sustenta a decisão (arquitetura Q12, spec §6.3):

- **`Pedidos`.** `IX_Pedidos_DataPedido (DataPedido DESC, Id DESC) INCLUDE (ClienteId, ValorTotal)` já existe para a listagem e o relatório. Ele cobre a query corrigida, porque o seek devolve `Id` e `ClienteId` sem *Key Lookup*. Também cobre a variante, que usa `ValorTotal`.
- **`ItensPedido`.** A `PK_ItensPedido` é clusterizada por `(PedidoId, ProdutoId)`. Um índice não clusterizado `(PedidoId) INCLUDE (Quantidade, PrecoUnitario)` teria praticamente o mesmo conteúdo e o mesmo número de páginas, então não reduziria as 3 278 leituras. O que elimina essa leitura é **não precisar dos itens**, como faz a `03`: `ValorTotal` é a soma gravada dos itens (RN-09), e o pedido é imutável (RN-12).
- **`Clientes`.** A `PK_Clientes` é lida inteira, com 63 leituras, porque todos os 5 000 clientes da carga compraram em 2026. Não há o que ganhar aqui.

## 8. Por que a entrega é a `02`, e não a `03`

A `02` responde ao enunciado nos termos dele: soma `Quantidade * PrecoUnitario` dos itens, como a original. É a correção pedida pelo backlog (T-10.2) e pela arquitetura (Q12).

A `03` é a forma **mais rápida**, segundo as medições acima, e é a que a API já usa em `GET /relatorios/top-clientes` (arquitetura Q5). A escolha é de produto e fica registrada para o README:

- se `ValorTotal` é confiável, a `03` é a recomendação de produção;
- se a pergunta é sobre os itens, independentemente do cabeçalho, fica a `02`.

## 9. Achado lateral: estatísticas desatualizadas nos testes

Na primeira execução do CA-33, o plano estimado da `02` sobre a carga de exemplo mostrou *Index Scan* em `IX_Pedidos_ClienteId_DataPedido`, com estimativa de **2 linhas** para 23 pedidos reais em 2026.

A causa estava no teste: o Respawn esvazia as tabelas entre os testes, e as estatísticas continuavam descrevendo o conteúdo de testes anteriores. Em tabelas pequenas, poucas alterações não atingem o limiar de atualização automática.

Depois de `UPDATE STATISTICS ... WITH FULLSCAN` (`ScriptsParte4.AtualizarEstatisticasAsync`), o plano passou a ser o seek por `DataPedido`, e o CA-33 passa. A lição vale para produção: **um plano bom depende de estatísticas que descrevem os dados**. A carga de volume termina com `UPDATE STATISTICS ... WITH FULLSCAN` pelo mesmo motivo.

## 10. Como reproduzir

**Pelos testes** (exigem Docker). Gera todos os números deste documento no log e grava os planos reais (`.sqlplan`, abrem no SSMS ou no Azure Data Studio) em `backend/tests/Joya.Pedidos.IntegrationTests/bin/Debug/net8.0/parte4-planos/`:

```bash
dotnet test backend/tests/Joya.Pedidos.IntegrationTests --filter "FullyQualifiedName~Parte4" --logger "console;verbosity=detailed"
```

O teste de medição leva cerca de 2 minutos e tem o trait `Categoria=Medicao`. Para pulá-lo, use `--filter "Categoria!=Medicao"`.

Os `.sqlplan` não são versionados. O plano da original contém o texto da query com a função sobre a coluna, e o checklist DB-03 só admite essa função em `01-query-original.sql`.

**Pelo compose e pelo MCP**, assim que o `AUTH_FAILED` do CG-05 for resolvido:

1. `docker compose up -d db db-init`.
2. Como `sa`, `sqlcmd -S localhost -U sa -C -I -b -i database/parte4/00-carga-volume.sql`. É opcional, só para ter volume, e usa banco de desenvolvimento.
3. MCP `explain_sql` com o `SELECT` de cada arquivo. Ele devolve o plano estimado. Procure *Index Scan* contra *Index Seek* em `Pedidos` e o `datepart(year, ...)` nos predicados da original.
4. Tempo e leituras pelo MCP: o `claude_mcp` não tem `VIEW SERVER STATE`, então `sys.dm_exec_query_stats` não está acessível a ele. A alternativa é o Query Store (`sys.query_store_runtime_stats`), que exige só `VIEW DATABASE STATE` e vem ligado por padrão em bancos novos no SQL Server 2022.

## 11. Limites desta investigação

- **Ruído nos tempos.** Uma única máquina de desenvolvimento, carregada por outro build. Os tempos servem para comparar as três consultas entre si, não como referência absoluta. As leituras lógicas e os planos são reprodutíveis.
- **Cache quente.** As médias não incluem leitura física. Com cache frio, a diferença a favor do seek em `Pedidos` tende a aumentar, mas isso **não foi medido**.
- **Distribuição sintética.** Os pedidos se espalham por quase quatro anos, com 22 % em 2026. Com um ano que cobrisse quase toda a tabela, o seek em `Pedidos` perderia a vantagem. A `02` continuaria correta, só sem o ganho de leitura.
- **Compose não medido.** O plano no banco do compose não foi capturado nesta rodada, porque o MCP estava fora do ar (CG-05). Fica como verificação pendente pelo passo 3 da seção 10.
