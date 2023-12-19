using Azure.AI.OpenAI;

namespace Azure.AISearch.WebApp.Services;

public class AzureOpenAIEmbeddingService : IEmbeddingService
{
    private readonly OpenAIClient openAIClient;
    private readonly string embeddingDeploymentName;

    public AzureOpenAIEmbeddingService(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings.OpenAIEndpoint);
        ArgumentNullException.ThrowIfNull(settings.OpenAIApiKey);
        ArgumentNullException.ThrowIfNull(settings.OpenAIEmbeddingDeployment);
        this.openAIClient = new OpenAIClient(new Uri(settings.OpenAIEndpoint), new AzureKeyCredential(settings.OpenAIApiKey));
        this.embeddingDeploymentName = settings.OpenAIEmbeddingDeployment;
    }

    public async Task<IReadOnlyList<float>> GetEmbeddingAsync(string text)
    {
        var response = await this.openAIClient.GetEmbeddingsAsync(new EmbeddingsOptions(this.embeddingDeploymentName, new[] { text }));
        return response.Value.Data[0].Embedding.ToArray();
    }
}