using MarketOurs.Data;
using MarketOurs.Data.DataModels;
using Microsoft.EntityFrameworkCore;

namespace MarketOurs.DataAPI.Repos;

public interface ISensitiveWordRepo
{
    /// <summary>
    /// 获取数据
    /// </summary>
    /// <returns></returns>
    Task<List<string>> GetEnabledWordsAsync();
    
    /// <summary>
    /// 如果为空的话进行保存
    /// </summary>
    /// <param name="words"></param>
    /// <param name="category"></param>
    /// <returns></returns>
    Task SeedIfEmptyAsync(IEnumerable<string> words, string category = "seed");
}

public class SensitiveWordRepo(IDbContextFactory<MarketContext> dbContextFactory) : ISensitiveWordRepo
{
    /// <inheritdoc/>
    public async Task<List<string>> GetEnabledWordsAsync()
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        return await context.SensitiveWords
            .Where(item => item.IsEnabled)
            .OrderBy(item => item.Word)
            .Select(item => item.Word)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task SeedIfEmptyAsync(IEnumerable<string> words, string category = "seed")
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        if (await context.SensitiveWords.AnyAsync())
        {
            return;
        }

        var now = DateTime.UtcNow;
        var entities = words
            .Select(word => word.Trim())
            .Where(word => !string.IsNullOrWhiteSpace(word))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(word => new SensitiveWordModel
            {
                Word = word,
                Category = category,
                IsEnabled = true,
                CreatedAt = now,
                UpdatedAt = now
            })
            .ToList();

        if (entities.Count == 0)
        {
            return;
        }

        await context.SensitiveWords.AddRangeAsync(entities);
        await context.SaveChangesAsync();
    }
}
