namespace Azure.AISearch.WebApp.Models;

public class SearchAnswer
{
    public string? SearchIndexName { get; set; }
    public string? SearchIndexKey { get; set; }
    public string? DocumentId { get; set; }
    public string? DocumentTitle { get; set; }
    public double? Score { get; set; }
    public string? Text { get; set; }
}