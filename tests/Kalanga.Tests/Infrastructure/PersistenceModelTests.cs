using Kalanga.Infrastructure.Persistence;
using Kalanga.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Kalanga.Tests.Infrastructure;

public sealed class PersistenceModelTests
{
    [Fact]
    public void Model_includes_required_indexes_and_unique_constraints()
    {
        using var db = CreateModelContext();
        var model = db.Model;

        AssertIndex(model, typeof(LessonRecord), "idx_lessons_language_level");
        AssertIndex(model, typeof(PhraseRecord), "idx_phrases_lesson");
        AssertIndex(model, typeof(AudioRecordingRecord), "idx_audio_phrase");
        AssertIndex(model, typeof(LearnerProgressRecord), "idx_progress_user");
        AssertIndex(model, typeof(SpacedRepetitionRecordRow), "idx_srs_user_next");
        AssertIndex(model, typeof(RequestRecord), "idx_requests_language_upvotes");
        AssertIndex(model, typeof(SyncCheckpointRecord), "idx_sync_user");
        AssertIndex(model, typeof(SyncPushReceiptRecord), "ux_sync_push_receipts_client_operation");
        AssertIndex(model, typeof(NotificationOutboxRecord), "idx_notification_outbox_pending");

        var progress = model.FindEntityType(typeof(LearnerProgressRecord))!;
        Assert.Contains(
            progress.GetIndexes(),
            index => index.IsUnique
                     && index.Properties.Select(p => p.Name).SequenceEqual(["UserId", "LessonId"]));

        var upvotes = model.FindEntityType(typeof(RequestUpvoteRecord))!;
        Assert.Equal(["RequestId", "UserId"], upvotes.FindPrimaryKey()!.Properties.Select(p => p.Name));
    }

    private static KalangaDbContext CreateModelContext()
    {
        var options = new DbContextOptionsBuilder<KalangaDbContext>();
        KalangaDbContextOptions.Configure(
            options,
            "Host=localhost;Database=kalanga;Username=kalanga;Password=kalanga");
        return new KalangaDbContext(options.Options);
    }

    private static void AssertIndex(IModel model, Type entityType, string name)
    {
        var entity = model.FindEntityType(entityType);
        Assert.NotNull(entity);
        Assert.Contains(entity.GetIndexes(), index => index.GetDatabaseName() == name);
    }
}
