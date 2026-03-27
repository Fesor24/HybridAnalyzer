using HybridAnalyzer.Config;
using HybridAnalyzer.Models;

namespace HybridAnalyzer.Detection;
internal sealed class HybridSmellDetector(DetectionRule detectionRule)
{
    public List<string> DetectChattyServices(
        List<ServiceDependency> dependencies,
        List<ServiceInteractionMetric> metrics)
    {
        var chattyServices = new HashSet<string>();

        foreach (var service in dependencies)
        {
            int staticFanOut = service.Dependencies.Count;

            int dynamicFanOut = metrics
                .Where(m => m.SourceService == service.ServiceName)
                .Select(m => m.TargetService)
                .Distinct()
                .Count();

            // Structural chatty
            if (staticFanOut >= detectionRule.FanOutThreshold || 
                dynamicFanOut >= detectionRule.FanOutThreshold)
                chattyServices.Add(service.ServiceName);

            // Behavioural chatty
            double totalRequestRate = metrics
                .Where(m => m.SourceService == service.ServiceName) // TODO: Confirm if name tallies here
                .Sum(m => m.RequestRate);

            if (totalRequestRate > detectionRule.RateThreshold)
                chattyServices.Add(service.ServiceName);
        }

        return chattyServices.ToList();
    }

    public List<(string A, string B)> DetectOperationalCycles(
        List<ServiceDependency> dependencies, 
        List<ServiceInteractionMetric> metrics)
    {
        var confirmedCycles = new HashSet<(string, string)>();

        foreach(var service in dependencies)
        {
            foreach(var dep in service.Dependencies)
            {
                var reverse = dependencies
                    .FirstOrDefault(d => d.ServiceName == dep);

                if (reverse != null && 
                    reverse.Dependencies.Contains(service.ServiceName))
                {
                    if(ConfirmCycle(service.ServiceName, dep, metrics))
                    {
                        var ordered = OrderPair(service.ServiceName, dep);
                        confirmedCycles.Add(ordered);
                    }
                }
            }
        }

        return confirmedCycles.ToList();
    }

    private static bool ConfirmCycle(string serviceA, string serviceB,
     List<ServiceInteractionMetric> metrics)
    {
        bool aToB = metrics.Any(m =>
            m.SourceService == serviceA &&
            m.TargetService == serviceB);

        bool bToA = metrics.Any(m =>
            m.SourceService == serviceB &&
            m.TargetService == serviceA);

        return aToB && bToA;
    }

    private static (string, string) OrderPair(string a, string b) =>
        string.CompareOrdinal(a, b) < 0 ? (a, b) : (b, a);
}
