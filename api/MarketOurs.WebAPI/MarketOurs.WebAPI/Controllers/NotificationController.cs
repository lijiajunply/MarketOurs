using System.Text.Json;
using MarketOurs.Data.DataModels;
using MarketOurs.Data.DTOs;
using MarketOurs.DataAPI.Exceptions;
using MarketOurs.DataAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MarketOurs.WebAPI.Controllers;

/// <summary>
/// 通知控制器，提供用户通知列表查询、未读数统计、已读状态标记及推送设置管理功能
/// </summary>
[ApiController]
[Route("[controller]")]
[Authorize]
public class NotificationController(INotificationService notificationService) : ControllerBase
{
    /// <summary>
    /// Notification title translations. Key = Chinese title, Value = localized titles per language.
    /// </summary>
    private static readonly Dictionary<string, Dictionary<string, string>> TitleMap = new()
    {
        ["🔥 今日校园热榜"] = new Dictionary<string, string>
        {
            ["en"] = "🔥 Today's Hot List", ["ja"] = "🔥 今日のランキング", ["ko"] = "🔥 오늘의 인기", ["ru"] = "🔥 Горячее сегодня",
            ["fr"] = "🔥 Top du jour", ["de"] = "🔥 Heute top"
        },
        ["审核信息"] = new Dictionary<string, string>
        {
            ["en"] = "Review Result", ["ja"] = "審査結果", ["ko"] = "검토 결과", ["ru"] = "Результат проверки",
            ["fr"] = "Résultat de l'examen", ["de"] = "Prüfergebnis"
        },
        ["你的贴子收到了新评论"] = new Dictionary<string, string>
        {
            ["en"] = "New comment on your post", ["ja"] = "投稿に新しいコメント", ["ko"] = "게시물에 새 댓글",
            ["ru"] = "Новый комментарий к посту", ["fr"] = "Nouveau commentaire sur votre publication",
            ["de"] = "Neuer Kommentar zu deinem Beitrag"
        },
        ["你的帖子收到了新评论"] = new Dictionary<string, string>
        {
            ["en"] = "New comment on your post", ["ja"] = "投稿に新しいコメント", ["ko"] = "게시물에 새 댓글",
            ["ru"] = "Новый комментарий к посту", ["fr"] = "Nouveau commentaire sur votre publication",
            ["de"] = "Neuer Kommentar zu deinem Beitrag"
        },
        ["你的评论收到了回复"] = new Dictionary<string, string>
        {
            ["en"] = "Reply to your comment", ["ja"] = "コメントに返信がありました", ["ko"] = "댓글에 답글",
            ["ru"] = "Ответ на ваш комментарий", ["fr"] = "Réponse à votre commentaire",
            ["de"] = "Antwort auf deinen Kommentar"
        },
        ["系统通知"] = new Dictionary<string, string>
        {
            ["en"] = "System Notice", ["ja"] = "システム通知", ["ko"] = "시스템 알림", ["ru"] = "Системное уведомление",
            ["fr"] = "Notification système", ["de"] = "Systemmeldung"
        },
    };

    /// <summary>
    /// Content phrase translations (common notification body text).
    /// These are sentence fragments that get .Replace()'d in the content text.
    /// </summary>
    private static readonly Dictionary<string, Dictionary<string, string>> ContentMap = new()
    {
        // Hot list
        ["来看看大家都在聊什么："] = new Dictionary<string, string>
        {
            ["en"] = "See what everyone is talking about:", ["ja"] = "みんなが話題にしていること：", ["ko"] = "모두가 이야기하는 주제:",
            ["ru"] = "Смотрите, что все обсуждают:", ["fr"] = "Voyez ce dont tout le monde parle :",
            ["de"] = "Schau, worüber alle reden:"
        },
        // Review
        ["帖子审核通过"] = new Dictionary<string, string>
        {
            ["en"] = "Post approved", ["ja"] = "投稿が承認されました", ["ko"] = "게시물 승인됨", ["ru"] = "Пост одобрен",
            ["fr"] = "Publication approuvée", ["de"] = "Beitrag genehmigt"
        },
        ["帖子审核未通过"] = new Dictionary<string, string>
        {
            ["en"] = "Post rejected", ["ja"] = "投稿が却下されました", ["ko"] = "게시물 거부됨", ["ru"] = "Пост отклонён",
            ["fr"] = "Publication rejetée", ["de"] = "Beitrag abgelehnt"
        },
        ["评论审核通过"] = new Dictionary<string, string>
        {
            ["en"] = "Comment approved", ["ja"] = "コメントが承認されました", ["ko"] = "댓글 승인됨", ["ru"] = "Комментарий одобрен",
            ["fr"] = "Commentaire approuvé", ["de"] = "Kommentar genehmigt"
        },
        ["评论审核未通过"] = new Dictionary<string, string>
        {
            ["en"] = "Comment rejected", ["ja"] = "コメントが却下されました", ["ko"] = "댓글 거부됨", ["ru"] = "Комментарий отклонён",
            ["fr"] = "Commentaire rejeté", ["de"] = "Kommentar abgelehnt"
        },
        // Comments & replies (both 贴子 and 帖子 variants)
        ["评论了你的贴子:"] = new Dictionary<string, string>
        {
            ["en"] = "commented on your post:", ["ja"] = "があなたの投稿にコメントしました:", ["ko"] = "님이 게시물에 댓글:",
            ["ru"] = "прокомментировал(а) ваш пост:", ["fr"] = "a commenté votre publication:",
            ["de"] = "hat deinen Beitrag kommentiert:"
        },
        ["评论了你的帖子:"] = new Dictionary<string, string>
        {
            ["en"] = "commented on your post:", ["ja"] = "があなたの投稿にコメントしました:", ["ko"] = "님이 게시물에 댓글:",
            ["ru"] = "прокомментировал(а) ваш пост:", ["fr"] = "a commenté votre publication:",
            ["de"] = "hat deinen Beitrag kommentiert:"
        },
        ["回复了你:"] = new Dictionary<string, string>
        {
            ["en"] = "replied to you:", ["ja"] = "があなたに返信しました:", ["ko"] = "님이 답글:", ["ru"] = "ответил(а) вам:",
            ["fr"] = "vous a répondu:", ["de"] = "hat dir geantwortet:"
        },
        // Review format: "您的{a}: {name} 已通过" (both 帖子 and 贴子)
        ["您的帖子:"] = new Dictionary<string, string>
        {
            ["en"] = "Your post:", ["ja"] = "あなたの投稿:", ["ko"] = "게시물:", ["ru"] = "Ваш пост:",
            ["fr"] = "Votre publication:", ["de"] = "Dein Beitrag:"
        },
        ["您的贴子:"] = new Dictionary<string, string>
        {
            ["en"] = "Your post:", ["ja"] = "あなたの投稿:", ["ko"] = "게시물:", ["ru"] = "Ваш пост:",
            ["fr"] = "Votre publication:", ["de"] = "Dein Beitrag:"
        },
        ["您的评论:"] = new Dictionary<string, string>
        {
            ["en"] = "Your comment:", ["ja"] = "あなたのコメント:", ["ko"] = "댓글:", ["ru"] = "Ваш комментарий:",
            ["fr"] = "Votre commentaire:", ["de"] = "Dein Kommentar:"
        },
        ["您的"] = new Dictionary<string, string>
            { ["en"] = "Your", ["ja"] = "あなたの", ["ko"] = "귀하의", ["ru"] = "Ваш", ["fr"] = "Votre", ["de"] = "Dein" },
        ["已通过"] = new Dictionary<string, string>
        {
            ["en"] = " has been approved", ["ja"] = " が承認されました", ["ko"] = " 승인되었습니다", ["ru"] = " одобрен(а)",
            ["fr"] = " a été approuvé(e)", ["de"] = " wurde genehmigt"
        },
    };

    private string GetLanguageCode()
    {
        var lang = Request.Headers.AcceptLanguage.FirstOrDefault()?.Trim() ?? "zh";
        if (lang.StartsWith("zh")) return "zh";
        var code = lang.Split(',')[0].Split(';')[0].Split('-')[0].ToLower();
        return code is "en" or "ja" or "ko" or "ru" or "fr" or "de" ? code : "zh";
    }

    private static string LocalizeTitle(string chineseTitle, string lang)
    {
        if (lang == "zh") return chineseTitle;
        if (TitleMap.TryGetValue(chineseTitle, out var translations) &&
            translations.TryGetValue(lang, out var localized))
            return localized;
        return chineseTitle;
    }

    // private static readonly string[] LangCodes = ["en", "ja", "ko", "ru", "fr", "de"];

    private void LocalizeNotifications(List<NotificationDto> notifications)
    {
        var lang = GetLanguageCode();
        if (lang == "zh") return;

        foreach (var n in notifications)
        {
            n.Title = LocalizeTitle(n.Title, lang);

            // Translate notification content body
            if (n.Type == NotificationType.HotList)
            {
                LocalizeHotListContent(n, lang);
            }
            else
            {
                LocalizePlainContent(n, lang);
            }
        }
    }

    private static void LocalizeHotListContent(NotificationDto n, string lang)
    {
        var json = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(n.Content);
        if (json == null) return;
        if (!json.TryGetValue("header", out var headerEl)) return;
        var header = headerEl.GetString() ?? "";
        if (!ContentMap.TryGetValue("来看看大家都在聊什么：", out var trans) ||
            !trans.TryGetValue(lang, out var localized) || !header.Contains("来看看大家都在聊什么：")) return;
        json["header"] = JsonSerializer.SerializeToElement(localized);
        n.Content = JsonSerializer.Serialize(json);
    }

    private static void LocalizePlainContent(NotificationDto n, string lang)
    {
        if (string.IsNullOrEmpty(n.Content)) return;
        // Replace known Chinese content strings with translations
        foreach (var kv in ContentMap)
        {
            if (n.Content.Contains(kv.Key) && kv.Value.TryGetValue(lang, out var localized))
            {
                n.Content = n.Content.Replace(kv.Key, localized);
            }
        }
    }

    /// <summary>
    /// 获取当前登录用户的通知列表 (支持分页)
    /// </summary>
    /// <param name="params">分页参数</param>
    /// <returns>分页后的通知列表</returns>
    [HttpGet]
    public async Task<ApiResponse<PagedResultDto<NotificationDto>>> GetNotifications(
        [FromQuery] PaginationParams @params)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return ApiResponse<PagedResultDto<NotificationDto>>.Fail(ErrorCode.Unauthorized);

        var result = await notificationService.GetUserNotificationsAsync(userId, @params);
        LocalizeNotifications(result.Items);
        return ApiResponse<PagedResultDto<NotificationDto>>.Success(result, "获取通知成功");
    }

    /// <summary>
    /// 获取当前登录用户的未读通知总数
    /// </summary>
    /// <returns>未读通知数量</returns>
    [HttpGet("unread-count")]
    public async Task<ApiResponse<int>> GetUnreadCount()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return ApiResponse<int>.Fail(ErrorCode.Unauthorized);

        var count = await notificationService.GetUnreadCountAsync(userId);
        return ApiResponse<int>.Success(count, "获取未读数成功");
    }

    /// <summary>
    /// 将指定的通知标记为已读
    /// </summary>
    /// <param name="id">通知唯一标识</param>
    /// <returns>操作结果描述</returns>
    [HttpPost("{id}/read")]
    public async Task<ApiResponse> MarkAsRead(string id)
    {
        await notificationService.MarkAsReadAsync(id);
        return ApiResponse.Success("操作成功");
    }

    /// <summary>
    /// 将当前用户的所有通知一键标记为已读
    /// </summary>
    /// <returns>操作结果描述</returns>
    [HttpPost("read-all")]
    public async Task<ApiResponse> MarkAllAsRead()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return ApiResponse.Fail(ErrorCode.Unauthorized);

        await notificationService.MarkAllAsReadAsync(userId);
        return ApiResponse.Success("操作成功");
    }

    /// <summary>
    /// 获取当前用户的个性化推送设置 (如邮件通知、热门推送等)
    /// </summary>
    /// <returns>推送设置详情</returns>
    [HttpGet("settings")]
    public async Task<ApiResponse<PushSettingsDto>> GetSettings()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return ApiResponse<PushSettingsDto>.Fail(ErrorCode.Unauthorized);

        var settings = await notificationService.GetPushSettingsAsync(userId);
        return ApiResponse<PushSettingsDto>.Success(settings, "获取设置成功");
    }

    /// <summary>
    /// 更新当前用户的个性化推送设置
    /// </summary>
    /// <param name="settings">推送设置请求对象</param>
    /// <returns>操作结果描述</returns>
    [HttpPut("settings")]
    public async Task<ApiResponse> UpdateSettings([FromBody] PushSettingsDto settings)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return ApiResponse.Fail(ErrorCode.Unauthorized);

        await notificationService.UpdatePushSettingsAsync(userId, settings);
        return ApiResponse.Success("更新成功");
    }
}