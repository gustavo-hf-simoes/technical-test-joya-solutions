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

        // Location relativo, como na spec 5.2 (aponta para ObterPorIdAsync). Mantém o Created com URL relativa, não o helper por nome de action.
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

    /// <summary>Obtém um pedido com os itens e os preços praticados (spec 5.2).</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<PedidoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObterPorIdAsync(int id, CancellationToken ct)
    {
        var resultado = await consultas.ObterPorIdAsync(id, ct);

        return resultado.Match<IActionResult>(
            pedido => Ok(pedido),
            erro => erro.ToProblem(HttpContext));
    }
}
