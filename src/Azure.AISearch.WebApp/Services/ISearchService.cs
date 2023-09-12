namespace Azure.AISearch.WebApp.Services;

using Azure.AISearch.WebApp.Models;

public interface ISearchService
{
    bool CanHandle(SearchRequest request);
    Task<SearchResponse> SearchAsync(SearchRequest request);
}