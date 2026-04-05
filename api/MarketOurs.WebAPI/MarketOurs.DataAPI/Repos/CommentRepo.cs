using MarketOurs.Data;
using MarketOurs.Data.DataModels;
using Microsoft.EntityFrameworkCore;

namespace MarketOurs.DataAPI.Repos;

public interface ICommentRepo
{
    public Task<List<CommentModel>> GetAllAsync(int pageIndex, int pageSize);
    public Task<int> CountAsync();
    public Task<List<CommentModel>> SearchAsync(string keyword, int pageIndex, int pageSize);
    public Task<int> SearchCountAsync(string keyword);
    public Task<CommentModel?> GetByIdAsync(string id);
    public Task<List<CommentModel>?> GetByDateAsync(DateTimeOffset before, DateTimeOffset after);
    public Task<List<UserModel>?> GetLikeUsersAsync(string id);
    public Task<List<UserModel>?> GetLikeUsersAsync(string id, DateTime before, DateTime after);
    public Task<List<UserModel>?> GetDislikeUsersAsync(string id);
    public Task<List<UserModel>?> GetDislikeUsersAsync(string id, DateTime before, DateTime after);
    public Task<UserModel?> GetAuthorAsync(string id);
    public Task<List<CommentModel>?> GetCommentsAsync(string id);

    public Task CreateAsync(CommentModel comment);

    public Task UpdateAsync(UserModel user);

    public Task UpdateAsync(CommentModel comment);

    public Task SetLikesAsync(UserModel user, string id);
    public Task SetDislikesAsync(UserModel user, string id);
    public Task SetComment(UserModel user, string id);

    public Task DeleteAsync(string id);
    public Task DeleteCommentAsync(string id, string commentId);
    public Task DeleteLikesAsync(string id, string userId);
    public Task DeleteDislikesAsync(string id, string userId);
}

public class CommentRepo(IDbContextFactory<MarketContext> factory) : ICommentRepo
{
    public async Task<List<CommentModel>> GetAllAsync(int pageIndex, int pageSize)
    {
        await using var context = await factory.CreateDbContextAsync();

        return await context.Commits
            .OrderByDescending(x => x.CreatedAt)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> CountAsync()
    {
        await using var context = await factory.CreateDbContextAsync();
        return await context.Commits.CountAsync();
    }

    public async Task<List<CommentModel>> SearchAsync(string keyword, int pageIndex, int pageSize)
    {
        await using var context = await factory.CreateDbContextAsync();
        
        // "Cascade" search in this context means searching through all comments 
        // regardless of their level in the hierarchy.
        return await context.Commits
            .Include(x => x.User)
            .Include(x => x.Post)
            .Where(x => x.Content.Contains(keyword))
            .OrderByDescending(x => x.CreatedAt)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> SearchCountAsync(string keyword)
    {
        await using var context = await factory.CreateDbContextAsync();
        return await context.Commits
            .Where(x => x.Content.Contains(keyword))
            .CountAsync();
    }

    public async Task<CommentModel?> GetByIdAsync(string id)
    {
        await using var context = await factory.CreateDbContextAsync();

        return await context.Commits
            .Include(x => x.User)
            .Where(x => x.Id == id)
            .FirstOrDefaultAsync();
    }

    public async Task<List<CommentModel>?> GetByDateAsync(DateTimeOffset before, DateTimeOffset after)
    {
        await using var context = await factory.CreateDbContextAsync();

        return await context.Commits.Where(x => x.CreatedAt >= before && x.CreatedAt <= after).ToListAsync();
    }

    public async Task<List<UserModel>?> GetLikeUsersAsync(string id)
    {
        await using var context = await factory.CreateDbContextAsync();

        return await context.Commits
            .Include(x => x.LikeUsers)
            .Where(x => x.Id == id)
            .Select(x => x.LikeUsers)
            .FirstOrDefaultAsync();
    }

    public async Task<List<UserModel>?> GetLikeUsersAsync(string id, DateTime before, DateTime after)
    {
        await using var context = await factory.CreateDbContextAsync();

        return await context.Commits
            .Include(x => x.LikeUsers)
            .Where(x => x.Id == id && x.CreatedAt >= before && x.CreatedAt <= after)
            .Select(x => x.LikeUsers)
            .FirstOrDefaultAsync();
    }

    public async Task<List<UserModel>?> GetDislikeUsersAsync(string id)
    {
        await using var context = await factory.CreateDbContextAsync();

        return await context.Commits
            .Include(x => x.DislikeUsers)
            .Where(x => x.Id == id)
            .Select(x => x.DislikeUsers)
            .FirstOrDefaultAsync();
    }

    public async Task<List<UserModel>?> GetDislikeUsersAsync(string id, DateTime before, DateTime after)
    {
        await using var context = await factory.CreateDbContextAsync();

        return await context.Commits
            .Include(x => x.DislikeUsers)
            .Where(x => x.Id == id && x.CreatedAt >= before && x.CreatedAt <= after)
            .Select(x => x.DislikeUsers)
            .FirstOrDefaultAsync();
    }

    public async Task<UserModel?> GetAuthorAsync(string id)
    {
        await using var context = await factory.CreateDbContextAsync();

        return await context.Commits
            .Include(x => x.User)
            .Where(x => x.Id == id)
            .Select(x => x.User)
            .FirstOrDefaultAsync();
    }

    public async Task<List<CommentModel>?> GetCommentsAsync(string id)
    {
        await using var context = await factory.CreateDbContextAsync();

        return await context.Commits
            .Include(x => x.Comments)
            .Where(x => x.Id == id)
            .Select(x => x.Comments)
            .FirstOrDefaultAsync();
    }

    public async Task CreateAsync(CommentModel comment)
    {
        await using var context = await factory.CreateDbContextAsync();
        comment.Id = comment.GetHashKey();
        context.Commits.Add(comment);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(UserModel user)
    {
        await using var context = await factory.CreateDbContextAsync();
        context.Users.Update(user);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(CommentModel comment)
    {
        await using var context = await factory.CreateDbContextAsync();
        context.Commits.Update(comment);
        await context.SaveChangesAsync();
    }

    public async Task SetLikesAsync(UserModel user, string id)
    {
        await using var context = await factory.CreateDbContextAsync();
        var comment = await context.Commits.Include(c => c.LikeUsers).FirstOrDefaultAsync(c => c.Id == id);
        if (comment == null) return;

        var trackedUser = await context.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
        if (trackedUser == null) return;

        if (comment.LikeUsers.All(u => u.Id != user.Id))
        {
            comment.LikeUsers.Add(trackedUser);
            comment.Likes++;
            await context.SaveChangesAsync();
        }
    }

    public async Task SetDislikesAsync(UserModel user, string id)
    {
        await using var context = await factory.CreateDbContextAsync();
        var comment = await context.Commits.Include(c => c.DislikeUsers).FirstOrDefaultAsync(c => c.Id == id);
        if (comment == null) return;

        var trackedUser = await context.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
        if (trackedUser == null) return;

        if (comment.DislikeUsers.All(u => u.Id != user.Id))
        {
            comment.DislikeUsers.Add(trackedUser);
            comment.Dislikes++;
            await context.SaveChangesAsync();
        }
    }

    public async Task SetComment(UserModel user, string id)
    {
        await using var context = await factory.CreateDbContextAsync();
        var comment = await context.Commits.FirstOrDefaultAsync(c => c.Id == id);
        if (comment == null) return;

        var trackedUser = await context.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
        if (trackedUser == null) return;

        comment.User = trackedUser;
        comment.UserId = trackedUser.Id;
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(string id)
    {
        await using var context = await factory.CreateDbContextAsync();
        var comment = await context.Commits.FirstOrDefaultAsync(c => c.Id == id);
        if (comment != null)
        {
            context.Commits.Remove(comment);
            await context.SaveChangesAsync();
        }
    }

    public async Task DeleteCommentAsync(string id, string commentId)
    {
        await using var context = await factory.CreateDbContextAsync();
        var comment = await context.Commits.Include(c => c.Comments).FirstOrDefaultAsync(c => c.Id == id);
        if (comment != null)
        {
            var childComment = comment.Comments.FirstOrDefault(c => c.Id == commentId);
            if (childComment != null)
            {
                comment.Comments.Remove(childComment);
                context.Commits.Remove(childComment);
                await context.SaveChangesAsync();
            }
        }
    }

    public async Task DeleteLikesAsync(string id, string userId)
    {
        await using var context = await factory.CreateDbContextAsync();
        var comment = await context.Commits.Include(c => c.LikeUsers).FirstOrDefaultAsync(c => c.Id == id);
        if (comment != null)
        {
            var user = comment.LikeUsers.FirstOrDefault(u => u.Id == userId);
            if (user != null)
            {
                comment.LikeUsers.Remove(user);
                comment.Likes--;
                await context.SaveChangesAsync();
            }
        }
    }

    public async Task DeleteDislikesAsync(string id, string userId)
    {
        await using var context = await factory.CreateDbContextAsync();
        var comment = await context.Commits.Include(c => c.DislikeUsers).FirstOrDefaultAsync(c => c.Id == id);
        if (comment != null)
        {
            var user = comment.DislikeUsers.FirstOrDefault(u => u.Id == userId);
            if (user != null)
            {
                comment.DislikeUsers.Remove(user);
                comment.Dislikes--;
                await context.SaveChangesAsync();
            }
        }
    }
}