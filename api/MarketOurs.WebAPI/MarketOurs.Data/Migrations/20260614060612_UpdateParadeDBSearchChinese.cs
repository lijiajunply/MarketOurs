using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketOurs.Data.Migrations
{
    /// <summary>
    /// Rebuild the ParadeDB BM25 index with Chinese tokenization and sortable/filterable fields.
    /// </summary>
    public partial class UpdateParadeDBSearchChinese : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder.ActiveProvider != "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                return;
            }

            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM pg_available_extensions WHERE name = 'paradedb') THEN
                        CREATE EXTENSION IF NOT EXISTS paradedb CASCADE;

                        DROP INDEX IF EXISTS posts_search_idx;

                        CREATE INDEX posts_search_idx
                        ON posts
                        USING bm25 (
                            "Id",
                            "Title" pdb.chinese_compatible,
                            "Content" pdb.chinese_compatible,
                            "IsReview",
                            "CreatedAt"
                        )
                        WITH (key_field = 'Id');
                    ELSE
                        RAISE NOTICE 'ParadeDB extension is not installed; skipping BM25 index rebuild.';
                    END IF;
                END
                $$;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder.ActiveProvider != "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                return;
            }

            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM pg_extension WHERE extname = 'paradedb') THEN
                        DROP INDEX IF EXISTS posts_search_idx;

                        CALL paradedb.create_bm25(
                            index_name => 'posts_search_idx',
                            table_name => 'posts',
                            columns => '{"Title": {}, "Content": {}}'
                        );
                    END IF;
                END
                $$;
                """);
        }
    }
}
