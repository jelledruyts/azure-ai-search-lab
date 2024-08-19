using System.Text.Json.Serialization;

namespace Azure.AISearch.FunctionApp.Models;

public class SkillResponse
{
    [JsonPropertyName("values")]
    public IList<SkillResponseRecord> Values { get; set; } = new List<SkillResponseRecord>();
}