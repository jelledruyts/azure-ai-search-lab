namespace Azure.AISearch.WebApp.Models;

public class SearchIndexStatus
{
    public string? Name { get; set; }
    public long DocumentCount { get; set; }
    public bool HasIndexer { get; set;}
    public string? IndexerStatus { get; set; }
    public DateTimeOffset? IndexerLastRunTime { get; set; }
}