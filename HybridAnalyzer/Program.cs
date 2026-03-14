using HybridAnalyzer.Analyzers;
using HybridAnalyzer.Config;
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

Read();