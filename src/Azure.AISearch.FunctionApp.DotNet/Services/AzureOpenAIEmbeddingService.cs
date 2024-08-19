using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;

namespace Azure.AISearch.FunctionApp.Services;

public class AzureOpenAIEmbeddingService
{
    private readonly OpenAIClient openAIClient;

    public AzureOpenAIEmbeddingService(IConfiguration configuration)
    {
        var settings = configuration.Get<AppSettings>();
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(settings.OpenAIEndpoint);
        ArgumentNullException.ThrowIfNull(settings.OpenAIApiKey);
        this.openAIClient = new OpenAIClient(new Uri(settings.OpenAIEndpoint), new AzureKeyCredential(settings.OpenAIApiKey));
    }

    public async Task<IReadOnlyList<float>> GetEmbeddingAsync(string embeddingDeploymentName, string text)
    {
        var response = await this.openAIClient.GetEmbeddingsAsync(new EmbeddingsOptions(embeddingDeploymentName, new[] { text }));
        return response.Value.Data[0].Embedding.ToArray();
    }
}