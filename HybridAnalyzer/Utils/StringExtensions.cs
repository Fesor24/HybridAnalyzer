namespace HybridAnalyzer.Utils;

internal static class StringExtensions
{
    extension(string)
    {
        public static string TrimAndToLower(string value) => value
            .Replace(" ", "")
            .ToLower();
    }
}
