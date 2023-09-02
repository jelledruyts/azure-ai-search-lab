using System.Net;
using Azure.AISearch.WebApp.Models;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;

namespace Azure.AISearch.WebApp.Services;

public class AzureCognitiveSearchConfigurationService
{
    private readonly AppSettings settings;
    private readonly SearchIndexClient indexClient;
    private readonly SearchIndexerClient indexerClient;

    public AzureCognitiveSearchConfigurationService(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings.SearchServiceUrl);
        ArgumentNullException.ThrowIfNull(settings.SearchServiceAdminKey);
        this.settings = settings;
        var searchServiceUrl = new Uri(this.settings.SearchServiceUrl);
        var searchServiceAdminCredential = new AzureKeyCredential(this.settings.SearchServiceAdminKey);
        this.indexClient = new SearchIndexClient(searchServiceUrl, searchServiceAdminCredential);
        this.indexerClient = new SearchIndexerClient(searchServiceUrl, searchServiceAdminCredential);
    }

    public async Task InitializeAsync(AppSettingsOverride? settingsOverride = null)
    {
        ArgumentNullException.ThrowIfNull(this.settings.SearchIndexNameBlobDocuments);
        ArgumentNullException.ThrowIfNull(this.settings.StorageContainerNameBlobDocuments);
        ArgumentNullException.ThrowIfNull(this.settings.StorageContainerNameBlobChunks);
        ArgumentNullException.ThrowIfNull(this.settings.SearchIndexNameBlobChunks);
        if (!await SearchIndexExistsAsync(this.settings.SearchIndexNameBlobDocuments))
        {
            await CreateDocumentsIndex(settingsOverride, this.settings.SearchIndexNameBlobDocuments, this.settings.StorageContainerNameBlobDocuments, this.settings.StorageContainerNameBlobChunks);
        }
        if (!await SearchIndexExistsAsync(this.settings.SearchIndexNameBlobChunks))
        {
            await CreateChunksIndex(settingsOverride, this.settings.SearchIndexNameBlobChunks, this.settings.StorageContainerNameBlobChunks);
        }
    }

    public async Task UninitializeAsync()
    {
        ArgumentNullException.ThrowIfNull(this.settings.SearchIndexNameBlobDocuments);
        ArgumentNullException.ThrowIfNull(this.settings.SearchIndexNameBlobChunks);
        await this.indexerClient.DeleteIndexerAsync(GetIndexerName(this.settings.SearchIndexNameBlobDocuments));
        await this.indexerClient.DeleteIndexerAsync(GetIndexerName(this.settings.SearchIndexNameBlobChunks));
        await this.indexerClient.DeleteDataSourceConnectionAsync(GetDataSourceName(this.settings.SearchIndexNameBlobDocuments));
        await this.indexerClient.DeleteDataSourceConnectionAsync(GetDataSourceName(this.settings.SearchIndexNameBlobChunks));
        await this.indexerClient.DeleteSkillsetAsync(GetSkillsetName(this.settings.SearchIndexNameBlobDocuments));
        await this.indexClient.DeleteIndexAsync(this.settings.SearchIndexNameBlobDocuments);
        await this.indexClient.DeleteIndexAsync(this.settings.SearchIndexNameBlobChunks);
    }

    public async Task<IList<SearchIndexStatus>> GetSearchIndexStatusesAsync()
    {
        ArgumentNullException.ThrowIfNull(this.settings.SearchIndexNameBlobDocuments);
        ArgumentNullException.ThrowIfNull(this.settings.SearchIndexNameBlobChunks);
        return new List<SearchIndexStatus>
        {
            await GetSearchIndexStatusAsync(this.settings.SearchIndexNameBlobDocuments),
            await GetSearchIndexStatusAsync(this.settings.SearchIndexNameBlobChunks)
        };
    }

    public async Task RunSearchIndexerAsync(string indexName)
    {
        await this.indexerClient.RunIndexerAsync(GetIndexerName(indexName));
    }

    private async Task<SearchIndexStatus> GetSearchIndexStatusAsync(string indexName)
    {
        var searchIndex = new SearchIndexStatus
        {
            Name = indexName
        };
        var indexStatistics = await this.indexClient.GetIndexStatisticsAsync(indexName);
        searchIndex.DocumentCount = indexStatistics.Value?.DocumentCount ?? 0;
        var indexerStatus = await this.indexerClient.GetIndexerStatusAsync(GetIndexerName(indexName));
        if (indexerStatus.Value?.LastResult == null)
        {
            searchIndex.IndexerStatus = "Never run";
        }
        else
        {
            searchIndex.IndexerStatus = GetIndexerStatus(indexerStatus.Value.LastResult.Status);
            searchIndex.IndexerLastRunTime = indexerStatus.Value.LastResult.EndTime;
        }
        return searchIndex;
    }

    private static string GetIndexerStatus(IndexerExecutionStatus status)
    {
        switch (status)
        {
            case IndexerExecutionStatus.TransientFailure:
                return "Transient failure";
            case IndexerExecutionStatus.Success:
                return "Succeeded";
            case IndexerExecutionStatus.InProgress:
                return "In progress";
            case IndexerExecutionStatus.Reset:
                return "Reset";
            default:
                return status.ToString();
        }
    }

    private async Task<bool> SearchIndexExistsAsync(string indexName)
    {
        try
        {
            await this.indexClient.GetIndexAsync(indexName);
            return true;
        }
        catch (RequestFailedException ex) when (ex.Status == (int)HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    private async Task CreateDocumentsIndex(AppSettingsOverride? settingsOverride, string documentsIndexName, string documentsContainerName, string chunksContainerName)
    {
        // Create the search index for the documents.
        var documentsIndex = GetDocumentsSearchIndex(documentsIndexName);
        await this.indexClient.CreateIndexAsync(documentsIndex);

        // Create the Blob Storage data source for the documents.
        var documentsDataSourceConnection = new SearchIndexerDataSourceConnection(GetDataSourceName(documentsIndexName), SearchIndexerDataSourceType.AzureBlob, this.settings.StorageAccountConnectionString, new SearchIndexerDataContainer(documentsContainerName));
        await this.indexerClient.CreateDataSourceConnectionAsync(documentsDataSourceConnection);

        // Create the skillset which chunks and vectorizes the document's content and stores it as JSON
        // files in blob storage (as a knowledge store) so it can be indexed separately.
        var skillset = GetDocumentsSearchIndexerSkillset(settingsOverride, documentsIndexName, chunksContainerName);
        await this.indexerClient.CreateSkillsetAsync(skillset);

        // Create the indexer.
        var documentsIndexer = new SearchIndexer(GetIndexerName(documentsIndexName), documentsDataSourceConnection.Name, documentsIndex.Name)
        {
            Schedule = new IndexingSchedule(GetIndexingSchedule(settingsOverride)),
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
        await this.indexerClient.CreateIndexerAsync(documentsIndexer);
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

    private SearchIndexerSkillset GetDocumentsSearchIndexerSkillset(AppSettingsOverride? settingsOverride, string indexName, string knowledgeStoreContainerName)
    {
        var skillset = new SearchIndexerSkillset(GetSkillsetName(indexName), Array.Empty<SearchIndexerSkill>())
        {
            Skills =
            {
                new WebApiSkill(Array.Empty<InputFieldMappingEntry>(), Array.Empty<OutputFieldMappingEntry>(), this.settings.TextEmbedderFunctionEndpoint)
                {
                    Name = "chunking-embedding-skill",
                    Context = $"/document/{nameof(Document.Content)}",
                    HttpMethod = "POST",
                    Timeout = TimeSpan.FromMinutes(3),
                    BatchSize = 1,
                    DegreeOfParallelism = 1,
                    HttpHeaders =
                    {
                        { "Authorization", this.settings.TextEmbedderFunctionApiKey }
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
            KnowledgeStore = new KnowledgeStore(this.settings.StorageAccountConnectionString, Array.Empty<KnowledgeStoreProjection>())
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

        // Configure any optional settings that can be overridden by the indexer rather than depending on the default
        // values in the text embedder Function App.
        var textEmbedderNumTokens = settingsOverride?.TextEmbedderNumTokens ?? this.settings.TextEmbedderNumTokens;
        if (textEmbedderNumTokens != null)
        {
            skillset.Skills[0].Inputs.Add(new InputFieldMappingEntry("num_tokens") { Source = $"={textEmbedderNumTokens}" });
        }
        var textEmbedderTokenOverlap = settingsOverride?.TextEmbedderTokenOverlap ?? this.settings.TextEmbedderTokenOverlap;
        if (textEmbedderTokenOverlap != null)
        {
            skillset.Skills[0].Inputs.Add(new InputFieldMappingEntry("token_overlap") { Source = $"={textEmbedderTokenOverlap}" });
        }
        var textEmbedderMinChunkSize = settingsOverride?.TextEmbedderMinChunkSize ?? this.settings.TextEmbedderMinChunkSize;
        if (textEmbedderMinChunkSize != null)
        {
            skillset.Skills[0].Inputs.Add(new InputFieldMappingEntry("min_chunk_size") { Source = $"={textEmbedderMinChunkSize}" });
        }
        return skillset;
    }

    private async Task CreateChunksIndex(AppSettingsOverride? settingsOverride, string chunksIndexName, string chunksContainerName)
    {
        // Create the index which represents the chunked data from the main indexer's knowledge store.
        var chunkSearchIndex = GetChunksSearchIndex(chunksIndexName);
        await this.indexClient.CreateIndexAsync(chunkSearchIndex);

        // Create the Storage data source for the chunked data.
        var chunksDataSourceConnection = new SearchIndexerDataSourceConnection(GetDataSourceName(chunksIndexName), SearchIndexerDataSourceType.AzureBlob, this.settings.StorageAccountConnectionString, new SearchIndexerDataContainer(chunksContainerName));
        await this.indexerClient.CreateDataSourceConnectionAsync(chunksDataSourceConnection);

        // Create the chunk indexer based on the JSON files in the knowledge store.
        var chunksIndexer = new SearchIndexer(GetIndexerName(chunksIndexName), chunksDataSourceConnection.Name, chunkSearchIndex.Name)
        {
            Schedule = new IndexingSchedule(GetIndexingSchedule(settingsOverride)),
            Parameters = new IndexingParameters()
            {
                IndexingParametersConfiguration = new IndexingParametersConfiguration()
                {
                    ParsingMode = BlobIndexerParsingMode.Json
                }
            }
        };
        await this.indexerClient.CreateIndexerAsync(chunksIndexer);
    }

    private SearchIndex GetChunksSearchIndex(string chunkIndexName)
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
                new SearchField(nameof(DocumentChunk.ContentVector), SearchFieldDataType.Collection(SearchFieldDataType.Single)) { IsFilterable = false, IsSortable = false, IsFacetable = false, IsSearchable = true, VectorSearchDimensions = this.settings.OpenAIEmbeddingVectorDimensions, VectorSearchConfiguration = Constants.ConfigurationNames.VectorSearchConfigurationNameDefault },
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

    private TimeSpan GetIndexingSchedule(AppSettingsOverride? settingsOverride)
    {
        var minutes = settingsOverride?.SearchIndexerScheduleMinutes ?? this.settings.SearchIndexerScheduleMinutes ?? 5;
        minutes = Math.Max(5, minutes); // Ensure the minimum is 5 minutes, as required by Azure Cognitive Search.
        return TimeSpan.FromMinutes(minutes);
    }

    private static string GetIndexerName(string indexName)
    {
        return $"{indexName}-indexer";
    }

    private static string GetDataSourceName(string indexName)
    {
        return $"{indexName}-datasource";
    }

    private static string GetSkillsetName(string indexName)
    {
        return $"{indexName}-skillset";
    }
}