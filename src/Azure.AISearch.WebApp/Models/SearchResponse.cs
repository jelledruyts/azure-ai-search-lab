namespace Azure.AISearch.WebApp.Models;

public class SearchResponse
{
    public int Priority { get; set; }
    public string? ServiceId { get; set; }
    public string? ServiceName { get; set; }
    public string? Error { get; set; }
    public IList<SearchAnswer> Answers { get; set; } = new List<SearchAnswer>();
    public IList<string> Captions { get; set; } = new List<string>();
    public IList<SearchResult> SearchResults { get; set; } = new List<SearchResult>();
}