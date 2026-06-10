using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketOurs.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPostCommentIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_posts_IsReview_CreatedAt",
                table: "posts",
                columns: new[] { "IsReview", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_comments_IsReview_CreatedAt",
                table: "comments",
                columns: new[] { "IsReview", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_posts_IsReview_CreatedAt",
                table: "posts");

            migrationBuilder.DropIndex(
                name: "IX_comments_IsReview_CreatedAt",
                table: "comments");
        }
    }
}
