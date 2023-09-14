using Newtonsoft.Json;

namespace Azure.AISearch.FunctionApp.Models;

public class SkillResponseRecordData
{
    [JsonProperty("total_files")]
    public int TotalFiles { get; set; } = 1;
    
    [JsonProperty("num_unsupported_format_files")]
    public int NumUnsupportedFormatFiles { get; set; }

    [JsonProperty("num_files_with_errors")]
    public int NumFilesWithErrors { get; set; }

    [JsonProperty("skipped_chunks")]
    public int SkippedChunks { get; set; }
}