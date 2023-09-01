using Azure.AISearch.WebApp.Models;
using Azure.AISearch.WebApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Azure.AISearch.WebApp.Pages;

public class ContentModel : PageModel
{
    public const string RunSearchIndexer = nameof(RunSearchIndexer);

    private readonly AzureCognitiveSearchConfigurationService azureCognitiveSearchConfigurationService;
    private readonly AzureStorageConfigurationService azureStorageConfigurationService;

    public IList<SearchIndexStatus>? SearchIndexStatuses { get; set; }

    public ContentModel(AzureCognitiveSearchConfigurationService azureCognitiveSearchConfigurationService, AzureStorageConfigurationService azureStorageConfigurationService)
    {
        this.azureCognitiveSearchConfigurationService = azureCognitiveSearchConfigurationService;
        this.azureStorageConfigurationService = azureStorageConfigurationService;
    }

    public async Task OnGet()
    {
        this.SearchIndexStatuses = await this.azureCognitiveSearchConfigurationService.GetSearchIndexStatusesAsync();
    }

    public async Task<IActionResult> OnPost(string action, string searchIndexName)
    {
        if (action == RunSearchIndexer)
        {
            await this.azureCognitiveSearchConfigurationService.RunSearchIndexerAsync(searchIndexName);
        }
        return RedirectToPage();
    }
}