using AzureBlobStorageAPI.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<BlobService>();

var app = builder.Build();


app.MapGet("/", () => "Hello World!");

app.MapPost("api/blob/upload", async (BlobService blobService, IFormFile file) =>
{
    using var fileStream = file.OpenReadStream();
    var url = await blobService.UploadAsync(file.FileName, fileStream);
    return Results.Ok(new { url });
}).Accepts<IFormFile>("multipart/form-data");

app.MapGet("api/blob/download/{fileName}", async (BlobService blobService, string fileName) =>
{
    var fileStream = await blobService.DownloadAsync(fileName);
    return Results.File(fileStream, "application/octet-stream", fileName);
});

app.Run();