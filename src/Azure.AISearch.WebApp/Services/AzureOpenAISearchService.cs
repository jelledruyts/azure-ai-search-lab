using System.Text.Json;
using Azure.AISearch.WebApp.Models;

namespace Azure.AISearch.WebApp.Services;

public class AzureOpenAISearchService
{
    private readonly AppSettings settings;
    private readonly IHttpClientFactory httpClientFactory;

    public AzureOpenAISearchService(AppSettings options, IHttpClientFactory httpClientFactory)
    {
        this.settings = options;
        this.httpClientFactory = httpClientFactory;
    }

    public async Task<SearchResponse> SearchAsync(SearchRequest request)
    {
        ArgumentNullException.ThrowIfNull(this.settings.OpenAIEndpoint);
        var messages = new List<ChatRequestMessage>();
        messages.Add(new ChatRequestMessage { Role = Constants.ChatRoles.System, Content = request.SystemRoleInformation });
        if (request.History != null && request.History.Any())
        {
            var role = Constants.ChatRoles.User;
            foreach (var item in request.History)
            {
                messages.Add(new ChatRequestMessage { Role = role, Content = item });
                role = role == Constants.ChatRoles.User ? Constants.ChatRoles.Assistant : Constants.ChatRoles.User;
            }
        }
        messages.Add(new ChatRequestMessage { Role = Constants.ChatRoles.User, Content = request.Query });
        var serviceRequest = new ChatCompletionsRequest
        {
            Messages = messages
        };

        var chatGptUrl = new Uri(new Uri(this.settings.OpenAIEndpoint), $"openai/deployments/{this.settings.OpenAIGptDeployment}/chat/completions?api-version=2023-06-01-preview");
        var serviceRequestUrl = chatGptUrl;
        if (request.DataSource == DataSourceType.AzureCognitiveSearch)
        {
            serviceRequestUrl = new Uri(new Uri(this.settings.OpenAIEndpoint), $"openai/deployments/{this.settings.OpenAIGptDeployment}/extensions/chat/completions?api-version=2023-06-01-preview");
            serviceRequest.DataSources = new[] { GetAzureCognitiveSearchDataSource(request) };
        }

        var searchResponse = new SearchResponse
        {
            RequestId = request.Id,
            DisplayName = request.DisplayName
        };

        var httpClient = this.httpClientFactory.CreateClient();
        httpClient.DefaultRequestHeaders.Add("api-key", this.settings.OpenAIApiKey);
        httpClient.DefaultRequestHeaders.Add("chatgpt_url", chatGptUrl.ToString());
        httpClient.DefaultRequestHeaders.Add("chatgpt_key", this.settings.OpenAIApiKey);
        // NOTE: PostAsJsonAsync doesn't work for some reason and only for OpenAI "On Your Data",
        // where it results in HTTP 500 "Response payload is not completed".
        // var serviceResponseMessage = await httpClient.PostAsJsonAsync(serviceRequestUrl, serviceRequest);
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
        return searchResponse;
    }

    private DataSource GetAzureCognitiveSearchDataSource(SearchRequest request)
    {
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
                    ContentFields = new[] { nameof(DocumentChunk.Content) },
                    TitleField = nameof(DocumentChunk.SourceDocumentTitle),
                    UrlField = nameof(DocumentChunk.SourceDocumentFilePath),
                    FilepathField = nameof(DocumentChunk.SourceDocumentFilePath)
                },
                InScope = request.LimitToDataSource, // Limit responses to data from the data source only
                QueryType = GetQueryType(request),
                SemanticConfiguration = request.TextQueryType == TextQueryType.Semantic ? Constants.ConfigurationNames.SemanticConfigurationNameDefault : null,
                RoleInformation = request.SystemRoleInformation
            }
        };
    }

    private AzureCognitiveSearchQueryType GetQueryType(SearchRequest request)
    {
        if (request.QueryType == QueryType.Text && request.TextQueryType == TextQueryType.Standard)
        {
            return AzureCognitiveSearchQueryType.simple;
        }
        else if (request.QueryType == QueryType.Text && request.TextQueryType == TextQueryType.Semantic)
        {
            return AzureCognitiveSearchQueryType.semantic;
        }
        else if (request.QueryType == QueryType.Vector)
        {
            return AzureCognitiveSearchQueryType.vector;
        }
        else if (request.QueryType == QueryType.Hybrid && request.TextQueryType == TextQueryType.Semantic)
        {
            return AzureCognitiveSearchQueryType.vectorSemanticHybrid;
        }
        else if (request.QueryType == QueryType.Hybrid && request.TextQueryType == TextQueryType.Semantic)
        {
            return AzureCognitiveSearchQueryType.vectorSimpleHybrid;
        }
        else
        {
            throw new NotSupportedException($"Unsupported combination of query type \"{request.QueryType}\" and text query type \"{request.TextQueryType}\".");
        }
    }
}