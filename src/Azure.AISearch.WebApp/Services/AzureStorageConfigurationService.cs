using Azure.Storage.Blobs;

namespace Azure.AISearch.WebApp.Services;

public class AzureStorageConfigurationService
{
    private readonly BlobServiceClient blobServiceClient;

    public AzureStorageConfigurationService(AppSettings settings)
    {
        this.blobServiceClient = new BlobServiceClient(settings.StorageAccountConnectionString);
    }

    public async Task<IList<string>> GetContainersAsync()
    {
        var containerNames = new List<string>();
        await foreach (var container in this.blobServiceClient.GetBlobContainersAsync())
        {
            containerNames.Add(container.Name);
        }
        return containerNames;
    }

    public async Task CreateContainerIfNotExistsAsync(string containerName)
    {
        var containerClient = this.blobServiceClient.GetBlobContainerClient(containerName);
        await containerClient.CreateIfNotExistsAsync();
    }
}