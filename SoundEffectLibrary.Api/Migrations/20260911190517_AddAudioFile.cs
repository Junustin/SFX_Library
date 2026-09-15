using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoundEffectLibrary.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAudioFile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "audiofiles",
                schema: "assets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    ContentType = table.Column<string>(type: "text", nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    StorageKey = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audiofiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_audiofiles_audioassets_AssetId",
                        column: x => x.AssetId,
                        principalSchema: "assets",
                        principalTable: "audioassets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_audiofiles_AssetId",
                schema: "assets",
                table: "audiofiles",
                column: "AssetId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audiofiles",
                schema: "assets");
        }
    }
}
