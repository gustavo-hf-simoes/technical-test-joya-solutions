using Microsoft.AspNetCore.Mvc;

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
}

public sealed record CorpoDeTeste(string Nome);
