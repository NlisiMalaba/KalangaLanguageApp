namespace Kalanga.Domain;

public static class DomainRules
{
    public const int DefaultLessonXpReward = 10;
    public const int MaxAudioFileSizeBytes = 10 * 1024 * 1024;
    public const int AudioUploadMultipartOverheadBytes = 256 * 1024;
    public const int AudioUploadMaxRequestsPerWindow = 20;
    public const int AudioUploadRateLimitWindowSeconds = 60;
    public const decimal DefaultEaseFactor = 2.5m;
    public const decimal MinEaseFactor = 1.3m;
    public const decimal MaxEaseFactor = 99.99m;
    public const int DefaultSrsIntervalDays = 1;
    public const int SrsSecondIntervalDays = 6;
    public const int SrsPassingQuality = 3;
    public const int IntermediateXpThreshold = 500;
    public const int AdvancedXpThreshold = 2000;
    public const int MaxLessonScore = 100;
    public const int WeakAreaScoreThreshold = 70;
    public const int MaxSyncProgressItems = 200;
    public const int MaxSyncSrsItems = 500;
    public const int MaxSyncClientOperationIdLength = 128;
    public const int MaxSyncPayloadBytes = 256 * 1024;
}
