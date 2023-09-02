using Azure.Storage.Blobs;

namespace Azure.AISearch.WebApp.Services;

public class AzureStorageConfigurationService
{
    private readonly ILogger<AzureStorageConfigurationService> logger;
    private readonly AppSettings settings;
    private readonly IHttpClientFactory httpClientFactory;
    private readonly BlobContainerClient documentsContainerClient;
    private readonly BlobContainerClient chunksContainerClient;

    public AzureStorageConfigurationService(ILogger<AzureStorageConfigurationService> logger, AppSettings settings, IHttpClientFactory httpClientFactory)
    {
        this.logger = logger;
        this.settings = settings;
        this.httpClientFactory = httpClientFactory;
        var blobServiceClient = new BlobServiceClient(settings.StorageAccountConnectionString);
        this.documentsContainerClient = blobServiceClient.GetBlobContainerClient(this.settings.StorageContainerNameBlobDocuments);
        this.chunksContainerClient = blobServiceClient.GetBlobContainerClient(this.settings.StorageContainerNameBlobChunks);
    }

    public async Task InitializeAsync()
    {
        ArgumentNullException.ThrowIfNull(this.settings.StorageContainerNameBlobDocuments);
        ArgumentNullException.ThrowIfNull(this.settings.StorageContainerNameBlobChunks);

        // Create the documents container if it doesn't exist yet.
        var documentsContainerWasJustCreated = await CreateContainerIfNotExistsAsync(this.documentsContainerClient);
        if (documentsContainerWasJustCreated && !string.IsNullOrWhiteSpace(this.settings.InitialDocumentUrls))
        {
            // Upload initial documents to the container only upon initial creation.
            this.logger.LogInformation($"Uploading initial documents to the storage container: {this.settings.InitialDocumentUrls}");
            var initialDocumentUrls = this.settings.InitialDocumentUrls.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var initialDocumentUrl in initialDocumentUrls)
            {
                try
                {
                    this.logger.LogInformation($"Uploading initial document to the storage container: {initialDocumentUrl}");
                    var httpClient = this.httpClientFactory.CreateClient();
                    using var fileStream = await httpClient.GetStreamAsync(initialDocumentUrl);
                    await UploadDocumentAsync(fileStream, Path.GetFileName(initialDocumentUrl));
                }
                catch (Exception ex)
                {
                    this.logger.LogWarning(ex, $"Failed to upload initial document to the storage container: {initialDocumentUrl}");
                }
            }
        }

        // Create the chunks container if it doesn't exist yet.
        await CreateContainerIfNotExistsAsync(this.chunksContainerClient);
    }

    public async Task UploadDocumentAsync(Stream fileStream, string fileName)
    {
        await this.documentsContainerClient.UploadBlobAsync(fileName, fileStream);
        await fileStream.FlushAsync();
    }

    public async Task UninitializeAsync()
    {
        // Don't delete the documents container or anything in it, as this might hold
        // additional documents that were uploaded by the user.
        // Also don't delete the chunks container itself, as it takes a while before you
        // can recreate a container with the same name; instead delete all blobs inside it.
        // This ensures there are no left-over chunks that would get picked up by the indexer.
        await foreach (var blob in this.chunksContainerClient.GetBlobsAsync())
        {
            await this.chunksContainerClient.DeleteBlobAsync(blob.Name);
        }
    }

    private async Task<bool> CreateContainerIfNotExistsAsync(BlobContainerClient containerClient)
    {
        var containerExists = await containerClient.ExistsAsync();
        if (!containerExists)
        {
            await containerClient.CreateAsync();
            return true;
        }
        return false;
    }
}