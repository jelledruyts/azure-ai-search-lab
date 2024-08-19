using System.Text.Json.Serialization;

namespace Azure.AISearch.FunctionApp.Models;

public class SkillResponseRecordData
{
    [JsonPropertyName("total_files")]
    public int TotalFiles { get; set; } = 1;
    
    [JsonPropertyName("num_unsupported_format_files")]
    public int NumUnsupportedFormatFiles { get; set; }

    [JsonPropertyName("num_files_with_errors")]
    public int NumFilesWithErrors { get; set; }

    [JsonPropertyName("skipped_chunks")]
    public int SkippedChunks { get; set; }
}