namespace HybridAnalyzer.Extensions;

internal static class StringExtensions
{
    extension(string)
    {
        public static string CustomNormalize(string value) => value
            .Replace(" ", "")
            .ToLower();
    }
}
