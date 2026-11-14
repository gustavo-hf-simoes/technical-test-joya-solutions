var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();

var app = builder.Build();
app.MapControllers();
app.Run();

// Necessário para WebApplicationFactory nos testes de integração.
public partial class Program;
