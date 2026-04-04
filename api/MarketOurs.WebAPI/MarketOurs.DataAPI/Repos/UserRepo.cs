using MarketOurs.Data;
using MarketOurs.Data.DataModels;
using MarketOurs.DataAPI.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace MarketOurs.DataAPI.Repos;

public interface IUserRepo
{
    Task<List<UserModel>> GetAllAsync();
    Task<UserModel?> GetByIdAsync(string id);
    Task<UserModel?> GetByAccountAsync(string account);
    Task<List<UserModel>> GetByDateAsync(DateTime before, DateTime after);
    Task<List<PostModel>?> GetPostsAsync(string id);
    Task<List<CommentModel>?> GetCommentsAsync(string id);
    Task<List<PostModel>?> GetLikePostsAsync(string id);
    Task<List<CommentModel>?> GetLikeCommentsAsync(string id);
    Task<List<PostModel>?> GetDislikePostsAsync(string id);
    Task<List<CommentModel>?> GetDislikeCommentsAsync(string id);

    Task CreateAsync(UserModel user);
    Task UpdateAsync(UserModel user);
    Task DeleteAsync(string id);
}

public class UserRepo(IDbContextFactory<MarketContext> factory) : IUserRepo
{
    public async Task<List<UserModel>> GetAllAsync()
    {
        await using var context = await factory.CreateDbContextAsync();
        return await context.Users.ToListAsync();
    }

    public async Task<UserModel?> GetByIdAsync(string id)
    {
        await using var context = await factory.CreateDbContextAsync();
        return await context.Users
            .Where(x => x.Id == id)
            .FirstOrDefaultAsync();
    }

    public async Task<UserModel?> GetByAccountAsync(string account)
    {
        if (string.IsNullOrWhiteSpace(account)) return null;

        await using var context = await factory.CreateDbContextAsync();
        
        return await context.Users
            .Where(x => account.Contains('@') ? x.Email == account : x.Phone == account) // 如果含有 @ 的话就是 email
            .FirstOrDefaultAsync();
    }

    public async Task<List<UserModel>> GetByDateAsync(DateTime before, DateTime after)
    {
        await using var context = await factory.CreateDbContextAsync();
        return await context.Users
            .Where(x => x.CreatedAt >= before && x.CreatedAt <= after)
            .ToListAsync();
    }

    public async Task<List<PostModel>?> GetPostsAsync(string id)
    {
        await using var context = await factory.CreateDbContextAsync();
        return await context.Users
            .Include(x => x.Posts)
            .Where(x => x.Id == id)
            .Select(x => x.Posts)
            .FirstOrDefaultAsync();
    }

    public async Task<List<CommentModel>?> GetCommentsAsync(string id)
    {
        await using var context = await factory.CreateDbContextAsync();
        return await context.Users
            .Include(x => x.Comments)
            .Where(x => x.Id == id)
            .Select(x => x.Comments)
            .FirstOrDefaultAsync();
    }

    public async Task<List<PostModel>?> GetLikePostsAsync(string id)
    {
        await using var context = await factory.CreateDbContextAsync();
        return await context.Users
            .Include(x => x.LikePosts)
            .Where(x => x.Id == id)
            .Select(x => x.LikePosts)
            .FirstOrDefaultAsync();
    }

    public async Task<List<CommentModel>?> GetLikeCommentsAsync(string id)
    {
        await using var context = await factory.CreateDbContextAsync();
        return await context.Users
            .Include(x => x.LikeComments)
            .Where(x => x.Id == id)
            .Select(x => x.LikeComments)
            .FirstOrDefaultAsync();
    }

    public async Task<List<PostModel>?> GetDislikePostsAsync(string id)
    {
        await using var context = await factory.CreateDbContextAsync();
        return await context.Users
            .Include(x => x.DislikesPosts)
            .Where(x => x.Id == id)
            .Select(x => x.DislikesPosts)
            .FirstOrDefaultAsync();
    }

    public async Task<List<CommentModel>?> GetDislikeCommentsAsync(string id)
    {
        await using var context = await factory.CreateDbContextAsync();
        return await context.Users
            .Include(x => x.DislikesComments)
            .Where(x => x.Id == id)
            .Select(x => x.DislikesComments)
            .FirstOrDefaultAsync();
    }

    public async Task CreateAsync(UserModel user)
    {
        await using var context = await factory.CreateDbContextAsync();
        
        var account = string.IsNullOrEmpty(user.Email)  ? user.Phone : user.Email;

        if (await context.Users.AnyAsync(x => account.Contains('@') ? x.Email == account : x.Phone == account))
        {
            throw new AuthException(ErrorCode.ResourceAlreadyExists, "已有相关验证");
        }
        
        user.Id = user.GetHashKey();
        await context.Users.AddAsync(user);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(UserModel user)
    {
        await using var context = await factory.CreateDbContextAsync();
        context.Users.Update(user);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(string id)
    {
        await using var context = await factory.CreateDbContextAsync();
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user != null)
        {
            context.Users.Remove(user);
            await context.SaveChangesAsync();
        }
    }
}