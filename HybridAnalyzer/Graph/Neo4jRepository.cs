using HybridAnalyzer.Config;
using HybridAnalyzer.Models;
using Neo4j.Driver;

namespace HybridAnalyzer.Graph;

internal class Neo4jRepository
{
    private readonly IDriver _driver;
    private readonly Neo4jConfig _config;

    public Neo4jRepository(Neo4jConfig config)
    {
        _config = config;

        _driver = GraphDatabase.Driver(
            config.Uri,
            AuthTokens.Basic(config.Username, config.Password));
    }

    private IAsyncSession GetSession() =>
        _driver.AsyncSession(o => o.WithDatabase(_config.Database));

    public async Task ClearGraphAsync()
    {
        await using var session = GetSession();

        await session.ExecuteWriteAsync(async tx =>
        {
            await tx.RunAsync("MATCH (n) DETACH DELETE n");
        });
    }

    public async Task WriteDependenciesAsync(List<ServiceDependency> dependencies)
    {
        await using var session = GetSession();

        foreach (var service in dependencies)
        {
            foreach (var dependency in service.Dependencies)
            {
                var query = @"
                MERGE (s:Service {name:$service})
                MERGE (d:Service {name:$dependency})
                MERGE (s)-[:DEPENDS_ON]->(d)";

                await session.ExecuteWriteAsync(async tx =>
                {
                    await tx.RunAsync(query, new
                    {
                        service = service.ServiceName,
                        dependency
                    });
                });
            }
        }
    }

    public async Task<List<ServiceFanOut>> GetServiceFanOutAsync()
    {
        var result = new List<ServiceFanOut>();

        await using var session = GetSession();

        var query = @"
        MATCH (s:Service)-[r:DEPENDS_ON]->()
        RETURN s.name AS service, count(r) AS fanOut
    ";

        var cursor = await session.RunAsync(query);

        await foreach (var record in cursor)
        {
            result.Add(new ServiceFanOut(record["service"].As<string>(),
                record["fanOut"].As<int>()));
        }

        return result;
    }
}

