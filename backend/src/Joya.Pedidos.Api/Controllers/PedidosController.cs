using Joya.Pedidos.Api.Erros;
using Joya.Pedidos.Api.Limites;
using Joya.Pedidos.Core.Common;
using Joya.Pedidos.Core.Pedidos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Joya.Pedidos.Api.Controllers;

[ApiController]
[Route("pedidos")]
[Produces("application/json")]
public sealed class PedidosController(IPedidoService service, IPedidoConsultaService consultas) : ControllerBase
{
    /// <summary>Cria um pedido com itens, debitando o estoque de forma atômica (spec 5.2).</summary>
    [HttpPost]
    [EnableRateLimiting(LimitesHttp.PoliticaCriarPedido)]
    [ProducesResponseType<PedidoResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> CriarAsync([FromBody] CriarPedidoRequest request, CancellationToken ct)
    {
        var resultado = await service.CriarAsync(request, ct);

        // Location relativo, como na spec 5.2. O GET /pedidos/{id} chega na T-11.1; até lá não há action para o CreatedAtAction.
        return resultado.Match<IActionResult>(
            pedido => Created($"/pedidos/{pedido.Id}", pedido),
            erro => erro.ToProblem(HttpContext));
    }

    /// <summary>Lista pedidos com filtros opcionais por cliente e período, paginados (spec 5.3).</summary>
    [HttpGet]
    [ProducesResponseType<PaginaResponse<PedidoResumoResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ListarAsync([FromQuery] ListarPedidosQuery query, CancellationToken ct)
    {
        var resultado = await consultas.ListarAsync(query, ct);

        return resultado.Match<IActionResult>(
            pagina => Ok(pagina),
            erro => erro.ToProblem(HttpContext));
    }
}
