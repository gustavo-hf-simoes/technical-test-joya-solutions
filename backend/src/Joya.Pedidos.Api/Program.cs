using System.Diagnostics;
using FluentValidation;
using Joya.Pedidos.Api.Erros;
using Joya.Pedidos.Api.Limites;
using Joya.Pedidos.Api.Saude;
using Joya.Pedidos.Core.Pedidos;
using Joya.Pedidos.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// --- T-05.1: log estruturado, Problem Details, tratamento de exceção e Swagger ---
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(); // log estruturado em console (RNF-05)

builder.Services.AddControllers(mvc => mvc.Filters.Add<ValidationFilter>()) // T-06.6: RN-01 a RN-04 antes da action
    .ConfigureApiBehaviorOptions(opcoes =>
        opcoes.InvalidModelStateResponseFactory = contexto => ProblemasHttp.Validacao(ErrosDoModelState(contexto), contexto.HttpContext));

// --- T-06.6: casos de uso, validators e relógio (UTC, RN-13) ---
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IPedidoService, PedidoService>();
builder.Services.AddSingleton<IValidator<CriarPedidoRequest>, CriarPedidoValidator>();
builder.Services.AddProblemDetails(opcoes => opcoes.CustomizeProblemDetails = contexto =>
    contexto.ProblemDetails.Extensions.TryAdd(
        "traceId",
        Activity.Current?.Id ?? contexto.HttpContext.TraceIdentifier));
builder.Services.AddExceptionHandler<RequisicaoHttpInvalidaExceptionHandler>(); // T-05.3: 413/400 do servidor HTTP, antes do 500
builder.Services.AddExceptionHandler<ErroInternoExceptionHandler>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --- T-05.2: health checks (live sem verificações; ready consulta o SQL Server) ---
builder.Services.AddInfrastructure();
builder.Services.AddHealthChecks()
    .AddCheck<BancoDeDadosHealthCheck>("sqlserver", tags: [BancoDeDadosHealthCheck.Tag]);

// --- T-05.3: limites contra abuso (RNF-06, HL-12, ADR-0002) ---
// Corpo: limite global do Kestrel (cobre também corpo chunked) e filtro global que barra Content-Length acima
// do limite antes da leitura. Os dois saem como 413 pelo RequisicaoHttpInvalidaExceptionHandler.
builder.WebHost.ConfigureKestrel(kestrel => kestrel.Limits.MaxRequestBodySize = LimitesHttp.CorpoMaximoBytes);
builder.Services.Configure<MvcOptions>(mvc => mvc.Filters.Add<LimiteDeCorpoFilter>());

// IP real do cliente atrás do Nginx (e do Caddy no deploy): ForwardedLimit 1 no local, 2 no deploy (ADR-0002, item 3).
// Aceita qualquer proxy porque a porta da API nunca é publicada fora do loopback nem no deploy.
builder.Services.AddOptions<ForwardedHeadersOptions>()
    .Configure<IConfiguration>((opcoes, configuracao) =>
    {
        opcoes.ForwardedHeaders = ForwardedHeaders.XForwardedFor;
        opcoes.ForwardLimit = configuracao.GetValue("Proxy:ForwardedLimit", 1);
        opcoes.KnownNetworks.Clear();
        opcoes.KnownProxies.Clear();
    })
    .Validate(opcoes => opcoes.ForwardLimit is >= 1, "Proxy:ForwardedLimit deve ser maior ou igual a 1.")
    .ValidateOnStart();

// Taxa: janela fixa por IP só no POST /pedidos, com valores de configuração (HL-19).
builder.Services.AddOptions<LimiteDeTaxaOptions>()
    .BindConfiguration(LimiteDeTaxaOptions.Secao)
    .Validate(opcoes => opcoes.EhValido(), $"{LimiteDeTaxaOptions.Secao}: Permissoes e JanelaSegundos devem ser positivos.")
    .ValidateOnStart();
builder.Services.AddRateLimiter(opcoes =>
{
    opcoes.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    opcoes.AddPolicy<string, LimiteCriarPedidoPolicy>(LimitesHttp.PoliticaCriarPedido);
});

var app = builder.Build();

// --- T-05.1 ---
app.UseExceptionHandler();
app.UseStatusCodePages(); // 404/405/415 sem corpo também saem como Problem Details

// Swagger só em Development, Docker e no ambiente de teste de integração (spec 5.1, PROP-12).
if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Docker") || app.Environment.IsEnvironment("Testing"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// --- T-05.3 --- (ForwardedHeaders antes do rate limiter, que particiona pelo IP)
app.UseForwardedHeaders();
app.UseRateLimiter();

app.MapControllers();

// --- T-05.2 ---
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registro => registro.Tags.Contains(BancoDeDadosHealthCheck.Tag),
});

app.Run();

// JSON malformado, tipo errado e cabeçalho inválido saem com o mesmo 400 VALIDACAO do ValidationFilter (arquitetura, item 3).
static Dictionary<string, string[]> ErrosDoModelState(ActionContext contexto) =>
    contexto.ModelState
        .Where(par => par.Value is { Errors.Count: > 0 })
        .ToDictionary(
            par => par.Key,
            par => par.Value is null
                ? []
                : par.Value.Errors
                    .Select(e => string.IsNullOrEmpty(e.ErrorMessage) ? "Valor inválido." : e.ErrorMessage)
                    .ToArray());

// Necessário para WebApplicationFactory nos testes de integração.
public partial class Program;
