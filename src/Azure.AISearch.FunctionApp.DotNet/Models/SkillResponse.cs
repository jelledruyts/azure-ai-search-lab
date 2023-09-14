using Newtonsoft.Json;

namespace Azure.AISearch.FunctionApp.Models;

public class SkillResponse
{
    [JsonProperty("values")]
    public IList<SkillResponseRecord> Values { get; set; } = new List<SkillResponseRecord>();
}