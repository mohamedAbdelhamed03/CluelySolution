using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cluely.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddContentCommandOutcomes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ContentCommandOutcomes",
                columns: table => new
                {
                    IdempotencyKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CommandName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DictionaryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionLabel = table.Column<int>(type: "int", nullable: false),
                    WordCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentCommandOutcomes", x => x.IdempotencyKey);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContentCommandOutcomes_DictionaryId_CommandName",
                table: "ContentCommandOutcomes",
                columns: new[] { "DictionaryId", "CommandName" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContentCommandOutcomes");
        }
    }
}
