using Newtonsoft.Json;

namespace Azure.AISearch.FunctionApp.Models;

public class SkillRequestRecordData
{
    [JsonProperty("document_id")]
    public string? DocumentId { get; set; }

    [JsonProperty("text")]
    public string? Text { get; set; }
    
    [JsonProperty("filepath")]
    public string? FilePath { get; set; }
    
    [JsonProperty("title")]
    public string? Title { get; set; }

    [JsonProperty("fieldname")]
    public string? FieldName { get; set; }

    [JsonProperty("num_tokens")]
    public int? NumTokens { get; set; }

    [JsonProperty("token_overlap")]
    public int? TokenOverlap { get; set; }

    [JsonProperty("min_chunk_size")]
    public int? MinChunkSize { get; set; }

    [JsonProperty("embedding_deployment_name")]
    public string? EmbeddingDeploymentName { get; set; }
}