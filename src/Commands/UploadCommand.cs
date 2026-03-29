using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;
using Storage.Console.Config;
using Storage.Console.Storage;

namespace Storage.Console.Commands;

public sealed class UploadCommand : AsyncCommand<UploadCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "<PROFILE>")]
        [Description("Storage profile name from config.yaml")]
        public required string Profile { get; init; }

        [CommandArgument(1, "<PATH>")]
        [Description("File or folder path to upload")]
        public required string Path { get; init; }

        [CommandOption("--zipped")]
        [Description("Force zip even for single files")]
        public bool Zipped { get; init; }

        [CommandOption("--expires <DATE>")]
        [Description("Expiry date for the SAS URL (YYYY-MM-DD). Defaults to 30 days from now.")]
        public string? Expires { get; init; }

        public override ValidationResult Validate()
        {
            if (!File.Exists(Path) && !Directory.Exists(Path))
                return ValidationResult.Error($"Path not found: {Path}");

            if (Expires is not null && !DateOnly.TryParse(Expires, out _))
                return ValidationResult.Error($"Invalid date format: {Expires}. Use YYYY-MM-DD.");

            return ValidationResult.Success();
        }
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings,
        CancellationToken cancellation = default)
    {
        try
        {
            var config = ConfigLoader.Load();

            if (!config.Storage.TryGetValue(settings.Profile, out var profile))
            {
                AnsiConsole.MarkupLine($"[red]Unknown storage profile:[/] {Markup.Escape(settings.Profile)}");
                AnsiConsole.MarkupLine($"[grey]Available profiles: {string.Join(", ", config.Storage.Keys)}[/]");
                return 1;
            }

            if (!string.Equals(profile.Provider, "azure", StringComparison.OrdinalIgnoreCase))
            {
                AnsiConsole.MarkupLine($"[red]Unsupported provider:[/] {Markup.Escape(profile.Provider)}. Only 'azure' is supported.");
                return 1;
            }

            var expires = settings.Expires is not null
                ? new DateTimeOffset(DateOnly.Parse(settings.Expires), TimeOnly.MinValue, TimeSpan.Zero)
                : DateTimeOffset.UtcNow.AddDays(30);

            var fullPath = System.IO.Path.GetFullPath(settings.Path);
            var isDirectory = Directory.Exists(fullPath);
            var shouldZip = isDirectory || settings.Zipped;

            string uploadFilePath;
            string blobName;

            if (shouldZip)
            {
                AnsiConsole.MarkupLine("[grey]Zipping...[/]");

                if (isDirectory)
                {
                    uploadFilePath = ZipHelper.ZipDirectory(fullPath);
                    blobName = $"{System.IO.Path.GetFileName(fullPath)}.zip";
                }
                else
                {
                    uploadFilePath = ZipHelper.ZipSingleFile(fullPath);
                    blobName = $"{System.IO.Path.GetFileNameWithoutExtension(fullPath)}.zip";
                }
            }
            else
            {
                uploadFilePath = fullPath;
                blobName = System.IO.Path.GetFileName(fullPath);
            }

            try
            {
                AnsiConsole.MarkupLine("[grey]Uploading...[/]");

                var sasUrl = await AzureBlobUploader.UploadAsync(
                    profile.AccountName,
                    profile.ContainerName,
                    blobName,
                    uploadFilePath,
                    expires,
                    cancellation);

                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[green]Uploaded successfully![/]");
                AnsiConsole.MarkupLine($"[grey]Expires:[/] {expires:yyyy-MM-dd}");
                AnsiConsole.WriteLine();
                AnsiConsole.WriteLine(sasUrl);

                return 0;
            }
            finally
            {
                if (shouldZip && File.Exists(uploadFilePath))
                    File.Delete(uploadFilePath);
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Upload failed:[/] {Markup.Escape(ex.Message)}");
            return 1;
        }
    }
}
