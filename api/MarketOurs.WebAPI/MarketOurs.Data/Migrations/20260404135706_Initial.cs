using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketOurs.Data.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Email = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Phone = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Password = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Role = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Avatar = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Info = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsEmailVerified = table.Column<bool>(type: "boolean", nullable: false),
                    IsPhoneVerified = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Posts",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Title = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Content = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    Images = table.Column<List<string>>(type: "text[]", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Likes = table.Column<int>(type: "integer", nullable: false),
                    Dislikes = table.Column<int>(type: "integer", nullable: false),
                    Watch = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Posts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Posts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Commits",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Content = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Images = table.Column<List<string>>(type: "text[]", nullable: false),
                    Likes = table.Column<int>(type: "integer", nullable: false),
                    Dislikes = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PostId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ParentCommentId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Commits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Commits_Commits_ParentCommentId",
                        column: x => x.ParentCommentId,
                        principalTable: "Commits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Commits_Posts_PostId",
                        column: x => x.PostId,
                        principalTable: "Posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Commits_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PostModelUserModel",
                columns: table => new
                {
                    LikePostsId = table.Column<string>(type: "character varying(64)", nullable: false),
                    LikeUsersId = table.Column<string>(type: "character varying(64)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostModelUserModel", x => new { x.LikePostsId, x.LikeUsersId });
                    table.ForeignKey(
                        name: "FK_PostModelUserModel_Posts_LikePostsId",
                        column: x => x.LikePostsId,
                        principalTable: "Posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PostModelUserModel_Users_LikeUsersId",
                        column: x => x.LikeUsersId,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PostModelUserModel1",
                columns: table => new
                {
                    DislikeUsersId = table.Column<string>(type: "character varying(64)", nullable: false),
                    DislikesPostsId = table.Column<string>(type: "character varying(64)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostModelUserModel1", x => new { x.DislikeUsersId, x.DislikesPostsId });
                    table.ForeignKey(
                        name: "FK_PostModelUserModel1_Posts_DislikesPostsId",
                        column: x => x.DislikesPostsId,
                        principalTable: "Posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PostModelUserModel1_Users_DislikeUsersId",
                        column: x => x.DislikeUsersId,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CommentModelUserModel",
                columns: table => new
                {
                    LikeCommentsId = table.Column<string>(type: "character varying(64)", nullable: false),
                    LikeUsersId = table.Column<string>(type: "character varying(64)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommentModelUserModel", x => new { x.LikeCommentsId, x.LikeUsersId });
                    table.ForeignKey(
                        name: "FK_CommentModelUserModel_Commits_LikeCommentsId",
                        column: x => x.LikeCommentsId,
                        principalTable: "Commits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CommentModelUserModel_Users_LikeUsersId",
                        column: x => x.LikeUsersId,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CommentModelUserModel1",
                columns: table => new
                {
                    DislikeUsersId = table.Column<string>(type: "character varying(64)", nullable: false),
                    DislikesCommentsId = table.Column<string>(type: "character varying(64)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommentModelUserModel1", x => new { x.DislikeUsersId, x.DislikesCommentsId });
                    table.ForeignKey(
                        name: "FK_CommentModelUserModel1_Commits_DislikesCommentsId",
                        column: x => x.DislikesCommentsId,
                        principalTable: "Commits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CommentModelUserModel1_Users_DislikeUsersId",
                        column: x => x.DislikeUsersId,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CommentModelUserModel_LikeUsersId",
                table: "CommentModelUserModel",
                column: "LikeUsersId");

            migrationBuilder.CreateIndex(
                name: "IX_CommentModelUserModel1_DislikesCommentsId",
                table: "CommentModelUserModel1",
                column: "DislikesCommentsId");

            migrationBuilder.CreateIndex(
                name: "IX_Commits_ParentCommentId",
                table: "Commits",
                column: "ParentCommentId");

            migrationBuilder.CreateIndex(
                name: "IX_Commits_PostId",
                table: "Commits",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "IX_Commits_UserId",
                table: "Commits",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PostModelUserModel_LikeUsersId",
                table: "PostModelUserModel",
                column: "LikeUsersId");

            migrationBuilder.CreateIndex(
                name: "IX_PostModelUserModel1_DislikesPostsId",
                table: "PostModelUserModel1",
                column: "DislikesPostsId");

            migrationBuilder.CreateIndex(
                name: "IX_Posts_UserId",
                table: "Posts",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CommentModelUserModel");

            migrationBuilder.DropTable(
                name: "CommentModelUserModel1");

            migrationBuilder.DropTable(
                name: "PostModelUserModel");

            migrationBuilder.DropTable(
                name: "PostModelUserModel1");

            migrationBuilder.DropTable(
                name: "Commits");

            migrationBuilder.DropTable(
                name: "Posts");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
