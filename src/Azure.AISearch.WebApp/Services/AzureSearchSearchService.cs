using Azure.AISearch.WebApp.Models;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;

namespace Azure.AISearch.WebApp.Services;

public class AzureSearchSearchService : ISearchService
{
    public const string ServiceIdTextStandard = "az-cogsearch-text-standard";
    public const string ServiceIdTextSemantic = "az-cogsearch-text-semantic";
    public const string ServiceIdChunksTextStandard = "az-cogsearch-chunks-text-standard";
    public const string ServiceIdChunksTextSemantic = "az-cogsearch-chunks-text-semantic";
    public const string ServiceIdChunksVector = "az-cogsearch-chunks-vector";
    public const string ServiceIdChunksHybridStandard = "az-cogsearch-chunks-hybrid-standard";
    public const string ServiceIdChunksHybridSemantic = "az-cogsearch-chunks-hybrid-semantic";
    private readonly AppSettings settings;
    private readonly Uri searchServiceUrl;
    private readonly AzureKeyCredential searchServiceAdminCredential;
    private readonly IEmbeddingService embeddingService;

    public AzureSearchSearchService(AppSettings settings, IEmbeddingService embeddingService)
    {
        ArgumentNullException.ThrowIfNull(settings.SearchServiceUrl);
        ArgumentNullException.ThrowIfNull(settings.SearchServiceAdminKey);
        this.settings = settings;
        this.embeddingService = embeddingService;
        this.searchServiceUrl = new Uri(this.settings.SearchServiceUrl);
        this.searchServiceAdminCredential = new AzureKeyCredential(this.settings.SearchServiceAdminKey);
    }

    public IList<Task<SearchResponse>> SearchAsync(SearchRequest request)
    {
        var searchTasks = new List<Task<SearchResponse>>();
        if (request.ShouldInclude(ServiceIdTextStandard))
        {
            searchTasks.Add(SearchAsync(request, 310, ServiceIdTextStandard, "Azure Cognitive Search - Standard", SearchQueryType.Simple));
        }
        if (request.ShouldInclude(ServiceIdTextSemantic))
        {
            searchTasks.Add(SearchAsync(request, 300, ServiceIdTextSemantic, "Azure Cognitive Search - Semantic", SearchQueryType.Semantic));
        }
        if (request.ShouldInclude(ServiceIdChunksTextStandard))
        {
            searchTasks.Add(SearchAsync(request, 240, ServiceIdChunksTextStandard, "Azure Cognitive Search - Chunks - Text - Standard", SearchQueryType.Simple, useChunksIndex: true, useVectorSearch: false));
        }
        if (request.ShouldInclude(ServiceIdChunksTextSemantic))
        {
            searchTasks.Add(SearchAsync(request, 230, ServiceIdChunksTextSemantic, "Azure Cognitive Search - Chunks - Text - Semantic", SearchQueryType.Semantic, useChunksIndex: true, useVectorSearch: false));
        }
        if (request.ShouldInclude(ServiceIdChunksVector))
        {
            searchTasks.Add(SearchAsync(request, 220, ServiceIdChunksVector, "Azure Cognitive Search - Chunks - Vector", null, useChunksIndex: true, useVectorSearch: true, vectorOnlySearch: true));
        }
        if (request.ShouldInclude(ServiceIdChunksHybridStandard))
        {
            searchTasks.Add(SearchAsync(request, 210, ServiceIdChunksHybridStandard, "Azure Cognitive Search - Chunks - Hybrid - Standard", SearchQueryType.Simple, useChunksIndex: true, useVectorSearch: true, vectorOnlySearch: false));
        }
        if (request.ShouldInclude(ServiceIdChunksHybridSemantic))
        {
            searchTasks.Add(SearchAsync(request, 200, ServiceIdChunksHybridSemantic, "Azure Cognitive Search - Chunks - Hybrid - Semantic", SearchQueryType.Semantic, useChunksIndex: true, useVectorSearch: true, vectorOnlySearch: false));
        }
        return searchTasks;
    }

    private async Task<SearchResponse> SearchAsync(SearchRequest request, int priority, string serviceId, string serviceName, SearchQueryType? queryType, bool useChunksIndex = false, bool useVectorSearch = false, bool vectorOnlySearch = false)
    {
        var response = new SearchResponse
        {
            Priority = priority,
            ServiceId = serviceId,
            ServiceName = serviceName
        };
        try
        {
            var searchOptions = new SearchOptions
            {
                QueryType = queryType,
                HighlightPreTag = "<mark>",
                HighlightPostTag = "</mark>"
            };

            if (queryType == SearchQueryType.Semantic)
            {
                searchOptions.SemanticConfigurationName = Constants.ConfigurationNames.SemanticConfigurationNameDefault;
                searchOptions.QueryLanguage = QueryLanguage.EnUs;
                searchOptions.QueryAnswer = QueryAnswerType.Extractive;
                searchOptions.QueryCaption = QueryCaptionType.Extractive;
            }

            var indexName = Constants.IndexNames.BlobDocuments;
            if (!useChunksIndex)
            {
                searchOptions.Select.Add(nameof(Document.Id));
                searchOptions.Select.Add(nameof(Document.Title));
                searchOptions.Select.Add(nameof(Document.FilePath));
                searchOptions.HighlightFields.Add(nameof(Document.Content));
            }
            else
            {
                indexName = Constants.IndexNames.BlobChunks;
                searchOptions.Select.Add(nameof(DocumentChunk.Id));
                searchOptions.Select.Add(nameof(DocumentChunk.SourceDocumentId));
                searchOptions.Select.Add(nameof(DocumentChunk.SourceDocumentTitle));
                searchOptions.Select.Add(nameof(DocumentChunk.Content));
                searchOptions.Select.Add(nameof(DocumentChunk.ChunkIndex));
                if (!vectorOnlySearch)
                {
                    // Don't request highlights for vector-only search, as that doesn't make
                    // sense and will return an error.
                    searchOptions.HighlightFields.Add(nameof(DocumentChunk.Content));
                }
            }
            var requestedSearchClient = new SearchClient(this.searchServiceUrl, indexName, this.searchServiceAdminCredential);

            if (useVectorSearch)
            {
                ArgumentNullException.ThrowIfNull(request.Query);

                // Generate an embedding vector for the search query text.
                var queryEmbeddings = await this.embeddingService.GetEmbeddingAsync(request.Query);

                // Pass the vector as part of the search options.
                searchOptions.Vectors.Add(new SearchQueryVector
                {
                    KNearestNeighborsCount = 3,
                    Fields = { nameof(DocumentChunk.ContentVector) },
                    Value = queryEmbeddings
                });
            }

            // Don't pass the search query text for vector-only search.
            var searchText = vectorOnlySearch ? null : request.Query;

            // Perform the search.
            var serviceResponse = await requestedSearchClient.SearchAsync<SearchDocument>(searchText, searchOptions);
            response.Answers = serviceResponse.Value.Answers == null ? Array.Empty<SearchAnswer>() : serviceResponse.Value.Answers.Select(a => new SearchAnswer { Key = a.Key, Score = a.Score, Text = string.IsNullOrWhiteSpace(a.Highlights) ? a.Text : a.Highlights }).ToList();
            response.Captions = serviceResponse.Value.Captions == null ? Array.Empty<string>() : serviceResponse.Value.Captions.Select(c => string.IsNullOrWhiteSpace(c.Highlights) ? c.Text : c.Highlights).ToList();
            foreach (var result in serviceResponse.Value.GetResults())
            {
                var documentKey = default(string);
                var documentId = default(string);
                var documentTitle = default(string);
                var documentContent = default(string);
                var chunkIndex = default(int?);
                if (!useChunksIndex)
                {
                    documentKey = result.Document.GetString(nameof(Document.Id));
                    documentId = documentKey;
                    documentTitle = result.Document.GetString(nameof(Document.Title));
                }
                else
                {
                    documentKey = result.Document.GetString(nameof(DocumentChunk.Id));
                    documentId = result.Document.GetString(nameof(DocumentChunk.SourceDocumentId));
                    documentTitle = result.Document.GetString(nameof(DocumentChunk.SourceDocumentTitle));
                    documentContent = result.Document.GetString(nameof(DocumentChunk.Content));
                    chunkIndex = result.Document.GetInt32(nameof(DocumentChunk.ChunkIndex));
                }
                var searchResult = new SearchResult
                {
                    Score = result.Score,
                    Highlights = result.Highlights ?? new Dictionary<string, IList<string>>(),
                    Captions = result.Captions == null ? new List<string>() : result.Captions.Select(c => string.IsNullOrWhiteSpace(c.Highlights) ? c.Text : c.Highlights).ToList(),
                    DocumentId = documentId,
                    DocumentTitle = documentTitle,
                    ChunkIndex = chunkIndex
                };
                response.SearchResults.Add(searchResult);

                // If using the chunks index for vector-only search, add the actual chunk content to the
                // response's captions to at least show the context of the response.
                if (vectorOnlySearch && !string.IsNullOrWhiteSpace(documentContent))
                {
                    searchResult.Captions.Add(documentContent);
                }

                // Answers may refer to chunk IDs, ensure to map them to the right document ID.
                foreach (var answerForDocumentKey in response.Answers.Where(a => a.Key == documentKey))
                {
                    answerForDocumentKey.DocumentId = documentId;
                    answerForDocumentKey.DocumentTitle = documentTitle;
                }
            }
        }
        catch (Exception ex)
        {
            response.Error = ex.Message;
        }
        return response;
    }
}