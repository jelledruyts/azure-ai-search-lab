using System.Text;
using Azure.AISearch.WebApp.Models;
using Azure.AISearch.WebApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Azure.AISearch.WebApp.Pages;

[RequestFormLimits(MultipartBodyLengthLimit = MaxDocumentUploadSize)]
[RequestSizeLimit(MaxDocumentUploadSize)]
public class ManageModel : PageModel
{
    private const int MaxDocumentUploadSize = 209715200; // 200 MB
    public const string UploadDocument = nameof(UploadDocument);
    public const string RunSearchIndexer = nameof(RunSearchIndexer);
    public const string ResetSearchConfiguration = nameof(ResetSearchConfiguration);

    private readonly AppSettings settings;
    private readonly AzureCognitiveSearchConfigurationService azureCognitiveSearchConfigurationService;
    private readonly AzureStorageConfigurationService azureStorageConfigurationService;

    public string? Message { get; set; }
    public SearchServiceStatus? SearchServiceStatus { get; set; }
    public IList<SearchIndexStatus>? SearchIndexStatuses { get; set; }

    public ManageModel(AppSettings settings, AzureCognitiveSearchConfigurationService azureCognitiveSearchConfigurationService, AzureStorageConfigurationService azureStorageConfigurationService)
    {
        this.settings = settings;
        this.azureCognitiveSearchConfigurationService = azureCognitiveSearchConfigurationService;
        this.azureStorageConfigurationService = azureStorageConfigurationService;
    }

    public async Task OnGet(string? message)
    {
        this.Message = message;

        // Show important limits for the selected SKU, see https://learn.microsoft.com/azure/search/search-limits-quotas-capacity#indexer-limits.
        this.SearchServiceStatus = GetSearchServiceStatus();

        // Get the status of all search indexes.
        this.SearchIndexStatuses = await this.azureCognitiveSearchConfigurationService.GetSearchIndexStatusesAsync();
    }

    public async Task<IActionResult> OnPost(string action, IList<IFormFile>? documents, string? searchIndexName, AppSettingsOverride? settingsOverride)
    {
        this.SearchServiceStatus = GetSearchServiceStatus();
        var maxFileSize = (this.SearchServiceStatus?.BlobIndexerMaxFileSizeMB * 1024 * 1024) ?? long.MaxValue;
        var message = new StringBuilder();

        if (action == UploadDocument && !settings.DisableUploadDocuments && documents != null && documents.Any())
        {
            foreach (var document in documents)
            {
                try
                {
                    using var fileStream = document.OpenReadStream();
                    await this.azureStorageConfigurationService.UploadDocumentAsync(fileStream, document.FileName);
                    message.Append($"The file \"{document.FileName}\" was uploaded successfully. ");
                    if (document.Length > maxFileSize)
                    {
                        message.Append($"However, it is too large for the current service tier, and will not be processed by the search indexer. ");
                    }
                }
                catch (Exception ex)
                {
                    message.Append($"The file \"{document.FileName}\" could not be uploaded: {ex.Message}. ");
                }
            }
        }
        else if (action == RunSearchIndexer && !string.IsNullOrEmpty(searchIndexName))
        {
            await this.azureCognitiveSearchConfigurationService.RunSearchIndexerAsync(searchIndexName);
            message.Append($"The indexer for search index \"{searchIndexName}\" was started. ");
        }
        else if (action == ResetSearchConfiguration && !this.settings.DisableResetSearchConfiguration)
        {
            await this.azureCognitiveSearchConfigurationService.UninitializeAsync();
            await this.azureStorageConfigurationService.UninitializeAsync();

            await this.azureStorageConfigurationService.InitializeAsync();
            await this.azureCognitiveSearchConfigurationService.InitializeAsync(settingsOverride);
        }
        return RedirectToPage(new { message = message.ToString().Trim() });
    }

    private SearchServiceStatus? GetSearchServiceStatus()
    {
        return (this.settings.SearchServiceSku?.ToLowerInvariant()) switch
        {
            "basic" => new SearchServiceStatus
            {
                Sku = "Basic",
                BlobIndexerMaxFileSizeMB = 16,
                BlobIndexerMaxCharactersExtractedPerFile = 64000
            },
            "standard" => new SearchServiceStatus
            {
                Sku = "Standard S1",
                BlobIndexerMaxFileSizeMB = 128,
                BlobIndexerMaxCharactersExtractedPerFile = 4000000
            },
            "standard2" => new SearchServiceStatus
            {
                Sku = "Standard S2",
                BlobIndexerMaxFileSizeMB = 256,
                BlobIndexerMaxCharactersExtractedPerFile = 8000000
            },
            "standard3" => new SearchServiceStatus
            {
                Sku = "Standard S3",
                BlobIndexerMaxFileSizeMB = 256,
                BlobIndexerMaxCharactersExtractedPerFile = 16000000
            },
            _ => null,
        };
    }
}