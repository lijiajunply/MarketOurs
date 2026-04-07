using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketOurs.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminSystemSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "system_settings",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SiteName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    AllowRegistration = table.Column<bool>(type: "boolean", nullable: false),
                    MaintenanceMode = table.Column<bool>(type: "boolean", nullable: false),
                    MaxPostImages = table.Column<int>(type: "integer", nullable: false),
                    AutoApprovePosts = table.Column<bool>(type: "boolean", nullable: false),
                    SupportEmail = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Announcement = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_system_settings", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "system_settings");
        }
    }
}
