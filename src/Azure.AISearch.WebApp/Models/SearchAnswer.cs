namespace Azure.AISearch.WebApp.Models;

public class SearchAnswer
{
    public string? Key { get; set; }
    public string? DocumentId { get; set; }
    public string? DocumentTitle { get; set; }
    public double? Score { get; set; }
    public string? Text { get; set; }
}