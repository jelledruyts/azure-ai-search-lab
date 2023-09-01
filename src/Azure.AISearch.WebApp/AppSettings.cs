namespace Azure.AISearch.WebApp;

public class AppSettings
{
    public string? OpenAIEndpoint { get; set; }
    public string? OpenAIApiKey { get; set; }
    public string? OpenAIApiVersion { get; set; }
    public string? OpenAIEmbeddingDeployment { get; set; }
    public int? OpenAIEmbeddingVectorDimensions { get; set; }
    public string? OpenAIGptDeployment { get; set; }
    public string? StorageAccountConnectionString { get; set; }
    public string? StorageContainerNameBlobDocuments { get; set; }
    public string? StorageContainerNameBlobChunks { get; set; }
    public string? TextEmbedderFunctionEndpoint { get; set; }
    public string? TextEmbedderFunctionApiKey { get; set; }
    public int? TextEmbedderNumTokens { get; set; }
    public int? TextEmbedderTokenOverlap { get; set; }
    public int? TextEmbedderMinChunkSize { get; set; }
    public string? SearchServiceUrl { get; set; }
    public string? SearchServiceAdminKey { get; set; }
    public string? SearchIndexNameBlobDocuments { get; set; }
    public string? SearchIndexNameBlobChunks { get; set; }
    public int? SearchIndexerScheduleMinutes { get; set; }
    public string? InitialDocumentUrls { get; set; }
}