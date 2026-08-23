namespace Kalanga.Domain.ValueObjects;

public readonly record struct LanguageId
{
    public Guid Value { get; }

    private LanguageId(Guid value) => Value = value;

    public static LanguageId New() => new(Guid.CreateVersion7());

    public static LanguageId From(Guid value) => new(Guard.NotEmpty(value, nameof(value)));

    public override string ToString() => Value.ToString();
}

public readonly record struct UserId
{
    public Guid Value { get; }

    private UserId(Guid value) => Value = value;

    public static UserId New() => new(Guid.CreateVersion7());

    public static UserId From(Guid value) => new(Guard.NotEmpty(value, nameof(value)));

    public override string ToString() => Value.ToString();
}

public readonly record struct LessonId
{
    public Guid Value { get; }

    private LessonId(Guid value) => Value = value;

    public static LessonId New() => new(Guid.CreateVersion7());

    public static LessonId From(Guid value) => new(Guard.NotEmpty(value, nameof(value)));

    public override string ToString() => Value.ToString();
}

public readonly record struct PhraseId
{
    public Guid Value { get; }

    private PhraseId(Guid value) => Value = value;

    public static PhraseId New() => new(Guid.CreateVersion7());

    public static PhraseId From(Guid value) => new(Guard.NotEmpty(value, nameof(value)));

    public override string ToString() => Value.ToString();
}

public readonly record struct LanguageVariationId
{
    public Guid Value { get; }

    private LanguageVariationId(Guid value) => Value = value;

    public static LanguageVariationId New() => new(Guid.CreateVersion7());

    public static LanguageVariationId From(Guid value) => new(Guard.NotEmpty(value, nameof(value)));

    public override string ToString() => Value.ToString();
}

public readonly record struct AudioRecordingId
{
    public Guid Value { get; }

    private AudioRecordingId(Guid value) => Value = value;

    public static AudioRecordingId New() => new(Guid.CreateVersion7());

    public static AudioRecordingId From(Guid value) => new(Guard.NotEmpty(value, nameof(value)));

    public override string ToString() => Value.ToString();
}

public readonly record struct ExerciseId
{
    public Guid Value { get; }

    private ExerciseId(Guid value) => Value = value;

    public static ExerciseId New() => new(Guid.CreateVersion7());

    public static ExerciseId From(Guid value) => new(Guard.NotEmpty(value, nameof(value)));

    public override string ToString() => Value.ToString();
}

public readonly record struct ContentPackId
{
    public Guid Value { get; }

    private ContentPackId(Guid value) => Value = value;

    public static ContentPackId New() => new(Guid.CreateVersion7());

    public static ContentPackId From(Guid value) => new(Guard.NotEmpty(value, nameof(value)));

    public override string ToString() => Value.ToString();
}

public readonly record struct LearnerProgressId
{
    public Guid Value { get; }

    private LearnerProgressId(Guid value) => Value = value;

    public static LearnerProgressId New() => new(Guid.CreateVersion7());

    public static LearnerProgressId From(Guid value) => new(Guard.NotEmpty(value, nameof(value)));

    public override string ToString() => Value.ToString();
}

public readonly record struct SpacedRepetitionRecordId
{
    public Guid Value { get; }

    private SpacedRepetitionRecordId(Guid value) => Value = value;

    public static SpacedRepetitionRecordId New() => new(Guid.CreateVersion7());

    public static SpacedRepetitionRecordId From(Guid value) => new(Guard.NotEmpty(value, nameof(value)));

    public override string ToString() => Value.ToString();
}

public readonly record struct LearnerGamificationId
{
    public Guid Value { get; }

    private LearnerGamificationId(Guid value) => Value = value;

    public static LearnerGamificationId New() => new(Guid.CreateVersion7());

    public static LearnerGamificationId From(Guid value) => new(Guard.NotEmpty(value, nameof(value)));

    public override string ToString() => Value.ToString();
}

public readonly record struct RequestId
{
    public Guid Value { get; }

    private RequestId(Guid value) => Value = value;

    public static RequestId New() => new(Guid.CreateVersion7());

    public static RequestId From(Guid value) => new(Guard.NotEmpty(value, nameof(value)));

    public override string ToString() => Value.ToString();
}

public readonly record struct SyncCheckpointId
{
    public Guid Value { get; }

    private SyncCheckpointId(Guid value) => Value = value;

    public static SyncCheckpointId New() => new(Guid.CreateVersion7());

    public static SyncCheckpointId From(Guid value) => new(Guard.NotEmpty(value, nameof(value)));

    public override string ToString() => Value.ToString();
}

public readonly record struct SyncPushReceiptId
{
    public Guid Value { get; }

    private SyncPushReceiptId(Guid value) => Value = value;

    public static SyncPushReceiptId New() => new(Guid.CreateVersion7());

    public static SyncPushReceiptId From(Guid value) => new(Guard.NotEmpty(value, nameof(value)));

    public override string ToString() => Value.ToString();
}
