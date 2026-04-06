namespace MarketOurs.DataAPI.Configs;

/// <summary>
/// 脱敏类型枚举
/// </summary>
public enum MaskingType
{
    /// <summary>
    /// 手机号脱敏（保留前3后4）
    /// </summary>
    PhoneNumber,

    /// <summary>
    /// 身份证号脱敏（保留前6后1）
    /// </summary>
    IdCard,

    /// <summary>
    /// 银行卡号脱敏（保留前4后4）
    /// </summary>
    BankCard,

    /// <summary>
    /// 邮箱脱敏（保留前2后@域名）
    /// </summary>
    Email,

    /// <summary>
    /// 姓名脱敏（保留姓）
    /// </summary>
    Name,

    /// <summary>
    /// 地址脱敏（保留省市区，隐藏详细地址）
    /// </summary>
    Address,

    /// <summary>
    /// 密码脱敏（全部替换为*）
    /// </summary>
    Password,

    /// <summary>
    /// 自定义正则脱敏
    /// </summary>
    CustomRegex
}

/// <summary>
/// 脱敏配置类
/// </summary>
[Serializable]
public class MaskingConfig
{
    /// <summary>
    /// 是否启用脱敏
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 脱敏规则映射
    /// </summary>
    public Dictionary<string, MaskingRule> Rules { get; set; } = new()
    {
        { "PhoneNumber", new MaskingRule { Type = MaskingType.PhoneNumber } },
        { "IdCard", new MaskingRule { Type = MaskingType.IdCard } },
        { "BankCard", new MaskingRule { Type = MaskingType.BankCard } },
        { "Email", new MaskingRule { Type = MaskingType.Email } },
        { "Name", new MaskingRule { Type = MaskingType.Name } },
        { "Address", new MaskingRule { Type = MaskingType.Address } },
        { "Password", new MaskingRule { Type = MaskingType.Password } }
    };
}

/// <summary>
/// 脱敏规则类
/// </summary>
[Serializable]
public class MaskingRule
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
    /// 是否启用
    /// </summary>
    public bool Enabled { get; set; } = true;
}
