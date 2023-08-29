namespace Azure.AISearch.WebApp.Models;

public class SearchScenario
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? DocumentationUrl { get; set; }
    public SearchRequest SearchRequest { get; set; } = new SearchRequest();

    public static IList<SearchScenario> GetScenarios()
    {
        return new List<SearchScenario>
        {
            new SearchScenario
            {
                Id = "az-cognitivesearch-documents-text-standard",
                Name = "Azure Cognitive Search - Documents - Text - Standard",
                Description = null,
                DocumentationUrl = null,
                SearchRequest = new SearchRequest
                {
                    PrimaryService = PrimaryServiceType.AzureCognitiveSearch,
                    SearchIndexName = Constants.IndexNames.BlobDocuments,
                    QueryType = QueryType.TextStandard
                }
            },
            new SearchScenario
            {
                Id = "az-cognitivesearch-documents-text-semantic",
                Name = "Azure Cognitive Search - Documents - Text - Semantic",
                Description = null,
                DocumentationUrl = null,
                SearchRequest = new SearchRequest
                {
                    PrimaryService = PrimaryServiceType.AzureCognitiveSearch,
                    SearchIndexName = Constants.IndexNames.BlobDocuments,
                    QueryType = QueryType.TextSemantic
                }
            },
            new SearchScenario
            {
                Id = "az-cognitivesearch-chunks-text-standard",
                Name = "Azure Cognitive Search - Chunks - Text - Standard",
                Description = null,
                DocumentationUrl = null,
                SearchRequest = new SearchRequest
                {
                    PrimaryService = PrimaryServiceType.AzureCognitiveSearch,
                    SearchIndexName = Constants.IndexNames.BlobChunks,
                    QueryType = QueryType.TextStandard
                }
            },
            new SearchScenario
            {
                Id = "az-cognitivesearch-chunks-text-semantic",
                Name = "Azure Cognitive Search - Chunks - Text - Semantic",
                Description = null,
                DocumentationUrl = null,
                SearchRequest = new SearchRequest
                {
                    PrimaryService = PrimaryServiceType.AzureCognitiveSearch,
                    SearchIndexName = Constants.IndexNames.BlobChunks,
                    QueryType = QueryType.TextSemantic
                }
            },
            new SearchScenario
            {
                Id = "az-cognitivesearch-chunks-vector",
                Name = "Azure Cognitive Search - Chunks - Vector",
                Description = null,
                DocumentationUrl = null,
                SearchRequest = new SearchRequest
                {
                    PrimaryService = PrimaryServiceType.AzureCognitiveSearch,
                    SearchIndexName = Constants.IndexNames.BlobChunks,
                    QueryType = QueryType.Vector
                }
            },
            new SearchScenario
            {
                Id = "az-cognitivesearch-chunks-hybrid-standard",
                Name = "Azure Cognitive Search - Chunks - Hybrid - Standard",
                Description = null,
                DocumentationUrl = null,
                SearchRequest = new SearchRequest
                {
                    PrimaryService = PrimaryServiceType.AzureCognitiveSearch,
                    SearchIndexName = Constants.IndexNames.BlobChunks,
                    QueryType = QueryType.HybridStandard
                }
            },
            new SearchScenario
            {
                Id = "az-cognitivesearch-chunks-hybrid-semantic",
                Name = "Azure Cognitive Search - Chunks - Hybrid - Semantic",
                Description = null,
                DocumentationUrl = null,
                SearchRequest = new SearchRequest
                {
                    PrimaryService = PrimaryServiceType.AzureCognitiveSearch,
                    SearchIndexName = Constants.IndexNames.BlobChunks,
                    QueryType = QueryType.HybridSemantic
                }
            },
            new SearchScenario
            {
                Id = "az-openai-standard",
                Name = "Azure OpenAI - Standard",
                Description = null,
                DocumentationUrl = null,
                SearchRequest = new SearchRequest
                {
                    PrimaryService = PrimaryServiceType.AzureOpenAI,
                    DataSource = DataSourceType.None                }
            },
            new SearchScenario
            {
                Id = "az-openai-cognitivesearch-text-standard",
                Name = "Azure OpenAI - On Your Data",
                Description = null,
                DocumentationUrl = null,
                SearchRequest = new SearchRequest
                {
                    PrimaryService = PrimaryServiceType.AzureOpenAI,
                    SearchIndexName = Constants.IndexNames.BlobChunks, // As a built-in scenario, always use the chunks index for best results
                    QueryType = QueryType.TextSemantic, // As a built-in scenario, always use semantic search for best results
                    DataSource = DataSourceType.AzureCognitiveSearch,
                    LimitToDataSource = true
                }
            }
        };
    }
}
