using Azure.AISearch.WebApp.Models;

namespace Azure.AISearch.WebApp.Services;

public interface ISearchService
{
    IList<Task<SearchResponse>> SearchAsync(SearchRequest request);
}