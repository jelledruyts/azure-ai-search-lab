using System.Text.Json;
using Azure.AISearch.WebApp.Models;
using Azure.AI.OpenAI;
using System.Text.Json.Serialization;

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
        var chatCompletionsOptions = new ChatCompletionsOptions
        {
            DeploymentName = this.settings.OpenAIGptDeployment,
            MaxTokens = request.MaxTokens ?? Constants.Defaults.MaxTokens,
            Temperature = (float)(request.Temperature ?? Constants.Defaults.Temperature),
            NucleusSamplingFactor = (float)(request.TopP ?? Constants.Defaults.TopP),
            FrequencyPenalty = (float)(request.FrequencyPenalty ?? Constants.Defaults.FrequencyPenalty),
            PresencePenalty = (float)(request.PresencePenalty ?? Constants.Defaults.PresencePenalty),
        };
        var stopSequences = (request.StopSequences ?? Constants.Defaults.StopSequences).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (stopSequences.Any())
        {
            foreach (var stopSequence in stopSequences)
            {
                chatCompletionsOptions.StopSequences.Add(stopSequence);
            }
        }
        chatCompletionsOptions.Messages.Add(new ChatRequestSystemMessage(request.SystemRoleInformation));

        if (request.History != null && request.History.Any())
        {
            var role = ChatRole.User;
            foreach (var item in request.History)
            {
                chatCompletionsOptions.Messages.Add(role == ChatRole.User ? new ChatRequestUserMessage(item) : new ChatRequestAssistantMessage(item));
                searchResponse.History.Add(item);
                role = role == ChatRole.User ? ChatRole.Assistant : ChatRole.User;
            }
        }
        chatCompletionsOptions.Messages.Add(new ChatRequestUserMessage(request.Query));
        searchResponse.History.Add(request.Query);

        if (request.DataSource == DataSourceType.AzureCognitiveSearch)
        {
            chatCompletionsOptions.AzureExtensionsOptions = new AzureChatExtensionsOptions
            {
                Extensions = { GetAzureCognitiveSearchDataSource(request) }
            };
        }

        var serviceResponse = await this.client.GetChatCompletionsAsync(chatCompletionsOptions);

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

        if (answerMessage.AzureExtensionsContext != null)
        {
            // Process citations within the answer, which take the form "[doc1][doc2]..." and refer to the (1-based) index of
            // the citations in the tool message.
            var citationIndex = 0;
            foreach (var citation in answerMessage.AzureExtensionsContext.Citations)
            {
                answerText = answerText.Replace($"[doc{++citationIndex}]", $"<cite>{citation.Title}</cite>", StringComparison.OrdinalIgnoreCase);
                searchResponse.SearchResults.Add(new SearchResult
                {
                    DocumentId = citation.Filepath,
                    DocumentTitle = citation.Title,
                    Captions = string.IsNullOrWhiteSpace(citation.Content) ? Array.Empty<string>() : new[] { citation.Content }
                });
            }
        }

        searchResponse.Answers = new[] { new SearchAnswer { Text = answerText } };
        searchResponse.History.Add(answerText);
        return searchResponse;
    }

    private AzureSearchChatExtensionConfiguration GetAzureCognitiveSearchDataSource(SearchRequest request)
    {
        ArgumentNullException.ThrowIfNull(this.settings.SearchServiceUrl);
        ArgumentNullException.ThrowIfNull(this.settings.SearchServiceAdminKey);
        ArgumentNullException.ThrowIfNull(this.settings.OpenAIEndpoint);
        ArgumentNullException.ThrowIfNull(this.settings.OpenAIApiKey);
        var useDocumentsIndex = request.SearchIndex == SearchIndexType.Documents;
        return new AzureSearchChatExtensionConfiguration
        {
            SearchEndpoint = new Uri(this.settings.SearchServiceUrl),
            Authentication = new OnYourDataApiKeyAuthenticationOptions(this.settings.SearchServiceAdminKey),
            IndexName = useDocumentsIndex ? this.settings.SearchIndexNameBlobDocuments : this.settings.SearchIndexNameBlobChunks,
            FieldMappingOptions = new AzureSearchIndexFieldMappingOptions
            {
                ContentFieldNames = { useDocumentsIndex ? nameof(Document.Content) : nameof(DocumentChunk.Content) },
                TitleFieldName = useDocumentsIndex ? nameof(Document.Title) : nameof(DocumentChunk.SourceDocumentTitle),
                UrlFieldName = useDocumentsIndex ? nameof(Document.FilePath) : nameof(DocumentChunk.SourceDocumentFilePath),
                FilepathFieldName = useDocumentsIndex ? nameof(Document.FilePath) : nameof(DocumentChunk.SourceDocumentFilePath),
                VectorFieldNames = { useDocumentsIndex ? null : nameof(DocumentChunk.ContentVector) }
            },
            ShouldRestrictResultScope = request.LimitToDataSource, // Limit responses to data from the data source only
            QueryType = GetQueryType(request),
            RoleInformation = request.SystemRoleInformation ?? Constants.Defaults.SystemRoleInformation,
            Strictness = request.Strictness ?? Constants.Defaults.Strictness,
            DocumentCount = request.DocumentCount ?? Constants.Defaults.DocumentCount,
            SemanticConfiguration = request.IsSemanticSearch ? Constants.ConfigurationNames.SemanticConfigurationNameDefault : null,
            VectorizationSource = request.IsVectorSearch ? new OnYourDataDeploymentNameVectorizationSource(this.settings.OpenAIEmbeddingDeployment) : null
        };
    }

    private AzureSearchQueryType GetQueryType(SearchRequest request)
    {
        if (request.QueryType == QueryType.TextStandard)
        {
            return AzureSearchQueryType.Simple;
        }
        else if (request.QueryType == QueryType.TextSemantic)
        {
            return AzureSearchQueryType.Semantic;
        }
        else if (request.QueryType == QueryType.Vector)
        {
            return AzureSearchQueryType.Vector;
        }
        else if (request.QueryType == QueryType.HybridStandard)
        {
            return AzureSearchQueryType.VectorSimpleHybrid;
        }
        else if (request.QueryType == QueryType.HybridSemantic)
        {
            return AzureSearchQueryType.VectorSemanticHybrid;
        }
        else
        {
            throw new NotSupportedException($"Unsupported query type \"{request.QueryType}\".");
        }
    }
}