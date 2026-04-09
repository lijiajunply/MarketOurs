using MarketOurs.DataAPI.Repos;
using Microsoft.Extensions.Caching.Memory;

namespace MarketOurs.DataAPI.Services;

public interface ISensitiveWordService
{
    Task<SensitiveWordFilter> GetFilterAsync();
}

public class SensitiveWordService(
    ISensitiveWordRepo sensitiveWordRepo,
    IMemoryCache memoryCache) : ISensitiveWordService
{
    private const string FilterCacheKey = "ai_sensitive_word_filter";
    private static readonly TimeSpan FilterCacheTtl = TimeSpan.FromMinutes(10);

    private static readonly string[] DefaultWords =
    [
        "色情", "约炮", "成人视频", "色情网", "色情网盘", "成人视频资源",
        "赌博", "赌局", "博彩", "彩票代投", "外围下注",
        "毒品", "冰毒", "大麻", "摇头丸", "违禁药",
        "枪支", "枪械", "弹药", "爆炸物", "炸药",
        "办证", "假证", "代开发票", "发票代开",
        "刷单", "刷信誉", "兼职刷单", "代考", "替考",
        "裸聊", "陪睡", "援交", "卖淫", "嫖娼",
        "自杀", "轻生", "炸学校", "砍人", "报复社会",
        "人肉搜索", "开盒", "网暴", "辱骂", "校园霸凌"
    ];

    public async Task<SensitiveWordFilter> GetFilterAsync()
    {
        if (memoryCache.TryGetValue<SensitiveWordFilter>(FilterCacheKey, out var filter) && filter != null)
        {
            return filter;
        }

        await sensitiveWordRepo.SeedIfEmptyAsync(DefaultWords);
        var words = await sensitiveWordRepo.GetEnabledWordsAsync();

        filter = new SensitiveWordFilter(words);
        memoryCache.Set(FilterCacheKey, filter, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = FilterCacheTtl,
            Size = 1
        });

        return filter;
    }
}
