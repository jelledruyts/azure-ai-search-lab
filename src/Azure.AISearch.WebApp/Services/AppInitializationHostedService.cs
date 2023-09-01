namespace Azure.AISearch.WebApp.Services;

public class AppInitializationHostedService : IHostedService
{
    private readonly AzureStorageConfigurationService azureStorageConfigurationService;
    private readonly AzureCognitiveSearchConfigurationService azureSearchConfigurationService;

    public AppInitializationHostedService(AzureCognitiveSearchConfigurationService azureSearchConfigurationService, AzureStorageConfigurationService azureStorageConfigurationService)
    {
        this.azureSearchConfigurationService = azureSearchConfigurationService;
        this.azureStorageConfigurationService = azureStorageConfigurationService;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Initialize the Azure Storage service.
        await this.azureStorageConfigurationService.InitializeAsync();

        // Initialize the Azure Cognitive Search indexes, datasources, skillsets and indexers.
        await this.azureSearchConfigurationService.InitializeAsync();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}