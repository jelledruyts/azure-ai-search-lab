namespace Azure.AISearch.WebApp.Models;

public class SearchScenario
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public SearchRequest SearchRequest { get; set; } = new SearchRequest();

    public static IList<SearchScenario> GetScenarios()
    {
        return new List<SearchScenario>
        {
            new SearchScenario
            {
                Id = "az-cognitivesearch-documents-text-standard",
                Name = "Azure Cognitive Search - Documents - Text - Standard",
                Description = "This scenario uses Azure Cognitive Search to perform a full-text search across the original documents. It uses the standard ('simple') search mode.",
                SearchRequest = new SearchRequest
                {
                    Engine = EngineType.AzureCognitiveSearch,
                    SearchIndexName = Constants.IndexNames.BlobDocuments,
                    QueryType = QueryType.TextStandard
                }
            },
            new SearchScenario
            {
                Id = "az-cognitivesearch-documents-text-semantic",
                Name = "Azure Cognitive Search - Documents - Text - Semantic",
                Description = "This scenario uses Azure Cognitive Search to perform a full-text search across the original documents. It uses semantic search which returns more relevant results by applying language understanding to an initial search result.",
                SearchRequest = new SearchRequest
                {
                    Engine = EngineType.AzureCognitiveSearch,
                    SearchIndexName = Constants.IndexNames.BlobDocuments,
                    QueryType = QueryType.TextSemantic
                }
            },
            new SearchScenario
            {
                Id = "az-cognitivesearch-chunks-text-standard",
                Name = "Azure Cognitive Search - Chunks - Text - Standard",
                Description = "This scenario uses Azure Cognitive Search to perform a full-text search across the smaller chunks of the original documents. It uses the standard ('simple') search mode.",
                SearchRequest = new SearchRequest
                {
                    Engine = EngineType.AzureCognitiveSearch,
                    SearchIndexName = Constants.IndexNames.BlobChunks,
                    QueryType = QueryType.TextStandard
                }
            },
            new SearchScenario
            {
                Id = "az-cognitivesearch-chunks-text-semantic",
                Name = "Azure Cognitive Search - Chunks - Text - Semantic",
                Description = "This scenario uses Azure Cognitive Search to perform a full-text search across the smaller chunks of the original documents. It uses semantic search which returns more relevant results by applying language understanding to an initial search result.",
                SearchRequest = new SearchRequest
                {
                    Engine = EngineType.AzureCognitiveSearch,
                    SearchIndexName = Constants.IndexNames.BlobChunks,
                    QueryType = QueryType.TextSemantic
                }
            },
            new SearchScenario
            {
                Id = "az-cognitivesearch-chunks-vector",
                Name = "Azure Cognitive Search - Chunks - Vector",
                Description = "This scenario uses Azure Cognitive Search to perform a pure vector search across the smaller chunks of the original documents, where each chunk is represented as a vector of numbers as determined by an Azure OpenAI embeddings model. The search query itself is first vectorized using the same embeddings model, and the best matching results for the search query are determined based on the distance between the query vector and the chunk vector.",
                SearchRequest = new SearchRequest
                {
                    Engine = EngineType.AzureCognitiveSearch,
                    SearchIndexName = Constants.IndexNames.BlobChunks,
                    QueryType = QueryType.Vector
                }
            },
            new SearchScenario
            {
                Id = "az-cognitivesearch-chunks-hybrid-standard",
                Name = "Azure Cognitive Search - Chunks - Hybrid - Standard",
                Description = "This scenario uses Azure Cognitive Search to perform a hybrid (text + vector) search across the smaller chunks of the original documents, where each chunk is represented as a vector of numbers as determined by an Azure OpenAI embeddings model. The search query itself is first vectorized using the same embeddings model, and the best matching results for the search query are determined based on merging the results of the text and vector searches. The full-text search uses the standard ('simple') search mode.",
                SearchRequest = new SearchRequest
                {
                    Engine = EngineType.AzureCognitiveSearch,
                    SearchIndexName = Constants.IndexNames.BlobChunks,
                    QueryType = QueryType.HybridStandard
                }
            },
            new SearchScenario
            {
                Id = "az-cognitivesearch-chunks-hybrid-semantic",
                Name = "Azure Cognitive Search - Chunks - Hybrid - Semantic",
                Description = "This scenario uses Azure Cognitive Search to perform a hybrid (text + vector) search across the smaller chunks of the original documents, where each chunk is represented as a vector of numbers as determined by an Azure OpenAI embeddings model. The search query itself is first vectorized using the same embeddings model, and the best matching results for the search query are determined based on merging the results of the text and vector searches. The full-text search uses semantic search for even more accuracy with L2 reranking using the same language models that power Bing.",
                SearchRequest = new SearchRequest
                {
                    Engine = EngineType.AzureCognitiveSearch,
                    SearchIndexName = Constants.IndexNames.BlobChunks,
                    QueryType = QueryType.HybridSemantic
                }
            },
            new SearchScenario
            {
                Id = "az-openai-standard",
                Name = "Azure OpenAI - Standard",
                Description = "This scenario uses a GPT model in Azure OpenAI to perform a chat-based search experience. It uses only publicly available information that the model was trained on, and has no access to any private data sources.",
                SearchRequest = new SearchRequest
                {
                    Engine = EngineType.AzureOpenAI,
                    DataSource = DataSourceType.None                }
            },
            new SearchScenario
            {
                Id = "az-openai-cognitivesearch-text-standard",
                Name = "Azure OpenAI - On Your Data",
                Description = "This scenario uses a GPT model in Azure OpenAI to perform a chat-based search experience. It uses your own data, which is indexed in Azure Cognitive Search. The search query is first sent to Azure Cognitive Search, and the top results are then used as the input to the GPT model to generate a response. This scenario is useful if you want to use your own data, but still want to use the GPT model to generate responses.",
                SearchRequest = new SearchRequest
                {
                    Engine = EngineType.AzureOpenAI,
                    SearchIndexName = Constants.IndexNames.BlobChunks, // As a built-in scenario, always use the chunks index for best results
                    QueryType = QueryType.TextSemantic, // As a built-in scenario, always use semantic search for best results
                    DataSource = DataSourceType.AzureCognitiveSearch,
                    LimitToDataSource = true
                }
            }
        };
    }
}
