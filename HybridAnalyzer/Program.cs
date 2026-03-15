using HybridAnalyzer.Analyzers;
using HybridAnalyzer.Config;
using HybridAnalyzer.Detection;
using HybridAnalyzer.Extractors;
using HybridAnalyzer.Services;
using Microsoft.Extensions.Configuration;
using static System.Console;

IConfigurationBuilder builder = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", false, true);

IConfigurationRoot config = builder.Build();

MicroServiceConfig microServiceConfig = new();

config.GetSection("MicroServicesConfig")
    .Bind(microServiceConfig);

Neo4jConfig neo4jConfig = new();

config.GetSection("Neo4J")
    .Bind(neo4jConfig);

await StaticArchitectureAnalyzer.Detect(microServiceConfig, neo4jConfig);

var serviceDependencies = DependencyExtractor
        .ReadServiceDependencies(microServiceConfig);

HttpClient client = new();

var prometheusClient = new PrometheusMetricsClient(client);

var metrics = await prometheusClient.GetRequestRatesAsync();

var detector = new HybridSmellDetector();

var chattyServices = detector.DetectChattyServices(serviceDependencies, metrics);

WriteLine("Chatty services:");

foreach (var service in chattyServices)
{
    WriteLine(service);
}



Read();