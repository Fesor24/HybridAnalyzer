using System.Text.Json;
using HybridAnalyzer.Models;

namespace HybridAnalyzer.Services;

internal sealed class PrometheusMetricsClient(HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient;

    private static Dictionary<string, string> _portToServiceMap = new()
    {
        ["5001"] = "inventory-service",
        ["5002"] = "logger-service",
        ["5003"] = "notification-service",
        ["5004"] = "product-service",
        ["5005"] = "recommendation-service",
        ["5006"] = "search-service",
    };

    public async Task<List<ServiceInteractionMetric>> GetRequestRatesAsync()
    {
        string query = "avg_over_time( sum by (service_name, server_port) ( rate(http_client_request_duration_seconds_count{server_port!=\"4317\"}[1m]) )[5m:])";

        string url = $"/api/v1/query?query={Uri.EscapeDataString(query)}";

        var response = await _httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
            throw new Exception("Failed to query Prometheus");

        var json = await response.Content.ReadAsStringAsync();

        var result = JsonSerializer.Deserialize<PrometheusQueryResponse>(json);

        return ParseMetrics(result);
    }

    private static List<ServiceInteractionMetric> ParseMetrics(
        PrometheusQueryResponse? response)
    {
        var metrics = new List<ServiceInteractionMetric>();

        if (response is null) return metrics;

        foreach (var item in response.Data.Result)
        {
            var source = item.Metric["service_name"];
            var target = _portToServiceMap[item.Metric["server_port"]];

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
