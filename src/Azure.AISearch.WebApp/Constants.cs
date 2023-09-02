namespace Azure.AISearch.WebApp;

public static class Constants
{
    public static class ChatRoles
    {
        public const string System = "system";
        public const string Assistant = "assistant";
        public const string Tool = "tool";
        public const string User = "user";
    }

    public static class ConfigurationNames
    {
        public const string SemanticConfigurationNameDefault = "default";
        public const string VectorSearchConfigurationNameDefault = "default";
    }
}