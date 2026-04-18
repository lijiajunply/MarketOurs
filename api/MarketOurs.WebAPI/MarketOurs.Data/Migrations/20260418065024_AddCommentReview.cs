using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketOurs.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCommentReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsReview",
                table: "comments",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsReview",
                table: "comments");
        }
    }
}
