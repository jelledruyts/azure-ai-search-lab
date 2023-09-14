using Newtonsoft.Json;

namespace Azure.AISearch.FunctionApp.Models;

public class SkillResponseMessage
{
    [JsonProperty("message")]
    public string? Message { get; set; }
}