using System.Text.Json;
using HybridAnalyzer.Models;

namespace HybridAnalyzer.Services;

internal sealed class PrometheusMetricsClient
{
    private readonly HttpClient _httpClient;

    public PrometheusMetricsClient(HttpClient httpClient, string prometheusBaseUri)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(prometheusBaseUri);
    }

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
        string query = "avg_over_time( sum by (service_name, server_port) ( increase(http_client_request_duration_seconds_count{server_port!=\"4317\"}[1m]) )[5m:])";

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

            bool portExist = _portToServiceMap
                .TryGetValue(item.Metric["server_port"], out var targetService);

            // In this case not a service port
            if (!portExist) continue;

            JsonElement metricVal = (JsonElement)(item.Value[1]);

            double value = double.Parse(metricVal.GetString() ?? "0");

            metrics.Add(new ServiceInteractionMetric
            (
                source.ToLower().Replace("-", ""),
                targetService!.ToLower().Replace("-", ""),
                value
            ));
        }

        return metrics;
    }
}
