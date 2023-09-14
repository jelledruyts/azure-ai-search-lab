using Newtonsoft.Json;

namespace Azure.AISearch.FunctionApp.Models;

public class SkillRequestRecord
{
    [JsonProperty("recordId")]
    public string? RecordId { get; set; }

    [JsonProperty("data")]
    public SkillRequestRecordData Data { get; set; } = new SkillRequestRecordData();
}