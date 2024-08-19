namespace Azure.AISearch.FunctionApp;

using Azure.AISearch.FunctionApp.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public class Program
{
    public static void Main()
    {
        var host = new HostBuilder()
            .ConfigureFunctionsWebApplication()
            .ConfigureAppConfiguration(options =>
            {
                options.AddUserSecrets<Program>(optional: true);
            })
            .ConfigureServices(s =>
            {
                s.AddSingleton<SemanticKernelChunkingService>();
                s.AddSingleton<AzureOpenAIEmbeddingService>();
                s.AddSingleton<AzureCognitiveSearchService>();
            })
            .Build();

        host.Run();
    }
}