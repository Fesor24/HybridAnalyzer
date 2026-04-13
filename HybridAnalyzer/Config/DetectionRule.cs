namespace HybridAnalyzer.Config;

internal class DetectionRule
{
    public int FanOutThreshold {  get; set; }
    public int CycleRateThreshold { get; set; }
    public double RequestCountThreshold { get; set; }
}
