using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using MarketOurs.Data.DataModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MarketOurs.Data;

public class MarketContext(DbContextOptions<MarketContext> options) : DbContext(options)
{
    public DbSet<UserModel> Users { get; set; }
    public DbSet<PostModel> Posts { get; set; }
    public DbSet<CommentModel> Commits { get; set; }
    public DbSet<NotificationModel> Notifications { get; set; }
    public DbSet<SystemSettingsModel> SystemSettings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserModel>()
            .HasMany(u => u.Posts)
            .WithOne(p => p.User)
            .HasForeignKey(p => p.UserId);
        
        modelBuilder.Entity<UserModel>()
            .HasMany(u => u.Comments)
            .WithOne(c => c.User)
            .HasForeignKey(c => c.UserId);

        modelBuilder.Entity<PostModel>()
            .HasMany(x => x.Comments)
            .WithOne(x => x.Post)
            .HasForeignKey(x => x.PostId);

        modelBuilder.Entity<PostModel>()
            .HasMany(x => x.LikeUsers)
            .WithMany(x => x.LikePosts);

        modelBuilder.Entity<PostModel>()
            .HasMany(x => x.DislikeUsers)
            .WithMany(x => x.DislikesPosts);

        modelBuilder.Entity<CommentModel>()
            .HasOne(x => x.User)
            .WithMany(x => x.Comments)
            .HasForeignKey(x => x.UserId);

        modelBuilder.Entity<CommentModel>()
            .HasMany(x => x.LikeUsers)
            .WithMany(x => x.LikeComments);

        modelBuilder.Entity<CommentModel>()
            .HasMany(x => x.DislikeUsers)
            .WithMany(x => x.DislikesComments);

        modelBuilder.Entity<CommentModel>()
            .HasMany(x => x.Comments)
            .WithOne(x => x.ParentComment)
            .HasForeignKey(x => x.ParentCommentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

[Serializable]
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<MarketContext>
{
    public MarketContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<MarketContext>();
        optionsBuilder.UseNpgsql("");
        return new MarketContext(optionsBuilder.Options);
    }
}

public static class DataTool
{
    /// <summary>
    /// 使用BCrypt进行密码加密。如果输入为 null，则视为内容为空字符串。
    /// </summary>
    /// <param name="s">密码</param>
    /// <returns>加密后的哈希值</returns>
    public static string StringToHash(this string s)
    {
        return BCrypt.Net.BCrypt.HashPassword(s ?? string.Empty, workFactor: 12); // 工作因子为 12 ，可自行调整
    }

    /// <summary>
    /// 检测密码是否匹配，兼容老的 MD5 加密 和 新的 BCrypt 加密
    /// </summary>
    /// <param name="password">密码原文</param>
    /// <param name="hashPassword">加密之后的密码，一般从数据库中提取出来</param>
    /// <returns></returns>
    public static bool IsOk(string password, string hashPassword)
    {
        if (hashPassword.Length > 32 || hashPassword.StartsWith("$2")) // 检测是否为 BCrypt 加密
        {
            return BCrypt.Net.BCrypt.Verify(password, hashPassword);
        }

        return ToMd5Hash(password) == hashPassword;
    }

    /// <summary>
    /// 使用 MD5 进行加密，多使用于一般的Id生成
    /// </summary>
    /// <param name="s"></param>
    /// <returns></returns>
    public static string ToMd5Hash(string s)
    {
        var data = Encoding.UTF8.GetBytes(s);
        var hash = MD5.HashData(data);
        var hashStringBuilder = new StringBuilder();
        foreach (var t in hash)
            hashStringBuilder.Append(t.ToString("x2"));
        return hashStringBuilder.ToString();
    }

    public static string GetProperties<T>(T t)
    {
        var builder = new StringBuilder();
        if (t == null) return builder.ToString();

        var properties = t.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);

        if (properties.Length <= 0) return builder.ToString();

        foreach (var item in properties)
        {
            var name = item.Name;
            var value = item.GetValue(t, null);
            if (item.PropertyType.IsValueType || item.PropertyType.Name.StartsWith("String"))
            {
                builder.Append($"{name}:{value ?? "null"},");
            }
        }

        return builder.ToString();
    }

    /// <summary>
    /// 检测是否为加密之后的数据
    /// </summary>
    /// <param name="modelPasswordHash"></param>
    /// <returns></returns>
    public static bool IsValidHash(string modelPasswordHash)
    {
        return modelPasswordHash.Length >= 32;
    }
}

public abstract class DataModel
{
    public override string ToString() => $"{GetType()} : {DataTool.GetProperties(this)}; Guid: {Guid.NewGuid():N}";

    /// <summary>
    /// 获取 Hash 字符串，通过 ToString
    /// </summary>
    /// <returns></returns>
    public string GetHashKey() => DataTool.ToMd5Hash(ToString());

    /// <summary>
    /// 更新数据
    /// </summary>
    public abstract void Update(DataModel model);
}
