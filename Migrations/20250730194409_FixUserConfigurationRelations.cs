using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RAG.Migrations
{
    /// <inheritdoc />
    public partial class FixUserConfigurationRelations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Files_UserConfigurations_UserConfigurationUserId",
                table: "Files");

            migrationBuilder.DropForeignKey(
                name: "FK_KnowledgeRules_UserConfigurations_UserConfigurationUserId",
                table: "KnowledgeRules");

            migrationBuilder.DropIndex(
                name: "IX_KnowledgeRules_UserConfigurationUserId",
                table: "KnowledgeRules");

            migrationBuilder.DropIndex(
                name: "IX_Files_UserConfigurationUserId",
                table: "Files");

            migrationBuilder.DropColumn(
                name: "UserConfigurationUserId",
                table: "KnowledgeRules");

            migrationBuilder.DropColumn(
                name: "UserConfigurationUserId",
                table: "Files");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Files_UserConfigurations_UserId",
                table: "Files");

            migrationBuilder.DropForeignKey(
                name: "FK_KnowledgeRules_UserConfigurations_UserId",
                table: "KnowledgeRules");

            migrationBuilder.AddColumn<Guid>(
                name: "UserConfigurationUserId",
                table: "KnowledgeRules",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserConfigurationUserId",
                table: "Files",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeRules_UserConfigurationUserId",
                table: "KnowledgeRules",
                column: "UserConfigurationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Files_UserConfigurationUserId",
                table: "Files",
                column: "UserConfigurationUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Files_UserConfigurations_UserConfigurationUserId",
                table: "Files",
                column: "UserConfigurationUserId",
                principalTable: "UserConfigurations",
                principalColumn: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_KnowledgeRules_UserConfigurations_UserConfigurationUserId",
                table: "KnowledgeRules",
                column: "UserConfigurationUserId",
                principalTable: "UserConfigurations",
                principalColumn: "UserId");
        }
    }
}
