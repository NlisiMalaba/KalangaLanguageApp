using Kalanga.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kalanga.Infrastructure.Persistence.Configurations;

internal static class PersistenceModel
{
    public static void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new LanguageConfiguration());
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new RefreshTokenConfiguration());
        modelBuilder.ApplyConfiguration(new LessonConfiguration());
        modelBuilder.ApplyConfiguration(new PhraseConfiguration());
        modelBuilder.ApplyConfiguration(new LanguageVariationConfiguration());
        modelBuilder.ApplyConfiguration(new AudioRecordingConfiguration());
        modelBuilder.ApplyConfiguration(new ExerciseConfiguration());
        modelBuilder.ApplyConfiguration(new ContentPackConfiguration());
        modelBuilder.ApplyConfiguration(new ContentPackLessonConfiguration());
        modelBuilder.ApplyConfiguration(new LearnerProgressConfiguration());
        modelBuilder.ApplyConfiguration(new ExerciseResultConfiguration());
        modelBuilder.ApplyConfiguration(new SpacedRepetitionConfiguration());
        modelBuilder.ApplyConfiguration(new LearnerGamificationConfiguration());
        modelBuilder.ApplyConfiguration(new RequestConfiguration());
        modelBuilder.ApplyConfiguration(new RequestUpvoteConfiguration());
        modelBuilder.ApplyConfiguration(new SyncCheckpointConfiguration());
        modelBuilder.ApplyConfiguration(new NotificationOutboxConfiguration());
    }

    internal static void RestrictToLanguage<T>(EntityTypeBuilder<T> builder)
        where T : class
    {
        builder.HasOne<LanguageRecord>()
            .WithMany()
            .HasForeignKey("LanguageId")
            .OnDelete(DeleteBehavior.Restrict);
    }
}

file sealed class LanguageConfiguration : IEntityTypeConfiguration<LanguageRecord>
{
    public void Configure(EntityTypeBuilder<LanguageRecord> builder)
    {
        builder.ToTable("languages");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Code).HasMaxLength(10).IsRequired();
        builder.Property(e => e.Name).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Region).HasMaxLength(100).IsRequired();
        builder.HasIndex(e => e.Code).IsUnique();
    }
}

file sealed class UserConfiguration : IEntityTypeConfiguration<UserRecord>
{
    public void Configure(EntityTypeBuilder<UserRecord> builder)
    {
        builder.ToTable("users");
        builder.HasKey(e => e.Id);
        PersistenceModel.RestrictToLanguage(builder);
        builder.Property(e => e.Email).HasMaxLength(255).IsRequired();
        builder.Property(e => e.PasswordHash).HasMaxLength(255).IsRequired();
        builder.Property(e => e.DisplayName).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Role).HasMaxLength(32).IsRequired();
        builder.Property(e => e.Status).HasMaxLength(32).IsRequired();
        builder.HasIndex(e => e.Email).IsUnique();
    }
}

file sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshTokenRecord>
{
    public void Configure(EntityTypeBuilder<RefreshTokenRecord> builder)
    {
        builder.ToTable("refresh_tokens");
        builder.HasKey(e => e.Id);
        PersistenceModel.RestrictToLanguage(builder);
        builder.HasOne<UserRecord>()
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Property(e => e.TokenHash).HasMaxLength(255).IsRequired();
        builder.HasIndex(e => e.TokenHash).IsUnique();
    }
}

file sealed class LessonConfiguration : IEntityTypeConfiguration<LessonRecord>
{
    public void Configure(EntityTypeBuilder<LessonRecord> builder)
    {
        builder.ToTable("lessons");
        builder.HasKey(e => e.Id);
        PersistenceModel.RestrictToLanguage(builder);
        builder.Property(e => e.Title).HasMaxLength(255).IsRequired();
        builder.Property(e => e.Level).HasMaxLength(32).IsRequired();
        builder.Property(e => e.Category).HasMaxLength(100).IsRequired();
        builder.Property(e => e.IsScenario).HasDefaultValue(false);
        builder.Property(e => e.Status).HasMaxLength(32).IsRequired();
        builder.Property(e => e.XpReward).HasDefaultValue(10);
        builder.HasOne<UserRecord>()
            .WithMany()
            .HasForeignKey(e => e.ContributorId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserRecord>()
            .WithMany()
            .HasForeignKey(e => e.ReviewedBy)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(e => new { e.LanguageId, e.Level, e.Status })
            .HasDatabaseName("idx_lessons_language_level");
    }
}

file sealed class PhraseConfiguration : IEntityTypeConfiguration<PhraseRecord>
{
    public void Configure(EntityTypeBuilder<PhraseRecord> builder)
    {
        builder.ToTable("phrases");
        builder.HasKey(e => e.Id);
        PersistenceModel.RestrictToLanguage(builder);
        builder.Property(e => e.KalangaText).IsRequired();
        builder.Property(e => e.EnglishTranslation).IsRequired();
        builder.HasOne<LessonRecord>()
            .WithMany()
            .HasForeignKey(e => e.LessonId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(e => e.LessonId).HasDatabaseName("idx_phrases_lesson");
    }
}

file sealed class LanguageVariationConfiguration : IEntityTypeConfiguration<LanguageVariationRecord>
{
    public void Configure(EntityTypeBuilder<LanguageVariationRecord> builder)
    {
        builder.ToTable("language_variations");
        builder.HasKey(e => e.Id);
        PersistenceModel.RestrictToLanguage(builder);
        builder.Property(e => e.KalangaText).IsRequired();
        builder.Property(e => e.RegisterLabel).HasMaxLength(50).IsRequired();
        builder.HasOne<PhraseRecord>()
            .WithMany()
            .HasForeignKey(e => e.PhraseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

file sealed class AudioRecordingConfiguration : IEntityTypeConfiguration<AudioRecordingRecord>
{
    public void Configure(EntityTypeBuilder<AudioRecordingRecord> builder)
    {
        builder.ToTable("audio_recordings");
        builder.HasKey(e => e.Id);
        PersistenceModel.RestrictToLanguage(builder);
        builder.Property(e => e.CdnUrl).IsRequired();
        builder.Property(e => e.FileFormat).HasMaxLength(8).IsRequired();
        builder.Property(e => e.SpeakerGender).HasMaxLength(32).IsRequired();
        builder.Property(e => e.Status).HasMaxLength(32).IsRequired();
        builder.Property(e => e.DialectLabel).HasMaxLength(100);
        builder.HasOne<PhraseRecord>()
            .WithMany()
            .HasForeignKey(e => e.PhraseId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<LanguageVariationRecord>()
            .WithMany()
            .HasForeignKey(e => e.VariationId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.HasOne<UserRecord>()
            .WithMany()
            .HasForeignKey(e => e.ContributorId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(e => new { e.PhraseId, e.Status }).HasDatabaseName("idx_audio_phrase");
    }
}

file sealed class ExerciseConfiguration : IEntityTypeConfiguration<ExerciseRecord>
{
    public void Configure(EntityTypeBuilder<ExerciseRecord> builder)
    {
        builder.ToTable("exercises");
        builder.HasKey(e => e.Id);
        PersistenceModel.RestrictToLanguage(builder);
        builder.Property(e => e.ExerciseType).HasMaxLength(32).IsRequired();
        builder.Property(e => e.PromptData).HasColumnType("jsonb").IsRequired();
        builder.Property(e => e.CorrectAnswer).IsRequired();
        builder.HasOne<LessonRecord>()
            .WithMany()
            .HasForeignKey(e => e.LessonId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

file sealed class ContentPackConfiguration : IEntityTypeConfiguration<ContentPackRecord>
{
    public void Configure(EntityTypeBuilder<ContentPackRecord> builder)
    {
        builder.ToTable("content_packs");
        builder.HasKey(e => e.Id);
        PersistenceModel.RestrictToLanguage(builder);
        builder.Property(e => e.Name).HasMaxLength(255).IsRequired();
        builder.Property(e => e.Level).HasMaxLength(32);
        builder.Property(e => e.Category).HasMaxLength(100);
        builder.Property(e => e.Version).HasDefaultValue(1);
        builder.Property(e => e.ManifestUrl).IsRequired();
    }
}

file sealed class ContentPackLessonConfiguration : IEntityTypeConfiguration<ContentPackLessonRecord>
{
    public void Configure(EntityTypeBuilder<ContentPackLessonRecord> builder)
    {
        builder.ToTable("content_pack_lessons");
        builder.HasKey(e => new { e.PackId, e.LessonId });
        builder.HasOne<ContentPackRecord>()
            .WithMany()
            .HasForeignKey(e => e.PackId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<LessonRecord>()
            .WithMany()
            .HasForeignKey(e => e.LessonId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

file sealed class LearnerProgressConfiguration : IEntityTypeConfiguration<LearnerProgressRecord>
{
    public void Configure(EntityTypeBuilder<LearnerProgressRecord> builder)
    {
        builder.ToTable("learner_progress");
        builder.HasKey(e => e.Id);
        PersistenceModel.RestrictToLanguage(builder);
        builder.Property(e => e.XpAwarded).HasDefaultValue(0);
        builder.HasOne<UserRecord>()
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<LessonRecord>()
            .WithMany()
            .HasForeignKey(e => e.LessonId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(e => new { e.UserId, e.LessonId }).IsUnique();
        builder.HasIndex(e => new { e.UserId, e.LanguageId }).HasDatabaseName("idx_progress_user");
    }
}

file sealed class ExerciseResultConfiguration : IEntityTypeConfiguration<ExerciseResultRecord>
{
    public void Configure(EntityTypeBuilder<ExerciseResultRecord> builder)
    {
        builder.ToTable("exercise_results");
        builder.HasKey(e => e.Id);
        PersistenceModel.RestrictToLanguage(builder);
        builder.HasOne<UserRecord>()
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<ExerciseRecord>()
            .WithMany()
            .HasForeignKey(e => e.ExerciseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

file sealed class SpacedRepetitionConfiguration : IEntityTypeConfiguration<SpacedRepetitionRecordRow>
{
    public void Configure(EntityTypeBuilder<SpacedRepetitionRecordRow> builder)
    {
        builder.ToTable("spaced_repetition_records");
        builder.HasKey(e => e.Id);
        PersistenceModel.RestrictToLanguage(builder);
        builder.Property(e => e.EaseFactor).HasPrecision(4, 2).HasDefaultValue(2.5m);
        builder.Property(e => e.IntervalDays).HasDefaultValue(1);
        builder.Property(e => e.Repetitions).HasDefaultValue(0);
        builder.HasOne<UserRecord>()
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<PhraseRecord>()
            .WithMany()
            .HasForeignKey(e => e.PhraseId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<LanguageVariationRecord>()
            .WithMany()
            .HasForeignKey(e => e.VariationId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(e => new { e.UserId, e.NextReviewAt }).HasDatabaseName("idx_srs_user_next");
        builder.HasIndex(e => new { e.UserId, e.PhraseId })
            .IsUnique()
            .HasFilter("variation_id IS NULL")
            .HasDatabaseName("ux_srs_user_phrase_base");
        builder.HasIndex(e => new { e.UserId, e.PhraseId, e.VariationId })
            .IsUnique()
            .HasFilter("variation_id IS NOT NULL")
            .HasDatabaseName("ux_srs_user_phrase_variation");
    }
}

file sealed class LearnerGamificationConfiguration : IEntityTypeConfiguration<LearnerGamificationRecord>
{
    public void Configure(EntityTypeBuilder<LearnerGamificationRecord> builder)
    {
        builder.ToTable("learner_gamification");
        builder.HasKey(e => e.Id);
        PersistenceModel.RestrictToLanguage(builder);
        builder.Property(e => e.TotalXp).HasDefaultValue(0);
        builder.Property(e => e.CurrentStreak).HasDefaultValue(0);
        builder.Property(e => e.LongestStreak).HasDefaultValue(0);
        builder.Property(e => e.ProgressLevel).HasMaxLength(32).IsRequired();
        builder.HasOne<UserRecord>()
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(e => e.UserId).IsUnique();
    }
}

file sealed class RequestConfiguration : IEntityTypeConfiguration<RequestRecord>
{
    public void Configure(EntityTypeBuilder<RequestRecord> builder)
    {
        builder.ToTable("requests");
        builder.HasKey(e => e.Id);
        PersistenceModel.RestrictToLanguage(builder);
        builder.Property(e => e.Title).HasMaxLength(255).IsRequired();
        builder.Property(e => e.Description).IsRequired();
        builder.Property(e => e.UpvoteCount).HasDefaultValue(0);
        builder.Property(e => e.Status).HasMaxLength(32).IsRequired();
        builder.HasOne<UserRecord>()
            .WithMany()
            .HasForeignKey(e => e.SubmitterId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<LessonRecord>()
            .WithMany()
            .HasForeignKey(e => e.FulfilledByLessonId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(e => new { e.LanguageId, e.UpvoteCount })
            .IsDescending(false, true)
            .HasDatabaseName("idx_requests_language_upvotes");
    }
}

file sealed class RequestUpvoteConfiguration : IEntityTypeConfiguration<RequestUpvoteRecord>
{
    public void Configure(EntityTypeBuilder<RequestUpvoteRecord> builder)
    {
        builder.ToTable("request_upvotes");
        builder.HasKey(e => new { e.RequestId, e.UserId });
        builder.HasOne<RequestRecord>()
            .WithMany()
            .HasForeignKey(e => e.RequestId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<UserRecord>()
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

file sealed class SyncCheckpointConfiguration : IEntityTypeConfiguration<SyncCheckpointRecord>
{
    public void Configure(EntityTypeBuilder<SyncCheckpointRecord> builder)
    {
        builder.ToTable("sync_checkpoints");
        builder.HasKey(e => e.Id);
        PersistenceModel.RestrictToLanguage(builder);
        builder.Property(e => e.SyncVersion).HasDefaultValue(0L);
        builder.HasOne<UserRecord>()
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(e => new { e.UserId, e.LanguageId })
            .IsUnique()
            .HasDatabaseName("idx_sync_user");
    }
}

file sealed class NotificationOutboxConfiguration : IEntityTypeConfiguration<NotificationOutboxRecord>
{
    public void Configure(EntityTypeBuilder<NotificationOutboxRecord> builder)
    {
        builder.ToTable("notification_outbox");
        builder.HasKey(e => e.Id);
        PersistenceModel.RestrictToLanguage(builder);
        builder.Property(e => e.NotificationType).HasMaxLength(64).IsRequired();
        builder.Property(e => e.Payload).IsRequired();
        builder.HasOne<UserRecord>()
            .WithMany()
            .HasForeignKey(e => e.RecipientUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<LessonRecord>()
            .WithMany()
            .HasForeignKey(e => e.LessonId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(e => new { e.LanguageId, e.ProcessedAt })
            .HasDatabaseName("idx_notification_outbox_pending");
    }
}
