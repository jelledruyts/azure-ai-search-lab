using Azure.AISearch.WebApp.Models;
using Azure.AISearch.WebApp.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Azure.AISearch.WebApp.Pages;

public class IndexModel : PageModel
{
    private readonly AppSettings settings;
    private readonly SearchRequestHandler searchRequestHandler;
    private readonly SearchScenarioProvider searchScenarioProvider;

    public IList<SearchScenario> Scenarios { get; set; }
    public SearchRequest SearchRequest { get; set; }
    public SearchResponse? SearchResponse { get; set; }

    public IndexModel(AppSettings settings, SearchRequestHandler searchRequestHandler, SearchScenarioProvider searchScenarioProvider)
    {
        this.settings = settings;
        this.searchRequestHandler = searchRequestHandler;
        this.searchScenarioProvider = searchScenarioProvider;
        this.Scenarios = this.searchScenarioProvider.GetSearchScenarios();
        this.SearchRequest = new SearchRequest
        {
            SystemRoleInformation = this.settings.DefaultSystemRoleInformation
        };
    }

    public async Task OnPost(SearchRequest searchRequest)
    {
        this.SearchRequest = searchRequest;
        this.SearchResponse = await this.searchRequestHandler.HandleSearchRequestAsync(this.SearchRequest);
    }
}