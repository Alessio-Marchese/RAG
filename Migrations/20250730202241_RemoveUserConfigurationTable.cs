using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RAG.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUserConfigurationTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Files_UserConfigurations_UserId",
                table: "Files");

            migrationBuilder.DropForeignKey(
                name: "FK_KnowledgeRules_UserConfigurations_UserId",
                table: "KnowledgeRules");

            migrationBuilder.DropTable(
                name: "UserConfigurations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserConfigurations",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserConfigurations", x => x.UserId);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_Files_UserConfigurations_UserId",
                table: "Files",
                column: "UserId",
                principalTable: "UserConfigurations",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_KnowledgeRules_UserConfigurations_UserId",
                table: "KnowledgeRules",
                column: "UserId",
                principalTable: "UserConfigurations",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
