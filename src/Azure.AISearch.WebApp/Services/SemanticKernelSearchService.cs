using System.Text;
using Azure.AISearch.WebApp.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace Azure.AISearch.WebApp.Services;

// This is a simple, somewhat naive implementation of a custom orchestration service that uses Semantic Kernel to generate answers.
// For inspiration, see:
// https://github.com/Azure-Samples/azure-search-openai-demo-csharp/blob/feature/embeddingSearch/app/backend/Services/RetrieveThenReadApproachService.cs
// https://github.com/Azure-Samples/azure-search-openai-demo-csharp/blob/feature/embeddingSearch/app/backend/Extensions/SearchClientExtensions.cs

public class SemanticKernelSearchService : ISearchService
{
    private readonly AppSettings settings;
    private readonly AzureCognitiveSearchService azureCognitiveSearchService;

    public SemanticKernelSearchService(AppSettings settings, AzureCognitiveSearchService azureCognitiveSearchService)
    {
        this.settings = settings;
        this.azureCognitiveSearchService = azureCognitiveSearchService;
    }

    public bool CanHandle(SearchRequest request)
    {
        return request.Engine == EngineType.CustomOrchestration;
    }

    public async Task<SearchResponse> SearchAsync(SearchRequest request)
    {
        ArgumentNullException.ThrowIfNull(settings.OpenAIEndpoint);
        ArgumentNullException.ThrowIfNull(settings.OpenAIApiKey);
        ArgumentNullException.ThrowIfNull(request.Query);

        var openAIGptDeployment = string.IsNullOrEmpty(request.OpenAIGptDeployment) ? this.settings.OpenAIGptDeployment : request.OpenAIGptDeployment;
        ArgumentNullException.ThrowIfNull(openAIGptDeployment);
        var prompt = string.IsNullOrWhiteSpace(request.CustomOrchestrationPrompt) ? this.settings.GetDefaultCustomOrchestrationPrompt() : request.CustomOrchestrationPrompt;
        var kernelBuilder = Kernel.CreateBuilder();
        kernelBuilder.Services.AddLogging(c => c.AddDebug().SetMinimumLevel(LogLevel.Trace));
        kernelBuilder.AddAzureOpenAIChatCompletion(openAIGptDeployment, this.settings.OpenAIEndpoint, this.settings.OpenAIApiKey);
        var kernel = kernelBuilder.Build();
        var executionSettings = new OpenAIPromptExecutionSettings
        {
            MaxTokens = request.MaxTokens ?? Constants.Defaults.MaxTokens,
            Temperature = request.Temperature ?? Constants.Defaults.Temperature,
            TopP = request.TopP ?? Constants.Defaults.TopP,
            FrequencyPenalty = request.FrequencyPenalty ?? Constants.Defaults.FrequencyPenalty,
            PresencePenalty = request.PresencePenalty ?? Constants.Defaults.PresencePenalty,
            StopSequences = (request.StopSequences ?? Constants.Defaults.StopSequences).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        };
        var function = kernel.CreateFunctionFromPrompt(prompt, executionSettings);

        var response = new SearchResponse();
        var arguments = new KernelArguments
        {
            { "query", request.Query }
        };
        
        // Query the search index for relevant data first, by passing through the original request
        // to the Azure AI Search service.
        var azureCognitiveSearchResponse = await this.azureCognitiveSearchService.SearchAsync(request);

        // Copy the document results over, as these are used to generate the answer.
        response.SearchResults = azureCognitiveSearchResponse.SearchResults;

        // Build a string with all the sources, where each source is prefixed with the document title.
        var sourcesBuilder = new StringBuilder();
        foreach (var result in azureCognitiveSearchResponse.SearchResults)
        {
            foreach (var caption in result.Captions)
            {
                sourcesBuilder.AppendLine($"{result.DocumentTitle}: {Normalize(caption)}");
            }
            foreach (var highlight in result.Highlights.SelectMany(h => h.Value))
            {
                sourcesBuilder.AppendLine($"{result.DocumentTitle}: {Normalize(highlight)}");
            }
        }

        // Add the sources string to the arguments, so that the semantic function can use it to construct the prompt.
        arguments.Add("sources", sourcesBuilder.ToString());

        // Run the semantic function to generate the answer.
        try
        {
            var kernelResult = await kernel.InvokeAsync(function, arguments);
            var answer = kernelResult.GetValue<string>();
            if (!string.IsNullOrWhiteSpace(answer))
            {
                response.Answers.Add(new SearchAnswer { Text = answer });
            }
        }
        catch (Exception exc)
        {
            response.Error = exc.Message;
        }
        return response;
    }

    private static string Normalize(string value)
    {
        return value.Replace('\r', ' ').Replace('\n', ' ');
    }
}