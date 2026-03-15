namespace HybridAnalyzer.Models;

internal sealed class PrometheusQueryResponse
{
    public PrometheusData Data { get; set; }
}

internal sealed class PrometheusData
{
    public List<PrometheusResult> Result { get; set; }
}

internal sealed class PrometheusResult
{
    public Dictionary<string, string> Metric { get; set; }

    public List<object> Value { get; set; }
}
