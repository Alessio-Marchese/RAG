using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RAG.Migrations
{
    /// <inheritdoc />
    public partial class initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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

            migrationBuilder.CreateTable(
                name: "Files",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Size = table.Column<long>(type: "INTEGER", nullable: false),
                    UserConfigurationUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Files", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Files_UserConfigurations_UserConfigurationUserId",
                        column: x => x.UserConfigurationUserId,
                        principalTable: "UserConfigurations",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    UserConfigurationUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KnowledgeRules_UserConfigurations_UserConfigurationUserId",
                        column: x => x.UserConfigurationUserId,
                        principalTable: "UserConfigurations",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Files_UserConfigurationUserId",
                table: "Files",
                column: "UserConfigurationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Files_UserId",
                table: "Files",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeRules_UserConfigurationUserId",
                table: "KnowledgeRules",
                column: "UserConfigurationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeRules_UserId",
                table: "KnowledgeRules",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Files");

            migrationBuilder.DropTable(
                name: "KnowledgeRules");

            migrationBuilder.DropTable(
                name: "UserConfigurations");
        }
    }
}
