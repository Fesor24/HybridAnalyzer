using System.Text.Json;
using HybridAnalyzer.Models;

namespace HybridAnalyzer.Services;

internal sealed class PrometheusMetricsClient(HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<List<ServiceInteractionMetric>> GetRequestRatesAsync()
    {
        string query = "rate(http_requests_total[1m])";

        string url = $"/api/v1/query?query={Uri.EscapeDataString(query)}";

        var response = await _httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
            throw new Exception("Failed to query Prometheus");

        var json = await response.Content.ReadAsStringAsync();

        var result = JsonSerializer.Deserialize<PrometheusQueryResponse>(json);

        return ParseMetrics(result);
    }

    private static List<ServiceInteractionMetric> ParseMetrics(
        PrometheusQueryResponse response)
    {
        var metrics = new List<ServiceInteractionMetric>();

        foreach (var item in response.Data.Result)
        {
            var source = item.Metric["source_service"];
            var target = item.Metric["target_service"];

            double value = double.Parse((string)item.Value[1]);

            metrics.Add(new ServiceInteractionMetric
            (
                source.ToLower(),
                target.ToLower(),
                value
            ));
        }

        return metrics;
    }
}
