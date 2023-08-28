using Azure.AISearch.WebApp.Models;

namespace Azure.AISearch.WebApp.Services;

// TODO: Rename to SearchServiceHandler/SearchRequestHandler?
public class SearchService
{
    private readonly AzureCognitiveSearchService azureCognitiveSearchService;
    private readonly AzureOpenAISearchService azureOpenAISearchService;

    public SearchService(AzureCognitiveSearchService azureCognitiveSearchService, AzureOpenAISearchService azureOpenAISearchService)
    {
        // TODO: Use list of search services and call all of them in parallel, if null response the service couldn't handle it.
        this.azureCognitiveSearchService = azureCognitiveSearchService;
        this.azureOpenAISearchService = azureOpenAISearchService;
    }

    public async Task<SearchResponse?> SearchAsync(SearchRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Query))
            {
                return null;
            }
            else if (request.PrimaryService == PrimaryServiceType.AzureCognitiveSearch)
            {
                return await this.azureCognitiveSearchService.SearchAsync(request);
            }
            else if (request.PrimaryService == PrimaryServiceType.AzureOpenAI)
            {
                return await this.azureOpenAISearchService.SearchAsync(request);
            }
            else
            {
                throw new NotSupportedException($"Service \"{request.PrimaryService}\" is not supported.");
            }
        }
        catch (Exception ex)
        {
            return new SearchResponse
            {
                RequestId = request.Id,
                DisplayName = request.DisplayName,
                Error = ex.Message
            };
        }
    }
}