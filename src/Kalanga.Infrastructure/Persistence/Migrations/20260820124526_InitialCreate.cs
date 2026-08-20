using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kalanga.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "languages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    region = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_languages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "content_packs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    language_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    level = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    manifest_url = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_content_packs", x => x.id);
                    table.ForeignKey(
                        name: "fk_content_packs_languages_language_id",
                        column: x => x.language_id,
                        principalTable: "languages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    language_id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    display_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                    table.ForeignKey(
                        name: "fk_users_languages_language_id",
                        column: x => x.language_id,
                        principalTable: "languages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "learner_gamification",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    language_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    total_xp = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    current_streak = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    longest_streak = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    last_activity_date = table.Column<DateOnly>(type: "date", nullable: true),
                    progress_level = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_learner_gamification", x => x.id);
                    table.ForeignKey(
                        name: "fk_learner_gamification_languages_language_id",
                        column: x => x.language_id,
                        principalTable: "languages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_learner_gamification_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "lessons",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    language_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    level = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_scenario = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    scenario_context = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    contributor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reviewed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    review_feedback = table.Column<string>(type: "text", nullable: true),
                    xp_reward = table.Column<int>(type: "integer", nullable: false, defaultValue: 10),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lessons", x => x.id);
                    table.ForeignKey(
                        name: "fk_lessons_languages_language_id",
                        column: x => x.language_id,
                        principalTable: "languages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_lessons_users_contributor_id",
                        column: x => x.contributor_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_lessons_users_reviewed_by",
                        column: x => x.reviewed_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    language_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_refresh_tokens", x => x.id);
                    table.ForeignKey(
                        name: "fk_refresh_tokens_languages_language_id",
                        column: x => x.language_id,
                        principalTable: "languages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_refresh_tokens_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sync_checkpoints",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    language_id = table.Column<Guid>(type: "uuid", nullable: false),
                    last_synced_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    sync_version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sync_checkpoints", x => x.id);
                    table.ForeignKey(
                        name: "fk_sync_checkpoints_languages_language_id",
                        column: x => x.language_id,
                        principalTable: "languages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sync_checkpoints_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "content_pack_lessons",
                columns: table => new
                {
                    pack_id = table.Column<Guid>(type: "uuid", nullable: false),
                    lesson_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_content_pack_lessons", x => new { x.pack_id, x.lesson_id });
                    table.ForeignKey(
                        name: "fk_content_pack_lessons_content_packs_pack_id",
                        column: x => x.pack_id,
                        principalTable: "content_packs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_content_pack_lessons_lessons_lesson_id",
                        column: x => x.lesson_id,
                        principalTable: "lessons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "exercises",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    language_id = table.Column<Guid>(type: "uuid", nullable: false),
                    lesson_id = table.Column<Guid>(type: "uuid", nullable: false),
                    exercise_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    prompt_data = table.Column<string>(type: "jsonb", nullable: false),
                    correct_answer = table.Column<string>(type: "text", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_exercises", x => x.id);
                    table.ForeignKey(
                        name: "fk_exercises_languages_language_id",
                        column: x => x.language_id,
                        principalTable: "languages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_exercises_lessons_lesson_id",
                        column: x => x.lesson_id,
                        principalTable: "lessons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "learner_progress",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    language_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    lesson_id = table.Column<Guid>(type: "uuid", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    score = table.Column<int>(type: "integer", nullable: true),
                    xp_awarded = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_learner_progress", x => x.id);
                    table.ForeignKey(
                        name: "fk_learner_progress_languages_language_id",
                        column: x => x.language_id,
                        principalTable: "languages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_learner_progress_lessons_lesson_id",
                        column: x => x.lesson_id,
                        principalTable: "lessons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_learner_progress_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "phrases",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    language_id = table.Column<Guid>(type: "uuid", nullable: false),
                    lesson_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kalanga_text = table.Column<string>(type: "text", nullable: false),
                    english_translation = table.Column<string>(type: "text", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_phrases", x => x.id);
                    table.ForeignKey(
                        name: "fk_phrases_languages_language_id",
                        column: x => x.language_id,
                        principalTable: "languages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_phrases_lessons_lesson_id",
                        column: x => x.lesson_id,
                        principalTable: "lessons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    language_id = table.Column<Guid>(type: "uuid", nullable: false),
                    submitter_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    upvote_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    fulfilled_by_lesson_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_requests", x => x.id);
                    table.ForeignKey(
                        name: "fk_requests_languages_language_id",
                        column: x => x.language_id,
                        principalTable: "languages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_requests_lessons_fulfilled_by_lesson_id",
                        column: x => x.fulfilled_by_lesson_id,
                        principalTable: "lessons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_requests_users_submitter_id",
                        column: x => x.submitter_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "exercise_results",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    language_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    exercise_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_correct = table.Column<bool>(type: "boolean", nullable: false),
                    answered_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_exercise_results", x => x.id);
                    table.ForeignKey(
                        name: "fk_exercise_results_exercises_exercise_id",
                        column: x => x.exercise_id,
                        principalTable: "exercises",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_exercise_results_languages_language_id",
                        column: x => x.language_id,
                        principalTable: "languages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_exercise_results_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "language_variations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    language_id = table.Column<Guid>(type: "uuid", nullable: false),
                    phrase_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kalanga_text = table.Column<string>(type: "text", nullable: false),
                    register_label = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_language_variations", x => x.id);
                    table.ForeignKey(
                        name: "fk_language_variations_languages_language_id",
                        column: x => x.language_id,
                        principalTable: "languages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_language_variations_phrases_phrase_id",
                        column: x => x.phrase_id,
                        principalTable: "phrases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "request_upvotes",
                columns: table => new
                {
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_request_upvotes", x => new { x.request_id, x.user_id });
                    table.ForeignKey(
                        name: "fk_request_upvotes_requests_request_id",
                        column: x => x.request_id,
                        principalTable: "requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_request_upvotes_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "audio_recordings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    language_id = table.Column<Guid>(type: "uuid", nullable: false),
                    phrase_id = table.Column<Guid>(type: "uuid", nullable: true),
                    variation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    contributor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cdn_url = table.Column<string>(type: "text", nullable: false),
                    file_format = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    file_size_bytes = table.Column<int>(type: "integer", nullable: false),
                    speaker_gender = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    dialect_label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    duration_ms = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audio_recordings", x => x.id);
                    table.ForeignKey(
                        name: "fk_audio_recordings_language_variations_variation_id",
                        column: x => x.variation_id,
                        principalTable: "language_variations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_audio_recordings_languages_language_id",
                        column: x => x.language_id,
                        principalTable: "languages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_audio_recordings_phrases_phrase_id",
                        column: x => x.phrase_id,
                        principalTable: "phrases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_audio_recordings_users_contributor_id",
                        column: x => x.contributor_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "spaced_repetition_records",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    language_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    phrase_id = table.Column<Guid>(type: "uuid", nullable: false),
                    variation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ease_factor = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: false, defaultValue: 2.5m),
                    interval_days = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    repetitions = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    next_review_at = table.Column<DateOnly>(type: "date", nullable: false),
                    last_reviewed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_spaced_repetition_records", x => x.id);
                    table.ForeignKey(
                        name: "fk_spaced_repetition_records_language_variations_variation_id",
                        column: x => x.variation_id,
                        principalTable: "language_variations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_spaced_repetition_records_languages_language_id",
                        column: x => x.language_id,
                        principalTable: "languages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_spaced_repetition_records_phrases_phrase_id",
                        column: x => x.phrase_id,
                        principalTable: "phrases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_spaced_repetition_records_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_audio_phrase",
                table: "audio_recordings",
                columns: new[] { "phrase_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_audio_recordings_contributor_id",
                table: "audio_recordings",
                column: "contributor_id");

            migrationBuilder.CreateIndex(
                name: "ix_audio_recordings_language_id",
                table: "audio_recordings",
                column: "language_id");

            migrationBuilder.CreateIndex(
                name: "ix_audio_recordings_variation_id",
                table: "audio_recordings",
                column: "variation_id");

            migrationBuilder.CreateIndex(
                name: "ix_content_pack_lessons_lesson_id",
                table: "content_pack_lessons",
                column: "lesson_id");

            migrationBuilder.CreateIndex(
                name: "ix_content_packs_language_id",
                table: "content_packs",
                column: "language_id");

            migrationBuilder.CreateIndex(
                name: "ix_exercise_results_exercise_id",
                table: "exercise_results",
                column: "exercise_id");

            migrationBuilder.CreateIndex(
                name: "ix_exercise_results_language_id",
                table: "exercise_results",
                column: "language_id");

            migrationBuilder.CreateIndex(
                name: "ix_exercise_results_user_id",
                table: "exercise_results",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_exercises_language_id",
                table: "exercises",
                column: "language_id");

            migrationBuilder.CreateIndex(
                name: "ix_exercises_lesson_id",
                table: "exercises",
                column: "lesson_id");

            migrationBuilder.CreateIndex(
                name: "ix_language_variations_language_id",
                table: "language_variations",
                column: "language_id");

            migrationBuilder.CreateIndex(
                name: "ix_language_variations_phrase_id",
                table: "language_variations",
                column: "phrase_id");

            migrationBuilder.CreateIndex(
                name: "ix_languages_code",
                table: "languages",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_learner_gamification_language_id",
                table: "learner_gamification",
                column: "language_id");

            migrationBuilder.CreateIndex(
                name: "ix_learner_gamification_user_id",
                table: "learner_gamification",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_progress_user",
                table: "learner_progress",
                columns: new[] { "user_id", "language_id" });

            migrationBuilder.CreateIndex(
                name: "ix_learner_progress_language_id",
                table: "learner_progress",
                column: "language_id");

            migrationBuilder.CreateIndex(
                name: "ix_learner_progress_lesson_id",
                table: "learner_progress",
                column: "lesson_id");

            migrationBuilder.CreateIndex(
                name: "ix_learner_progress_user_id_lesson_id",
                table: "learner_progress",
                columns: new[] { "user_id", "lesson_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_lessons_language_level",
                table: "lessons",
                columns: new[] { "language_id", "level", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_lessons_contributor_id",
                table: "lessons",
                column: "contributor_id");

            migrationBuilder.CreateIndex(
                name: "ix_lessons_reviewed_by",
                table: "lessons",
                column: "reviewed_by");

            migrationBuilder.CreateIndex(
                name: "idx_phrases_lesson",
                table: "phrases",
                column: "lesson_id");

            migrationBuilder.CreateIndex(
                name: "ix_phrases_language_id",
                table: "phrases",
                column: "language_id");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_language_id",
                table: "refresh_tokens",
                column: "language_id");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_token_hash",
                table: "refresh_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_user_id",
                table: "refresh_tokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_request_upvotes_user_id",
                table: "request_upvotes",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "idx_requests_language_upvotes",
                table: "requests",
                columns: new[] { "language_id", "upvote_count" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_requests_fulfilled_by_lesson_id",
                table: "requests",
                column: "fulfilled_by_lesson_id");

            migrationBuilder.CreateIndex(
                name: "ix_requests_submitter_id",
                table: "requests",
                column: "submitter_id");

            migrationBuilder.CreateIndex(
                name: "idx_srs_user_next",
                table: "spaced_repetition_records",
                columns: new[] { "user_id", "next_review_at" });

            migrationBuilder.CreateIndex(
                name: "ix_spaced_repetition_records_language_id",
                table: "spaced_repetition_records",
                column: "language_id");

            migrationBuilder.CreateIndex(
                name: "ix_spaced_repetition_records_phrase_id",
                table: "spaced_repetition_records",
                column: "phrase_id");

            migrationBuilder.CreateIndex(
                name: "ix_spaced_repetition_records_variation_id",
                table: "spaced_repetition_records",
                column: "variation_id");

            migrationBuilder.CreateIndex(
                name: "ux_srs_user_phrase_base",
                table: "spaced_repetition_records",
                columns: new[] { "user_id", "phrase_id" },
                unique: true,
                filter: "variation_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_srs_user_phrase_variation",
                table: "spaced_repetition_records",
                columns: new[] { "user_id", "phrase_id", "variation_id" },
                unique: true,
                filter: "variation_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "idx_sync_user",
                table: "sync_checkpoints",
                columns: new[] { "user_id", "language_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sync_checkpoints_language_id",
                table: "sync_checkpoints",
                column: "language_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_language_id",
                table: "users",
                column: "language_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audio_recordings");

            migrationBuilder.DropTable(
                name: "content_pack_lessons");

            migrationBuilder.DropTable(
                name: "exercise_results");

            migrationBuilder.DropTable(
                name: "learner_gamification");

            migrationBuilder.DropTable(
                name: "learner_progress");

            migrationBuilder.DropTable(
                name: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "request_upvotes");

            migrationBuilder.DropTable(
                name: "spaced_repetition_records");

            migrationBuilder.DropTable(
                name: "sync_checkpoints");

            migrationBuilder.DropTable(
                name: "content_packs");

            migrationBuilder.DropTable(
                name: "exercises");

            migrationBuilder.DropTable(
                name: "requests");

            migrationBuilder.DropTable(
                name: "language_variations");

            migrationBuilder.DropTable(
                name: "phrases");

            migrationBuilder.DropTable(
                name: "lessons");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "languages");
        }
    }
}
