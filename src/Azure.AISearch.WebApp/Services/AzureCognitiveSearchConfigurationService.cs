using System.Net;
using Azure.AISearch.WebApp.Models;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;

namespace Azure.AISearch.WebApp.Services;

public class AzureCognitiveSearchConfigurationService
{
    private readonly AppSettings settings;
    private readonly Uri searchServiceUrl;
    private readonly AzureKeyCredential searchServiceAdminCredential;

    public AzureCognitiveSearchConfigurationService(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings.SearchServiceUrl);
        ArgumentNullException.ThrowIfNull(settings.SearchServiceAdminKey);
        this.settings = settings;
        this.searchServiceUrl = new Uri(this.settings.SearchServiceUrl);
        this.searchServiceAdminCredential = new AzureKeyCredential(this.settings.SearchServiceAdminKey);
    }

    public async Task InitializeSearchAsync(string documentsIndexName, string chunksIndexName, string documentsContainerName, string chunksContainerName)
    {
        var indexClient = new SearchIndexClient(this.searchServiceUrl, searchServiceAdminCredential);
        var indexerClient = new SearchIndexerClient(this.searchServiceUrl, searchServiceAdminCredential);
        if (!await SearchIndexExistsAsync(indexClient, documentsIndexName))
        {
            await CreateDocumentsIndex(indexClient, indexerClient, documentsIndexName, documentsContainerName, chunksContainerName);
        }
        if (!await SearchIndexExistsAsync(indexClient, chunksIndexName))
        {
            await CreateChunksIndex(indexClient, indexerClient, chunksIndexName, chunksContainerName);
        }
    }

    private static async Task<bool> SearchIndexExistsAsync(SearchIndexClient indexClient, string indexName)
    {
        try
        {
            await indexClient.GetIndexAsync(indexName);
            return true;
        }
        catch (RequestFailedException ex) when (ex.Status == (int)HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    private async Task CreateDocumentsIndex(SearchIndexClient indexClient, SearchIndexerClient indexerClient, string documentsIndexName, string documentsContainerName, string chunksContainerName)
    {
        // Create the search index for the documents.
        var documentsIndex = GetDocumentsSearchIndex(documentsIndexName);
        await indexClient.CreateIndexAsync(documentsIndex);

        // Create the Blob Storage data source for the documents.
        var documentsDataSourceConnection = new SearchIndexerDataSourceConnection($"{documentsIndexName}-datasource", SearchIndexerDataSourceType.AzureBlob, this.settings.StorageAccountConnectionString, new SearchIndexerDataContainer(documentsContainerName));
        await indexerClient.CreateDataSourceConnectionAsync(documentsDataSourceConnection);

        // Create the skillset which chunks and vectorizes the document's content and stores it as JSON
        // files in blob storage (as a knowledge store) so it can be indexed separately.
        var skillset = GetDocumentsSearchIndexerSkillset(documentsIndexName, chunksContainerName);
        await indexerClient.CreateSkillsetAsync(skillset);

        // Create the indexer.
        var documentsIndexer = new SearchIndexer($"{documentsIndexName}-indexer", documentsDataSourceConnection.Name, documentsIndex.Name)
        {
            Schedule = new IndexingSchedule(TimeSpan.FromMinutes(5)),
            FieldMappings =
                {
                    // Map the full blob URL to the document ID, base64 encoded to ensure it has only valid characters for a document ID.
                    new FieldMapping("metadata_storage_path") { TargetFieldName = nameof(Document.Id), MappingFunction = new FieldMappingFunction("base64Encode") },
                    // Map the file name to the document title.
                    new FieldMapping("metadata_storage_name") { TargetFieldName = nameof(Document.Title) },
                    // Map the file content to the document content.
                    new FieldMapping("content") { TargetFieldName = nameof(Document.Content) },
                    // Map the full blob URL as the document file path.
                    new FieldMapping("metadata_storage_path") { TargetFieldName = nameof(Document.FilePath) }
                },
            // Use the skillset for chunking and embedding.
            SkillsetName = skillset.Name
        };
        await indexerClient.CreateIndexerAsync(documentsIndexer);
    }

    private static SearchIndex GetDocumentsSearchIndex(string documentsIndexName)
    {
        return new SearchIndex(documentsIndexName)
        {
            Fields =
            {
                new SearchField(nameof(Document.Id), SearchFieldDataType.String) { IsKey = true, IsFilterable = true, IsSortable = true, IsFacetable = false, IsSearchable = true },
                new SearchField(nameof(Document.Title), SearchFieldDataType.String) { IsFilterable = true, IsSortable = true, IsFacetable = false, IsSearchable = true, AnalyzerName = LexicalAnalyzerName.EnMicrosoft },
                new SearchField(nameof(Document.Content), SearchFieldDataType.String) { IsFilterable = false, IsSortable = false, IsFacetable = false, IsSearchable = true, AnalyzerName = LexicalAnalyzerName.EnMicrosoft },
                new SearchField(nameof(Document.FilePath), SearchFieldDataType.String) { IsFilterable = true, IsSortable = true, IsFacetable = false, IsSearchable = true, AnalyzerName = LexicalAnalyzerName.StandardLucene }
            },
            SemanticSettings = new SemanticSettings
            {
                Configurations =
                {
                    new SemanticConfiguration
                    (
                        Constants.ConfigurationNames.SemanticConfigurationNameDefault,
                        new PrioritizedFields
                        {
                            TitleField = new SemanticField { FieldName = nameof(Document.Title) },
                            ContentFields =
                            {
                                new SemanticField { FieldName = nameof(Document.Content) }
                            }
                        }
                    )
                }
            }
        };
    }

    private SearchIndexerSkillset GetDocumentsSearchIndexerSkillset(string indexName, string knowledgeStoreContainerName)
    {
        return new SearchIndexerSkillset($"{indexName}-skillset", Array.Empty<SearchIndexerSkill>())
        {
            Skills =
            {
                new WebApiSkill(Array.Empty<InputFieldMappingEntry>(), Array.Empty<OutputFieldMappingEntry>(), settings.TextEmbedderFunctionEndpoint)
                {
                    Name = "chunking-embedding-skill",
                    Context = $"/document/{nameof(Document.Content)}",
                    HttpMethod = "POST",
                    Timeout = TimeSpan.FromMinutes(3),
                    BatchSize = 1,
                    DegreeOfParallelism = 1,
                    HttpHeaders =
                    {
                        { "Authorization", settings.TextEmbedderFunctionApiKey }
                    },
                    Inputs =
                    {
                        // Pass the document ID.
                        new InputFieldMappingEntry("document_id") { Source = $"/document/{nameof(Document.Id)}" },
                        // Pass the document content as the text to chunk and created the embeddings for.
                        new InputFieldMappingEntry("text") { Source = $"/document/{nameof(Document.Content)}" },
                        // Pass the document file path.
                        new InputFieldMappingEntry("filepath") { Source = $"/document/{nameof(Document.FilePath)}" },
                        // Pass the field name as a string literal.
                        new InputFieldMappingEntry("fieldname") { Source = $"='{nameof(Document.Content)}'" },
                        // Pass the embedding deployment to use as a string literal.
                        new InputFieldMappingEntry("embedding_deployment_name") { Source = $"='{this.settings.OpenAIEmbeddingDeployment}'" }
                    },
                    Outputs =
                    {
                        // Store the chunks output under "/document/Content/chunks".
                        new OutputFieldMappingEntry("chunks") { TargetName = "chunks" }
                    }
                }
            },
            KnowledgeStore = new KnowledgeStore(settings.StorageAccountConnectionString, Array.Empty<KnowledgeStoreProjection>())
            {
                Projections =
                {
                    new KnowledgeStoreProjection
                    {
                        // Project the chunks to a knowledge store container, where each chunk will be its own JSON document that can be indexed later.
                        Objects =
                        {
                            new KnowledgeStoreObjectProjectionSelector(knowledgeStoreContainerName)
                            {
                                GeneratedKeyName = nameof(DocumentChunk.Id),
                                // Iterate over each chunk in "/document/Content/chunks".
                                SourceContext = $"/document/{nameof(Document.Content)}/chunks/*",
                                Inputs =
                                {
                                    // Map the document ID.
                                    new InputFieldMappingEntry(nameof(DocumentChunk.SourceDocumentId)) { Source = $"/document/{nameof(Document.Id)}" },
                                    // Map the document file path.
                                    new InputFieldMappingEntry(nameof(DocumentChunk.SourceDocumentFilePath)) { Source = $"/document/{nameof(Document.FilePath)}" },
                                    // Map the Content field name.
                                    new InputFieldMappingEntry(nameof(DocumentChunk.SourceDocumentContentField)) { Source = $"/document/{nameof(Document.Content)}/chunks/*/embedding_metadata/fieldname" },
                                    // Map the document title.
                                    new InputFieldMappingEntry(nameof(DocumentChunk.SourceDocumentTitle)) { Source = $"/document/{nameof(Document.Title)}" },
                                    // Map the chunked content.
                                    new InputFieldMappingEntry(nameof(DocumentChunk.Content)) { Source = $"/document/{nameof(Document.Content)}/chunks/*/content" },
                                    // Map the embedding vector.
                                    new InputFieldMappingEntry(nameof(DocumentChunk.ContentVector)) { Source = $"/document/{nameof(Document.Content)}/chunks/*/embedding_metadata/embedding" },
                                    // Map the chunk index.
                                    new InputFieldMappingEntry(nameof(DocumentChunk.ChunkIndex)) { Source = $"/document/{nameof(Document.Content)}/chunks/*/embedding_metadata/index" },
                                    // Map the chunk offset.
                                    new InputFieldMappingEntry(nameof(DocumentChunk.ChunkOffset)) { Source = $"/document/{nameof(Document.Content)}/chunks/*/embedding_metadata/offset" },
                                    // Map the chunk length.
                                    new InputFieldMappingEntry(nameof(DocumentChunk.ChunkLength)) { Source = $"/document/{nameof(Document.Content)}/chunks/*/embedding_metadata/length" }
                                }
                            }
                        }
                    }
                }
            }
        };
    }

    private async Task CreateChunksIndex(SearchIndexClient indexClient, SearchIndexerClient indexerClient, string chunksIndexName, string chunksContainerName)
    {
        // Create the index which represents the chunked data from the main indexer's knowledge store.
        var chunkSearchIndex = GetChunksSearchIndex(chunksIndexName);
        await indexClient.CreateIndexAsync(chunkSearchIndex);

        // Create the Storage data source for the chunked data.
        var chunksDataSourceConnection = new SearchIndexerDataSourceConnection($"{chunksIndexName}-datasource", SearchIndexerDataSourceType.AzureBlob, settings.StorageAccountConnectionString, new SearchIndexerDataContainer(chunksContainerName));
        await indexerClient.CreateDataSourceConnectionAsync(chunksDataSourceConnection);

        // Create the chunk indexer based on the JSON files in the knowledge store.
        var chunksIndexer = new SearchIndexer($"{chunksIndexName}-indexer", chunksDataSourceConnection.Name, chunkSearchIndex.Name)
        {
            Schedule = new IndexingSchedule(TimeSpan.FromMinutes(5)),
            Parameters = new IndexingParameters()
            {
                IndexingParametersConfiguration = new IndexingParametersConfiguration()
                {
                    ParsingMode = BlobIndexerParsingMode.Json
                }
            }
        };
        await indexerClient.CreateIndexerAsync(chunksIndexer);
    }

    private static SearchIndex GetChunksSearchIndex(string chunkIndexName)
    {
        return new SearchIndex(chunkIndexName)
        {
            Fields =
            {
                new SearchField(nameof(DocumentChunk.Id), SearchFieldDataType.String) { IsKey = true, IsFilterable = true, IsSortable = true, IsFacetable = false, IsSearchable = false },
                new SearchField(nameof(DocumentChunk.ChunkIndex), SearchFieldDataType.Int64) { IsFilterable = true, IsSortable = true, IsFacetable = false, IsSearchable = false },
                new SearchField(nameof(DocumentChunk.ChunkOffset), SearchFieldDataType.Int64) { IsFilterable = true, IsSortable = true, IsFacetable = false, IsSearchable = false },
                new SearchField(nameof(DocumentChunk.ChunkLength), SearchFieldDataType.Int64) { IsFilterable = true, IsSortable = true, IsFacetable = false, IsSearchable = false },
                new SearchField(nameof(DocumentChunk.Content), SearchFieldDataType.String) { IsFilterable = false, IsSortable = false, IsFacetable = false, IsSearchable = true, AnalyzerName = LexicalAnalyzerName.EnMicrosoft },
                new SearchField(nameof(DocumentChunk.ContentVector), SearchFieldDataType.Collection(SearchFieldDataType.Single)) { IsFilterable = false, IsSortable = false, IsFacetable = false, IsSearchable = true, VectorSearchDimensions = Constants.VectorDimensions.TextEmbeddingAda002, VectorSearchConfiguration = Constants.ConfigurationNames.VectorSearchConfigurationNameDefault },
                new SearchField(nameof(DocumentChunk.SourceDocumentId), SearchFieldDataType.String) { IsFilterable = true, IsSortable = true, IsFacetable = false, IsSearchable = false },
                new SearchField(nameof(DocumentChunk.SourceDocumentContentField), SearchFieldDataType.String) { IsFilterable = true, IsSortable = true, IsFacetable = false, IsSearchable = false },
                new SearchField(nameof(DocumentChunk.SourceDocumentTitle), SearchFieldDataType.String) { IsFilterable = true, IsSortable = true, IsFacetable = false, IsSearchable = true, AnalyzerName = LexicalAnalyzerName.EnMicrosoft },
                new SearchField(nameof(DocumentChunk.SourceDocumentFilePath), SearchFieldDataType.String) { IsFilterable = true, IsSortable = true, IsFacetable = false, IsSearchable = true, AnalyzerName = LexicalAnalyzerName.StandardLucene }
            },
            SemanticSettings = new SemanticSettings
            {
                Configurations =
                {
                    new SemanticConfiguration
                    (
                        Constants.ConfigurationNames.SemanticConfigurationNameDefault,
                        new PrioritizedFields()
                        {
                            TitleField = new SemanticField { FieldName = nameof(DocumentChunk.SourceDocumentTitle) },
                            ContentFields =
                            {
                                new SemanticField { FieldName = nameof(DocumentChunk.Content) }
                            },
                            KeywordFields =
                            {
                            }
                        }
                    )
                }
            },
            VectorSearch = new VectorSearch
            {
                AlgorithmConfigurations =
                {
                    new HnswVectorSearchAlgorithmConfiguration(Constants.ConfigurationNames.VectorSearchConfigurationNameDefault)
                    {
                        Parameters = new HnswParameters
                        {
                            M = 4,
                            EfConstruction = 400,
                            EfSearch = 500,
                            Metric = VectorSearchAlgorithmMetric.Cosine
                        }
                    }
                }
            }
        };
    }
}