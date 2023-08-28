namespace Azure.AISearch.WebApp.Services;

using Azure.AISearch.WebApp.Models;

public interface ISearchService
{
    Task<SearchResponse?> SearchAsync(SearchRequest request);
}