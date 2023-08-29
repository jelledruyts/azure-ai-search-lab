namespace Azure.AISearch.WebApp;

public class AppSettings
{
    public string? OpenAIEndpoint { get; set; }
    public string? OpenAIApiKey { get; set; }
    public string? OpenAIApiVersion { get; set; }
    public string? OpenAIEmbeddingDeployment { get; set; }
    public string? OpenAIGptDeployment { get; set; }
    public string? StorageAccountConnectionString { get; set; }
    public string? TextEmbedderFunctionEndpoint { get; set; }
    public string? TextEmbedderFunctionApiKey { get; set; }
    public string? SearchServiceUrl { get; set; }
    public string? SearchServiceAdminKey { get; set; }
    public string? InitialDocumentUrls { get; set; }
}