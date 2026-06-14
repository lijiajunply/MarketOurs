using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketOurs.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPostTags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TagId",
                table: "posts",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "post_tags",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Color = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_post_tags", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_posts_TagId",
                table: "posts",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_post_tags_Name",
                table: "post_tags",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_posts_post_tags_TagId",
                table: "posts",
                column: "TagId",
                principalTable: "post_tags",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_posts_post_tags_TagId",
                table: "posts");

            migrationBuilder.DropTable(
                name: "post_tags");

            migrationBuilder.DropIndex(
                name: "IX_posts_TagId",
                table: "posts");

            migrationBuilder.DropColumn(
                name: "TagId",
                table: "posts");
        }
    }
}
