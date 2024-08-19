using Azure.AISearch.FunctionApp.Models;
using Azure.Search.Documents;
using Microsoft.Extensions.Configuration;

namespace Azure.AISearch.FunctionApp.Services;

public class AzureCognitiveSearchService
{
    private readonly SearchClient searchClient;

    public AzureCognitiveSearchService(IConfiguration configuration)
    {
        var settings = configuration.Get<AppSettings>();
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(settings.SearchServiceUrl);
        ArgumentNullException.ThrowIfNull(settings.SearchServiceAdminKey);
        ArgumentNullException.ThrowIfNull(settings.SearchIndexNameBlobChunks);
        var searchServiceUrl = new Uri(settings.SearchServiceUrl);
        var searchServiceAdminCredential = new AzureKeyCredential(settings.SearchServiceAdminKey);
        this.searchClient = new SearchClient(searchServiceUrl, settings.SearchIndexNameBlobChunks, searchServiceAdminCredential);
    }

    public async Task UploadDocumentChunksAsync(string? sourceDocumentId, IList<DocumentChunk> documentChunks)
    {
        if (!string.IsNullOrWhiteSpace(sourceDocumentId))
        {
            // Find and delete all existing chunk documents for the same parent document.
            while (true)
            {
                var options = new SearchOptions
                {
                    Size = 1000, // Max allowed by Azure AISearch.
                    Select = { nameof(DocumentChunk.Id) }, // Only return the key field to minimize data transfer.
                    IncludeTotalCount = true,
                    Filter = $"{nameof(DocumentChunk.SourceDocumentId)} eq '{sourceDocumentId}'"
                };
                var existingChunksResult = await this.searchClient.SearchAsync<DocumentChunk>(string.Empty, options);
                var existingChunkIds = existingChunksResult.Value.GetResults().Select(r => r.Document.Id).ToList();
                if (existingChunkIds.Any())
                {
                    await this.searchClient.DeleteDocumentsAsync(nameof(DocumentChunk.Id), existingChunkIds);
                }
                if (existingChunksResult.Value.TotalCount <= existingChunkIds.Count)
                {
                    break;
                }
            }
        }

        // Upload all new chunk documents.
        await this.searchClient.UploadDocumentsAsync(documentChunks);
    }
}