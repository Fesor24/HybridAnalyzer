using System.Text.Json;
using System.Text.RegularExpressions;
using HybridAnalyzer.Config;
using HybridAnalyzer.Extensions;
using HybridAnalyzer.Models;

namespace HybridAnalyzer.Extractors;

internal sealed class DependencyExtractor
{
    public static List<ServiceDependency> ReadServiceDependencies(MicroServiceConfig config)
    {
        // Get the directories (the service folders)
        var serviceDirectories = Directory.GetDirectories(config.RootDirectory);

        // Get the file names from the service directories
        var services = serviceDirectories
            .Select(Path.GetFileName)
            .Select(s => s.ToLower())
            .ToList();

        //Console.WriteLine("Discovered services:");
        //foreach (var s in services)
        //{
        //    Console.WriteLine($" - {s}");
        //}

        //Console.WriteLine("\nDeclared dependencies:\n");

        // Regex to detect URLs
        var urlRegex = new Regex(@"https?:\/\/([a-zA-Z0-9\-\.]+)",
            RegexOptions.IgnoreCase);

        List<ServiceDependency> serviceDependencies = [];

        // Scan each service
        foreach (var folder in serviceDirectories)
        {
            // Get path for the service
            var serviceName = Path.GetFileName(folder).ToLower();

            ServiceDependency serviceDependency = new(serviceName, []);

            // Set the path for the appsettings for the respective service
            var configPath = Path.Combine(folder, "appsettings.json");

            //Console.WriteLine(configPath);

            // If the file does not exist, we continue iteration
            if (!File.Exists(configPath))
                continue;

            // Read the contents of appsettings
            var json = File.ReadAllText(configPath);

            //Console.WriteLine(json);

            // Parse the json document
            using var document = JsonDocument.Parse(json);

            // Extracts all the values
            var keyValues = ExtractAllKeyValues(document.RootElement);

            var dependencies = new HashSet<string>();

            foreach (var (key, value) in keyValues)
            {
                var normalizedKey = string.CustomNormalize(key);

                foreach (var knownService in services)
                {
                    var normalizedService = string.CustomNormalize(knownService);

                    if (normalizedKey.Contains(normalizedService) &&
                        knownService != serviceName)
                    {
                        dependencies.Add(knownService);
                        serviceDependency.Dependencies.Add(knownService);
                    }
                }
            }

            serviceDependencies.Add(serviceDependency);

            //Console.WriteLine($"{serviceName}");

            //if (dependencies.Count == 0)
            //{
            //    Console.WriteLine("   No declared dependencies\n");
            //    continue;
            //}

            //foreach (var dep in dependencies)
            //{
            //    Console.WriteLine($"   -> {dep}");
            //}

            //Console.WriteLine();
        }

        return serviceDependencies;
    }

    private static List<(string Key, string Value)> ExtractAllKeyValues(JsonElement element)
    {
        var values = new List<(string Key, string Value)>();

        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (property.Value.ValueKind == JsonValueKind.String)
                {
                    values.Add((property.Name, property.Value.GetString()));
                }

                values.AddRange(ExtractAllKeyValues(property.Value));
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                values.AddRange(ExtractAllKeyValues(item));
            }
        }

        return values;
    }

}
