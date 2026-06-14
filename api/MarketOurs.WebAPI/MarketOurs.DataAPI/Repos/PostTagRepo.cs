using MarketOurs.Data;
using MarketOurs.Data.DataModels;
using Microsoft.EntityFrameworkCore;

namespace MarketOurs.DataAPI.Repos;

public interface IPostTagRepo
{
    Task<List<PostTagModel>> GetActiveAsync();
    Task<List<PostTagModel>> GetAllAsync();
    Task<PostTagModel?> GetByIdAsync(string id);
    Task<PostTagModel?> GetByNameAsync(string name);
    Task CreateAsync(PostTagModel tag);
    Task UpdateAsync(PostTagModel tag);
}

public class PostTagRepo(IDbContextFactory<MarketContext> factory) : IPostTagRepo
{
    public async Task<List<PostTagModel>> GetActiveAsync()
    {
        await using var context = await factory.CreateDbContextAsync();
        return await context.PostTags
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync();
    }

    public async Task<List<PostTagModel>> GetAllAsync()
    {
        await using var context = await factory.CreateDbContextAsync();
        return await context.PostTags
            .AsNoTracking()
            .OrderByDescending(x => x.IsActive)
            .ThenBy(x => x.Name)
            .ToListAsync();
    }

    public async Task<PostTagModel?> GetByIdAsync(string id)
    {
        await using var context = await factory.CreateDbContextAsync();
        return await context.PostTags
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<PostTagModel?> GetByNameAsync(string name)
    {
        await using var context = await factory.CreateDbContextAsync();
        return await context.PostTags
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Name == name);
    }

    public async Task CreateAsync(PostTagModel tag)
    {
        await using var context = await factory.CreateDbContextAsync();
        tag.Id = tag.GetHashKey();
        await context.PostTags.AddAsync(tag);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(PostTagModel tag)
    {
        await using var context = await factory.CreateDbContextAsync();
        context.PostTags.Update(tag);
        await context.SaveChangesAsync();
    }
}
