using Azure.AISearch.WebApp.Models;
using Azure.AISearch.WebApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Azure.AISearch.WebApp.Pages;

public class ManageModel : PageModel
{
    public const string RunSearchIndexer = nameof(RunSearchIndexer);
    public const string ResetSearchConfiguration = nameof(ResetSearchConfiguration);

    private readonly AzureCognitiveSearchConfigurationService azureCognitiveSearchConfigurationService;
    private readonly AzureStorageConfigurationService azureStorageConfigurationService;

    public IList<SearchIndexStatus>? SearchIndexStatuses { get; set; }

    public ManageModel(AzureCognitiveSearchConfigurationService azureCognitiveSearchConfigurationService, AzureStorageConfigurationService azureStorageConfigurationService)
    {
        this.azureCognitiveSearchConfigurationService = azureCognitiveSearchConfigurationService;
        this.azureStorageConfigurationService = azureStorageConfigurationService;
    }

    public async Task OnGet()
    {
        this.SearchIndexStatuses = await this.azureCognitiveSearchConfigurationService.GetSearchIndexStatusesAsync();
    }

    public async Task<IActionResult> OnPost(string action, string? searchIndexName, AppSettingsOverride? settingsOverride)
    {
        if (action == RunSearchIndexer && !string.IsNullOrEmpty(searchIndexName))
        {
            await this.azureCognitiveSearchConfigurationService.RunSearchIndexerAsync(searchIndexName);
        }
        else if (action == ResetSearchConfiguration)
        {
            await this.azureCognitiveSearchConfigurationService.UninitializeAsync();
            await this.azureStorageConfigurationService.UninitializeAsync();

            await this.azureStorageConfigurationService.InitializeAsync();
            await this.azureCognitiveSearchConfigurationService.InitializeAsync(settingsOverride);
        }
        return RedirectToPage();
    }
}