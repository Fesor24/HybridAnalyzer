using HybridAnalyzer.Graph;
using HybridAnalyzer.Models;

namespace HybridAnalyzer.Services;

internal sealed class StaticSmellDetectorService(Neo4jRepository repository)
{
    private readonly Neo4jRepository _repository = repository;
    private readonly int _chattyServiceThreshold = 3;

    public async Task DetectCyclicDependencies(List<ServiceDependency> dependencies)
    {
        // Clear the db
        await _repository.ClearGraphAsync();

        // Write the service dependencies to db
        await _repository.WriteDependenciesAsync(dependencies);
    }

    public async Task<List<ServiceFanOut>> DetectChattyServicesAsync() =>
        [.. (await _repository.GetServiceFanOutAsync())
        .Where(service => service.FanOut >= _chattyServiceThreshold)];
}
