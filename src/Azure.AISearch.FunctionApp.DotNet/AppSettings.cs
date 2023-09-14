namespace Azure.AISearch.FunctionApp;

public class AppSettings
{
    public string? OpenAIEndpoint { get; set; }
    public string? OpenAIApiKey { get; set; }
    public string? OpenAIEmbeddingDeployment { get; set; }
    public string? TextEmbedderFunctionApiKey { get; set; }
    public int? TextEmbedderNumTokens { get; set; }
    public int? TextEmbedderTokenOverlap { get; set; }
    public int? TextEmbedderMinChunkSize { get; set; }
    public string? SearchServiceUrl { get; set; }
    public string? SearchServiceAdminKey { get; set; }
    public string? SearchIndexNameBlobChunks { get; set; }
}