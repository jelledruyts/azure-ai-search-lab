using Azure.AISearch.FunctionApp.Services;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(Azure.AISearch.FunctionApp.Startup))]

namespace Azure.AISearch.FunctionApp;

public class Startup : FunctionsStartup
{
    public override void Configure(IFunctionsHostBuilder builder)
    {
        builder.Services.AddSingleton<SemanticKernelChunkingService>();
        builder.Services.AddSingleton<AzureOpenAIEmbeddingService>();
        builder.Services.AddSingleton<AzureCognitiveSearchService>();
    }
}