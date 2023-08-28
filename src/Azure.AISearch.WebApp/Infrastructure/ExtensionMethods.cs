namespace Azure.AISearch.WebApp.Infrastructure;

public static class ExtensionMethods
{
    public static string ToCountString<T>(this ICollection<T> value, string singular)
    {
        var plural = singular.EndsWith('y') ? singular.Substring(0, singular.Length - 1) + "ies" : singular + "s";
        return value.Count == 1 ? $"1 {singular}" : $"{value.Count} {plural}";
    }

    public static string ToScoreString(this double? value)
    {
        return value.HasValue ? value.Value.ToString("0.000000") : string.Empty;
    }
}