using System.Text.Json;
using Azure.AISearch.WebApp.Infrastructure;
using Azure.AISearch.WebApp.Models;

namespace Azure.AISearch.WebApp.Services;

public class AzureOpenAISearchService : ISearchService
{
    private readonly AppSettings settings;
    private readonly IHttpClientFactory httpClientFactory;
    private readonly Uri chatCompletionUrl;
    private readonly Uri extensionChatCompletionUrl;
    private readonly Uri embeddingsUrl;

    public AzureOpenAISearchService(AppSettings settings, IHttpClientFactory httpClientFactory)
    {
        ArgumentNullException.ThrowIfNull(settings.OpenAIEndpoint);
        ArgumentNullException.ThrowIfNull(settings.OpenAIGptDeployment);
        ArgumentNullException.ThrowIfNull(settings.OpenAIEmbeddingDeployment);
        this.settings = settings;
        this.httpClientFactory = httpClientFactory;
        var baseUrl = new Uri(this.settings.OpenAIEndpoint);
        this.chatCompletionUrl = GetAzureOpenAIUrl(baseUrl, this.settings.OpenAIGptDeployment, "chat/completions");
        this.extensionChatCompletionUrl = GetAzureOpenAIUrl(baseUrl, this.settings.OpenAIGptDeployment, "extensions/chat/completions");
        this.embeddingsUrl = GetAzureOpenAIUrl(baseUrl, this.settings.OpenAIEmbeddingDeployment, "embeddings");
    }

    private Uri GetAzureOpenAIUrl(Uri baseUrl, string deploymentName, string path)
    {
        return new Uri(baseUrl, $"openai/deployments/{deploymentName}/{path}?api-version=2023-06-01-preview");
    }

    public async Task<SearchResponse?> SearchAsync(SearchRequest request)
    {
        if (request.Engine != EngineType.AzureOpenAI)
        {
            return null;
        }
        ArgumentNullException.ThrowIfNull(request.Query);

        var searchResponse = new SearchResponse();
        var messages = new List<ChatRequestMessage>();
        messages.Add(new ChatRequestMessage { Role = Constants.ChatRoles.System, Content = request.SystemRoleInformation });
        if (request.History != null && request.History.Any())
        {
            var role = Constants.ChatRoles.User;
            foreach (var item in request.History)
            {
                messages.Add(new ChatRequestMessage { Role = role, Content = item });
                searchResponse.History.Add(item);
                role = role == Constants.ChatRoles.User ? Constants.ChatRoles.Assistant : Constants.ChatRoles.User;
            }
        }
        messages.Add(new ChatRequestMessage { Role = Constants.ChatRoles.User, Content = request.Query });
        searchResponse.History.Add(request.Query);
        var serviceRequest = new ChatCompletionsRequest
        {
            Messages = messages
        };

        if (request.DataSource == DataSourceType.AzureCognitiveSearch)
        {
            serviceRequest.DataSources = new[] { GetAzureCognitiveSearchDataSource(request) };
        }

        var httpClient = this.httpClientFactory.CreateClient();
        httpClient.DefaultRequestHeaders.Add("api-key", this.settings.OpenAIApiKey);
        httpClient.DefaultRequestHeaders.Add("chatgpt_url", this.chatCompletionUrl.ToString());
        httpClient.DefaultRequestHeaders.Add("chatgpt_key", this.settings.OpenAIApiKey);
        // NOTE: PostAsJsonAsync doesn't work for some reason and only for OpenAI "On Your Data",
        // where it results in HTTP 500 "Response payload is not completed".
        // var serviceResponseMessage = await httpClient.PostAsJsonAsync(serviceRequestUrl, serviceRequest);
        var serviceRequestUrl = request.DataSource == DataSourceType.None ? this.chatCompletionUrl : this.extensionChatCompletionUrl;
        var serviceResponseMessage = await httpClient.PostAsync(serviceRequestUrl, new StringContent(JsonSerializer.Serialize(serviceRequest, JsonConfiguration.DefaultJsonOptions), System.Text.Encoding.UTF8, "application/json"));
        if (!serviceResponseMessage.IsSuccessStatusCode)
        {
            searchResponse.Error = await serviceResponseMessage.Content.ReadAsStringAsync();
            return searchResponse;
        }
        var serviceResponse = await serviceResponseMessage.Content.ReadFromJsonAsync<ChatCompletionsResponse>();

        if (serviceResponse == null || !serviceResponse.Choices.Any())
        {
            throw new InvalidOperationException("Azure OpenAI didn't return a meaningful response.");
        }
        var choice = serviceResponse.Choices.First(); // Use the first choice only.

        // Deserialize nested JSON content from messages produced by tools.
        foreach (var message in choice.Messages.Where(m => string.Equals(m.Role, Constants.ChatRoles.Tool, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(m.Content)))
        {
            message.ContentObject = JsonSerializer.Deserialize<ChatResponseMessageContent>(message.Content!);
        }

        // In the standard case, the answer is the one and only message.
        // If we're using the "On Your Data" service, we need to find the message that indicates the end of the turn.
        // This is the message that contains the answer.
        var answerText = choice.Message?.Content ?? choice.Messages.Where(m => m.EndTurn == true).SingleOrDefault()?.Content;
        if (answerText == null)
        {
            throw new InvalidOperationException("Azure OpenAI didn't return a meaningful response.");
        }

        // Process citations within the answer, which take the form "[doc1][doc2]..." and refer to the (1-based) index of
        // the citations in the tool message.
        var citationsMessage = choice.Messages.OrderByDescending(m => m.Index).Where(m => m.ContentObject?.Citations != null && m.ContentObject.Citations.Any()).FirstOrDefault();
        if (citationsMessage != null)
        {
            var citations = citationsMessage.ContentObject!.Citations;
            var citationIndex = 0;
            foreach (var citation in citations)
            {
                answerText = answerText.Replace($"[doc{++citationIndex}]", $"<cite>{citation.Title}</cite>", StringComparison.OrdinalIgnoreCase);
                searchResponse.SearchResults.Add(new SearchResult
                {
                    DocumentId = citation.Id,
                    DocumentTitle = citation.Title,
                    Captions = string.IsNullOrWhiteSpace(citation.Content) ? Array.Empty<string>() : new[] { citation.Content }
                });
            }
        }
        searchResponse.Answers = new[] { new SearchAnswer { Text = answerText } };
        searchResponse.History.Add(answerText);
        return searchResponse;
    }

    private DataSource GetAzureCognitiveSearchDataSource(SearchRequest request)
    {
        if (request.SearchIndexName != Constants.IndexNames.BlobDocuments && request.SearchIndexName != Constants.IndexNames.BlobChunks)
        {
            // Cannot infer which shape the search results will have, so don't continue.
            throw new NotSupportedException($"Search index \"{request.SearchIndexName}\" is not supported.");
        }
        var useDocumentsIndex = request.SearchIndexName == Constants.IndexNames.BlobDocuments;
        return new DataSource
        {
            Type = "AzureCognitiveSearch",
            Parameters = new AzureCognitiveSearchParameters
            {
                Endpoint = this.settings.SearchServiceUrl,
                Key = this.settings.SearchServiceAdminKey,
                IndexName = request.SearchIndexName,
                FieldsMapping = new AzureCognitiveSearchParametersFieldsMapping
                {
                    ContentFields = new[] { useDocumentsIndex ? nameof(Document.Content) : nameof(DocumentChunk.Content) },
                    TitleField = useDocumentsIndex ? nameof(Document.Title) : nameof(DocumentChunk.SourceDocumentTitle),
                    UrlField = useDocumentsIndex ? nameof(Document.FilePath) : nameof(DocumentChunk.SourceDocumentFilePath),
                    FilepathField = useDocumentsIndex ? nameof(Document.FilePath) : nameof(DocumentChunk.SourceDocumentFilePath),
                    VectorFields = useDocumentsIndex ? Array.Empty<string>() : new[] { nameof(DocumentChunk.ContentVector) }
                },
                InScope = request.LimitToDataSource, // Limit responses to data from the data source only
                QueryType = GetQueryType(request),
                SemanticConfiguration = request.IsSemanticSearch ? Constants.ConfigurationNames.SemanticConfigurationNameDefault : null,
                RoleInformation = request.SystemRoleInformation,
                EmbeddingEndpoint = request.IsVectorSearch ? this.embeddingsUrl.ToString() : null,
                EmbeddingKey = request.IsVectorSearch ? this.settings.OpenAIApiKey : null
            }
        };
    }

    private AzureCognitiveSearchQueryType GetQueryType(SearchRequest request)
    {
        if (request.QueryType == QueryType.TextStandard)
        {
            return AzureCognitiveSearchQueryType.simple;
        }
        else if (request.QueryType == QueryType.TextSemantic)
        {
            return AzureCognitiveSearchQueryType.semantic;
        }
        else if (request.QueryType == QueryType.Vector)
        {
            return AzureCognitiveSearchQueryType.vector;
        }
        else if (request.QueryType == QueryType.HybridStandard)
        {
            return AzureCognitiveSearchQueryType.vectorSimpleHybrid;
        }
        else if (request.QueryType == QueryType.HybridSemantic)
        {
            return AzureCognitiveSearchQueryType.vectorSemanticHybrid;
        }
        else
        {
            throw new NotSupportedException($"Unsupported query type \"{request.QueryType}\".");
        }
    }
}