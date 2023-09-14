using Newtonsoft.Json;

namespace Azure.AISearch.FunctionApp.Models;

public class SkillRequest
{
    [JsonProperty("values")]
    public IList<SkillRequestRecord> Values { get; set; } = new List<SkillRequestRecord>();
}