using MarketOurs.Data;
using MarketOurs.Data.DataModels;
using Microsoft.EntityFrameworkCore;

namespace MarketOurs.DataAPI.Repos;

public interface IPostRepo
{
    Task<List<PostModel>> GetAllAsync();
    Task<List<PostModel>> GetHotAsync(int count);
    Task<PostModel?> GetByIdAsync(string id);
    Task<List<PostModel>?> GetByDateAsync(DateTime before, DateTime after);
    Task<List<UserModel>?> GetLikeUsersAsync(string id);
    Task<List<UserModel>?> GetLikeUsersAsync(string id, DateTime before, DateTime after);
    Task<List<UserModel>?> GetDislikeUsersAsync(string id);
    Task<List<UserModel>?> GetDislikeUsersAsync(string id, DateTime before, DateTime after);
    Task<UserModel?> GetAuthorAsync(string id);
    Task<List<CommentModel>?> GetCommentsAsync(string id, string type);

    Task CreateAsync(PostModel post);
    Task UpdateAsync(PostModel post);

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
    public async Task<List<PostModel>> GetAllAsync()
    {
        await using var context = await factory.CreateDbContextAsync();
        return await context.Posts.ToListAsync();
    }

    public async Task<List<PostModel>> GetHotAsync(int count)
    {
        await using var context = await factory.CreateDbContextAsync();
        return await context.Posts
            .Include(x => x.User)
            .OrderByDescending(x => x.Watch + (x.Likes * 3) - (x.Dislikes * 2))
            .Take(count)
            .ToListAsync();
    }

    public async Task<PostModel?> GetByIdAsync(string id)
    {
        await using var context = await factory.CreateDbContextAsync();
        return await context.Posts
            .Include(x => x.User)
            .Where(x => x.Id == id)
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

        if (type == "Like")
        {
            return await context.Posts
                .Include(x => x.Comments)
                .Where(x => x.Id == id)
                .OrderByDescending(x => x.Likes)
                .Select(x => x.Comments)
                .FirstOrDefaultAsync();
        }

        return await context.Posts
            .Include(x => x.Comments)
            .Where(x => x.Id == id)
            .OrderByDescending(x => x.UpdatedAt)
            .Select(x => x.Comments)
            .FirstOrDefaultAsync();
    }

    public async Task CreateAsync(PostModel post)
    {
        await using var context = await factory.CreateDbContextAsync();
        post.Id = post.GetHashKey();
        await context.Posts.AddAsync(post);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(PostModel post)
    {
        await using var context = await factory.CreateDbContextAsync();
        context.Posts.Update(post);
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