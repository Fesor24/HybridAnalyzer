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

Neo4jConfig neo4jConfig = new();

config.GetSection("Neo4J")
    .Bind(neo4jConfig);

DetectionRule detectionRule = new();
config.GetSection("DetectionRule")
    .Bind(detectionRule);

string microServicesRootDirectory = config.GetSection("MicroServicesRootDirectory").Get<string>() ?? "";

var serviceDependencies = DependencyExtractor
        .ReadServiceDependencies(microServicesRootDirectory);

Neo4jRepository graphRepository = new(neo4jConfig);

Dictionary<string, string> portToServiceMap = config
    .GetSection("PortToServiceMap").Get<Dictionary<string, string>>() ?? [];

WriteLine("Press 'Y' to start detection");

string userInput = ReadLine();

while(userInput != null && 
    userInput.Equals("Y", StringComparison.CurrentCultureIgnoreCase))
{
    WriteLine("Detecting...");

    await graphRepository.ClearGraphAsync();

    await Detect();

    WriteLine("Detection complete. Press 'Y' to start new detection");
    
    userInput = ReadLine();
}


async Task Detect()
{
    HttpClient client = new();

    string prometheusBaseUri = config.GetSection("PrometheusUri").Get<string>() ?? "";

    var prometheusClient = new PrometheusMetricsClient(client, prometheusBaseUri, portToServiceMap);

    var metrics = await prometheusClient.GetRequestCountsAsync();

    var detector = new HybridSmellDetector(detectionRule);

    var chattyServices = detector.DetectChattyServices(serviceDependencies, metrics);

    await graphRepository.WriteServicesAsync(chattyServices);

    var cyclicDeps = detector.DetectOperationalCycles(serviceDependencies, metrics);

    if(cyclicDeps.Count >= detectionRule.CycleRateThreshold)
        await graphRepository.WriteMutualDependenciesAsync(cyclicDeps);
}



