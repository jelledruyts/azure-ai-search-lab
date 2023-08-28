using Azure.AISearch.WebApp.Models;
using Azure.AISearch.WebApp.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Azure.AISearch.WebApp.Pages;

public class IndexModel : PageModel
{
    private readonly SearchService searchService;
    public SearchRequest SearchRequest { get; set; }
    public SearchResponse? SearchResponse { get; set; }

    public IndexModel(SearchService searchService)
    {
        this.searchService = searchService;
        this.SearchRequest = new SearchRequest();
    }

    public async Task OnPost(SearchRequest searchRequest)
    {
        this.SearchRequest = searchRequest;
        this.SearchResponse = await this.searchService.SearchAsync(this.SearchRequest);
    }
}