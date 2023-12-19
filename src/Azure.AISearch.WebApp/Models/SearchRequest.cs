namespace Azure.AISearch.WebApp.Models;

public class SearchRequest
{
    public string? Query { get; set; }
    public IList<string> History { get; set; } = new List<string>();
    public EngineType Engine { get; set; } = EngineType.AzureCognitiveSearch;
    public SearchIndexType SearchIndex { get; set; } = SearchIndexType.Documents;
    public QueryType QueryType { get; set; } = QueryType.TextStandard;
    public QuerySyntax QuerySyntax { get; set; } = QuerySyntax.Simple;
    public DataSourceType DataSource { get; set; } = DataSourceType.None;
    public string? OpenAIGptDeployment { get; set; }
    public bool UseIntegratedVectorization { get; set; } = true;
    public int? VectorNearestNeighborsCount { get; set; } = Constants.Defaults.VectorNearestNeighborsCount;
    public bool LimitToDataSource { get; set; } = true; // "Limit responses to your data content"
    public string? SystemRoleInformation { get; set; } // Give the model instructions about how it should behave and any context it should reference when generating a response. You can describe the assistant’s personality, tell it what it should and shouldn’t answer, and tell it how to format responses. There’s no token limit for this section, but it will be included with every API call, so it counts against the overall token limit.
    public string? CustomOrchestrationPrompt { get; set; }
    public int? MaxTokens { get; set; } = Constants.Defaults.MaxTokens;
    public double? Temperature { get; set; } = Constants.Defaults.Temperature;
    public double? TopP { get; set; } = Constants.Defaults.TopP;
    public double? FrequencyPenalty { get; set; } = Constants.Defaults.FrequencyPenalty;
    public double? PresencePenalty { get; set; } = Constants.Defaults.PresencePenalty;
    public string? StopSequences { get; set; } = Constants.Defaults.StopSequences;
    public int? Strictness { get; set; } = Constants.Defaults.Strictness;
    public int? DocumentCount { get; set; } = Constants.Defaults.DocumentCount;

    public bool IsVectorSearch => QueryType == Models.QueryType.Vector || QueryType == Models.QueryType.HybridStandard || QueryType == Models.QueryType.HybridSemantic;
    public bool IsSemanticSearch => QueryType == Models.QueryType.TextSemantic || QueryType == Models.QueryType.HybridSemantic;
}