using System.Text.Json;
using System.Text.Json.Serialization;

namespace Azure.AISearch.WebApp.Infrastructure;

public static class JsonConfiguration
{
    public static readonly JsonSerializerOptions DefaultJsonOptions = ApplyDefaultConfiguration(new JsonSerializerOptions());

    public static JsonSerializerOptions ApplyDefaultConfiguration(JsonSerializerOptions options)
    {
        options.WriteIndented = true; // Write with indentation for increased readability
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase; // Write with camel casing
        options.PropertyNameCaseInsensitive = true; // Ignore case when reading
        options.Converters.Add(new JsonStringEnumConverter()); // Serialize enums as their string representation instead of their integer values
        return options;
    }
}