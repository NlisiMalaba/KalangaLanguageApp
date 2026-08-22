using Kalanga.Infrastructure.Persistence.Configurations;
using Kalanga.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kalanga.Infrastructure.Persistence;

public sealed class KalangaDbContext(DbContextOptions<KalangaDbContext> options) : DbContext(options)
{
    public DbSet<LanguageRecord> Languages => Set<LanguageRecord>();
    public DbSet<UserRecord> Users => Set<UserRecord>();
    public DbSet<RefreshTokenRecord> RefreshTokens => Set<RefreshTokenRecord>();
    public DbSet<LessonRecord> Lessons => Set<LessonRecord>();
    public DbSet<PhraseRecord> Phrases => Set<PhraseRecord>();
    public DbSet<LanguageVariationRecord> LanguageVariations => Set<LanguageVariationRecord>();
    public DbSet<AudioRecordingRecord> AudioRecordings => Set<AudioRecordingRecord>();
    public DbSet<ExerciseRecord> Exercises => Set<ExerciseRecord>();
    public DbSet<ContentPackRecord> ContentPacks => Set<ContentPackRecord>();
    public DbSet<ContentPackLessonRecord> ContentPackLessons => Set<ContentPackLessonRecord>();
    public DbSet<LearnerProgressRecord> LearnerProgress => Set<LearnerProgressRecord>();
    public DbSet<ExerciseResultRecord> ExerciseResults => Set<ExerciseResultRecord>();
    public DbSet<SpacedRepetitionRecordRow> SpacedRepetitionRecords => Set<SpacedRepetitionRecordRow>();
    public DbSet<LearnerGamificationRecord> LearnerGamification => Set<LearnerGamificationRecord>();
    public DbSet<RequestRecord> Requests => Set<RequestRecord>();
    public DbSet<RequestUpvoteRecord> RequestUpvotes => Set<RequestUpvoteRecord>();
    public DbSet<SyncCheckpointRecord> SyncCheckpoints => Set<SyncCheckpointRecord>();
    public DbSet<NotificationOutboxRecord> NotificationOutbox => Set<NotificationOutboxRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        PersistenceModel.Configure(modelBuilder);
    }
}
