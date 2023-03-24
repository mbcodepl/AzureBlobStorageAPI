namespace AzureBlobStorageAPI.Models;

public class BlobsInfo
{
    public string Name { get; set; }
    public string? BlobType { get; set; }
    public string? AccessTier { get; set; }
    public DateTimeOffset? LastModified { get; set; }
}