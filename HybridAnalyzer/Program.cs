using HybridAnalyzer.Config;
using HybridAnalyzer.Detection;
using HybridAnalyzer.Extractors;
using HybridAnalyzer.Graph;
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

DetectionRule detectionRule = new();
config.GetSection("DetectionRule")
    .Bind(detectionRule);

var serviceDependencies = DependencyExtractor
        .ReadServiceDependencies(microServiceConfig);

Neo4jRepository graphRepository = new(neo4jConfig);

WriteLine("Press 'Y' to start detection");

string userInput = ReadLine();

while(userInput != null && 
    userInput.Equals("Y", StringComparison.CurrentCultureIgnoreCase))
{
    await graphRepository.ClearGraphAsync();

    await Detect();

    WriteLine("Press 'Y' to start detection");

    userInput = ReadLine();
}


async Task Detect()
{
    HttpClient client = new();

    string prometheusBaseUri = "http://localhost:9090";

    var prometheusClient = new PrometheusMetricsClient(client, prometheusBaseUri);

    var metrics = await prometheusClient.GetRequestCountsAsync();

    var detector = new HybridSmellDetector(detectionRule);

    var chattyServices = detector.DetectChattyServices(serviceDependencies, metrics);

    await graphRepository.WriteServicesAsync(chattyServices);

    //WriteLine("Chatty services:");

    //foreach (var service in chattyServices)
    //{
    //    WriteLine(service);
    //}

    var cyclicDeps = detector.DetectOperationalCycles(serviceDependencies, metrics);

    if(cyclicDeps.Count > 0)
        await graphRepository.WriteMutualDependenciesAsync(cyclicDeps);

    //WriteLine("Cyclic deps: ");

    //foreach (var (a, b) in cyclicDeps)
    //{
    //    WriteLine($"Dependency exists between services: {a} and {b}");
    //}
}



