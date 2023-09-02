using Azure.Storage.Blobs;

namespace Azure.AISearch.WebApp.Services;

public class AzureStorageConfigurationService
{
    private readonly ILogger<AzureStorageConfigurationService> logger;
    private readonly AppSettings settings;
    private readonly IHttpClientFactory httpClientFactory;
    private readonly BlobServiceClient blobServiceClient;

    public AzureStorageConfigurationService(ILogger<AzureStorageConfigurationService> logger, AppSettings settings, IHttpClientFactory httpClientFactory)
    {
        this.logger = logger;
        this.settings = settings;
        this.httpClientFactory = httpClientFactory;
        this.blobServiceClient = new BlobServiceClient(settings.StorageAccountConnectionString);
    }

    public async Task InitializeAsync()
    {
        ArgumentNullException.ThrowIfNull(this.settings.StorageContainerNameBlobDocuments);
        ArgumentNullException.ThrowIfNull(this.settings.StorageContainerNameBlobChunks);

        // Create the documents container if it doesn't exist yet.
        var documentsContainerWasJustCreated = await CreateContainerIfNotExistsAsync(this.settings.StorageContainerNameBlobDocuments);
        if (documentsContainerWasJustCreated && !string.IsNullOrWhiteSpace(this.settings.InitialDocumentUrls))
        {
            // Upload initial documents to the container only upon initial creation.
            this.logger.LogInformation($"Uploading initial documents to the storage container: {this.settings.InitialDocumentUrls}");
            var initialDocumentUrls = this.settings.InitialDocumentUrls.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var containerClient = this.blobServiceClient.GetBlobContainerClient(this.settings.StorageContainerNameBlobDocuments);
            foreach (var initialDocumentUrl in initialDocumentUrls)
            {
                try
                {
                    this.logger.LogInformation($"Uploading initial document to the storage container: {initialDocumentUrl}");
                    var blobName = Path.GetFileName(initialDocumentUrl);
                    var httpClient = this.httpClientFactory.CreateClient();
                    using var fileStream = await httpClient.GetStreamAsync(initialDocumentUrl);
                    await containerClient.UploadBlobAsync(blobName, fileStream);
                    await fileStream.FlushAsync();
                }
                catch (Exception ex)
                {
                    this.logger.LogWarning(ex, $"Failed to upload initial document to the storage container: {initialDocumentUrl}");
                }
            }
        }

        // Create the chunks container if it doesn't exist yet.
        await CreateContainerIfNotExistsAsync(this.settings.StorageContainerNameBlobChunks);
    }

    public async Task UninitializeAsync()
    {
        // Don't delete the documents container or anything in it, as this might hold
        // additional documents that were uploaded by the user.
        // Also don't delete the chunks container itself, as it takes a while before you
        // can recreate a container with the same name; instead delete all blobs inside it.
        // This ensures there are no left-over chunks that would get picked up by the indexer.
        var containerClient = this.blobServiceClient.GetBlobContainerClient(this.settings.StorageContainerNameBlobChunks);
        await foreach (var blob in containerClient.GetBlobsAsync())
        {
            await containerClient.DeleteBlobAsync(blob.Name);
        }
    }

    private async Task<bool> CreateContainerIfNotExistsAsync(string containerName)
    {
        var containerClient = this.blobServiceClient.GetBlobContainerClient(containerName);
        var containerExists = await containerClient.ExistsAsync();
        if (!containerExists)
        {
            await containerClient.CreateAsync();
            return true;
        }
        return false;
    }
}