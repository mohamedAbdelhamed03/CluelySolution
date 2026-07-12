using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cluely.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddContentPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DictionarySnapshots",
                columns: table => new
                {
                    DictionaryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Visibility = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    State = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", maxLength: 4096, nullable: false),
                    Language = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Region = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    TagsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CurrentVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CurrentVersionLabel = table.Column<int>(type: "int", nullable: true),
                    Version = table.Column<int>(type: "int", nullable: false),
                    IdempotencyKey = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SnapshotSchemaVersion = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    SerializedState = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DictionarySnapshots", x => x.DictionaryId);
                });

            migrationBuilder.CreateTable(
                name: "DictionaryShareGrants",
                columns: table => new
                {
                    DictionaryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GranteeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DictionaryShareGrants", x => new { x.DictionaryId, x.GranteeId });
                    table.ForeignKey(
                        name: "FK_DictionaryShareGrants_DictionarySnapshots_DictionaryId",
                        column: x => x.DictionaryId,
                        principalTable: "DictionarySnapshots",
                        principalColumn: "DictionaryId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DictionaryShareGrants_GranteeId",
                table: "DictionaryShareGrants",
                column: "GranteeId");

            migrationBuilder.CreateIndex(
                name: "IX_DictionarySnapshots_IdempotencyKey",
                table: "DictionarySnapshots",
                column: "IdempotencyKey",
                unique: true,
                filter: "[IdempotencyKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DictionarySnapshots_OwnerId",
                table: "DictionarySnapshots",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_DictionarySnapshots_Visibility",
                table: "DictionarySnapshots",
                column: "Visibility");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DictionaryShareGrants");

            migrationBuilder.DropTable(
                name: "DictionarySnapshots");
        }
    }
}
