using Azure.Storage.Blobs.Models;
using AzureBlobStorageAPI.Services;
using Azure.Identity;
using AzureBlobStorageAPI.Models;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerUI;
using Swashbuckle.AspNetCore.SwaggerGen;


var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddAzureKeyVault(
    new Uri($"{builder.Configuration["AzureKeyVault:VaultUri"]}"),
    new DefaultAzureCredential());

builder.Services.AddSingleton<BlobService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => c.SwaggerDoc("v1", new OpenApiInfo { Title = "AzureBlobStorageAPI", Version = "v1" }));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("api/blob/listcontainers", async (BlobService blobService) =>
{
    List<string> containers = await blobService.ListContainersAsync();
    return Results.Ok(containers);
});

app.MapGet("api/blob/listblobs", async (BlobService blobService) =>
{
    List<BlobsInfo> blobs = await blobService.ListBlobsAsync();
    return Results.Ok(blobs);
});

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

app.MapPost("api/blob/upload-sample-files", async (BlobService blobService) =>
{
    byte[] blockBlobContent = System.Text.Encoding.UTF8.GetBytes("This is a sample block blob.");
    using var blockBlobStream = new MemoryStream(blockBlobContent);
    await blobService.UploadBlockBlobAsync("sample-block-blob.txt", blockBlobStream);

    byte[] appendBlobContent = System.Text.Encoding.UTF8.GetBytes("This is a sample append blob.");
    using var appendBlobStream = new MemoryStream(appendBlobContent);
    await blobService.UploadAppendBlobAsync("sample-append-blob.txt", appendBlobStream);
    
    byte[] pageBlobContent = System.Text.Encoding.UTF8.GetBytes("This is a sample page blob.".PadRight(512, '\0'));
    using var pageBlobStream = new MemoryStream(pageBlobContent);
    await blobService.UploadPageBlobAsync("sample-page-blob.txt", pageBlobStream);

    return Results.Ok(new { message = "Sample files uploaded successfully." });
});

app.Run();