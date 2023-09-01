namespace Azure.AISearch.WebApp.Models;

public class SearchScenario
{
    public string? Id { get; set; }
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public SearchRequest SearchRequest { get; set; } = new SearchRequest();
}