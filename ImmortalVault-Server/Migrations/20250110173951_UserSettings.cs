using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ImmortalVault_Server.Migrations
{
    /// <inheritdoc />
    public partial class UserSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UsersSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Language = table.Column<string>(type: "text", nullable: false),
                    Is12HoursFormat = table.Column<bool>(type: "boolean", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsersSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UsersSettings_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UsersSettings_UserId",
                table: "UsersSettings",
                column: "UserId",
                unique: true);
            
            migrationBuilder.Sql(@"
                INSERT INTO ""UsersSettings"" (""UserId"", ""Language"", ""Is12HoursFormat"")
                SELECT ""UserId"", ""Language"", FALSE
                FROM ""UsersLocalizations"";
            ");
            
            migrationBuilder.DropTable(
                name: "UsersLocalizations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UsersSettings");
            
            migrationBuilder.CreateTable(
                name: "UsersLocalizations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Language = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsersLocalizations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UsersLocalizations_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UsersLocalizations_UserId",
                table: "UsersLocalizations",
                column: "UserId",
                unique: true);
        }
    }
}
