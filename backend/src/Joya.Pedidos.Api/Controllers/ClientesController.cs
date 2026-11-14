using Joya.Pedidos.Core.Clientes;
using Microsoft.AspNetCore.Mvc;

namespace Joya.Pedidos.Api.Controllers;

[ApiController]
[Route("clientes")]
[Produces("application/json")]
public sealed class ClientesController(IClienteService service) : ControllerBase
{
    /// <summary>Lista clientes ordenados por nome; `ativo` é opcional (spec 5.5, HL-18).</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ClienteResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<ClienteResponse>>> ListarAsync([FromQuery] bool? ativo, CancellationToken ct) =>
        Ok(await service.ListarAsync(ativo, ct));
}
