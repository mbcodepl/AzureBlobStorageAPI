using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using AzureBlobStorageAPI.Models;

namespace AzureBlobStorageAPI.Services;

public class BlobService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly string? _containerName;

    public BlobService(IConfiguration configuration)
    {
        string? connectionString = configuration["AzureBlobConnectionString"];
        _containerName = configuration["AzureBlobStorage:ContainerName"];
        _blobServiceClient = new BlobServiceClient(connectionString);
    }

    public async Task<string> UploadAsync(string fileName, Stream fileStream)
    {
        BlobContainerClient? containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        await containerClient.CreateIfNotExistsAsync();
        BlobClient? blobClient = containerClient.GetBlobClient(fileName);
        await blobClient.UploadAsync(fileStream, overwrite: true);
        return blobClient.Uri.ToString();
    }

    public async Task<Stream> DownloadAsync(string fileName)
    {
        BlobContainerClient? containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        BlobClient? blobClient = containerClient.GetBlobClient(fileName);
        Response<BlobDownloadInfo>? response = await blobClient.DownloadAsync();
        return response.Value.Content;
    }

    public async Task DeleteAsync(string fileName)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        var blobClient = containerClient.GetBlobClient(fileName);
        await blobClient.DeleteAsync();
    }

    public async Task SetBlobTierAsync(string fileName, AccessTier tier)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        var blobClient = containerClient.GetBlobClient(fileName);
        await blobClient.SetAccessTierAsync(tier);
    }

    public async Task<List<string>> ListContainersAsync()
    {
        AsyncPageable<BlobContainerItem>? containers = _blobServiceClient.GetBlobContainersAsync();
        List<string> containerNames = new();

        await foreach (var container in containers)
        {
            containerNames.Add(container.Name);
        }

        return containerNames;
    }

    public async Task<List<BlobsInfo>> ListBlobsAsync()
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        AsyncPageable<BlobItem>? blobs = containerClient.GetBlobsAsync();
        List<BlobsInfo> blobInfos = new();

        await foreach (var blob in blobs)
        {
            blobInfos.Add(new BlobsInfo()
            {
                Name = blob.Name,
                AccessTier = blob.Properties.AccessTier.ToString(),
                BlobType = blob.Properties.BlobType.ToString(),
                LastModified = blob.Properties.LastModified,
            });
        }

        return blobInfos;
    }

    public async Task UploadBlockBlobAsync(string fileName, Stream fileStream)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        var blobClient = containerClient.GetBlobClient(fileName);
        await blobClient.UploadAsync(fileStream, overwrite: true);
    }

    public async Task UploadAppendBlobAsync(string fileName, Stream fileStream)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        var appendBlobClient = containerClient.GetAppendBlobClient(fileName);

        if (!await appendBlobClient.ExistsAsync())
        {
            await appendBlobClient.CreateAsync();
        }

        await appendBlobClient.AppendBlockAsync(fileStream);
    }

    public async Task UploadPageBlobAsync(string fileName, Stream fileStream)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        var pageBlobClient = containerClient.GetPageBlobClient(fileName);

        long fileSize = fileStream.Length;
        long roundedSize = (fileSize + 511) / 512 * 512;

        if (!await pageBlobClient.ExistsAsync())
        {
            await pageBlobClient.CreateAsync(roundedSize);
        }

        MemoryStream paddedStream = new MemoryStream(new byte[roundedSize]);
        await fileStream.CopyToAsync(paddedStream);
        paddedStream.Seek(0, SeekOrigin.Begin);

        await pageBlobClient.UploadPagesAsync(paddedStream, 0);
    }
    
    public async Task<string> CopyBlobAsync(string sourceBlobName, string destinationBlobName)
    {
        BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        BlobClient sourceBlobClient = containerClient.GetBlobClient(sourceBlobName);
        BlobClient destinationBlobClient = containerClient.GetBlobClient(destinationBlobName);

        Uri sourceBlobUri = sourceBlobClient.Uri;
        await destinationBlobClient.StartCopyFromUriAsync(sourceBlobUri);
        
        BlobProperties destinationBlobProperties = await destinationBlobClient.GetPropertiesAsync();
        while (destinationBlobProperties.CopyStatus == CopyStatus.Pending)
        {
            await Task.Delay(1000);
            destinationBlobProperties = await destinationBlobClient.GetPropertiesAsync();
        }

        return destinationBlobClient.Uri.ToString();
    }
}