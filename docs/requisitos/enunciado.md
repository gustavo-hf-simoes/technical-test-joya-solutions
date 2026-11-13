# Enunciado da vaga — cópia literal

> **Fonte única de verdade para os agentes `product-owner` e `qa`.** Texto copiado sem edição do e-mail da Joya Solutions (recebido em 13/11/2026). Não altere: qualquer interpretação vai em `docs/esteira/01-requisitos.md`, com referência à linha daqui (ex.: `ENU-L42`).

```text
001  TESTE TÉCNICO: MINI GESTÃO DE PEDIDOS
002  
003  Contexto
004  
005  Construa uma pequena aplicação de cadastro e consulta de Pedidos de Clientes. O objetivo não é a complexidade da regra de negócio, e sim como você trabalha com IA para entregar uma feature completa, bem planejada, testada e com qualidade.
006  
007  O uso do Claude Code é obrigatório e faz parte da avaliação. Queremos ver como você estrutura o trabalho com IA, não apenas o resultado final.
008  
009  Parte 1 — Especificação e planejamento (SDD)
010  
011  Antes de codar, crie na raiz do repositório:
012  
013  docs/spec.md: especificação da feature (objetivo, regras de negócio, endpoints, modelo de dados e critérios de aceite)
014  docs/plan.md: o plano de implementação gerado com o Claude em plan mode, junto com o que você ajustou ou discordou dele e por quê
015  Parte 2 — Configuração AI First
016  
017  Inclua a pasta .claude/ e o arquivo CLAUDE.md versionados no repositório, contendo:
018  
019  Rules: um CLAUDE.md com as convenções do projeto (arquitetura, padrões de nomenclatura, como rodar build e testes)
020  Hook: pelo menos um hook útil (ex.: rodar dotnet build, lint ou testes após edições)
021  Skill: pelo menos uma skill customizada (ex.: "criar novo endpoint seguindo o padrão do projeto")
022  MCP: conectar o Claude ao SQL Server por meio de um MCP e explicar no README como ele foi usado durante o desenvolvimento
023  Parte 3 — Implementação end to end
024  
025  Banco de dados (SQL Server)
026  
027  Tabelas Clientes, Produtos, Pedidos e ItensPedido, com chaves primárias, estrangeiras e índices adequados
028  Scripts de criação e de carga de dados de exemplo na pasta database/
029  Backend (.NET 8 ou superior)
030  
031  POST /pedidos: cria um pedido com itens, validando se o cliente existe e se há estoque suficiente
032  GET /pedidos: lista os pedidos com filtros por cliente e período, com paginação
033  GET /relatorios/top-clientes: retorna os 5 clientes com maior valor total de pedidos no período informado
034  Frontend (React ou Angular, com TypeScript)
035  
036  Tela de listagem de pedidos com filtros
037  Tela ou modal de criação de pedido
038  Parte 4 — Debug de query
039  
040  A query abaixo deveria retornar o ranking dos clientes que mais compraram em 2026, mas está lenta e retornando resultados incorretos. Corrija, otimize e explique no README o que estava errado e como você investigou o problema (plano de execução, índices etc.).
041  
042  SELECT c.Nome, SUM(i.Quantidade * i.PrecoUnitario) AS Total
043  FROM Clientes c
044  LEFT JOIN Pedidos p ON p.ClienteId = c.Id
045  LEFT JOIN ItensPedido i ON i.PedidoId = p.Id
046  WHERE YEAR(p.DataPedido) = 2026
047  GROUP BY c.Nome
048  ORDER BY Total
049  
050  Parte 5 — Testes
051  
052  Obrigatórios
053  
054  Testes unitários no backend, cobrindo pelo menos as regras de negócio e validações do pedido (cliente inexistente, estoque insuficiente, pedido sem itens etc.)
055  Testes de integração no backend, exercitando os endpoints contra um banco SQL Server real (ex.: via Testcontainers ou o SQL Server do próprio Docker Compose)
056  Diferencial (não obrigatório)
057  
058  Testes end to end com Playwright, cobrindo pelo menos o fluxo de criar um pedido e vê-lo na listagem
059  Parte 6 — Docker
060  
061  O projeto deve rodar inteiramente no Docker. Esperamos um docker-compose.yml na raiz que suba:
062  
063  SQL Server (com os scripts de criação e carga executados automaticamente)
064  Backend .NET
065  Frontend
066  Com o Docker instalado, a aplicação completa deve subir com um único comando, por exemplo:
067  
068  docker compose up
069  
070  Requisitos obrigatórios de entrega
071  
072  Repositório no GitHub, com o link enviado por e-mail
073  README na raiz contendo:
074  Como rodar o projeto com Docker
075  Como rodar os testes (unitários e de integração)
076  Principais decisões técnicas
077  Como você usou o Claude: o que funcionou bem e o que você precisou corrigir no código gerado
078  A explicação da Parte 4 (debug da query)
079  Projeto rodando no Docker conforme a Parte 6
080  Testes unitários e de integração conforme a Parte 5
081  Histórico de commits preservado. Vamos analisar os commits para entender como a solução evoluiu, então prefira commits pequenos, frequentes e com mensagens claras, em vez de um único commit com tudo pronto no final. Não faça squash nem reescreva o histórico.
082  Vídeo curto (até 10 minutos) mostrando seu fluxo de trabalho com o Claude Code e explicando o código.
083  Diferenciais (não obrigatórios, mas contam pontos)
084  
085  Testes end to end com Playwright
086  Pipeline de GitHub Actions (build e execução dos testes a cada push ou pull request)
087  Deploy online da aplicação, com o link informado no README
088  Critérios de avaliação
089  
090  Critério	Peso
091  Qualidade da spec e do plano (SDD)	15%
092  Uso estruturado do Claude (rules, hooks, skills e MCP)	20%
093  Backend .NET e modelagem SQL	15%
094  Debug e otimização da query	10%
095  Testes unitários e de integração	15%
096  Docker (projeto sobe com um comando, sem ajustes manuais)	5%
097  Histórico de commits (evolução, organização e clareza)	10%
098  Frontend funcional	5%
099  README, clareza e senso crítico sobre o código gerado	5%
100  Os diferenciais são considerados como pontos extras, além dos critérios acima.
```
