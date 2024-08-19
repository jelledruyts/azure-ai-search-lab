using System.Text.Json.Serialization;

namespace Azure.AISearch.FunctionApp.Models;

public class SkillRequestRecordData
{
    [JsonPropertyName("document_id")]
    public string? DocumentId { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }
    
    [JsonPropertyName("filepath")]
    public string? FilePath { get; set; }
    
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("fieldname")]
    public string? FieldName { get; set; }

    [JsonPropertyName("num_tokens")]
    public decimal? NumTokens { get; set; }

    [JsonPropertyName("token_overlap")]
    public decimal? TokenOverlap { get; set; }

    [JsonPropertyName("min_chunk_size")]
    public decimal? MinChunkSize { get; set; }

    [JsonPropertyName("embedding_deployment_name")]
    public string? EmbeddingDeploymentName { get; set; }
}