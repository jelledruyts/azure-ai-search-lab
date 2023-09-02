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
    public int? TextEmbedderNumTokens { get; set; } // If unspecified, will use the default as configured in the text embedder Function App.
    public int? TextEmbedderTokenOverlap { get; set; } // If unspecified, will use the default as configured in the text embedder Function App.
    public int? TextEmbedderMinChunkSize { get; set; } // If unspecified, will use the default as configured in the text embedder Function App.
    public string? SearchServiceUrl { get; set; }
    public string? SearchServiceAdminKey { get; set; }
    public string? SearchIndexNameBlobDocuments { get; set; }
    public string? SearchIndexNameBlobChunks { get; set; }
    public int? SearchIndexerScheduleMinutes { get; set; } // If unspecified, will be set to 5 minutes.
    public string? InitialDocumentUrls { get; set; }
    public bool DisableUploadDocuments { get; set; } // If true, the Upload Documents functionality will be disabled.
    public bool DisableResetSearchConfiguration { get; set; } // If true, the Reset Search Configuration functionality will be disabled.
}