using MarketOurs.Data;
using MarketOurs.Data.DataModels;
using Microsoft.EntityFrameworkCore;

namespace MarketOurs.DataAPI.Repos;

public interface ISensitiveWordRepo
{
    Task<List<string>> GetEnabledWordsAsync();
    Task SeedIfEmptyAsync(IEnumerable<string> words, string category = "seed");
}

public class SensitiveWordRepo(IDbContextFactory<MarketContext> dbContextFactory) : ISensitiveWordRepo
{
    public async Task<List<string>> GetEnabledWordsAsync()
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        return await context.SensitiveWords
            .Where(item => item.IsEnabled)
            .OrderBy(item => item.Word)
            .Select(item => item.Word)
            .ToListAsync();
    }

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
