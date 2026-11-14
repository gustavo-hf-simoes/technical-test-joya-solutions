using System.Diagnostics;
using Joya.Pedidos.Api.Erros;

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

app.Run();

// Necessário para WebApplicationFactory nos testes de integração.
public partial class Program;
