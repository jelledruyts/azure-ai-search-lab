using System.Net;
using Azure.AISearch.WebApp.Models;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;

namespace Azure.AISearch.WebApp.Services;

public class AzureSearchConfigurationService
{
    // TODO: Remove consts in favor of readability?
    // TODO: Allow content language to be specified?
    public const string SemanticConfigurationNameDefault = "default";
    private const string VectorSearchConfigurationNameDefault = "default";
    private readonly AppSettings settings;
    private readonly Uri searchServiceUrl;
    private readonly AzureKeyCredential searchServiceAdminCredential;

    public AzureSearchConfigurationService(AppSettings settings)
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
            // Create the search index for the documents.
            var documentsIndex = GetDocumentsSearchIndex(documentsIndexName);
            await indexClient.CreateIndexAsync(documentsIndex);

            // Create the Blob Storage data source for the documents.
            var documentsDataSourceConnection = new SearchIndexerDataSourceConnection($"{documentsIndexName}-datasource", SearchIndexerDataSourceType.AzureBlob, this.settings.StorageAccountConnectionString, new SearchIndexerDataContainer(documentsContainerName));
            await indexerClient.CreateDataSourceConnectionAsync(documentsDataSourceConnection);

            // Create the skillset which chunks and vectorizes the document's content and stores it as JSON
            // files in blob storage (as a knowledge store) so it can be indexed separately.
            var skillset = GetDocumentsIndexerSkillset(documentsIndexName, chunksContainerName);
            await indexerClient.CreateSkillsetAsync(skillset);

            // Create the indexer.
            var documentsIndexer = new SearchIndexer($"{documentsIndexName}-indexer", documentsDataSourceConnection.Name, documentsIndex.Name)
            {
                Schedule = new IndexingSchedule(TimeSpan.FromMinutes(5)),
                FieldMappings =
                {
                    new FieldMapping("metadata_storage_path") { TargetFieldName = nameof(Document.Id), MappingFunction = new FieldMappingFunction("base64Encode") },
                    new FieldMapping("metadata_storage_name") { TargetFieldName = nameof(Document.Title) },
                    new FieldMapping("content") { TargetFieldName = nameof(Document.Content) },
                    new FieldMapping("metadata_storage_path") { TargetFieldName = nameof(Document.FilePath) }
                },
                SkillsetName = skillset.Name
            };
            await indexerClient.CreateIndexerAsync(documentsIndexer);

            if (skillset != null)
            {
                // Create the index which represents the chunked data from the main indexer's knowledge store.
                // ArgumentNullException.ThrowIfNull(chunksIndexName);
                var chunkSearchIndex = GetDocumentChunksSearchIndex(chunksIndexName);
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
                        SemanticConfigurationNameDefault,
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

    private SearchIndexerSkillset GetDocumentsIndexerSkillset(string indexName, string knowledgeStoreContainerName)
    {
        // TODO: Remove consts in favor of readability?
        const string fieldToVectorize = nameof(Document.Content);
        const string enrichedDocumentSubPathForChunks = "chunks"; // Store the chunks under the "chunks" property of the enriched document.
        const string enrichedDocumentPathForChunks = $"/document/{fieldToVectorize}/{enrichedDocumentSubPathForChunks}/*";

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
                        new InputFieldMappingEntry("document_id") { Source = $"/document/{nameof(Document.Id)}" },
                        new InputFieldMappingEntry("text") { Source = $"/document/{fieldToVectorize}" },
                        new InputFieldMappingEntry("filepath") { Source = $"/document/{nameof(Document.FilePath)}" },
                        new InputFieldMappingEntry("fieldname") { Source = $"='{fieldToVectorize}'" }
                    },
                    Outputs =
                    {
                        new OutputFieldMappingEntry("chunks") { TargetName = enrichedDocumentSubPathForChunks } // Store the chunks output under "/document/Content/chunks".
                    }
                }
            },
            KnowledgeStore = new KnowledgeStore(settings.StorageAccountConnectionString, Array.Empty<KnowledgeStoreProjection>())
            {
                Projections =
                {
                    new KnowledgeStoreProjection
                    {
                        Objects =
                        {
                            new KnowledgeStoreObjectProjectionSelector(knowledgeStoreContainerName)
                            {
                                GeneratedKeyName = nameof(DocumentChunk.Id),
                                SourceContext = enrichedDocumentPathForChunks, // Iterate over each chunk in "/document/Content/chunks".
                                Inputs =
                                {
                                    new InputFieldMappingEntry(nameof(DocumentChunk.SourceDocumentId)) { Source = $"/document/{nameof(Document.Id)}" },
                                    new InputFieldMappingEntry(nameof(DocumentChunk.SourceDocumentFilePath)) { Source = $"/document/{nameof(Document.FilePath)}" },
                                    new InputFieldMappingEntry(nameof(DocumentChunk.SourceDocumentContentField)) { Source = $"{enrichedDocumentPathForChunks}/embedding_metadata/fieldname" },
                                    new InputFieldMappingEntry(nameof(DocumentChunk.SourceDocumentTitle)) { Source = $"/document/{nameof(Document.Title)}" },
                                    new InputFieldMappingEntry(nameof(DocumentChunk.Content)) { Source = $"{enrichedDocumentPathForChunks}/content" },
                                    new InputFieldMappingEntry(nameof(DocumentChunk.ContentVector)) { Source = $"{enrichedDocumentPathForChunks}/embedding_metadata/embedding" },
                                    new InputFieldMappingEntry(nameof(DocumentChunk.ChunkIndex)) { Source = $"{enrichedDocumentPathForChunks}/embedding_metadata/index" },
                                    new InputFieldMappingEntry(nameof(DocumentChunk.ChunkOffset)) { Source = $"{enrichedDocumentPathForChunks}/embedding_metadata/offset" },
                                    new InputFieldMappingEntry(nameof(DocumentChunk.ChunkLength)) { Source = $"{enrichedDocumentPathForChunks}/embedding_metadata/length" }
                                }
                            }
                        }
                    }
                }
            }
        };
    }

    private static SearchIndex GetDocumentChunksSearchIndex(string chunkIndexName)
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
                new SearchField(nameof(DocumentChunk.ContentVector), SearchFieldDataType.Collection(SearchFieldDataType.Single)) { IsFilterable = false, IsSortable = false, IsFacetable = false, IsSearchable = true, VectorSearchDimensions = 1536, VectorSearchConfiguration = VectorSearchConfigurationNameDefault },
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
                        SemanticConfigurationNameDefault,
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
                    new HnswVectorSearchAlgorithmConfiguration(VectorSearchConfigurationNameDefault)
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