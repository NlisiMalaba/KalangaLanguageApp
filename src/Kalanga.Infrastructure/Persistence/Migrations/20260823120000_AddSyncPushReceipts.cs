using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kalanga.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSyncPushReceipts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sync_push_receipts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    language_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_operation_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sync_push_receipts", x => x.id);
                    table.ForeignKey(
                        name: "fk_sync_push_receipts_languages_language_id",
                        column: x => x.language_id,
                        principalTable: "languages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sync_push_receipts_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_sync_push_receipts_user_id",
                table: "sync_push_receipts",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ux_sync_push_receipts_client_operation",
                table: "sync_push_receipts",
                columns: new[] { "language_id", "user_id", "client_operation_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sync_push_receipts");
        }
    }
}
