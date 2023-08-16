namespace Azure.AISearch.WebApp.Models;

public class SearchRequest
{
    public IList<string>? SearchServiceIds { get; set; } = new List<string>();
    public IList<string>? History { get; set; } = new List<string>();
    public string? Query { get; set; }

    public bool ShouldInclude(string searchServiceId)
    {
        return this.SearchServiceIds == null || !this.SearchServiceIds.Any() || this.SearchServiceIds.Any(s => string.Equals(s, searchServiceId, StringComparison.OrdinalIgnoreCase));
    }
}