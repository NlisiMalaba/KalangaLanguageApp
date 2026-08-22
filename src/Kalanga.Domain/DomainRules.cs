namespace Kalanga.Domain;

public static class DomainRules
{
    public const int DefaultLessonXpReward = 10;
    public const int MaxAudioFileSizeBytes = 10 * 1024 * 1024;
    public const int AudioUploadMultipartOverheadBytes = 256 * 1024;
    public const int AudioUploadMaxRequestsPerWindow = 20;
    public const int AudioUploadRateLimitWindowSeconds = 60;
    public const decimal DefaultEaseFactor = 2.5m;
    public const int DefaultSrsIntervalDays = 1;
    public const int IntermediateXpThreshold = 500;
    public const int AdvancedXpThreshold = 2000;
}
