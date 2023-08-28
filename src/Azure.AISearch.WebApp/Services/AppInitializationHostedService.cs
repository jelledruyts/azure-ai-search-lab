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
        // Create the Azure Storage containers if they don't already exist.
        await this.azureStorageConfigurationService.CreateContainerIfNotExistsAsync(Constants.ContainerNames.BlobDocuments);
        await this.azureStorageConfigurationService.CreateContainerIfNotExistsAsync(Constants.ContainerNames.BlobChunks);

        // Initialize the Azure Cognitive Search indexes, datasources, skillsets and indexers.
        await this.azureSearchConfigurationService.InitializeSearchAsync(Constants.IndexNames.BlobDocuments, Constants.IndexNames.BlobChunks, Constants.ContainerNames.BlobDocuments, Constants.ContainerNames.BlobChunks);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}