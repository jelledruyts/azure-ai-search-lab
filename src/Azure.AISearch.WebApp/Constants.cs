using Azure.Search.Documents.Indexes.Models;

namespace Azure.AISearch.WebApp;

public static class Constants
{
    public static class ConfigurationNames
    {
        public const string SemanticConfigurationNameDefault = "default";
        public const string VectorSearchConfigurationNameDefault = "default";
    }

    public static class SearchIndexerSkillTypes
    {
        public const string Pull = "pull";
        public const string Push = "push";
    }

    public static class Defaults
    {
        public const string SystemRoleInformation = "You are an AI assistant that helps people find information.";

        // Adapted from https://github.com/Azure-Samples/azure-search-openai-demo-csharp/blob/feature/embeddingSearch/app/backend/Services/RetrieveThenReadApproachService.cs
        public const string CustomOrchestrationPrompt = @"You are an intelligent assistant.
Use 'you' to refer to the individual asking the questions even if they ask with 'I'.
Answer the following question using only the data provided in the sources below.
For tabular information return it as an HTML table. Do not return markdown format.
Each source has a name followed by a colon and the actual information, always include the source name for each fact you use in the response.
If you cannot answer using the sources below, say you don't know.

###
Question: 'What is the deductible for the employee plan for a visit to Overlake in Bellevue?'

Sources:
info1.txt: deductibles depend on whether you are in-network or out-of-network. In-network deductibles are $500 for employees and $1000 for families. Out-of-network deductibles are $1000 for employees and $2000 for families.
info2.pdf: Overlake is in-network for the employee plan.
info3.pdf: Overlake is the name of the area that includes a park and ride near Bellevue.
info4.pdf: In-network institutions include Overlake, Swedish, and others in the region

Answer:
In-network deductibles are $500 for employees and $1000 for families <cite>info1.txt</cite> and Overlake is in-network for the employee plan <cite>info2.pdf</cite><cite>info4.pdf</cite>.

###
Question: {{$query}}?

Sources:
{{$sources}}

Answer:
";
        public const int VectorNearestNeighborsCount = 50;
        public const int MaxTokens = 800;
        public const double Temperature = 0.7;
        public const double TopP = 0.95;
        public const double FrequencyPenalty = 0.0;
        public const double PresencePenalty = 0.0;
        public const string StopSequences = "";
        public const int HnswParametersM = 4;
        public const int HnswParametersEfConstruction = 400;
        public const int HnswParametersEfSearch = 500;
        public static readonly VectorSearchAlgorithmMetric HnswParametersMetric = VectorSearchAlgorithmMetric.Cosine;
    }
}