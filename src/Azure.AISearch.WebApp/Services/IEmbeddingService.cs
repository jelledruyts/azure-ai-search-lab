namespace Azure.AISearch.WebApp.Services;

public interface IEmbeddingService
{
    Task<IReadOnlyList<float>> GetEmbeddingAsync(string text);
}