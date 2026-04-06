using Fluid;

namespace MarketOurs.DataAPI.Services;

public interface ITemplateService
{
    /// <summary>
    /// 渲染模板
    /// </summary>
    /// <param name="templateContent">模板内容</param>
    /// <param name="model">数据模型</param>
    /// <returns>渲染后的字符串</returns>
    Task<string> RenderAsync(string templateContent, object model);
}

public class FluidTemplateService : ITemplateService
{
    private static readonly FluidParser Parser = new();

    /// <inheritdoc/>
    public async Task<string> RenderAsync(string templateContent, object model)
    {
        if (!Parser.TryParse(templateContent, out var template, out var error))
        {
            throw new Exception($"Template parsing errors: {error}");
        }

        var context = new TemplateContext(model);
        return await template.RenderAsync(context);
    }
}