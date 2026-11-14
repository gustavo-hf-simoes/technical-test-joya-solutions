# Matriz de rastreabilidade

> Liga cada linha do [enunciado](../requisitos/enunciado.md) ao requisito, à história, à tarefa, ao teste e ao commit que a atendem (`HL-08`). O CI valida esta tabela em todo push e PR com [validar_rastreabilidade.py](../../.github/scripts/validar_rastreabilidade.py).
>
> - **Quem preenche:** o `product-owner` preenche as colunas até **Testes** na etapa 1. O orquestrador preenche **Commits** quando a tarefa é commitada.
> - **Enquanto a tabela não tem linhas**, o validador só avisa. A partir da primeira linha, todas as regras abaixo valem.

## Regras que o CI confere

1. Toda linha não vazia do enunciado aparece em alguma linha da matriz, mesmo as de classe `Contexto`.
2. **Classe** é `Obrigatório`, `Diferencial` ou `Contexto`.
3. As linhas `Obrigatório` e `Diferencial` têm **Requisito** (`RF-xx`, `RNF-xx` ou `ENT-xx`), **US**, **Tarefas** e **Testes** preenchidos. Use `—` para "vazio", o que só é aceito em `Contexto`.
4. Toda `US-xx` e toda `T-xx.n` citada existe em `docs/esteira/backlog.md`, gerado na etapa 1.
5. **Testes** traz nomes de teste entre crases (ex.: `` `CriarAsync_EstoqueInsuficiente_RetornaConflito` ``) ou uma verificação manual no formato `Manual: <como verificar>`, para entregáveis como o vídeo.
6. Quando **Commits** tem um hash:
   - o commit precisa existir no histórico;
   - cada teste citado entre crases precisa existir no código de teste (`backend/tests/`, `frontend/src/`, `e2e/`).

## Matriz

| ENU | Classe | Requisito | US | Tarefas | Testes | Commits |
|---|---|---|---|---|---|---|
| ENU-L001–L005 | Contexto | — | — | — | — | — |
| ENU-L007 | Obrigatório | ENT-23 | US-01, US-18 | T-01.1, T-18.3 | Manual: diário em docs/ia e seção Como usei o Claude no README mostram o uso do Claude Code em cada etapa | 501ae41, e48c12e |
| ENU-L009 | Contexto | — | — | — | — | — |
| ENU-L011 | Obrigatório | ENT-01, ENT-02 | US-01 | T-01.1, T-01.2 | Manual: git log mostra spec e plano antes do primeiro commit de código | 501ae41, 125952a |
| ENU-L013 | Obrigatório | ENT-01 | US-01 | T-01.2 | Manual: docs/spec.md tem objetivo, regras de negócio, endpoints, modelo de dados e critérios de aceite | 125952a |
| ENU-L014 | Obrigatório | ENT-02 | US-01 | T-01.3 | Manual: docs/plan.md tem a proposta do plan mode e a seção Human-in-the-Loop preenchida | abf8b91 |
| ENU-L015 | Contexto | — | — | — | — | — |
| ENU-L017 | Obrigatório | ENT-07 | US-01, US-18 | T-01.1, T-18.2 | Manual: git ls-files .claude CLAUDE.md lista agentes, hooks e skills versionados | 501ae41, 061162b |
| ENU-L019 | Obrigatório | ENT-03, RNF-03 | US-02, US-03, US-18 | T-02.1, T-03.2, T-18.2 | `Core_Dependencias_NaoReferenciaEfAspNetNemSqlClient`, `Core_Dependencias_NaoReferenciaInfrastructureNemApi`, `Infrastructure_Dependencias_NaoReferenciaApi`, `Controllers_Dependencias_NaoAcessamDbContext`, `Dtos_Declaracao_SaoSealedRecord`, `Entidades_Propriedades_NaoTemSetterPublico`, `MetodosAsync_Assinatura_RecebemCancellationTokenPorUltimo`, `Classes_Declaracao_SaoSealed`, `TestesDeRegra_Dependencias_NaoUsamInfrastructureNemApi` | 088ea5b, 86f7b47, 061162b |
| ENU-L020 | Obrigatório | ENT-04 | US-18 | T-18.2 | Manual: editar um arquivo do backend dispara post-edit.sh, que roda build e testes unitários | 061162b |
| ENU-L021 | Obrigatório | ENT-05 | US-06, US-18 | T-06.6, T-18.2 | Manual: T-06.6 e T-07.1 feitas com a skill new-endpoint, registradas no diário | 22132a4, 061162b |
| ENU-L022 | Obrigatório | ENT-06 | US-04, US-10, US-18 | T-04.3, T-10.3, T-18.2 | `LoginMcp_TentaEscrever_PermissaoNegada` | 97a88c1, 9eb76b7, 061162b |
| ENU-L023 | Contexto | — | — | — | — | — |
| ENU-L025 | Obrigatório | RNF-09 | US-04 | T-04.1 | `Schema_ScriptsAplicados_CriaTabelasChavesEConstraints` | 027a0ff |
| ENU-L027 | Obrigatório | RF-07, RNF-02 | US-04 | T-04.1, T-04.2 | `Schema_ScriptsAplicados_CriaTabelasChavesEConstraints`, `Indices_ScriptAplicado_ExistemIndicesDaSpec` | 027a0ff, 75b834d |
| ENU-L028 | Obrigatório | RF-08 | US-04 | T-04.1, T-04.4 | `Carga_AplicadaDuasVezes_NaoDuplicaLinhas` | 027a0ff, d2c6c85 |
| ENU-L029 | Obrigatório | RNF-09, RNF-03 | US-02 | T-02.1 | Manual: dotnet build da solução net8.0 termina com 0 warnings | 088ea5b |
| ENU-L031 | Obrigatório | RF-01, RNF-01 | US-06 | T-06.1, T-06.2, T-06.3, T-06.4, T-06.5, T-06.6, T-06.7 | `PostPedidos_PedidoValido_Retorna201ComLocationEDebitaEstoque`, `PostPedidos_ClienteInexistente_Retorna422`, `PostPedidos_EstoqueInsuficiente_Retorna409SemGravarNada`, `PostPedidos_VinteRequisicoesSimultaneas_EstoqueNuncaFicaNegativo` | 10d064c, 4f33672, 1b6564d, dac6332, 0506fe7, 22132a4, f969a53 |
| ENU-L032 | Obrigatório | RF-02 | US-08 | T-08.1, T-08.2 | `GetPedidos_FiltroCliente_RetornaSoDoCliente`, `GetPedidos_FiltroPeriodo_IncluiAsDuasPontas`, `GetPedidos_PercorreTodasAsPaginas_NaoRepeteNemOmite` | 8b98d09, 59eae2d |
| ENU-L033 | Obrigatório | RF-03 | US-09 | T-09.1, T-09.2 | `GetTopClientes_OitoClientes_RetornaCincoEmOrdemDecrescente`, `GetTopClientes_PedidosForaDoPeriodo_NaoContam`, `GetTopClientes_Homonimos_NaoSomaNomesIguais` | 15ae8c8, 9caca95 |
| ENU-L034 | Obrigatório | RNF-09 | US-14 | T-14.1 | `AppComponent_Inicializacao_UsaLocalePtBr` | 1779aa4 |
| ENU-L036 | Obrigatório | RF-05, RNF-08 | US-14 | T-14.2, T-14.3, T-14.4 | `PedidoListaComponent_FiltrosAplicados_AtualizaQueryString`, `PedidoListaComponent_FalhaNaApi_MostraErroComTentarNovamente`, `Contrato_ModelosDoFrontend_IgualAoOpenApiVersionado` | — |
| ENU-L037 | Obrigatório | RF-06, RF-10 | US-07, US-15 | T-07.1, T-15.1, T-15.2, T-15.3 | `PedidoFormComponent_Confirmar_RedirecionaParaListagemComSucesso`, `PedidoFormComponent_Resposta409_DestacaItensEPreservaDados`, `PedidoFormComponent_Reenvio_ReutilizaIdempotencyKey`, `GetProdutos_FiltroAtivo_RetornaSoAtivosOrdenadosPorNome`, `GetCatalogo_AtivoInvalido_Retorna400` | 61bb1c2, c757543, 32bc744 |
| ENU-L038 | Contexto | — | — | — | — | — |
| ENU-L040 | Obrigatório | RF-04, RNF-02, ENT-14 | US-10, US-18 | T-10.1, T-10.2, T-10.3, T-18.4 | `QueryCorrigida_Ranking2026_CoincideComTopClientes` | f37e391, 894a406, 9eb76b7, 2e88278 |
| ENU-L042–L048 | Obrigatório | RF-04 | US-10 | T-10.1, T-10.2 | `QueryOriginal_ClientesHomonimos_SomaIndevidamente`, `QueryOriginal_Ordenacao_RetornaMenorTotalPrimeiro`, `QueryCorrigida_Ranking2026_CoincideComTopClientes` | f37e391, 894a406 |
| ENU-L050–L052 | Contexto | — | — | — | — | — |
| ENU-L054 | Obrigatório | RNF-07 | US-06 | T-06.2, T-06.3 | `CriarAsync_ClienteInexistente_RetornaClienteNaoEncontrado`, `CriarAsync_EstoqueInsuficiente_RetornaConflitoSemGravar`, `Validate_ItensVazios_RetornaErroNoCampoItens` | 4f33672, 1b6564d |
| ENU-L055 | Obrigatório | RNF-07 | US-04, US-06, US-08, US-09 | T-04.1, T-06.6, T-08.2, T-09.2 | `PostPedidos_SemItens_Retorna400SemGravar`, `GetPedidos_SemFiltro_RetornaPrimeiraPaginaOrdenada`, `GetTopClientes_SemPeriodo_Retorna400` | 027a0ff, 22132a4, 59eae2d, 9caca95 |
| ENU-L056 | Contexto | — | — | — | — | — |
| ENU-L058 | Diferencial | ENT-19 | US-16, US-03 | T-16.1, T-03.5 | `CriarPedido_FluxoCompleto_ApareceNoTopoDaListagem` | b292800, 181d359 |
| ENU-L059 | Contexto | — | — | — | — | — |
| ENU-L061 | Obrigatório | RNF-04 | US-04, US-13 | T-04.5, T-13.1, T-13.2 | Manual: num clone limpo, docker compose up sobe db, api e frontend saudáveis a partir do docker-compose.yml da raiz | 994e5c0, ba65994, ffca2da |
| ENU-L063 | Obrigatório | RNF-04, RF-08 | US-04 | T-04.1, T-04.4, T-04.5 | Manual: docker compose up aplica schema e carga pelo db-init sem passo manual, e subir de novo não duplica a carga | 027a0ff, d2c6c85, 994e5c0 |
| ENU-L064 | Obrigatório | RNF-04 | US-13 | T-13.1 | Manual: serviço api do compose responde 200 em /health/ready | ba65994 |
| ENU-L065 | Obrigatório | RNF-04 | US-13, US-14 | T-14.5, T-13.2 | Manual: serviço frontend do compose abre a listagem em localhost:4200 | fe9cb83, ffca2da |
| ENU-L066–L068 | Obrigatório | RNF-04 | US-13 | T-13.2 | Manual: docker compose down -v e depois docker compose up, sem flags e sem .env, sobem a aplicação completa | ffca2da |
| ENU-L070 | Contexto | — | — | — | — | — |
| ENU-L072 | Obrigatório | ENT-08 | US-18 | T-18.6 | Manual: repositório público no GitHub abre sem login e o link foi enviado por e-mail | — |
| ENU-L073 | Obrigatório | ENT-09 | US-18 | T-18.1 | Manual: README.md na raiz contém as 5 seções exigidas | 2a47d6e |
| ENU-L074 | Obrigatório | ENT-10 | US-18 | T-18.1 | Manual: o comando Docker do README sobe a aplicação num clone limpo | 2a47d6e |
| ENU-L075 | Obrigatório | ENT-11 | US-18 | T-18.1 | Manual: os comandos de testes unitários e de integração do README passam | 2a47d6e |
| ENU-L076 | Obrigatório | ENT-12 | US-18 | T-18.3 | Manual: seção Decisões técnicas cita as decisões D, os ADRs e os HL | e48c12e |
| ENU-L077 | Obrigatório | ENT-13 | US-18 | T-18.3 | Manual: seção Como usei o Claude lista o que funcionou e as correções HL e CG com evidência | e48c12e |
| ENU-L078 | Obrigatório | ENT-14 | US-10, US-18 | T-10.3, T-18.4 | Manual: seção Parte 4 traz os defeitos, os planos antes e depois e os índices | 9eb76b7, 2e88278 |
| ENU-L079 | Obrigatório | ENT-15, RNF-04 | US-13 | T-13.2 | Manual: mesma verificação da Parte 6 em clone limpo | ffca2da |
| ENU-L080 | Obrigatório | ENT-16, RNF-07 | US-06, US-03 | T-06.3, T-06.6, T-03.3 | `CriarAsync_ClienteInexistente_RetornaClienteNaoEncontrado`, `PostPedidos_PedidoValido_Retorna201ComLocationEDebitaEstoque` | 1b6564d, 22132a4, 5503aa1 |
| ENU-L081 | Obrigatório | ENT-17 | US-01, US-18 | T-01.1, T-18.3 | Manual: git log da main mostra um merge por história, um commit por tarefa e nenhum squash ou force-push | 501ae41, e48c12e |
| ENU-L082 | Obrigatório | ENT-18 | US-18 | T-18.6, T-18.7 | Manual: vídeo de até 10 minutos com o fluxo no Claude Code, com link no README | — |
| ENU-L083 | Contexto | — | — | — | — | — |
| ENU-L085 | Diferencial | ENT-19 | US-16, US-03 | T-16.1, T-03.5 | `CriarPedido_FluxoCompleto_ApareceNoTopoDaListagem` | b292800, 181d359 |
| ENU-L086 | Diferencial | ENT-20 | US-03 | T-03.1, T-03.3, T-03.4, T-03.5 | Manual: workflow do GitHub Actions verde em push e em pull request | 8b0384e, 5503aa1, c5ebbaa, 181d359 |
| ENU-L087 | Diferencial | ENT-21 | US-17, US-18 | T-17.1, T-17.2, T-18.6 | Manual: a URL do README abre a aplicação via HTTPS | — |
| ENU-L088–L100 | Contexto | — | — | — | — | — |
