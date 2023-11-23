namespace Azure.AISearch.WebApp.Services;

public interface IEmbeddingService
{
    Task<ReadOnlyMemory<float>> GetEmbeddingAsync(string text);
}