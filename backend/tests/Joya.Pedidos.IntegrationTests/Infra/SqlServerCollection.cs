// Um contêiner por execução e testes em série (ADR-0003, item 5). Os cenários de
// concorrência (CA-10, CA-37) fazem o próprio paralelismo com Task.WhenAll.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Joya.Pedidos.IntegrationTests.Infra;

/// <summary>Coleção única que compartilha o <see cref="SqlServerFixture"/> entre as classes de teste.</summary>
[CollectionDefinition(Nome)]
public sealed class SqlServerCollection : ICollectionFixture<SqlServerFixture>
{
    public const string Nome = "SqlServer";
}
