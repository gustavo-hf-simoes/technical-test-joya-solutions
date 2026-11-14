using Joya.Pedidos.Api.Erros;
using Joya.Pedidos.Core.Relatorios;
using Microsoft.AspNetCore.Mvc;

namespace Joya.Pedidos.Api.Controllers;

[ApiController]
[Route("relatorios")]
[Produces("application/json")]
public sealed class RelatoriosController(IRelatorioService service) : ControllerBase
{
    /// <summary>Os 5 clientes com maior valor total no período, agrupados por cliente (spec 5.4, RN-17).</summary>
    [HttpGet("top-clientes")]
    [ProducesResponseType<IReadOnlyList<TopClienteResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> TopClientesAsync([FromQuery] TopClientesQuery query, CancellationToken ct)
    {
        var resultado = await service.TopClientesAsync(query, ct);

        return resultado.Match<IActionResult>(
            ranking => Ok(ranking),
            erro => erro.ToProblem(HttpContext));
    }
}
