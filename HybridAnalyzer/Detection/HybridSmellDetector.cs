using HybridAnalyzer.Models;

namespace HybridAnalyzer.Detection;
internal sealed class HybridSmellDetector
{
    public List<string> DetectChattyServices(
        List<ServiceDependency> dependencies,
        List<ServiceInteractionMetric> metrics)
    {
        var chattyServices = new List<string>();

        foreach (var service in dependencies)
        {
            int fanOut = service.Dependencies.Count;

            if (fanOut < 4)
                continue;

            double totalRequestRate = metrics
                .Where(m => m.SourceService == service.ServiceName)
                .Sum(m => m.RequestRate);

            if (totalRequestRate > 100)
            {
                chattyServices.Add(service.ServiceName);
            }
        }

        return chattyServices;
    }

    public List<(string A, string B)> DetectOperationalCycles(
        List<ServiceDependency> dependencies, 
        List<ServiceInteractionMetric> metrics)
    {
        List<(string, string)> confirmedCycles = [];

        foreach(var service in dependencies)
        {

        }
    }

    private bool ConfirmCycle(string serviceA, string serviceB,
        List<ServiceInteractionMetric> metrics)
    {
        bool aToB = metrics.Any(m => 
            m.SourceService == serviceA &&
            m.TargetService == serviceB &&
            m.RequestRate > 1);

        bool bToA = metrics.Any(m => 
            m.SourceService == serviceB &&
            m.TargetService == serviceA &&
            m.RequestRate > 1);

        return aToB && bToA;
    }
}
