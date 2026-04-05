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
                // 1. 启用 paradedb 扩展 (包含 pg_search)
                migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS paradedb CASCADE;");
                
                // 2. 设置搜索路径确保 pg_search 下的函数可见
                migrationBuilder.Sql("SET search_path TO public, paradedb, pg_search;");

                // 3. 创建针对 posts 表的 BM25 索引
                // 注意：posts 表必须在此迁移之前已经创建
                // 我们使用 JSON 风格的列定义，这是 ParadeDB 0.10+ 最稳定的方式
                migrationBuilder.Sql("CALL paradedb.create_bm25(index_name => 'posts_search_idx', table_name => 'posts', columns => '{\"title\": {}, \"content\": {}}');");
            }
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder.ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                migrationBuilder.Sql("CALL paradedb.drop_index(index_name => 'posts_search_idx');");
                migrationBuilder.Sql("DROP EXTENSION IF EXISTS paradedb CASCADE;");
            }
        }
    }
}
