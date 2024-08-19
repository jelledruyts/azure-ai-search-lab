using System.Text.Json.Serialization;

namespace Azure.AISearch.FunctionApp.Models;

public class SkillRequest
{
    [JsonPropertyName("values")]
    public IList<SkillRequestRecord> Values { get; set; } = new List<SkillRequestRecord>();
}