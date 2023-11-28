namespace Azure.AISearch.FunctionApp.Models;

public class DocumentChunk
{
    public string? Id { get; set; }
    public string? Content { get; set; }
    public IReadOnlyList<float>? ContentVector { get; set; }
    public string? SourceDocumentId { get; set; }
    public string? SourceDocumentTitle { get; set; }
    public string? SourceDocumentContentField { get; set; }
    public string? SourceDocumentFilePath { get; set; }
}