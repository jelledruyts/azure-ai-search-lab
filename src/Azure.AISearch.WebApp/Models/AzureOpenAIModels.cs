// These model classes are based on the Azure OpenAI playground and samples
// to use while waiting for .NET SDK support.

using System.Text.Json.Serialization;

namespace Azure.AISearch.WebApp.Models;

public class ChatCompletionsRequest
{
    [JsonPropertyName("messages")]
    public IList<ChatRequestMessage> Messages { get; set; } = new List<ChatRequestMessage>();

    [JsonPropertyName("deployment")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] // When calling the standard API, omit this parameter entirely when null.
    public string? Deployment { get; set; }

    [JsonPropertyName("temperature")]
    public int? Temperature { get; set; } = 0;

    [JsonPropertyName("max_tokens")]
    public int? MaxTokens { get; set; } = 800;

    [JsonPropertyName("top_p")]
    public int? TopP { get; set; } = 1;

    [JsonPropertyName("stream")]
    public bool? Stream { get; set; } = false;

    [JsonPropertyName("dataSources")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] // When calling the standard API, omit this parameter entirely when null.
    public IList<DataSource>? DataSources { get; set; }
}

public class ChatRequestMessage
{
    [JsonPropertyName("role")]
    public string? Role { get; set; }

    [JsonPropertyName("content")]
    public string? Content { get; set; }
}

public class DataSource
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("parameters")]
    public AzureCognitiveSearchParameters Parameters { get; set; } = new AzureCognitiveSearchParameters();
}

public class AzureCognitiveSearchParameters
{
    [JsonPropertyName("endpoint")]
    public string? Endpoint { get; set; }

    [JsonPropertyName("key")]
    public string? Key { get; set; }

    [JsonPropertyName("indexName")]
    public string? IndexName { get; set; }

    [JsonPropertyName("semanticConfiguration")]
    public string? SemanticConfiguration { get; set; }

    [JsonPropertyName("queryType")]
    public AzureCognitiveSearchQueryType? QueryType { get; set; } = AzureCognitiveSearchQueryType.semantic;

    [JsonPropertyName("fieldsMapping")]
    public AzureCognitiveSearchParametersFieldsMapping FieldsMapping { get; set; } = new AzureCognitiveSearchParametersFieldsMapping();

    [JsonPropertyName("inScope")]
    public bool? InScope { get; set; } // Limit responses to data from the data source only 

    [JsonPropertyName("roleInformation")]
    public string? RoleInformation { get; set; }

    [JsonPropertyName("filter")]
    public string? Filter { get; set; } // Filter pattern for security trimming, see https://learn.microsoft.com/azure/ai-services/openai/concepts/use-your-data#document-level-access-control.

    [JsonPropertyName("embeddingEndpoint")]
    public string? EmbeddingEndpoint { get; set; }

    [JsonPropertyName("embeddingKey")]
    public string? EmbeddingKey { get; set; }
}

// Note: There's no built-in mechanism to configure the serialized enum strings,
// so we just keep them as needed by the target API (camelCased rather than PascalCased).
// See https://github.com/dotnet/runtime/issues/74385.
public enum AzureCognitiveSearchQueryType
{
    simple,
    semantic,
    vector,
    vectorSemanticHybrid,
    vectorSimpleHybrid
}

public class AzureCognitiveSearchParametersFieldsMapping
{
    [JsonPropertyName("contentFieldsSeparator")]
    public string? ContentFieldsSeparator { get; set; } = "\\n";

    [JsonPropertyName("contentFields")]
    public IList<string> ContentFields { get; set; } = new List<string>();

    [JsonPropertyName("filepathField")]
    public string? FilepathField { get; set; }

    [JsonPropertyName("titleField")]
    public string? TitleField { get; set; }

    [JsonPropertyName("urlField")]
    public string? UrlField { get; set; }

    [JsonPropertyName("vectorFields")]
    public IList<string> VectorFields { get; set; } = new List<string>();
}

public class ChatCompletionsResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("model")]
    public string? Model { get; set; }

    [JsonPropertyName("created")]
    public int? Created { get; set; }

    [JsonPropertyName("object")]
    public string? Object { get; set; }

    [JsonPropertyName("choices")]
    public IList<Choice> Choices { get; set; } = new List<Choice>();
}

public class Choice
{
    [JsonPropertyName("index")]
    public int? Index { get; set; }

    [JsonPropertyName("message")]
    public ChatResponseMessage? Message { get; set; } // OpenAI Standard

    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; set; } // OpenAI Standard        

    [JsonPropertyName("messages")]
    public IList<ChatResponseMessage> Messages { get; set; } = new List<ChatResponseMessage>(); // OpenAI On Your Data
}

public class ChatResponseMessage
{
    [JsonPropertyName("index")]
    public int? Index { get; set; }

    [JsonPropertyName("role")]
    public string? Role { get; set; }

    [JsonPropertyName("content")]
    public string? Content { get; set; } // If role is "tool", this is a JSON object of type ChatResponseMessageContent

    [JsonIgnore]
    public ChatResponseMessageContent? ContentObject { get; set; }

    [JsonPropertyName("end_turn")]
    public bool? EndTurn { get; set; }
}

public class ChatResponseMessageContent
{
    [JsonPropertyName("citations")]
    public IList<Citation> Citations { get; set; } = new List<Citation>();

    [JsonPropertyName("intent")]
    public string? Intent { get; set; } // This seems to be yet another nested JSON object, as an array of strings
}

public class Citation
{
    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("filepath")]
    public string? Filepath { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("metadata")]
    public CitationMetadata Metadata { get; set; } = new CitationMetadata();

    [JsonPropertyName("chunk_id")]
    public string? ChunkId { get; set; }
}

public class CitationMetadata
{
    [JsonPropertyName("chunking")]
    public string? Chunking { get; set; }
}