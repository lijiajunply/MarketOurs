using MarketOurs.DataAPI.Configs;
using MarketOurs.WebAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MarketOurs.WebAPI.Filters;

/// <summary>
/// 启用数据脱敏特性，用于控制器或操作方法。
/// 作为 ActionFilter，它可以精准地对 ObjectResult 中的对象进行属性级别的脱敏。
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class DataMaskingAttribute : ActionFilterAttribute
{
    /// <summary>
    /// 在操作执行后，对结果进行脱敏处理
    /// </summary>
    /// <param name="context">操作执行上下文</param>
    public override void OnActionExecuted(ActionExecutedContext context)
    {
        // 如果标记了忽略脱敏，则跳过
        var ignoreAttr = context.ActionDescriptor.EndpointMetadata.OfType<IgnoreDataMaskingAttribute>()
            .FirstOrDefault();
        if (ignoreAttr != null)
        {
            base.OnActionExecuted(context);
            return;
        }

        if (context.Result is ObjectResult objectResult && objectResult.Value != null)
        {
            var maskingService = context.HttpContext.RequestServices.GetRequiredService<DataMaskingService>();
            objectResult.Value = maskingService.MaskData(objectResult.Value);
        }

        base.OnActionExecuted(context);
    }
}

/// <summary>
/// 忽略数据脱敏特性，用于操作方法排除脱敏
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class IgnoreDataMaskingAttribute : Attribute;

/// <summary>
/// 数据脱敏特性，用于标记需要脱敏的属性
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class MaskingAttribute : Attribute
{
    /// <summary>
    /// 脱敏类型
    /// </summary>
    public MaskingType Type { get; set; }

    /// <summary>
    /// 自定义正则表达式（仅当Type为CustomRegex时使用）
    /// </summary>
    public string? CustomRegex { get; set; }

    /// <summary>
    /// 替换模式（仅当Type为CustomRegex时使用）
    /// </summary>
    public string? ReplacePattern { get; set; }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="type">脱敏类型</param>
    public MaskingAttribute(MaskingType type)
    {
        Type = type;
    }
}