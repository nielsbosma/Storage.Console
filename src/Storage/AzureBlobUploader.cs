using System.Diagnostics;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace Storage.Console.Storage;

public static class AzureBlobUploader
{
    public static async Task<string> UploadAsync(
        string accountName,
        string containerName,
        string blobName,
        string filePath,
        DateTimeOffset expires,
        CancellationToken cancellationToken = default)
    {
        var uploadSasToken = await GenerateSasTokenAsync(
            accountName, containerName, permissions: "acw", expires, cancellationToken);

        var blobUri = new Uri(
            $"https://{accountName}.blob.core.windows.net/{containerName}/{blobName}?{uploadSasToken}");

        var blobClient = new BlobClient(blobUri);

        var contentType = GetContentType(blobName);
        var uploadOptions = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
        };

        await using var fileStream = File.OpenRead(filePath);
        await blobClient.UploadAsync(fileStream, uploadOptions, cancellationToken);

        var readSasToken = await GenerateSasTokenAsync(
            accountName, containerName, permissions: "r", expires, cancellationToken);

        return $"https://{accountName}.blob.core.windows.net/{containerName}/{blobName}?{readSasToken}";
    }

    private static string GetContentType(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".svg" => "image/svg+xml",
            ".pdf" => "application/pdf",
            ".json" => "application/json",
            ".xml" => "application/xml",
            ".html" or ".htm" => "text/html",
            ".css" => "text/css",
            ".js" => "application/javascript",
            ".txt" or ".log" or ".md" => "text/plain",
            ".csv" => "text/csv",
            ".zip" => "application/zip",
            ".gz" => "application/gzip",
            ".tar" => "application/x-tar",
            ".mp4" => "video/mp4",
            ".mp3" => "audio/mpeg",
            _ => "application/octet-stream"
        };
    }

    private static async Task<string> GenerateSasTokenAsync(
        string accountName,
        string containerName,
        string permissions,
        DateTimeOffset expires,
        CancellationToken cancellationToken)
    {
        var expiryStr = expires.ToString("yyyy-MM-dd");

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = OperatingSystem.IsWindows() ? "cmd.exe" : "az",
                Arguments = OperatingSystem.IsWindows()
                    ? $"/c az storage container generate-sas --account-name {accountName} --name {containerName} --permissions {permissions} --expiry {expiryStr} --https-only --output tsv"
                    : $"storage container generate-sas --account-name {accountName} --name {containerName} --permissions {permissions} --expiry {expiryStr} --https-only --output tsv",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();

        var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        var error = await process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
            throw new InvalidOperationException(
                $"Failed to generate SAS token via az CLI: {error.Trim()}");

        var sasToken = output.Trim();

        if (string.IsNullOrEmpty(sasToken))
            throw new InvalidOperationException(
                "az CLI returned an empty SAS token. Ensure you are logged in (az login) and have access to the storage account.");

        return sasToken;
    }
}
