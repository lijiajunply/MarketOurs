using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketOurs.Data.Migrations
{
    /// <summary>
    /// 初始化 ParadeDB 搜索引擎插件和 BM25 索引
    /// </summary>
    public partial class AddParadeDBSearch : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 仅在 PostgreSQL 数据库上执行
            if (migrationBuilder.ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                migrationBuilder.Sql("""
                    DO $$
                    BEGIN
                        IF EXISTS (SELECT 1 FROM pg_available_extensions WHERE name = 'paradedb') THEN
                            CREATE EXTENSION IF NOT EXISTS paradedb CASCADE;
                            PERFORM set_config('search_path', 'public, paradedb, pg_search', true);

                            -- posts 表已在此前迁移中创建；未安装 ParadeDB 时退回应用层 ILIKE 搜索。
                            CALL paradedb.create_bm25(
                                index_name => 'posts_search_idx',
                                table_name => 'posts',
                                columns => '{"title": {}, "content": {}}'
                            );
                        ELSE
                            RAISE NOTICE 'ParadeDB extension is not installed; skipping BM25 index creation.';
                        END IF;
                    END
                    $$;
                    """);
            }
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder.ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                migrationBuilder.Sql("""
                    DO $$
                    BEGIN
                        IF EXISTS (SELECT 1 FROM pg_extension WHERE extname = 'paradedb') THEN
                            CALL paradedb.drop_index(index_name => 'posts_search_idx');
                            DROP EXTENSION IF EXISTS paradedb CASCADE;
                        END IF;
                    END
                    $$;
                    """);
            }
        }
    }
}
