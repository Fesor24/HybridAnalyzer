namespace HybridAnalyzer.Models;

internal sealed record ServiceDependency(
    string ServiceName,
    List<string> Dependencies
    );
