using System.Text.Json.Serialization;

namespace Azure.AISearch.FunctionApp.Models;

public class SkillResponseRecord
{
    [JsonPropertyName("recordId")]
    public string? RecordId { get; set; }

    [JsonPropertyName("data")]
    public SkillResponseRecordData Data { get; set; } = new SkillResponseRecordData();

    [JsonPropertyName("errors")]
    public IList<SkillResponseMessage> Errors { get; set; } = new List<SkillResponseMessage>();

    [JsonPropertyName("warnings")]
    public IList<SkillResponseMessage> Warnings { get; set; } = new List<SkillResponseMessage>();
}