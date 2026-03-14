using HybridAnalyzer.Config;
using HybridAnalyzer.Extractors;
using HybridAnalyzer.Graph;
using HybridAnalyzer.Models;
using HybridAnalyzer.Services;
using static System.Console;

namespace HybridAnalyzer.Analyzers;

internal static class StaticArchitectureAnalyzer
{
    internal static async Task Detect(MicroServiceConfig microServiceConfig,
        Neo4jConfig neo4jConfig)
    {
        var serviceDependencies = DependencyExtractor
        .ReadServiceDependencies(microServiceConfig);

        var repo = new Neo4jRepository(neo4jConfig);
        var graphService = new StaticSmellDetectorService(repo);

        await graphService.DetectCyclicDependencies(serviceDependencies);

        List<ServiceFanOut> chattyServices = await graphService.DetectChattyServicesAsync();

        if (chattyServices.Count == 0)
            WriteLine("No potential chatty service detected");

        else
        {
            WriteLine("Potential Chatty Services: ");
            foreach (ServiceFanOut service in chattyServices)
                WriteLine($"Service: {service.Service} Fanout metric: {service.FanOut} \n");
        }
    }
}
