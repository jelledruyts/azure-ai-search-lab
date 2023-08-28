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
        if (!string.IsNullOrWhiteSpace(this.SearchRequest.Query))
        {
            this.SearchRequest.History.Add(this.SearchRequest.Query);
            this.SearchResponse = await this.searchService.SearchAsync(this.SearchRequest);
            var topAnswer = this.SearchResponse.Answers.Where(a => !string.IsNullOrWhiteSpace(a.Text)).OrderBy(a => a.Score).FirstOrDefault();
            if (topAnswer?.Text != null)
            {
                this.SearchRequest.History.Add(topAnswer.Text);
            }
            else
            {
                // TODO: This isn't needed if history contains the role (user/assistant).
                this.SearchRequest.History.Add("Sorry, I don't have an answer for you.");
            }
        }
    }
}