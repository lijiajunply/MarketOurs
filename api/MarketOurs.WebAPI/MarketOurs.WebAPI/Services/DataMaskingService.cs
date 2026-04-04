using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using MarketOurs.DataAPI.Configs;

namespace MarketOurs.WebAPI.Services;

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

/// <summary>
/// 数据脱敏服务
/// </summary>
public class DataMaskingService(MaskingConfig config)
{
    /// <summary>
    /// 对对象进行脱敏处理
    /// </summary>
    /// <param name="obj">要脱敏的对象</param>
    /// <returns>脱敏后的对象</returns>
    public object MaskData(object? obj)
    {
        if (obj == null || !config.Enabled)
            return obj!;

        var type = obj.GetType();

        // 处理基本类型
        if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal) || type == typeof(DateTime))
        {
            return MaskPrimitive(obj);
        }

        // 处理集合类型
        if (obj is System.Collections.IEnumerable enumerable)
        {
            var list = new List<object>();
            foreach (var item in enumerable)
            {
                if (item != null)
                {
                    list.Add(MaskData(item));
                }
            }

            return list;
        }

        // 处理对象类型
        var maskedObj = Activator.CreateInstance(type);
        if (maskedObj == null)
        {
            return obj;
        }

        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            var value = property.GetValue(obj);
            if (value == null)
                continue;

            var maskedValue = MaskProperty(value, property);
            property.SetValue(maskedObj, maskedValue);
        }

        return maskedObj;
    }

    /// <summary>
    /// 对属性进行脱敏处理
    /// </summary>
    /// <param name="value">属性值</param>
    /// <param name="property">属性信息</param>
    /// <returns>脱敏后的属性值</returns>
    private object MaskProperty(object value, PropertyInfo property)
    {
        // 检查是否有脱敏特性
        var maskingAttr = property.GetCustomAttribute<MaskingAttribute>();
        if (maskingAttr != null)
        {
            return ApplyMasking(value.ToString(), maskingAttr.Type, maskingAttr.CustomRegex,
                maskingAttr.ReplacePattern);
        }

        // 检查属性名是否匹配默认规则
        var ruleKey = property.Name;
        if (config.Rules.TryGetValue(ruleKey, out var rule) && rule.Enabled)
        {
            return ApplyMasking(value.ToString(), rule.Type, rule.CustomRegex, rule.ReplacePattern);
        }

        // 递归处理复杂类型
        if (!property.PropertyType.IsPrimitive && property.PropertyType != typeof(string) &&
            property.PropertyType != typeof(decimal) && property.PropertyType != typeof(DateTime))
        {
            return MaskData(value);
        }

        return value;
    }

    /// <summary>
    /// 对基本类型进行脱敏处理
    /// </summary>
    /// <param name="value">基本类型值</param>
    /// <returns>脱敏后的基本类型值</returns>
    private object MaskPrimitive(object value)
    {
        if (value is string strValue)
        {
            // 自动检测并脱敏
            return AutoDetectAndMask(strValue);
        }

        return value;
    }

    /// <summary>
    /// 自动检测并脱敏
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <returns>脱敏后的字符串</returns>
    private string AutoDetectAndMask(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // 检测手机号
        if (Regex.IsMatch(input, @"1[3-9]\d{9}"))
        {
            return ApplyMasking(input, MaskingType.PhoneNumber);
        }

        // 检测身份证号
        if (Regex.IsMatch(input, @"[1-9]\d{5}(18|19|20)\d{2}(0[1-9]|1[0-2])(0[1-9]|[12]\d|3[01])\d{3}[\dXx]"))
        {
            return ApplyMasking(input, MaskingType.IdCard);
        }

        // 检测银行卡号
        if (Regex.IsMatch(input, @"\d{16,19}"))
        {
            return ApplyMasking(input, MaskingType.BankCard);
        }

        // 检测邮箱
        if (Regex.IsMatch(input, @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}"))
        {
            return ApplyMasking(input, MaskingType.Email);
        }

        return input;
    }

    /// <summary>
    /// 应用脱敏规则
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <param name="type">脱敏类型</param>
    /// <param name="customRegex">自定义正则表达式</param>
    /// <param name="replacePattern">替换模式</param>
    /// <returns>脱敏后的字符串</returns>
    public string ApplyMasking(string? input, MaskingType type, string? customRegex = null,
        string? replacePattern = null)
    {
        // 密码类型特殊处理，无论输入是否为空都返回******
        if (type == MaskingType.Password)
        {
            return "******";
        }

        if (string.IsNullOrEmpty(input))
            return input ?? string.Empty;

        switch (type)
        {
            case MaskingType.PhoneNumber:
                return Regex.Replace(input, @"1([3-9])\d{9}", match =>
                {
                    var fullMatch = match.Value;
                    return $"{fullMatch.Substring(0, 3)}****{fullMatch.Substring(7)}";
                });
            case MaskingType.IdCard:
                return Regex.Replace(input, @"([1-9]\d{5})(\d{8})(\d{2})([\dXx])",
                    match => $"{match.Groups[1].Value}***********{match.Groups[4].Value}");
            case MaskingType.BankCard:
                return Regex.Replace(input, @"(\d{4})(\d+)(\d{4})", "$1********$3");
            case MaskingType.Email:
                return Regex.Replace(input, @"([a-zA-Z0-9._%+-]{1,2})[a-zA-Z0-9._%+-]*(@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,})",
                    "$1****$2");
            case MaskingType.Name:
                if (input.Length == 1)
                    return input;
                return input[0] + new string('*', input.Length - 1);
            case MaskingType.Address:
                // 简单实现：保留前4个字符，其余替换为*（可根据实际需求优化）
                return input.Length <= 4 ? input : input.Substring(0, 4) + new string('*', input.Length - 4);
            case MaskingType.CustomRegex:
                if (!string.IsNullOrEmpty(customRegex) && !string.IsNullOrEmpty(replacePattern))
                {
                    return Regex.Replace(input, customRegex, replacePattern);
                }

                return input;
            default:
                return input;
        }
    }

    /// <summary>
    /// 对JSON字符串进行脱敏处理
    /// </summary>
    /// <param name="json">JSON字符串</param>
    /// <returns>脱敏后的JSON字符串</returns>
    public string MaskJson(string json)
    {
        if (string.IsNullOrEmpty(json) || !config.Enabled)
            return json;

        var doc = JsonDocument.Parse(json);
        var maskedDoc = MaskJsonElement(doc.RootElement);

        var serialized = JsonSerializer.Serialize(maskedDoc, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });

        return serialized;
    }

    /// <summary>
    /// 对JSON元素进行脱敏处理
    /// </summary>
    /// <param name="element">JSON元素</param>
    /// <returns>脱敏后的JSON元素</returns>
    private object? MaskJsonElement(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var obj = new Dictionary<string, object?>();
                foreach (var property in element.EnumerateObject())
                {
                    var maskedValue = MaskJsonElement(property.Value);
                    obj[property.Name] = maskedValue;
                }

                return obj;
            case JsonValueKind.Array:
                var array = element.EnumerateArray().Select(MaskJsonElement).ToList();
                return array;
            case JsonValueKind.String:
                var str = element.GetString()!;
                return AutoDetectAndMask(str);
            case JsonValueKind.Number:
                return element.GetDouble();
            case JsonValueKind.True:
                return true;
            case JsonValueKind.False:
                return false;
            case JsonValueKind.Null:
                return null;
            default:
                return element.ToString();
        }
    }
}