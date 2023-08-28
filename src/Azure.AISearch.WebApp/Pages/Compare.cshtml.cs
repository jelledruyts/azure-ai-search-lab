using Azure.AISearch.WebApp.Models;
using Azure.AISearch.WebApp.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Azure.AISearch.WebApp.Pages;

public class CompareModel : PageModel
{
    private readonly SearchService searchService;

    public IList<SearchResponse>? SearchResponses { get; set; }

    public CompareModel(SearchService searchService)
    {
        this.searchService = searchService;
    }

    public async Task OnPost(string query)
    {
        if (!string.IsNullOrWhiteSpace(query))
        {
            var searchTasks = new List<Task<SearchResponse>>();
            foreach (var scenario in SearchScenario.GetScenarios())
            {
                var request = scenario.SearchRequest;
                request.DisplayName = scenario.Name;
                request.Query = query;
                searchTasks.Add(this.searchService.SearchAsync(request));
            }
            await Task.WhenAll(searchTasks);
            this.SearchResponses = searchTasks.Select(t => t.Result).ToList();
            this.Query = query;
        }
    }
}