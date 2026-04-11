using MarketOurs.DataAPI.Attributes;
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

        if (context.Result is ObjectResult { Value: not null } objectResult)
        {
            var maskingService = context.HttpContext.RequestServices.GetRequiredService<DataMaskingService>();
            objectResult.Value = maskingService.MaskData(objectResult.Value);
        }

        base.OnActionExecuted(context);
    }
}