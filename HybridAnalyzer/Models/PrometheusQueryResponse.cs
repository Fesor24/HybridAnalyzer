using System.Text.Json.Serialization;

namespace HybridAnalyzer.Models;

internal sealed class PrometheusQueryResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; }
    [JsonPropertyName("data")]
    public PrometheusData Data { get; set; }
}

internal sealed class PrometheusData
{
    [JsonPropertyName("resultType")]
    public string ResultType {  get; set; }
    [JsonPropertyName("result")]
    public List<PrometheusResult> Result { get; set; }
}

internal sealed class PrometheusResult
{
    [JsonPropertyName("metric")]
    public Dictionary<string, string> Metric { get; set; }
    [JsonPropertyName("value")]
    public List<object> Value { get; set; }
}
