namespace Kalanga.Infrastructure.Storage;

public sealed class AudioStorageOptions
{
    public const string SectionName = "AudioStorage";

    public string Bucket { get; set; } = string.Empty;

    public string Region { get; set; } = "eu-west-1";

    public string CdnBaseUrl { get; set; } = string.Empty;

    public int TimeoutSeconds { get; set; } = 30;

    public int MaxConcurrentUploads { get; set; } = 8;
}
