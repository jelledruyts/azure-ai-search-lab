namespace Azure.AISearch.WebApp.Models;

public class SearchResponse
{
    public string Id { get; } = Guid.NewGuid().ToString();
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public string? Error { get; set; }
    public IList<string> History { get; set; } = new List<string>();
    public IList<SearchAnswer> Answers { get; set; } = new List<SearchAnswer>();
    public IList<string> Captions { get; set; } = new List<string>();
    public IList<SearchResult> SearchResults { get; set; } = new List<SearchResult>();
}