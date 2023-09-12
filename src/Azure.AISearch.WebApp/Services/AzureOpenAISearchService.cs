// TODO: This doesn't work today, the API returns messages including the
// citations messages we need, but the title and filepath etc are null
// so it seems the field mappings aren't fully working (perhaps because
// the serialized names are wrong? ContentFieldNames is serialized as
// "contentFieldNames" but UrlFieldName as "urlField" for exampel).

using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.AI.OpenAI;
using Azure.AISearch.WebApp.Models;
using AzureCognitiveSearchQueryType = Azure.AI.OpenAI.AzureCognitiveSearchQueryType;

namespace Azure.AISearch.WebApp.Services;

public class AzureOpenAISearchService : ISearchService
{
    private readonly AppSettings settings;
    private readonly OpenAIClient client;

    public AzureOpenAISearchService(AppSettings settings, IHttpClientFactory httpClientFactory)
    {
        ArgumentNullException.ThrowIfNull(settings.OpenAIApiKey);
        ArgumentNullException.ThrowIfNull(settings.OpenAIEndpoint);
        this.settings = settings;
        this.client = new OpenAIClient(new Uri(this.settings.OpenAIEndpoint), new AzureKeyCredential(this.settings.OpenAIApiKey));
    }

    public bool CanHandle(SearchRequest request)
    {
        return request.Engine == EngineType.AzureOpenAI;
    }

    public async Task<SearchResponse> SearchAsync(SearchRequest request)
    {
        ArgumentNullException.ThrowIfNull(request.Query);

        var searchResponse = new SearchResponse();
        var chatCompletionsOptions = new ChatCompletionsOptions();
        chatCompletionsOptions.Messages.Add(new ChatMessage(ChatRole.System, request.SystemRoleInformation));

        if (request.History != null && request.History.Any())
        {
            var role = ChatRole.User;
            foreach (var item in request.History)
            {
                chatCompletionsOptions.Messages.Add(new ChatMessage(role, item));
                searchResponse.History.Add(item);
                role = role == ChatRole.User ? ChatRole.Assistant : ChatRole.User;
            }
        }
        chatCompletionsOptions.Messages.Add(new ChatMessage(ChatRole.User, request.Query));
        searchResponse.History.Add(request.Query);

        if (request.DataSource == DataSourceType.AzureCognitiveSearch)
        {
            chatCompletionsOptions.AzureExtensionsOptions = new AzureChatExtensionsOptions
            {
                Extensions = { GetAzureCognitiveSearchDataSource(request) }
            };
        }

        var serviceResponse = await this.client.GetChatCompletionsAsync(this.settings.OpenAIGptDeployment, chatCompletionsOptions);

        if (serviceResponse == null || !serviceResponse.Value.Choices.Any())
        {
            throw new InvalidOperationException("Azure OpenAI didn't return a meaningful response.");
        }
        var answerMessage = serviceResponse.Value.Choices.First().Message; // Use the first choice only.

        var answerText = answerMessage.Content;
        if (answerText == null)
        {
            throw new InvalidOperationException("Azure OpenAI didn't return a meaningful response.");
        }

        // Process citations within the answer, which take the form "[doc1][doc2]..." and refer to the (1-based) index of
        // the citations in the tool message.
        foreach (var extensionMessage in answerMessage.AzureExtensionsContext.Messages.Where(m => m.Role == ChatRole.Tool))
        {
            Console.WriteLine(extensionMessage.Content);
            var content = JsonSerializer.Deserialize<ChatResponseMessageContent>(extensionMessage.Content!);
            if (content?.Citations != null && content.Citations.Any())
            {
                var citationIndex = 0;
                foreach (var citation in content.Citations)
                {
                    // TODO: citation title etc are null here!
                    answerText = answerText.Replace($"[doc{++citationIndex}]", $"<cite>{citation.Title}</cite>", StringComparison.OrdinalIgnoreCase);
                    searchResponse.SearchResults.Add(new SearchResult
                    {
                        DocumentId = citation.Id,
                        DocumentTitle = citation.Title,
                        Captions = string.IsNullOrWhiteSpace(citation.Content) ? Array.Empty<string>() : new[] { citation.Content }
                    });
                }
                // Stop looping through the tool messages once we find the first one holding the citations.
                break;
            }
        }

        searchResponse.Answers = new[] { new SearchAnswer { Text = answerText } };
        searchResponse.History.Add(answerText);
        return searchResponse;
    }

    private AzureCognitiveSearchChatExtensionConfiguration GetAzureCognitiveSearchDataSource(SearchRequest request)
    {
        ArgumentNullException.ThrowIfNull(this.settings.SearchServiceUrl);
        ArgumentNullException.ThrowIfNull(this.settings.SearchServiceAdminKey);
        ArgumentNullException.ThrowIfNull(this.settings.OpenAIEndpoint);
        ArgumentNullException.ThrowIfNull(this.settings.OpenAIApiKey);
        var useDocumentsIndex = request.SearchIndex == SearchIndexType.Documents;
        return new AzureCognitiveSearchChatExtensionConfiguration
        {
            SearchEndpoint = new Uri(this.settings.SearchServiceUrl),
            SearchKey = new AzureKeyCredential(this.settings.SearchServiceAdminKey),
            IndexName = useDocumentsIndex ? this.settings.SearchIndexNameBlobDocuments : this.settings.SearchIndexNameBlobChunks,
            FieldMappingOptions = new AzureCognitiveSearchIndexFieldMappingOptions
            {
                ContentFieldNames = { useDocumentsIndex ? nameof(Document.Content) : nameof(DocumentChunk.Content) },
                TitleFieldName = useDocumentsIndex ? nameof(Document.Title) : nameof(DocumentChunk.SourceDocumentTitle),
                UrlFieldName = useDocumentsIndex ? nameof(Document.FilePath) : nameof(DocumentChunk.SourceDocumentFilePath),
                FilepathFieldName = useDocumentsIndex ? nameof(Document.FilePath) : nameof(DocumentChunk.SourceDocumentFilePath),
                VectorFieldNames = { useDocumentsIndex ? null : nameof(DocumentChunk.ContentVector) }
            },
            ShouldRestrictResultScope = request.LimitToDataSource, // Limit responses to data from the data source only
            QueryType = GetQueryType(request),
            SemanticConfiguration = request.IsSemanticSearch ? Constants.ConfigurationNames.SemanticConfigurationNameDefault : null,
            EmbeddingEndpoint = request.IsVectorSearch ? new Uri(new Uri(this.settings.OpenAIEndpoint), $"openai/deployments/{this.settings.OpenAIEmbeddingDeployment}/embeddings?api-version={this.settings.OpenAIApiVersion}") : null,
            EmbeddingKey = request.IsVectorSearch ? new AzureKeyCredential(this.settings.OpenAIApiKey) : null
        };
    }

    private AzureCognitiveSearchQueryType GetQueryType(SearchRequest request)
    {
        if (request.QueryType == QueryType.TextStandard)
        {
            return AzureCognitiveSearchQueryType.Simple;
        }
        else if (request.QueryType == QueryType.TextSemantic)
        {
            return AzureCognitiveSearchQueryType.Semantic;
        }
        else if (request.QueryType == QueryType.Vector)
        {
            return AzureCognitiveSearchQueryType.Vector;
        }
        else if (request.QueryType == QueryType.HybridStandard)
        {
            return AzureCognitiveSearchQueryType.VectorSimpleHybrid;
        }
        else if (request.QueryType == QueryType.HybridSemantic)
        {
            return AzureCognitiveSearchQueryType.VectorSemanticHybrid;
        }
        else
        {
            throw new NotSupportedException($"Unsupported query type \"{request.QueryType}\".");
        }
    }

    // These model classes are based on the Azure OpenAI playground and samples
    // to use while waiting for .NET SDK support.

    private class ChatResponseMessageContent
    {
        [JsonPropertyName("citations")]
        public IList<Citation> Citations { get; set; } = new List<Citation>();

        [JsonPropertyName("intent")]
        public string? Intent { get; set; } // This seems to be yet another nested JSON object, as an array of strings
    }

    private class Citation
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

    private class CitationMetadata
    {
        [JsonPropertyName("chunking")]
        public string? Chunking { get; set; }
    }
}