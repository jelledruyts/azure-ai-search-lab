using Azure.AISearch.FunctionApp.Models;
using Microsoft.SemanticKernel.Text;

namespace Azure.AISearch.FunctionApp.Services;

public class SemanticKernelChunkingService
{
    public IList<string> GetChunks(SkillRequestRecordData data)
    {
        ArgumentNullException.ThrowIfNull(data.Text);
        ArgumentNullException.ThrowIfNull(data.NumTokens);
        ArgumentNullException.ThrowIfNull(data.TokenOverlap);
        if (string.Equals(Path.GetExtension(data.FilePath), ".md", StringComparison.InvariantCultureIgnoreCase))
        {
            // Use specialized chunking for markdown files.
            var lines = TextChunker.SplitMarkDownLines(data.Text, data.NumTokens.Value);
            return TextChunker.SplitMarkdownParagraphs(lines, data.NumTokens.Value, data.TokenOverlap.Value);
        }
        else
        {
            // Treat everything else as plain text, assuming the search indexer has already
            // done the document cracking from its native file format to text.
            var lines = TextChunker.SplitPlainTextLines(data.Text, data.NumTokens.Value);
            return TextChunker.SplitPlainTextParagraphs(lines, data.NumTokens.Value, data.TokenOverlap.Value);
        }
    }

    public int EstimateChunkSize(string text)
    {
        // Estimate the chunk size based on the number of characters in the text.
        // This is a very rough estimate that assumes an average of 4 characters per token, see:
        // https://github.com/microsoft/semantic-kernel/blob/5a79a727e2e128fee54fa84372cbcae1d714fcc5/dotnet/src/SemanticKernel/Text/TextChunker.cs#L281-L284
        return text.Length / 4;
    }
}