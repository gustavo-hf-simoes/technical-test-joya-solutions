using Joya.Pedidos.Api.Limites;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Joya.Pedidos.IntegrationTests.Api;

/// <summary>Rotas existentes só no host de teste: exercitam o pipeline sem usar uma rota de produção.</summary>
[ApiController]
[Route("_teste")]
public sealed class RotasDeTesteController : ControllerBase
{
    [HttpGet("excecao")]
    public IActionResult Excecao() =>
        throw new InvalidOperationException("segredo-que-nao-pode-vazar");

    [HttpPost("eco")]
    public IActionResult Eco([FromBody] CorpoDeTeste corpo) => Ok(corpo);

    /// <summary>
    /// Espelho do <c>POST /pedidos</c> até a T-06.4: leva as mesmas proteções que a action real vai levar
    /// (ADR-0002). Quando a T-06.4 criar o <c>PedidosController</c>, os testes passam a mirar <c>/pedidos</c>.
    /// </summary>
    [HttpPost("pedidos")]
    [EnableRateLimiting(LimitesHttp.PoliticaCriarPedido)]
    public IActionResult CriarPedido([FromBody] CorpoDeTeste corpo) => Ok(corpo);
}

public sealed record CorpoDeTeste(string Nome);
