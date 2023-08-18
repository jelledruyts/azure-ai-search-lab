using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.AISearch.WebApp.Models;

namespace Azure.AISearch.WebApp.Services;

public class AzureOpenAISearchService : ISearchService
{
    public const string ServiceIdStandard = "az-openai-standard";
    public const string ServiceIdOnYourData = "az-openai-yourdata";
    private const string SystemRoleInformation = "You are an AI assistant that helps people find information."; // TODO: Make part of the request
    private readonly AppSettings settings;
    private readonly IHttpClientFactory httpClientFactory;

    public AzureOpenAISearchService(AppSettings options, IHttpClientFactory httpClientFactory)
    {
        this.settings = options;
        this.httpClientFactory = httpClientFactory;
    }

    public IList<Task<SearchResponse>> SearchAsync(SearchRequest request)
    {
        var searchTasks = new List<Task<SearchResponse>>();
        if (request.ShouldInclude(ServiceIdStandard))
        {
            searchTasks.Add(SearchAsync(request, 110, ServiceIdStandard, "Azure OpenAI - Standard"));
        }
        if (request.ShouldInclude(ServiceIdOnYourData))
        {
            searchTasks.Add(SearchAsync(request, 100, ServiceIdOnYourData, "Azure OpenAI - On Your Data"));
        }
        return searchTasks;
    }

    private async Task<SearchResponse> SearchAsync(SearchRequest request, int priority, string serviceId, string serviceName)
    {
        ArgumentNullException.ThrowIfNull(this.settings.OpenAIEndpoint);
        var messages = new List<ChatRequestMessage>();
        messages.Add(new ChatRequestMessage { Role = Constants.ChatRoles.System, Content = SystemRoleInformation });
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
        if (serviceId == ServiceIdOnYourData)
        {
            serviceRequestUrl = new Uri(new Uri(this.settings.OpenAIEndpoint), $"openai/deployments/{this.settings.OpenAIGptDeployment}/extensions/chat/completions?api-version=2023-06-01-preview");
            serviceRequest.DataSources = new[] { GetAzureSearchDataSource(Constants.IndexNames.BlobChunks) };
        }

        var searchResponse = new SearchResponse
        {
            Priority = priority,
            ServiceId = serviceId,
            ServiceName = serviceName
        };

        var httpClient = this.httpClientFactory.CreateClient();
        httpClient.DefaultRequestHeaders.Add("api-key", this.settings.OpenAIApiKey);
        httpClient.DefaultRequestHeaders.Add("chatgpt_url", chatGptUrl.ToString());
        httpClient.DefaultRequestHeaders.Add("chatgpt_key", this.settings.OpenAIApiKey);
        // NOTE: PostAsJsonAsync doesn't work for some reason and only for OpenAI "On Your Data",
        // where it results in HTTP 500 "Response payload is not completed".
        // var serviceResponseMessage = await httpClient.PostAsJsonAsync(serviceRequestUrl, serviceRequest);
        var serviceResponseMessage = await httpClient.PostAsync(serviceRequestUrl, new StringContent(JsonSerializer.Serialize(serviceRequest), System.Text.Encoding.UTF8, "application/json"));
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
        var answer = default(ChatResponseMessage);
        if (serviceId == ServiceIdOnYourData)
        {
            // If we're using the "On Your Data" service, we need to find the message that indicates the end of the turn.
            // This is the message that contains the answer.
            answer = choice.Messages.Where(m => m.EndTurn == true).SingleOrDefault();
        }
        else
        {
            // In the standard case, the answer is the one and only message.
            answer = choice.Message;
        }
        if (answer?.Content == null)
        {
            throw new InvalidOperationException("Azure OpenAI didn't return a meaningful response.");
        }
        var answerText = answer.Content;

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

    private DataSource GetAzureSearchDataSource(string indexName)
    {
        return new DataSource
        {
            Type = "AzureCognitiveSearch",
            Parameters = new AzureCognitiveSearchParameters
            {
                Endpoint = this.settings.SearchServiceUrl,
                Key = this.settings.SearchServiceAdminKey,
                IndexName = indexName,
                FieldsMapping = new AzureCognitiveSearchParametersFieldsMapping
                {
                    ContentFields = new[] { nameof(DocumentChunk.Content) },
                    TitleField = nameof(DocumentChunk.SourceDocumentTitle),
                    UrlField = nameof(DocumentChunk.SourceDocumentFilePath),
                    FilepathField = nameof(DocumentChunk.SourceDocumentFilePath)
                },
                InScope = true, // Limit responses to data from the data source only
                TopNDocuments = 5,
                QueryType = "semantic",
                SemanticConfiguration = Constants.ConfigurationNames.SemanticConfigurationNameDefault,
                RoleInformation = SystemRoleInformation
            }
        };
    }

    #region Models

    public class ChatCompletionsRequest
    {
        [JsonPropertyName("messages")]
        public IList<ChatRequestMessage> Messages { get; set; } = new List<ChatRequestMessage>();

        [JsonPropertyName("temperature")]
        public int? Temperature { get; set; } = 0;

        [JsonPropertyName("max_tokens")]
        public int? MaxTokens { get; set; } = 800;

        [JsonPropertyName("top_p")]
        public int? TopP { get; set; } = 1;

        [JsonPropertyName("stream")]
        public bool? Stream { get; set; } = false;

        [JsonPropertyName("dataSources")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] // When calling the standard API, omit the data sources entirely.
        public IList<DataSource>? DataSources { get; set; }
    }

    public class ChatRequestMessage
    {
        [JsonPropertyName("role")]
        public string? Role { get; set; }

        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }

    public class DataSource
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("parameters")]
        public AzureCognitiveSearchParameters Parameters { get; set; } = new AzureCognitiveSearchParameters();
    }

    public class AzureCognitiveSearchParameters
    {
        [JsonPropertyName("endpoint")]
        public string? Endpoint { get; set; }

        [JsonPropertyName("key")]
        public string? Key { get; set; }

        [JsonPropertyName("indexName")]
        public string? IndexName { get; set; }

        [JsonPropertyName("fieldsMapping")]
        public AzureCognitiveSearchParametersFieldsMapping FieldsMapping { get; set; } = new AzureCognitiveSearchParametersFieldsMapping();

        [JsonPropertyName("inScope")]
        public bool? InScope { get; set; } // Limit responses to data from the data source only 

        [JsonPropertyName("topNDocuments")]
        public int? TopNDocuments { get; set; } = 5;

        [JsonPropertyName("queryType")]
        public string? QueryType { get; set; } = "semantic";

        [JsonPropertyName("semanticConfiguration")]
        public string? SemanticConfiguration { get; set; }

        [JsonPropertyName("roleInformation")]
        public string? RoleInformation { get; set; }
    }

    public class AzureCognitiveSearchParametersFieldsMapping
    {
        [JsonPropertyName("contentFields")]
        public IList<string> ContentFields { get; set; } = new List<string>();

        [JsonPropertyName("titleField")]
        public string? TitleField { get; set; }

        [JsonPropertyName("urlField")]
        public string? UrlField { get; set; }

        [JsonPropertyName("filepathField")]
        public string? FilepathField { get; set; }
    }

    public class ChatCompletionsResponse
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("model")]
        public string? Model { get; set; }

        [JsonPropertyName("created")]
        public int? Created { get; set; }

        [JsonPropertyName("object")]
        public string? Object { get; set; }

        [JsonPropertyName("choices")]
        public IList<Choice> Choices { get; set; } = new List<Choice>();
    }

    public class Choice
    {
        [JsonPropertyName("index")]
        public int? Index { get; set; }

        [JsonPropertyName("message")]
        public ChatResponseMessage? Message { get; set; } // OpenAI Standard

        [JsonPropertyName("finish_reason")]
        public string? FinishReason { get; set; } // OpenAI Standard        

        [JsonPropertyName("messages")]
        public IList<ChatResponseMessage> Messages { get; set; } = new List<ChatResponseMessage>(); // OpenAI On Your Data
    }

    public class ChatResponseMessage
    {
        [JsonPropertyName("index")]
        public int? Index { get; set; }

        [JsonPropertyName("role")]
        public string? Role { get; set; }

        [JsonPropertyName("content")]
        public string? Content { get; set; } // If role is "tool", this is a JSON object of type ChatResponseMessageContent

        [JsonIgnore]
        public ChatResponseMessageContent? ContentObject { get; set; }

        [JsonPropertyName("end_turn")]
        public bool? EndTurn { get; set; }
    }

    public class ChatResponseMessageContent
    {
        [JsonPropertyName("citations")]
        public IList<Citation> Citations { get; set; } = new List<Citation>();

        [JsonPropertyName("intent")]
        public string? Intent { get; set; } // This seems to be yet another nested JSON object, as an array of strings
    }

    public class Citation
    {
        [JsonPropertyName("content")]
        public string? Content { get; set; }

        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("filepath")]
        public string? Filepath { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("metadata")]
        public CitationMetadata Metadata { get; set; } = new CitationMetadata();

        [JsonPropertyName("chunk_id")]
        public string? ChunkId { get; set; }
    }

    public class CitationMetadata
    {
        [JsonPropertyName("chunking")]
        public string? Chunking { get; set; }
    }

    #endregion
}