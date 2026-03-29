using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Storage.Console.Config;

public static class ConfigLoader
{
    private const string ConfigFileName = "config.yaml";

    public static StorageConfig Load()
    {
        var path = GetConfigPath();

        if (!File.Exists(path))
            throw new FileNotFoundException($"Config file not found: {path}");

        var yaml = File.ReadAllText(path);

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        return deserializer.Deserialize<StorageConfig>(yaml)
               ?? throw new InvalidOperationException("Config file is empty.");
    }

    private static string GetConfigPath()
    {
        var exeDir = AppContext.BaseDirectory;
        return Path.Combine(exeDir, ConfigFileName);
    }
}
