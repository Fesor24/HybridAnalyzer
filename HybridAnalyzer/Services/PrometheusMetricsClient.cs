using System.Text.Json;
using HybridAnalyzer.Models;

namespace HybridAnalyzer.Services;

internal sealed class PrometheusMetricsClient
{
    private readonly HttpClient _httpClient;
    private Dictionary<string, string> _portToServiceMap;

    public PrometheusMetricsClient(HttpClient httpClient, string prometheusBaseUri, 
        Dictionary<string, string> portToServiceMap)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(prometheusBaseUri);
        _portToServiceMap = portToServiceMap;
    }

    public async Task<List<ServiceInteractionMetric>> GetRequestCountsAsync()
    {
        string query = "sum by (service_name, server_port) (\r\n  increase(http_client_request_duration_seconds_count{server_port!=\"4317\"}[3m])\r\n)";

        string url = $"/api/v1/query?query={Uri.EscapeDataString(query)}";

        var response = await _httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine(response.ReasonPhrase);
            throw new Exception("Failed to query Prometheus");
        }

        var json = await response.Content.ReadAsStringAsync();

        var result = JsonSerializer.Deserialize<PrometheusQueryResponse>(json);

        return ParseMetrics(result);
    }

    private List<ServiceInteractionMetric> ParseMetrics(
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
