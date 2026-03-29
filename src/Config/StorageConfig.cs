namespace Storage.Console.Config;

public sealed class StorageConfig
{
    public Dictionary<string, StorageProfile> Storage { get; set; } = new();
}

public sealed class StorageProfile
{
    public required string Provider { get; set; }
    public required string AccountName { get; set; }
    public required string ContainerName { get; set; }
}
