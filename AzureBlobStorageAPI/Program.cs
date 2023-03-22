using Azure.Storage.Blobs.Models;
using AzureBlobStorageAPI.Services;
using Azure.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddAzureKeyVault(
    new Uri($"{builder.Configuration["AzureKeyVault:VaultUri"]}"),
    new DefaultAzureCredential());

builder.Services.AddSingleton<BlobService>();

var app = builder.Build();


app.MapGet("/", () => "Hello World!");

app.MapPost("api/blob/upload", async (BlobService blobService, IFormFile file) =>
{
    await using var fileStream = file.OpenReadStream();
    string url = await blobService.UploadAsync(file.FileName, fileStream);
    return Results.Ok(new { url });
}).Accepts<IFormFile>("multipart/form-data");

app.MapGet("api/blob/download/{fileName}", async (BlobService blobService, string fileName) =>
{
    var fileStream = await blobService.DownloadAsync(fileName);
    return Results.File(fileStream, "application/octet-stream", fileName);
});

app.MapDelete("api/blob/delete/{fileName}", async (BlobService blobService, string fileName) =>
{
    await blobService.DeleteAsync(fileName);
    return Results.Ok(new { message = $"File {fileName} has been deleted successfully." });
});

app.MapPut("api/blob/settier/{fileName}/{tier}", async (BlobService blobService, string fileName, string tier) =>
{
    AccessTier accessTier = tier switch
    {
        "Hot" => AccessTier.Hot,
        "Cool" => AccessTier.Cool,
        "Archive" => AccessTier.Archive,
        _ => AccessTier.Hot
    };
    
    await blobService.SetBlobTierAsync(fileName, accessTier);
    return Results.Ok(new { message = $"Tier for file {fileName} has been set to {tier}." });

});


app.Run();