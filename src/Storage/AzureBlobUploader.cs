using System.Diagnostics;
using System.Text.Json;
using Azure.Storage.Blobs;

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

        await using var fileStream = File.OpenRead(filePath);
        await blobClient.UploadAsync(fileStream, overwrite: true, cancellationToken);

        var readSasToken = await GenerateSasTokenAsync(
            accountName, containerName, permissions: "r", expires, cancellationToken);

        return $"https://{accountName}.blob.core.windows.net/{containerName}/{blobName}?{readSasToken}";
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
                FileName = "az",
                Arguments =
                    $"storage container generate-sas --account-name {accountName} --name {containerName} --permissions {permissions} --expiry {expiryStr} --https-only --output tsv",
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
