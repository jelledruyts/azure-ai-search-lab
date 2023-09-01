namespace Azure.AISearch.WebApp.Models;

public class SearchRequest
{
    public string? Query { get; set; }
    public IList<string> History { get; set; } = new List<string>();
    public EngineType Engine { get; set; } = EngineType.AzureCognitiveSearch;
    public SearchIndexType SearchIndex { get; set; } = SearchIndexType.Documents;
    public QueryType QueryType { get; set; } = QueryType.TextStandard;
    public DataSourceType DataSource { get; set; } = DataSourceType.None;
    public bool LimitToDataSource { get; set; } = true; // "Limit responses to your data content"
    public string SystemRoleInformation { get; set; } = Constants.SystemRoleInformations.Default; // Give the model instructions about how it should behave and any context it should reference when generating a response. You can describe the assistant’s personality, tell it what it should and shouldn’t answer, and tell it how to format responses. There’s no token limit for this section, but it will be included with every API call, so it counts against the overall token limit.

    public bool IsVectorSearch => QueryType == Models.QueryType.Vector || QueryType == Models.QueryType.HybridStandard || QueryType == Models.QueryType.HybridSemantic;
    public bool IsSemanticSearch => QueryType == Models.QueryType.TextSemantic || QueryType == Models.QueryType.HybridSemantic;
}