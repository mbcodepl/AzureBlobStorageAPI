using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace AzureBlobStorageAPI.Services;

public class BlobService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly string? _containerName;

    public BlobService(IConfiguration configuration)
    {
        string? connectionString = configuration["AzureBlobStorage:ConnectionString"];
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
}