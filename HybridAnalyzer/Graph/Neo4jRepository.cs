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

    public async Task WriteMutualDependenciesAsync(List<(string A, string B)> dependencies)
    {
        await using var session = GetSession();

        foreach (var (a, b) in dependencies)
        {
            var query = @"
            MERGE (nodeA:Service {name:$a})
            MERGE (nodeB:Service {name:$b})
            MERGE (nodeA)-[:DEPENDS_ON]->(nodeB)
            MERGE (nodeB)-[:DEPENDS_ON]->(nodeA)";

            await session.ExecuteWriteAsync(async tx =>
            {
                await tx.RunAsync(query, new
                {
                    a,
                    b
                });
            });
        }
    }

    public async Task WriteServicesAsync(List<string> services)
    {
        await using var session = GetSession();

        foreach (var service in services)
        {
            var query = @"
            MERGE (s:Service {name:$serviceName})
            SET s.source = 'chatty'"; // mark nodes as chatty services

            await session.ExecuteWriteAsync(async tx =>
            {
                await tx.RunAsync(query, new { serviceName = service });
            });
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

