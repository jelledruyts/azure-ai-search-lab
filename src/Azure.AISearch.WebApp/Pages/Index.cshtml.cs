using Azure.AISearch.WebApp.Models;
using Azure.AISearch.WebApp.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Azure.AISearch.WebApp.Pages;

public class IndexModel : PageModel
{
    private readonly AzureStorageConfigurationService azureStorageConfigurationService;
    private readonly AzureSearchConfigurationService azureSearchConfigurationService;
    private readonly IEnumerable<ISearchService> searchServices;

    public string? Query { get; set; }
    public IList<string>? History { get; set; }
    public IList<SearchResponse>? SearchResponses { get; set; }

    public IndexModel(AzureSearchConfigurationService azureSearchConfigurationService, AzureStorageConfigurationService azureStorageConfigurationService, IEnumerable<ISearchService> searchServices)
    {
        this.azureSearchConfigurationService = azureSearchConfigurationService;
        this.azureStorageConfigurationService = azureStorageConfigurationService;
        this.searchServices = searchServices;
    }

    public async Task OnGet()
    {
        await this.azureStorageConfigurationService.CreateContainerIfNotExistsAsync(Constants.ContainerNames.BlobDocuments);
        await this.azureStorageConfigurationService.CreateContainerIfNotExistsAsync(Constants.ContainerNames.BlobChunks);
        await this.azureSearchConfigurationService.InitializeSearchAsync(Constants.IndexNames.BlobDocuments, Constants.IndexNames.BlobChunks, Constants.ContainerNames.BlobDocuments, Constants.ContainerNames.BlobChunks);
    }

    public async Task OnPost(string query, IList<string> history)
    {
        if (!string.IsNullOrWhiteSpace(query))
        {
            var request = new SearchRequest
            {
                Query = query,
                History = history
            };
            var searchTasks = this.searchServices.SelectMany(s => s.SearchAsync(request)).ToArray();
            await Task.WhenAll(searchTasks);
            this.SearchResponses = searchTasks.Select(t => t.Result).ToList();
            this.Query = query;
            var updatedHistory = new List<string>(history);
            updatedHistory.Add(query);
            // Take the top answer from the highest priority service to include in the chat history.
            var topAnswer = this.SearchResponses.OrderBy(r => r.Priority).SelectMany(r => r.Answers.Where(a => !string.IsNullOrWhiteSpace(a.Text)).OrderBy(a => a.Score)).FirstOrDefault();
            if (topAnswer?.Text != null)
            {
                updatedHistory.Add(topAnswer.Text);
            }
            this.History = updatedHistory;
        }
    }
}