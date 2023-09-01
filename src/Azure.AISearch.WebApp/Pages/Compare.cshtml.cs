using Azure.AISearch.WebApp.Models;
using Azure.AISearch.WebApp.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Azure.AISearch.WebApp.Pages;

public class CompareModel : PageModel
{
    private readonly SearchRequestHandler searchRequestHandler;
    private readonly SearchScenarioProvider searchScenarioProvider;

    public string? Query { get; set; }
    public IList<SearchResponse>? SearchResponses { get; set; }

    public CompareModel(SearchRequestHandler searchRequestHandler, SearchScenarioProvider searchScenarioProvider)
    {
        this.searchRequestHandler = searchRequestHandler;
        this.searchScenarioProvider = searchScenarioProvider;
    }

    public async Task OnPost(string query)
    {
        this.Query = query;
        var searchScenarioTasks = this.searchScenarioProvider.GetSearchScenarios().Select(s => RunScenarioAsync(s, query)).ToList();
        await Task.WhenAll(searchScenarioTasks);
        this.SearchResponses = searchScenarioTasks.Select(t => t.Result).Where(r => r != null).Cast<SearchResponse>().ToList();
    }

    private async Task<SearchResponse?> RunScenarioAsync(SearchScenario scenario, string query)
    {
        var searchRequest = scenario.SearchRequest;
        searchRequest.Query = query;
        var response = await this.searchRequestHandler.HandleSearchRequestAsync(searchRequest);
        if (response != null)
        {
            response.DisplayName = scenario.DisplayName;
            response.Description = scenario.Description;
        }
        return response;
    }
}