using MarketOurs.Data;
using MarketOurs.Data.DataModels;
using Microsoft.EntityFrameworkCore;

namespace MarketOurs.DataAPI.Repos;

public interface IPostRepo
{
    Task<List<PostModel>> GetAllAsync(int pageIndex, int pageSize);
    Task<int> CountAsync();
    Task<List<PostModel>> GetByUserIdAsync(string userId, int pageIndex, int pageSize);
    Task<int> CountByUserIdAsync(string userId);
    Task<List<PostModel>> GetHotAsync(int count);
    Task<PostModel?> GetByIdAsync(string id);
    Task<PostModel?> GetReviewedByIdAsync(string id);
    Task<List<PostModel>?> GetByDateAsync(DateTime before, DateTime after);
    Task<List<UserModel>?> GetLikeUsersAsync(string id);
    Task<List<UserModel>?> GetLikeUsersAsync(string id, DateTime before, DateTime after);
    Task<List<UserModel>?> GetDislikeUsersAsync(string id);
    Task<List<UserModel>?> GetDislikeUsersAsync(string id, DateTime before, DateTime after);
    Task<UserModel?> GetAuthorAsync(string id);
    Task<List<CommentModel>?> GetCommentsAsync(string id, string type);
    Task<List<PostModel>> SearchAsync(string keyword, int pageIndex, int pageSize);
    Task<int> SearchCountAsync(string keyword);

    Task CreateAsync(PostModel post);
    Task UpdateAsync(PostModel post);
    Task SetReviewStatusAsync(string id, bool isReview);
    Task SetTagAsync(string id, string? tagId);

    Task SetLikesAsync(UserModel user, string id);
    Task SetDislikesAsync(UserModel user, string id);
    Task IncrementWatchAsync(string id);
    Task AddWatchCountAsync(string id, int count);
    Task SetAuthorAsync(UserModel user, string id);

    Task DeleteAsync(string id);
    Task DeleteCommentAsync(string id, string commentId);
    Task DeleteLikesAsync(string id, string userId);
    Task DeleteDislikesAsync(string id, string userId);
}

public class PostRepo(IDbContextFactory<MarketContext> factory) : IPostRepo
{
    public async Task<List<PostModel>> GetAllAsync(int pageIndex, int pageSize)
    {
        await using var context = await factory.CreateDbContextAsync();
        return await context.Posts
            .AsNoTracking()
            .Include(x => x.User)
            .Include(x => x.Tag)
            .Where(x => x.IsReview)
            .OrderByDescending(x => x.CreatedAt)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> CountAsync()
    {
        await using var context = await factory.CreateDbContextAsync();
        return await context.Posts.CountAsync(x => x.IsReview);
    }

    public async Task<List<PostModel>> GetByUserIdAsync(string userId, int pageIndex, int pageSize)
    {
        await using var context = await factory.CreateDbContextAsync();
        return await context.Posts
            .AsNoTracking()
            .Include(x => x.User)
            .Include(x => x.Tag)
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> CountByUserIdAsync(string userId)
    {
        await using var context = await factory.CreateDbContextAsync();
        return await context.Posts.CountAsync(x => x.UserId == userId);
    }

    public async Task<List<PostModel>> GetHotAsync(int count)
    {
        await using var context = await factory.CreateDbContextAsync();
        return await context.Posts
            .AsNoTracking()
            .Include(x => x.User)
            .Include(x => x.Tag)
            .Where(x => x.IsReview)
            .OrderByDescending(x => x.Watch + (x.Likes * 3) - (x.Dislikes * 2))
            .Take(count)
            .ToListAsync();
    }

    public async Task<PostModel?> GetByIdAsync(string id)
    {
        await using var context = await factory.CreateDbContextAsync();
        return await context.Posts
            .Include(x => x.User)
            .Include(x => x.Tag)
            .Where(x => x.Id == id)
            .FirstOrDefaultAsync();
    }

    public async Task<PostModel?> GetReviewedByIdAsync(string id)
    {
        await using var context = await factory.CreateDbContextAsync();
        return await context.Posts
            .AsNoTracking()
            .Include(x => x.User)
            .Include(x => x.Tag)
            .Where(x => x.Id == id && x.IsReview)
            .FirstOrDefaultAsync();
    }

    public async Task<List<PostModel>?> GetByDateAsync(DateTime before, DateTime after)
    {
        await using var context = await factory.CreateDbContextAsync();
        return await context.Posts
            .Where(x => x.CreatedAt >= before && x.CreatedAt <= after)
            .ToListAsync();
    }

    public async Task<List<UserModel>?> GetLikeUsersAsync(string id)
    {
        await using var context = await factory.CreateDbContextAsync();
        return await context.Posts
            .AsNoTracking()
            .Include(x => x.LikeUsers)
            .Where(x => x.Id == id)
            .Select(x => x.LikeUsers)
            .FirstOrDefaultAsync();
    }

    public async Task<List<UserModel>?> GetLikeUsersAsync(string id, DateTime before, DateTime after)
    {
        await using var context = await factory.CreateDbContextAsync();
        return await context.Posts
            .Include(x => x.LikeUsers)
            .Where(x => x.Id == id && x.CreatedAt >= before && x.CreatedAt <= after)
            .Select(x => x.LikeUsers)
            .FirstOrDefaultAsync();
    }

    public async Task<List<UserModel>?> GetDislikeUsersAsync(string id)
    {
        await using var context = await factory.CreateDbContextAsync();
        return await context.Posts
            .AsNoTracking()
            .Include(x => x.DislikeUsers)
            .Where(x => x.Id == id)
            .Select(x => x.DislikeUsers)
            .FirstOrDefaultAsync();
    }

    public async Task<List<UserModel>?> GetDislikeUsersAsync(string id, DateTime before, DateTime after)
    {
        await using var context = await factory.CreateDbContextAsync();
        return await context.Posts
            .Include(x => x.DislikeUsers)
            .Where(x => x.Id == id && x.CreatedAt >= before && x.CreatedAt <= after)
            .Select(x => x.DislikeUsers)
            .FirstOrDefaultAsync();
    }

    public async Task<UserModel?> GetAuthorAsync(string id)
    {
        await using var context = await factory.CreateDbContextAsync();
        return await context.Posts
            .Include(x => x.User)
            .Where(x => x.Id == id)
            .Select(x => x.User)
            .FirstOrDefaultAsync();
    }

    public async Task<List<CommentModel>?> GetCommentsAsync(string id, string type)
    {
        await using var context = await factory.CreateDbContextAsync();

        // 无论何种排序，都先获取贴子的所有评论（包含子评论）
        // 在该方案中，我们选择获取属于该 PostId 的所有评论，让 Service 层负责构建树
        return await context.Commits
            .AsNoTracking()
            .Include(x => x.User)
            .Where(x => x.PostId == id)
            .ToListAsync();
    }

    public async Task<List<PostModel>> SearchAsync(string keyword, int pageIndex, int pageSize)
    {
        await using var context = await factory.CreateDbContextAsync();
        var offset = (pageIndex - 1) * pageSize;

        if (context.Database.IsNpgsql())
        {
            try
            {
                return await context.Posts
                    .FromSqlInterpolated($"""
                        SELECT *
                        FROM posts
                        WHERE "IsReview" AND ("Title" @@@ {keyword} OR "Content" @@@ {keyword})
                        ORDER BY pdb.score("Id") DESC, "CreatedAt" DESC
                        LIMIT {pageSize} OFFSET {offset}
                        """)
                    .AsNoTracking()
                    .Include(x => x.User)
                    .Include(x => x.Tag)
                    .ToListAsync();
            }
            catch
            {
                return await SearchWithILike(context, keyword, pageIndex, pageSize);
            }
        }

        return await context.Posts
            .Where(x => x.IsReview && (x.Title.Contains(keyword) || x.Content.Contains(keyword)))
            .Include(x => x.User)
            .Include(x => x.Tag)
            .OrderByDescending(x => x.CreatedAt)
            .Skip(offset)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> SearchCountAsync(string keyword)
    {
        await using var context = await factory.CreateDbContextAsync();

        if (context.Database.IsNpgsql())
        {
            try
            {
                return await context.Posts
                    .FromSqlInterpolated($"""
                        SELECT *
                        FROM posts
                        WHERE "IsReview" AND ("Title" @@@ {keyword} OR "Content" @@@ {keyword})
                        """)
                    .AsNoTracking()
                    .CountAsync();
            }
            catch
            {
                return await SearchCountWithILike(context, keyword);
            }
        }

        return await context.Posts
            .Where(x => x.IsReview && (x.Title.Contains(keyword) || x.Content.Contains(keyword)))
            .CountAsync();
    }

    private static Task<List<PostModel>> SearchWithILike(MarketContext context, string keyword, int pageIndex, int pageSize)
    {
        return context.Posts
            .AsNoTracking()
            .Where(x => x.IsReview && EF.Functions.ILike(x.Title + " " + x.Content, $"%{keyword}%"))
            .Include(x => x.User)
            .Include(x => x.Tag)
            .OrderByDescending(x => x.CreatedAt)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    private static Task<int> SearchCountWithILike(MarketContext context, string keyword)
    {
        return context.Posts
            .AsNoTracking()
            .Where(x => x.IsReview && EF.Functions.ILike(x.Title + " " + x.Content, $"%{keyword}%"))
            .CountAsync();
    }

    public async Task CreateAsync(PostModel post)
    {
        await using var context = await factory.CreateDbContextAsync();
        post.Id = post.GetHashKey();

        var entity = new PostModel
        {
            Id = post.Id,
            Title = post.Title,
            Content = post.Content,
            Images = post.Images,
            CreatedAt = post.CreatedAt,
            UpdatedAt = post.UpdatedAt,
            UserId = post.UserId,
            TagId = post.TagId,
            Likes = post.Likes,
            Dislikes = post.Dislikes,
            Watch = post.Watch,
            IsReview = post.IsReview
        };

        await context.Posts.AddAsync(entity);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(PostModel post)
    {
        await using var context = await factory.CreateDbContextAsync();
        var entity = await context.Posts.FirstOrDefaultAsync(x => x.Id == post.Id);
        if (entity == null)
        {
            return;
        }

        entity.Title = post.Title;
        entity.Content = post.Content;
        entity.Images = post.Images;
        entity.CreatedAt = post.CreatedAt;
        entity.UpdatedAt = post.UpdatedAt;
        entity.UserId = post.UserId;
        entity.TagId = post.TagId;
        entity.Likes = post.Likes;
        entity.Dislikes = post.Dislikes;
        entity.Watch = post.Watch;
        entity.IsReview = post.IsReview;
        await context.SaveChangesAsync();
    }

    public async Task SetReviewStatusAsync(string id, bool isReview)
    {
        await using var context = await factory.CreateDbContextAsync();
        var post = await context.Posts.FirstOrDefaultAsync(x => x.Id == id);
        if (post == null)
        {
            return;
        }

        post.IsReview = isReview;
        await context.SaveChangesAsync();
    }

    public async Task SetTagAsync(string id, string? tagId)
    {
        await using var context = await factory.CreateDbContextAsync();
        var post = await context.Posts.FirstOrDefaultAsync(x => x.Id == id);
        if (post == null)
        {
            return;
        }

        post.TagId = string.IsNullOrWhiteSpace(tagId) ? null : tagId;
        post.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
    }

    public async Task SetLikesAsync(UserModel user, string id)
    {
        await using var context = await factory.CreateDbContextAsync();
        var post = await context.Posts.Include(p => p.LikeUsers).FirstOrDefaultAsync(p => p.Id == id);
        if (post == null) return;

        var trackedUser = await context.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
        if (trackedUser == null) return;

        if (post.LikeUsers.All(u => u.Id != user.Id))
        {
            post.LikeUsers.Add(trackedUser);
            post.Likes++;
            await context.SaveChangesAsync();
        }
    }

    public async Task SetDislikesAsync(UserModel user, string id)
    {
        await using var context = await factory.CreateDbContextAsync();
        var post = await context.Posts.Include(p => p.DislikeUsers).FirstOrDefaultAsync(p => p.Id == id);
        if (post == null) return;

        var trackedUser = await context.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
        if (trackedUser == null) return;

        if (post.DislikeUsers.All(u => u.Id != user.Id))
        {
            post.DislikeUsers.Add(trackedUser);
            post.Dislikes++;
            await context.SaveChangesAsync();
        }
    }

    public async Task IncrementWatchAsync(string id)
    {
        await using var context = await factory.CreateDbContextAsync();
        var post = await context.Posts.FirstOrDefaultAsync(p => p.Id == id);
        if (post != null)
        {
            post.Watch++;
            await context.SaveChangesAsync();
        }
    }

    public async Task AddWatchCountAsync(string id, int count)
    {
        await using var context = await factory.CreateDbContextAsync();
        var post = await context.Posts.FirstOrDefaultAsync(p => p.Id == id);
        if (post != null)
        {
            post.Watch += count;
            await context.SaveChangesAsync();
        }
    }

    public async Task SetAuthorAsync(UserModel user, string id)
    {
        await using var context = await factory.CreateDbContextAsync();
        var post = await context.Posts.FirstOrDefaultAsync(p => p.Id == id);
        if (post == null) return;

        var trackedUser = await context.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
        if (trackedUser == null) return;

        post.User = trackedUser;
        post.UserId = trackedUser.Id;
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(string id)
    {
        await using var context = await factory.CreateDbContextAsync();
        var post = await context.Posts.FirstOrDefaultAsync(p => p.Id == id);
        if (post != null)
        {
            context.Posts.Remove(post);
            await context.SaveChangesAsync();
        }
    }

    public async Task DeleteCommentAsync(string id, string commentId)
    {
        await using var context = await factory.CreateDbContextAsync();
        var post = await context.Posts.Include(p => p.Comments).FirstOrDefaultAsync(p => p.Id == id);
        if (post != null)
        {
            var childComment = post.Comments.FirstOrDefault(c => c.Id == commentId);
            if (childComment != null)
            {
                post.Comments.Remove(childComment);
                context.Commits.Remove(childComment);
                await context.SaveChangesAsync();
            }
        }
    }

    public async Task DeleteLikesAsync(string id, string userId)
    {
        await using var context = await factory.CreateDbContextAsync();
        var post = await context.Posts.Include(p => p.LikeUsers).FirstOrDefaultAsync(p => p.Id == id);
        if (post != null)
        {
            var user = post.LikeUsers.FirstOrDefault(u => u.Id == userId);
            if (user != null)
            {
                post.LikeUsers.Remove(user);
                post.Likes--;
                await context.SaveChangesAsync();
            }
        }
    }

    public async Task DeleteDislikesAsync(string id, string userId)
    {
        await using var context = await factory.CreateDbContextAsync();
        var post = await context.Posts.Include(p => p.DislikeUsers).FirstOrDefaultAsync(p => p.Id == id);
        if (post != null)
        {
            var user = post.DislikeUsers.FirstOrDefault(u => u.Id == userId);
            if (user != null)
            {
                post.DislikeUsers.Remove(user);
                post.Dislikes--;
                await context.SaveChangesAsync();
            }
        }
    }
}
