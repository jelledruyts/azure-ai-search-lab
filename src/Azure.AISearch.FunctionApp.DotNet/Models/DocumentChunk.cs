namespace Azure.AISearch.FunctionApp.Models;

public class DocumentChunk
{
    public string? Id { get; set; }
    public long ChunkIndex { get; set; }
    public long ChunkOffset { get; set; }
    public long ChunkLength { get; set; }
    public string? Content { get; set; }
    public IReadOnlyList<float>? ContentVector { get; set; }
    public string? SourceDocumentId { get; set; }
    public string? SourceDocumentTitle { get; set; }
    public string? SourceDocumentContentField { get; set; }
    public string? SourceDocumentFilePath { get; set; }
}