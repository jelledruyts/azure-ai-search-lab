using Azure.AISearch.WebApp.Models;
using Azure.AISearch.WebApp.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Azure.AISearch.WebApp.Pages;

public class IndexModel : PageModel
{
    private readonly SearchRequestHandler searchRequestHandler;

    public IList<SearchScenario> Scenarios { get; set; }
    public SearchRequest SearchRequest { get; set; }
    public SearchResponse? SearchResponse { get; set; }

    public IndexModel(SearchRequestHandler searchRequestHandler)
    {
        this.searchRequestHandler = searchRequestHandler;
        this.Scenarios = SearchScenario.GetScenarios();
        this.SearchRequest = new SearchRequest();
    }

    public async Task OnPost(SearchRequest searchRequest)
    {
        this.Scenarios = SearchScenario.GetScenarios();
        this.SearchRequest = searchRequest;
        this.SearchResponse = await this.searchRequestHandler.HandleSearchRequestAsync(this.SearchRequest);
    }
}