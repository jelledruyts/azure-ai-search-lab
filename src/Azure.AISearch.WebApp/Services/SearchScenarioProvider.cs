using Azure.AISearch.WebApp.Models;

namespace Azure.AISearch.WebApp.Services;

public class SearchScenarioProvider
{
    private readonly AppSettings settings;

    public SearchScenarioProvider(AppSettings settings)
    {
        this.settings = settings;
    }

    public IList<SearchScenario> GetSearchScenarios()
    {
        ArgumentNullException.ThrowIfNull(this.settings.SearchIndexNameBlobDocuments);
        ArgumentNullException.ThrowIfNull(this.settings.SearchIndexNameBlobChunks);
        return new List<SearchScenario>
        {
            new SearchScenario
            {
                Id = "az-cognitivesearch-documents-keyword-standard",
                DisplayName = "Azure AI Search - Documents - Keyword - Standard",
                Description = "This scenario uses Azure AI Search to perform keyword search across the original documents. It uses the standard ('simple') search mode.",
                SearchRequest = new SearchRequest
                {
                    Engine = EngineType.AzureCognitiveSearch,
                    SearchIndex = SearchIndexType.Documents,
                    QueryType = QueryType.TextStandard
                }
            },
            new SearchScenario
            {
                Id = "az-cognitivesearch-documents-keyword-semantic",
                DisplayName = "Azure AI Search - Documents - Keyword - Semantic",
                Description = "This scenario uses Azure AI Search to perform keyword search across the original documents. It uses semantic ranking which returns more relevant results by applying language understanding to an initial search result.",
                SearchRequest = new SearchRequest
                {
                    Engine = EngineType.AzureCognitiveSearch,
                    SearchIndex = SearchIndexType.Documents,
                    QueryType = QueryType.TextSemantic
                }
            },
            new SearchScenario
            {
                Id = "az-cognitivesearch-chunks-text-standard",
                DisplayName = "Azure AI Search - Chunks - Keyword - Standard",
                Description = "This scenario uses Azure AI Search to perform keyword search across the smaller chunks of the original documents. It uses the standard ('simple') search mode.",
                SearchRequest = new SearchRequest
                {
                    Engine = EngineType.AzureCognitiveSearch,
                    SearchIndex = SearchIndexType.Chunks,
                    QueryType = QueryType.TextStandard
                }
            },
            new SearchScenario
            {
                Id = "az-cognitivesearch-chunks-keyword-semantic",
                DisplayName = "Azure AI Search - Chunks - Keyword - Semantic",
                Description = "This scenario uses Azure AI Search to perform keyword search across the smaller chunks of the original documents. It uses semantic ranking which returns more relevant results by applying language understanding to an initial search result.",
                SearchRequest = new SearchRequest
                {
                    Engine = EngineType.AzureCognitiveSearch,
                    SearchIndex = SearchIndexType.Chunks,
                    QueryType = QueryType.TextSemantic
                }
            },
            new SearchScenario
            {
                Id = "az-cognitivesearch-chunks-vector",
                DisplayName = "Azure AI Search - Chunks - Vector",
                Description = "This scenario uses Azure AI Search to perform a pure vector search across the smaller chunks of the original documents, where each chunk is represented as a vector of numbers as determined by an Azure OpenAI embeddings model. The search query itself is first vectorized using the same embeddings model, and the best matching results for the search query are determined based on the distance between the query vector and the chunk vector.",
                SearchRequest = new SearchRequest
                {
                    Engine = EngineType.AzureCognitiveSearch,
                    SearchIndex = SearchIndexType.Chunks,
                    QueryType = QueryType.Vector
                }
            },
            new SearchScenario
            {
                Id = "az-cognitivesearch-chunks-hybrid-semantic",
                DisplayName = "Azure AI Search - Chunks - Hybrid - Semantic",
                Description = "This scenario uses Azure AI Search to perform a hybrid (keyword + vector) search across the smaller chunks of the original documents, where each chunk is represented as a vector of numbers as determined by an Azure OpenAI embeddings model. The search query itself is first vectorized using the same embeddings model, and the best matching results for the search query are determined based on merging the results of the keyword and vector searches. The keyword search uses semantic ranking for even more accuracy with L2 reranking using the same language models that power Bing.",
                SearchRequest = new SearchRequest
                {
                    Engine = EngineType.AzureCognitiveSearch,
                    SearchIndex = SearchIndexType.Chunks,
                    QueryType = QueryType.HybridSemantic
                }
            },
            new SearchScenario
            {
                Id = "az-openai-standard",
                DisplayName = "Azure OpenAI - Standard",
                Description = "This scenario uses a GPT model in Azure OpenAI to perform a chat-based search experience. It uses only publicly available information that the model was trained on, and has no access to any private data sources.",
                SearchRequest = new SearchRequest
                {
                    Engine = EngineType.AzureOpenAI,
                    DataSource = DataSourceType.None,
                    OpenAIGptDeployment = this.settings.OpenAIGptDeployment,
                    SystemRoleInformation = this.settings.GetDefaultSystemRoleInformation()
                }
            },
            new SearchScenario
            {
                Id = "az-openai-cognitivesearch-keyword-standard",
                DisplayName = "Azure OpenAI - On Your Data",
                Description = "This scenario uses a GPT model in Azure OpenAI to perform a chat-based search experience. It uses your own data, which is indexed in Azure AI Search. The search query is first sent to Azure AI Search to perform keyword search, and the top results are then used as the input to the GPT model to generate a response. This scenario is useful if you want to use your own data, but still want to use the GPT model to generate responses.",
                SearchRequest = new SearchRequest
                {
                    Engine = EngineType.AzureOpenAI,
                    DataSource = DataSourceType.AzureCognitiveSearch,
                    OpenAIGptDeployment = this.settings.OpenAIGptDeployment,
                    SystemRoleInformation = this.settings.GetDefaultSystemRoleInformation(),
                    SearchIndex = SearchIndexType.Chunks, // As a built-in scenario, always use the chunks index for best results
                    QueryType = QueryType.HybridSemantic, // As a built-in scenario, always use semantic ranking for best results
                    LimitToDataSource = true
                }
            },
            new SearchScenario
            {
                Id = "az-customorchestration-chunks-hybrid-semantic",
                DisplayName = "Custom Orchestration - Chunks - Hybrid - Semantic",
                Description = "This scenario first uses Azure AI Search to perform a hybrid (keyword + vector) semantic search across the smaller chunks of the original documents. It then uses those search results along with the original query to build a prompt for an AI model to generate an answer (with citations).",
                SearchRequest = new SearchRequest
                {
                    Engine = EngineType.CustomOrchestration,
                    OpenAIGptDeployment = this.settings.OpenAIGptDeployment,
                    CustomOrchestrationPrompt = this.settings.GetDefaultCustomOrchestrationPrompt(),
                    SearchIndex = SearchIndexType.Chunks, // As a built-in scenario, always use the chunks index for best results
                    QueryType = QueryType.HybridSemantic // As a built-in scenario, always use semantic ranking for best results
                }
            }
        };
    }
}