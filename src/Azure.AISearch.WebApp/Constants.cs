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
}