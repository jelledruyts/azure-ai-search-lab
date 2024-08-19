using System.Text.Json.Serialization;

namespace Azure.AISearch.FunctionApp.Models;

public class SkillResponseMessage
{
    [JsonPropertyName("message")]
    public string? Message { get; set; }
}