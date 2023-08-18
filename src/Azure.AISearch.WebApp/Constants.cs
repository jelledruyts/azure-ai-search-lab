namespace Azure.AISearch.WebApp;

public static class Constants
{
    public static class ContainerNames
    {
        public const string BlobDocuments = "blob-documents";
        public const string BlobChunks = "blob-chunks";
    }

    public static class IndexNames
    {
        public const string BlobDocuments = "blob-documents";
        public const string BlobChunks = "blob-chunks";
    }

    public static class ChatRoles
    {
        public const string System = "system";
        public const string Assistant = "assistant";
        public const string Tool = "tool";
        public const string User = "user";
    }

    public static class VectorDimensions
    {
        public const int TextEmbeddingAda002 = 1536;
    }

    public static class ConfigurationNames
    {
        public const string SemanticConfigurationNameDefault = "default";
        public const string VectorSearchConfigurationNameDefault = "default";
    }
}