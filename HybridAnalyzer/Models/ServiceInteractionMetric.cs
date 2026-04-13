namespace HybridAnalyzer.Models;

internal sealed record ServiceInteractionMetric(
    string SourceService,
    string TargetService,
    double RequestCount
    );
