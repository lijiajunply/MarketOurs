using MarketOurs.DataAPI.Configs;

namespace MarketOurs.DataAPI.Attributes;

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