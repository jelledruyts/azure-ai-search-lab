namespace Azure.AISearch.WebApp.Models;

// These are settings that are safe to override as part of the search indexer reset process
// as they are not used anywhere else and don't depend on other settings.
public class AppSettingsOverride
{
    public int? TextEmbedderNumTokens { get; set; } // If unspecified, will use the default as configured in the text embedder Function App.
    public int? TextEmbedderTokenOverlap { get; set; } // If unspecified, will use the default as configured in the text embedder Function App.
    public int? TextEmbedderMinChunkSize { get; set; } // If unspecified, will use the default as configured in the text embedder Function App.
    public string? SearchIndexerSkillType { get; set; } // If unspecified, will use the "pull" model.
    public int? SearchIndexerScheduleMinutes { get; set; } // If unspecified, will be set to 5 minutes.
}