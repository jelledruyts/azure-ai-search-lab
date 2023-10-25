using Azure.AISearch.WebApp.Models;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;

namespace Azure.AISearch.WebApp.Services;

public class AzureCognitiveSearchService : ISearchService
{
    private readonly AppSettings settings;
    private readonly Uri searchServiceUrl;
    private readonly AzureKeyCredential searchServiceAdminCredential;
    private readonly IEmbeddingService embeddingService;

    public AzureCognitiveSearchService(AppSettings settings, IEmbeddingService embeddingService)
    {
        ArgumentNullException.ThrowIfNull(settings.SearchServiceUrl);
        ArgumentNullException.ThrowIfNull(settings.SearchServiceAdminKey);
        this.settings = settings;
        this.embeddingService = embeddingService;
        this.searchServiceUrl = new Uri(this.settings.SearchServiceUrl);
        this.searchServiceAdminCredential = new AzureKeyCredential(this.settings.SearchServiceAdminKey);
    }

    public bool CanHandle(SearchRequest request)
    {
        return request.Engine == EngineType.AzureCognitiveSearch;
    }

    public async Task<SearchResponse> SearchAsync(SearchRequest request)
    {
        var useDocumentsIndex = request.SearchIndex == SearchIndexType.Documents;
        var indexName = useDocumentsIndex ? this.settings.SearchIndexNameBlobDocuments : this.settings.SearchIndexNameBlobChunks;
        var searchOptions = new SearchOptions
        {
            QueryType = request.IsSemanticSearch ? SearchQueryType.Semantic : (request.QuerySyntax == QuerySyntax.Lucene ? SearchQueryType.Full : SearchQueryType.Simple),
            HighlightPreTag = "<mark>",
            HighlightPostTag = "</mark>"
        };

        if (request.IsSemanticSearch)
        {
            searchOptions.SemanticConfigurationName = Constants.ConfigurationNames.SemanticConfigurationNameDefault;
            searchOptions.QueryLanguage = QueryLanguage.EnUs;
            searchOptions.QueryAnswer = QueryAnswerType.Extractive;
            searchOptions.QueryCaption = QueryCaptionType.Extractive;
        }

        if (useDocumentsIndex)
        {
            SetSearchOptionsForDocumentsIndex(searchOptions);
        }
        else
        {
            SetSearchOptionsForChunksIndex(searchOptions, request.QueryType);
        }
        var requestedSearchClient = new SearchClient(this.searchServiceUrl, indexName, this.searchServiceAdminCredential);

        if (request.IsVectorSearch)
        {
            ArgumentNullException.ThrowIfNull(request.Query);

            // Generate an embedding vector for the search query text.
            var queryEmbeddings = await this.embeddingService.GetEmbeddingAsync(request.Query);

            // Pass the vector as part of the search options.
            searchOptions.Vectors.Add(new SearchQueryVector
            {
                KNearestNeighborsCount = request.VectorNearestNeighborsCount ?? Constants.Defaults.VectorNearestNeighborsCount,
                Fields = { nameof(DocumentChunk.ContentVector) },
                Value = queryEmbeddings
            });
        }

        // Don't pass the search query text for vector-only search.
        var searchText = request.QueryType == QueryType.Vector ? null : request.Query;

        // Perform the search.
        var serviceResponse = await requestedSearchClient.SearchAsync<SearchDocument>(searchText, searchOptions);
        var response = new SearchResponse();
        response.Answers = serviceResponse.Value.Answers == null ? Array.Empty<SearchAnswer>() : serviceResponse.Value.Answers.Select(a => new SearchAnswer { SearchIndexName = indexName, SearchIndexKey = a.Key, Score = a.Score, Text = string.IsNullOrWhiteSpace(a.Highlights) ? a.Text : a.Highlights }).ToList();
        response.Captions = serviceResponse.Value.Captions == null ? Array.Empty<string>() : serviceResponse.Value.Captions.Select(c => string.IsNullOrWhiteSpace(c.Highlights) ? c.Text : c.Highlights).ToList();
        foreach (var result in serviceResponse.Value.GetResults())
        {
            var searchResult = useDocumentsIndex ? GetSearchResultForDocumentsIndex(result) : GetSearchResultForChunksIndex(result, request.QueryType);
            searchResult.SearchIndexName = indexName;
            response.SearchResults.Add(searchResult);

            // Answers may refer to chunk IDs, ensure to map them to the right document ID.
            foreach (var answerForDocumentKey in response.Answers.Where(a => a.SearchIndexKey == searchResult.SearchIndexKey))
            {
                answerForDocumentKey.DocumentId = searchResult.DocumentId;
                answerForDocumentKey.DocumentTitle = searchResult.DocumentTitle;
            }
        }
        return response;
    }

    private void SetSearchOptionsForDocumentsIndex(SearchOptions searchOptions)
    {
        searchOptions.Select.Add(nameof(Document.Id));
        searchOptions.Select.Add(nameof(Document.Title));
        searchOptions.Select.Add(nameof(Document.FilePath));
        searchOptions.HighlightFields.Add(nameof(Document.Content));
    }

    private SearchResult GetSearchResultForDocumentsIndex(SearchResult<SearchDocument> result)
    {
        var searchResult = GetSearchResult(result);

        searchResult.SearchIndexKey = result.Document.GetString(nameof(Document.Id));
        searchResult.DocumentId = result.Document.GetString(nameof(Document.Id));
        searchResult.DocumentTitle = result.Document.GetString(nameof(Document.Title));

        return searchResult;
    }

    private void SetSearchOptionsForChunksIndex(SearchOptions searchOptions, QueryType? queryType)
    {
        searchOptions.Select.Add(nameof(DocumentChunk.Id));
        searchOptions.Select.Add(nameof(DocumentChunk.SourceDocumentId));
        searchOptions.Select.Add(nameof(DocumentChunk.SourceDocumentTitle));
        searchOptions.Select.Add(nameof(DocumentChunk.Content));
        searchOptions.Select.Add(nameof(DocumentChunk.ChunkIndex));
        if (queryType != QueryType.Vector)
        {
            // Don't request highlights for vector-only search, as that doesn't make
            // sense and will return an error.
            searchOptions.HighlightFields.Add(nameof(DocumentChunk.Content));
        }
    }

    private SearchResult GetSearchResultForChunksIndex(SearchResult<SearchDocument> result, QueryType? queryType)
    {
        var searchResult = GetSearchResult(result);

        searchResult.SearchIndexKey = result.Document.GetString(nameof(DocumentChunk.Id));
        searchResult.DocumentId = result.Document.GetString(nameof(DocumentChunk.SourceDocumentId));
        searchResult.DocumentTitle = result.Document.GetString(nameof(DocumentChunk.SourceDocumentTitle));
        searchResult.ChunkIndex = result.Document.GetInt32(nameof(DocumentChunk.ChunkIndex));

        if (queryType == QueryType.Vector)
        {
            // If using vector-only search, add the actual chunk content to the response's captions
            // to at least show the context of the response.
            searchResult.Captions.Add(result.Document.GetString(nameof(DocumentChunk.Content)));
        }

        return searchResult;
    }

    private SearchResult GetSearchResult<T>(SearchResult<T> result)
    {
        return new SearchResult
        {
            Score = result.Score,
            Highlights = result.Highlights ?? new Dictionary<string, IList<string>>(),
            Captions = result.Captions == null ? new List<string>() : result.Captions.Select(c => string.IsNullOrWhiteSpace(c.Highlights) ? c.Text : c.Highlights).ToList()
        };
    }
}