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

    public IList<SearchIndexStatus>? SearchIndexStatuses { get; set; }

    public ManageModel(AppSettings settings, AzureCognitiveSearchConfigurationService azureCognitiveSearchConfigurationService, AzureStorageConfigurationService azureStorageConfigurationService)
    {
        this.settings = settings;
        this.azureCognitiveSearchConfigurationService = azureCognitiveSearchConfigurationService;
        this.azureStorageConfigurationService = azureStorageConfigurationService;
    }

    public async Task OnGet()
    {
        this.SearchIndexStatuses = await this.azureCognitiveSearchConfigurationService.GetSearchIndexStatusesAsync();
    }

    public async Task<IActionResult> OnPost(string action, IList<IFormFile>? documents, string? searchIndexName, AppSettingsOverride? settingsOverride)
    {
        if (action == UploadDocument && !settings.DisableUploadDocuments && documents != null && documents.Any())
        {
            foreach (var document in documents)
            {
                using var fileStream = document.OpenReadStream();
                await this.azureStorageConfigurationService.UploadDocumentAsync(fileStream, document.FileName);
            }
        }
        else if (action == RunSearchIndexer && !string.IsNullOrEmpty(searchIndexName))
        {
            await this.azureCognitiveSearchConfigurationService.RunSearchIndexerAsync(searchIndexName);
        }
        else if (action == ResetSearchConfiguration && !this.settings.DisableResetSearchConfiguration)
        {
            await this.azureCognitiveSearchConfigurationService.UninitializeAsync();
            await this.azureStorageConfigurationService.UninitializeAsync();

            await this.azureStorageConfigurationService.InitializeAsync();
            await this.azureCognitiveSearchConfigurationService.InitializeAsync(settingsOverride);
        }
        return RedirectToPage();
    }
}