using System.Text.Json.Serialization;

namespace Azure.AISearch.FunctionApp.Models;

public class SkillRequestRecord
{
    [JsonPropertyName("recordId")]
    public string? RecordId { get; set; }

    [JsonPropertyName("data")]
    public SkillRequestRecordData Data { get; set; } = new SkillRequestRecordData();
}