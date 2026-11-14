using Joya.Pedidos.Core.Produtos;
using Microsoft.AspNetCore.Mvc;

namespace Joya.Pedidos.Api.Controllers;

[ApiController]
[Route("produtos")]
[Produces("application/json")]
public sealed class ProdutosController(IProdutoService service) : ControllerBase
{
    /// <summary>Lista produtos ordenados por nome; `ativo` é opcional (spec 5.5, HL-18).</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ProdutoResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<ProdutoResponse>>> ListarAsync([FromQuery] bool? ativo, CancellationToken ct) =>
        Ok(await service.ListarAsync(ativo, ct));
}
