using Azure.AISearch.WebApp.Models;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;

namespace Azure.AISearch.WebApp.Services;

public class AzureCognitiveSearchService
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

    public async Task<SearchResponse> SearchAsync(SearchRequest request)
    {
        var response = new SearchResponse
        {
            RequestId = request.Id,
            DisplayName = request.DisplayName
        };
        var useDocumentsIndex = true;
        if (request.SearchIndexName == Constants.IndexNames.BlobDocuments)
        {
            useDocumentsIndex = true;
        }
        else if (request.SearchIndexName == Constants.IndexNames.BlobChunks)
        {
            useDocumentsIndex = false;
        }
        else
        {
            // Cannot infer which shape the search results will have, so don't continue.
            throw new NotSupportedException($"Search index \"{request.SearchIndexName}\" is not supported.");
        }

        var searchOptions = new SearchOptions
        {
            QueryType = request.TextQueryType == TextQueryType.Semantic ? SearchQueryType.Semantic : SearchQueryType.Simple,
            HighlightPreTag = "<mark>",
            HighlightPostTag = "</mark>"
        };

        if (request.TextQueryType == TextQueryType.Semantic)
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
        var requestedSearchClient = new SearchClient(this.searchServiceUrl, request.SearchIndexName, this.searchServiceAdminCredential);

        if (request.IsVectorSearch)
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
        var searchText = request.QueryType == QueryType.Vector ? null : request.Query;

        // Perform the search.
        var serviceResponse = await requestedSearchClient.SearchAsync<SearchDocument>(searchText, searchOptions);
        response.Answers = serviceResponse.Value.Answers == null ? Array.Empty<SearchAnswer>() : serviceResponse.Value.Answers.Select(a => new SearchAnswer { SearchIndexName = request.SearchIndexName, SearchIndexKey = a.Key, Score = a.Score, Text = string.IsNullOrWhiteSpace(a.Highlights) ? a.Text : a.Highlights }).ToList();
        response.Captions = serviceResponse.Value.Captions == null ? Array.Empty<string>() : serviceResponse.Value.Captions.Select(c => string.IsNullOrWhiteSpace(c.Highlights) ? c.Text : c.Highlights).ToList();
        foreach (var result in serviceResponse.Value.GetResults())
        {
            var searchResult = useDocumentsIndex ? GetSearchResultForDocumentsIndex(result) : GetSearchResultForChunksIndex(result, request.QueryType);
            searchResult.SearchIndexName = request.SearchIndexName;
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