using System.IO.Compression;

namespace Storage.Console.Storage;

public static class ZipHelper
{
    private static readonly HashSet<string> ExcludePatterns = new(StringComparer.OrdinalIgnoreCase)
    {
        ".git", "bin", "obj", "node_modules"
    };

    public static string ZipSingleFile(string filePath)
    {
        var zipFilePath = Path.Combine(Path.GetTempPath(), $"storage-cli-{Guid.NewGuid():N}.zip");

        using var archive = ZipFile.Open(zipFilePath, ZipArchiveMode.Create);
        archive.CreateEntryFromFile(filePath, Path.GetFileName(filePath));

        return zipFilePath;
    }

    public static string ZipDirectory(string sourceDirectory)
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), $"storage-cli-{Guid.NewGuid():N}");
        var zipFilePath = Path.Combine(Path.GetTempPath(), $"storage-cli-{Guid.NewGuid():N}.zip");

        try
        {
            CopyDirectory(sourceDirectory, tempDirectory);
            ZipFile.CreateFromDirectory(tempDirectory, zipFilePath);
            return zipFilePath;
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
                Directory.Delete(tempDirectory, recursive: true);
        }
    }

    private static void CopyDirectory(string source, string destination)
    {
        var sourceDir = new DirectoryInfo(source);

        Directory.CreateDirectory(destination);

        foreach (var file in sourceDir.GetFiles())
        {
            file.CopyTo(Path.Combine(destination, file.Name));
        }

        foreach (var subDir in sourceDir.GetDirectories())
        {
            if (ExcludePatterns.Contains(subDir.Name))
                continue;

            CopyDirectory(subDir.FullName, Path.Combine(destination, subDir.Name));
        }
    }
}
