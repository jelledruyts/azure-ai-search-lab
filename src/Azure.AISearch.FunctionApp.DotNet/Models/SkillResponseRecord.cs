using Newtonsoft.Json;

namespace Azure.AISearch.FunctionApp.Models;

public class SkillResponseRecord
{
    [JsonProperty("recordId")]
    public string? RecordId { get; set; }

    [JsonProperty("data")]
    public SkillResponseRecordData Data { get; set; } = new SkillResponseRecordData();

    [JsonProperty("errors")]
    public IList<SkillResponseMessage> Errors { get; set; } = new List<SkillResponseMessage>();

    [JsonProperty("warnings")]
    public IList<SkillResponseMessage> Warnings { get; set; } = new List<SkillResponseMessage>();
}