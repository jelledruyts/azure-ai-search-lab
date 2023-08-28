using Azure.AISearch.WebApp.Models;
using Azure.AISearch.WebApp.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Azure.AISearch.WebApp.Pages;

public class CompareModel : PageModel
{
    private readonly SearchService searchService;

    public string? Query { get; set; }
    public IList<SearchResponse>? SearchResponses { get; set; }

    public CompareModel(SearchService searchService)
    {
        this.searchService = searchService;
    }

    public async Task OnPost(string query)
    {
        this.Query = query;
        var searchTasks = new List<Task<SearchResponse?>>();
        foreach (var scenario in SearchScenario.GetScenarios())
        {
            scenario.SearchRequest.DisplayName = scenario.Name;
            scenario.SearchRequest.Query = this.Query;
            searchTasks.Add(this.searchService.SearchAsync(scenario.SearchRequest));
        }
        await Task.WhenAll(searchTasks);
        this.SearchResponses = searchTasks.Select(t => t.Result).Where(r => r != null).Cast<SearchResponse>().ToList();
    }
}