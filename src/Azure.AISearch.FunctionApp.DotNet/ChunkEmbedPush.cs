using System.Net;
using System.Text.Json;
using Azure.AISearch.FunctionApp.Models;
using Azure.AISearch.FunctionApp.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Azure.AISearch.FunctionApp;

public class ChunkEmbedPush
{
    private readonly ILogger logger;
    private readonly AppSettings settings;
    private readonly SemanticKernelChunkingService chunkingService;
    private readonly AzureOpenAIEmbeddingService embeddingService;
    private readonly AzureCognitiveSearchService searchService;

    public ChunkEmbedPush(ILoggerFactory loggerFactory, IConfiguration configuration, SemanticKernelChunkingService chunkingService, AzureOpenAIEmbeddingService embeddingService, AzureCognitiveSearchService searchService)
    {
        this.logger = loggerFactory.CreateLogger<ChunkEmbedPush>();
        var settings = configuration.Get<AppSettings>();
        ArgumentNullException.ThrowIfNull(settings);
        this.settings = settings;
        this.chunkingService = chunkingService;
        this.embeddingService = embeddingService;
        this.searchService = searchService;
    }

    [Function(nameof(ChunkEmbedPush))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "POST")] HttpRequest request)
    {
        this.logger.LogInformation("Skill request received");

        // Use basic API key authentication for demo purposes to avoid a dependency on the Function App keys.
        if (!string.IsNullOrWhiteSpace(this.settings.TextEmbedderFunctionApiKey))
        {
            var authorizationHeader = request.Headers["authorization"];
            if (authorizationHeader != this.settings.TextEmbedderFunctionApiKey)
            {
                return new StatusCodeResult(403);
            }
        }

        // Get the skill request.
        var skillResponse = new SkillResponse();
        using var bodyReader = new StreamReader(request.Body);
        var skillRequestJson = await bodyReader.ReadToEndAsync();
        if (!string.IsNullOrWhiteSpace(skillRequestJson))
        {
            var skillRequest = JsonSerializer.Deserialize<SkillRequest>(skillRequestJson);
            if (skillRequest?.Values != null)
            {
                // Process all records in the request.
                foreach (var record in skillRequest.Values)
                {
                    this.logger.LogInformation($"Processing record \"{record.RecordId}\" with document id \"{record.Data.DocumentId}\" and filepath \"{record.Data.FilePath}\".");
                    var responseRecord = new SkillResponseRecord
                    {
                        RecordId = record.RecordId
                    };
                    skillResponse.Values.Add(responseRecord);

                    // Use default settings if not specified in the request.
                    record.Data.NumTokens = record.Data.NumTokens ?? this.settings.TextEmbedderNumTokens ?? 2048;
                    record.Data.TokenOverlap = record.Data.TokenOverlap ?? this.settings.TextEmbedderTokenOverlap ?? 0;
                    record.Data.MinChunkSize = record.Data.MinChunkSize ?? this.settings.TextEmbedderMinChunkSize ?? 10;
                    record.Data.EmbeddingDeploymentName = record.Data.EmbeddingDeploymentName ?? this.settings.OpenAIEmbeddingDeployment ?? throw new InvalidOperationException("No embedding deployment name specified.");

                    if (!string.IsNullOrWhiteSpace(record.Data.Text))
                    {
                        // Generate chunks for the text in the record.
                        this.logger.LogInformation($"Chunking to {record.Data.NumTokens} tokens (min chunk size is {record.Data.MinChunkSize}, token overlap is {record.Data.TokenOverlap}).");
                        var chunks = this.chunkingService.GetChunks(record.Data);
                        var chunksToProcess = chunks.Where(c => this.chunkingService.EstimateChunkSize(c) >= record.Data.MinChunkSize).ToList();
                        responseRecord.Data.SkippedChunks = chunks.Count - chunksToProcess.Count;
                        this.logger.LogInformation($"Skipping {responseRecord.Data.SkippedChunks} chunk(s) with an estimated token size below the minimum chunk size.");

                        this.logger.LogInformation($"Generating embeddings for {chunks.Count} chunk(s) using deployment \"{record.Data.EmbeddingDeploymentName}\".");
                        var index = 0;
                        var documentChunks = new List<DocumentChunk>();
                        foreach (var chunk in chunks)
                        {
                            // For each chunk, generate an embedding.
                            var embedding = await this.embeddingService.GetEmbeddingAsync(record.Data.EmbeddingDeploymentName, chunk);

                            // For each chunk with its embedding, create a document to be stored in the search index.
                            var documentChunk = new DocumentChunk
                            {
                                Id = $"{record.Data.DocumentId}-{index}",
                                Content = chunk,
                                ContentVector = embedding,
                                SourceDocumentId = record.Data.DocumentId,
                                SourceDocumentTitle = record.Data.Title,
                                SourceDocumentFilePath = record.Data.FilePath
                            };
                            documentChunks.Add(documentChunk);
                            index++;
                        }

                        // Store the document chunks in the search index.
                        this.logger.LogInformation($"Uploading {documentChunks.Count} document chunk(s) to search service.");
                        await this.searchService.UploadDocumentChunksAsync(record.Data.DocumentId, documentChunks);
                    }
                }
            }
        }

        return new OkObjectResult(skillResponse);
    }
}