namespace Azure.AISearch.WebApp.Models;

public class SearchResult
{
    public string? SearchIndexName { get; set; }
    public string? SearchIndexKey { get; set; }
    public string? DocumentId { get; set; }
    public string? DocumentTitle { get; set; }
    public double? Score { get; set; }
    public IDictionary<string, IList<string>> Highlights { get; set; } = new Dictionary<string, IList<string>>();
    public IList<string> Captions { get; set; } = new List<string>();
}