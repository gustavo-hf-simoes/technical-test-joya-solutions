using System.Diagnostics;
using Joya.Pedidos.Api.Erros;
using Joya.Pedidos.Api.Saude;
using Joya.Pedidos.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// --- T-05.1: log estruturado, Problem Details, tratamento de exceção e Swagger ---
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(); // log estruturado em console (RNF-05)

builder.Services.AddControllers();
builder.Services.AddProblemDetails(opcoes => opcoes.CustomizeProblemDetails = contexto =>
    contexto.ProblemDetails.Extensions.TryAdd(
        "traceId",
        Activity.Current?.Id ?? contexto.HttpContext.TraceIdentifier));
builder.Services.AddExceptionHandler<ErroInternoExceptionHandler>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --- T-05.2: health checks (live sem verificações; ready consulta o SQL Server) ---
builder.Services.AddInfrastructure();
builder.Services.AddHealthChecks()
    .AddCheck<BancoDeDadosHealthCheck>("sqlserver", tags: [BancoDeDadosHealthCheck.Tag]);

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

app.MapControllers();

// --- T-05.2 ---
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registro => registro.Tags.Contains(BancoDeDadosHealthCheck.Tag),
});

app.Run();

// Necessário para WebApplicationFactory nos testes de integração.
public partial class Program;
