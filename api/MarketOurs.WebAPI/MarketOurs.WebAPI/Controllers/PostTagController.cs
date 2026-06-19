using MarketOurs.Data.DTOs;
using MarketOurs.DataAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MarketOurs.DataAPI.Exceptions;

namespace MarketOurs.WebAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class PostTagController(IPostTagService postTagService) : ControllerBase
{
    /// <summary>
    /// Tag name translations keyed by "tagId:languageCode"
    /// </summary>
    private static readonly Dictionary<string, string> TagTranslations = new()
    {
        // 二手闲置
        ["b7bf100e5bccd78b0f2194b0f6727e8f:en"] = "Second-hand",
        ["b7bf100e5bccd78b0f2194b0f6727e8f:ja"] = "中古品",
        ["b7bf100e5bccd78b0f2194b0f6727e8f:ko"] = "중고거래",
        ["b7bf100e5bccd78b0f2194b0f6727e8f:ru"] = "Б/у товары",
        ["b7bf100e5bccd78b0f2194b0f6727e8f:fr"] = "Occasion",
        ["b7bf100e5bccd78b0f2194b0f6727e8f:de"] = "Secondhand",

        // 八卦吐槽
        ["86edadb94b9cfb86fda26ee8c6630259:en"] = "Gossip",
        ["86edadb94b9cfb86fda26ee8c6630259:ja"] = "ゴシップ",
        ["86edadb94b9cfb86fda26ee8c6630259:ko"] = "잡담",
        ["86edadb94b9cfb86fda26ee8c6630259:ru"] = "Сплетни",
        ["86edadb94b9cfb86fda26ee8c6630259:fr"] = "Potins",
        ["86edadb94b9cfb86fda26ee8c6630259:de"] = "Tratsch",

        // 恋爱交友
        ["6198b329b39bccebed2210d415e5caed:en"] = "Dating",
        ["6198b329b39bccebed2210d415e5caed:ja"] = "恋愛",
        ["6198b329b39bccebed2210d415e5caed:ko"] = "연애",
        ["6198b329b39bccebed2210d415e5caed:ru"] = "Знакомства",
        ["6198b329b39bccebed2210d415e5caed:fr"] = "Rencontres",
        ["6198b329b39bccebed2210d415e5caed:de"] = "Dating",

        // 数码科技
        ["8ebbeab8447022bc8d171bd79c2f979c:en"] = "Tech",
        ["8ebbeab8447022bc8d171bd79c2f979c:ja"] = "デジタル",
        ["8ebbeab8447022bc8d171bd79c2f979c:ko"] = "디지털",
        ["8ebbeab8447022bc8d171bd79c2f979c:ru"] = "Техника",
        ["8ebbeab8447022bc8d171bd79c2f979c:fr"] = "Tech",
        ["8ebbeab8447022bc8d171bd79c2f979c:de"] = "Digital",

        // 校园生活
        ["5139bfdfb97db85c5b3ca0ad95474e54:en"] = "Campus Life",
        ["5139bfdfb97db85c5b3ca0ad95474e54:ja"] = "キャンパス",
        ["5139bfdfb97db85c5b3ca0ad95474e54:ko"] = "캠퍼스",
        ["5139bfdfb97db85c5b3ca0ad95474e54:ru"] = "Кампус",
        ["5139bfdfb97db85c5b3ca0ad95474e54:fr"] = "Vie du campus",
        ["5139bfdfb97db85c5b3ca0ad95474e54:de"] = "Campus",

        // 生活闲聊
        ["903808ba88059cd6a0827508aa9c0e88:en"] = "Lifestyle",
        ["903808ba88059cd6a0827508aa9c0e88:ja"] = "雑談",
        ["903808ba88059cd6a0827508aa9c0e88:ko"] = "일상",
        ["903808ba88059cd6a0827508aa9c0e88:ru"] = "Общение",
        ["903808ba88059cd6a0827508aa9c0e88:fr"] = "Discussion",
        ["903808ba88059cd6a0827508aa9c0e88:de"] = "Plaudern",
    };

    private string GetLanguageCode()
    {
        var lang = Request.Headers.AcceptLanguage.FirstOrDefault()?.Trim() ?? "zh";
        if (lang.StartsWith("zh")) return "zh";
        var code = lang.Split(',')[0].Split(';')[0].Split('-')[0].ToLower();
        return code switch
        {
            "en" => "en", "ja" => "ja", "ko" => "ko",
            "ru" => "ru", "fr" => "fr", "de" => "de",
            _ => "zh"
        };
    }

    private void LocalizeTags(List<PostTagDto> tags)
    {
        var lang = GetLanguageCode();
        if (lang == "zh") return;

        foreach (var tag in tags)
        {
            var key = $"{tag.Id}:{lang}";
            if (TagTranslations.TryGetValue(key, out var localized))
            {
                tag.Name = localized;
            }
        }
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ApiResponse<List<PostTagDto>>> GetActive()
    {
        var tags = await postTagService.GetActiveAsync();
        LocalizeTags(tags);
        return ApiResponse<List<PostTagDto>>.Success(tags, "获取成功");
    }

    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    public async Task<ApiResponse<List<PostTagDto>>> GetAll()
    {
        var tags = await postTagService.GetAllAsync();
        LocalizeTags(tags);
        return ApiResponse<List<PostTagDto>>.Success(tags, "获取成功");
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ApiResponse<PostTagDto>> GetById(string id)
    {
        var tag = await postTagService.GetByIdAsync(id);
        if (tag == null)
        {
            throw new ResourceAccessException(ErrorCode.InvalidStatusForOperation, "标签不存在", httpStatusCode: 404, resourceName: "PostTag", resourceId: id);
        }

        var key = $"{tag.Id}:{GetLanguageCode()}";
        if (TagTranslations.TryGetValue(key, out var localized))
        {
            tag.Name = localized;
        }
        return ApiResponse<PostTagDto>.Success(tag, "获取成功");
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ApiResponse<PostTagDto>> Create([FromBody] PostTagCreateDto request)
    {
        return ApiResponse<PostTagDto>.Success(await postTagService.CreateAsync(request), "创建成功");
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ApiResponse<PostTagDto>> Update(string id, [FromBody] PostTagUpdateDto request)
    {
        return ApiResponse<PostTagDto>.Success(await postTagService.UpdateAsync(id, request), "更新成功");
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ApiResponse<PostTagDto>> Deactivate(string id)
    {
        return ApiResponse<PostTagDto>.Success(await postTagService.DeactivateAsync(id), "停用成功");
    }
}
