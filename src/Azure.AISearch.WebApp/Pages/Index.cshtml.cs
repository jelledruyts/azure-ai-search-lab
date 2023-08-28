using Azure.AISearch.WebApp.Models;
using Azure.AISearch.WebApp.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Azure.AISearch.WebApp.Pages;

public class IndexModel : PageModel
{
    private readonly SearchRequestHandler searchRequestHandler;

    public SearchRequest SearchRequest { get; set; }
    public SearchResponse? SearchResponse { get; set; }

    public IndexModel(SearchRequestHandler searchRequestHandler)
    {
        this.searchRequestHandler = searchRequestHandler;
        this.SearchRequest = new SearchRequest();
    }

    public async Task OnPost(SearchRequest searchRequest)
    {
        this.SearchRequest = searchRequest;
        this.SearchResponse = await this.searchRequestHandler.HandleSearchRequestAsync(this.SearchRequest);
    }
}