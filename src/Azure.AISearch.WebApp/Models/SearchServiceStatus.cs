namespace Azure.AISearch.WebApp.Models;

public class SearchServiceStatus
{
    public string? Sku { get; set; }
    public int? BlobIndexerMaxFileSizeMB { get; set; }
    public int? BlobIndexerMaxCharactersExtractedPerFile { get; set; }
}