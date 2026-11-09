using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDay.Api.Storage;

// Thrown when the image cannot be stored (not set up, or Azure is unreachable).
public class BlobStorageException : Exception
{
    public BlobStorageException(string message) : base(message) { }
}

public interface IBlobStorage
{
    /// <summary>Uploads an image to Azure Blob Storage and returns its public URL.</summary>
    Task<string> UploadImageAsync(IFormFile file, string folder);
}

// Saves images in an Azure Blob Storage container.
public class AzureBlobStorage : IBlobStorage
{
    private readonly IConfiguration config;

    public AzureBlobStorage(IConfiguration config) { this.config = config; }

    public async Task<string> UploadImageAsync(IFormFile file, string folder)
    {
        string? connectionString = config["AzureBlob:ConnectionString"];
        string containerName = config["AzureBlob:ContainerName"] ?? "raceday-images";

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new BlobStorageException("Image storage is not configured. Add AzureBlob:ConnectionString.");

        try
        {
            // Create the container the first time. "Blob" access lets anyone view the image by its link.
            var container = new BlobContainerClient(connectionString, containerName);
            await container.CreateIfNotExistsAsync(PublicAccessType.Blob);

            // A random name stops two uploads from overwriting each other.
            string blobName = $"{folder}/{Guid.NewGuid():N}{Path.GetExtension(file.FileName).ToLower()}";
            var blob = container.GetBlobClient(blobName);

            await using var stream = file.OpenReadStream();
            await blob.UploadAsync(stream, new BlobHttpHeaders { ContentType = file.ContentType });

            return blob.Uri.ToString();
        }
        catch (RequestFailedException)
        {
            throw new BlobStorageException("The image could not be uploaded to Azure Blob Storage.");
        }
    }
}

// Checks that an uploaded file is a reasonable image before it is sent to Azure.
public static class ImageUpload
{
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
    private const long MaxBytes = 5 * 1024 * 1024;

    /// <summary>Returns an error message, or null when the file is a valid image.</summary>
    public static string? Validate(IFormFile? file)
    {
        if (file == null || file.Length == 0) return "Please choose an image to upload.";
        if (file.Length > MaxBytes) return "The image must be smaller than 5 MB.";
        if (!file.ContentType.StartsWith("image/")) return "Only image files can be uploaded.";
        if (!AllowedExtensions.Contains(Path.GetExtension(file.FileName).ToLower()))
            return "Allowed image types are JPG, PNG, GIF and WEBP.";
        return null;
    }
}
