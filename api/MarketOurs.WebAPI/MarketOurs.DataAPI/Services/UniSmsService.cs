using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;
using MarketOurs.DataAPI.Configs;

namespace MarketOurs.DataAPI.Services;

public interface ISmsService
{
    /// <summary>
    /// 发送请求
    /// </summary>
    /// <param name="action">方法</param>
    /// <param name="data">数据</param>
    /// <returns>返回 UniResponse </returns>
    public Task<SmsResponse> RequestAsync(string action, SmsModel data);
}

public class UniSmsService(
    SmsConfig config) : ISmsService
{
    private const string Name = "uni-csharp-sdk";
    private const string Version = "1.0.0"; // 对应 Python 的 __version__

    private readonly string _hmacAlgorithm = config.SigningAlgorithm.Split('-')[1].ToUpper();
    private static readonly HttpClient HttpClient = new();

    /// <summary>
    /// 身份验证
    /// </summary>
    /// <param name="query"></param>
    /// <exception cref="NotSupportedException"></exception>
    private void Sign(ref Dictionary<string, string> query)
    {
        query["algorithm"] = config.SigningAlgorithm;
        query["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

        // 生成 8 字节随机 nonce 并转为 hex
        var nonceBytes = new byte[8];
        RandomNumberGenerator.Fill(nonceBytes);
        query["nonce"] = Convert.ToHexStringLower(nonceBytes);

        // 1. 按 Key 升序排序
        var sortedQuery = query.OrderBy(x => x.Key, StringComparer.Ordinal);

        // 2. 构造 URL 编码字符串 (QueryString)
        var strToSign = string.Join("&", sortedQuery.Select(p =>
            $"{HttpUtility.UrlEncode(p.Key)}={HttpUtility.UrlEncode(p.Value)}"));

        // 3. HMAC 签名
        var keyBytes = Encoding.UTF8.GetBytes(config.AccessKeySecret);
        var messageBytes = Encoding.UTF8.GetBytes(strToSign);

        var hashBytes = _hmacAlgorithm switch
        {
            "SHA1" => HMACSHA1.HashData(keyBytes, messageBytes),
            "SHA256" => HMACSHA256.HashData(keyBytes, messageBytes),
            "SHA384" => HMACSHA384.HashData(keyBytes, messageBytes),
            "SHA512" => HMACSHA512.HashData(keyBytes, messageBytes),
            _ => throw new NotSupportedException($"Unsupported HMAC algorithm: {_hmacAlgorithm}")
        };
        query["signature"] = Convert.ToHexString(hashBytes).ToLower();
    }
    
    /// <inheritdoc/>
    public async Task<SmsResponse> RequestAsync(string action, SmsModel data)
    {
        if (data is not UniSmsModel uniSmsModel)
        {
            throw new Exception("Invalid data");
        }

        var query = new Dictionary<string, string>
        {
            { "action", action },
            { "accessKeyId", config.AccessKeyId }
        };

        Sign(ref query);

        // 构造 Query String
        var queryString = string.Join("&", query.Select(p =>
            $"{HttpUtility.UrlEncode(p.Key)}={HttpUtility.UrlEncode(p.Value)}"));

        var url = $"https://{config.Endpoint}/?{queryString}";
        var jsonBody = JsonSerializer.Serialize(uniSmsModel);

        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
        };

        request.Headers.Add("User-Agent", $"{Name}/{Version}");
        request.Headers.Add("Accept", "application/json");

        var response = await HttpClient.SendAsync(request);
        return await UniResponse.CreateAsync(response);
    }
}

public class UniException(string message, string code, string? requestId = null) : Exception(message)
{
    public string Code { get; } = code;
    public string? RequestId { get; } = requestId;

    public override string ToString()
    {
        return $"[{Code}] {Message}";
    }
}

public class SmsResponse;

public class SmsModel;

public class UniResponse : SmsResponse
{
    private const string RequestIdHeaderKey = "x-uni-request-id";
    public string Code { get; private set; } = "";
    public string Message { get; private set; } = "";
    public JsonElement Data { get; private set; }
    public string RequestId { get; private set; } = "";
    public HttpResponseMessage Raw { get; private set; } = new();

    public static async Task<UniResponse> CreateAsync(HttpResponseMessage res)
    {
        var response = new UniResponse { Raw = res };

        // 获取 Request ID
        if (res.Headers.TryGetValues(RequestIdHeaderKey, out var values))
        {
            response.RequestId = string.Join(",", values);
        }

        var rawBody = await res.Content.ReadAsStringAsync();

        try
        {
            using var doc = JsonDocument.Parse(rawBody);
            var root = doc.RootElement.Clone(); // 克隆以脱离 doc 生命周期

            if (root.TryGetProperty("code", out var codeProp))
            {
                var code = codeProp.GetString() ?? "";
                var message = root.GetProperty("message").GetString() ?? "";

                if (code != "0")
                {
                    throw new UniException(message, code, response.RequestId);
                }

                response.Code = code;
                response.Message = message;
                response.Data = root.GetProperty("data").Clone();
            }
            else
            {
                if (res.ReasonPhrase != null) throw new UniException(res.ReasonPhrase, "-1", response.RequestId);
            }
        }
        catch (JsonException)
        {
            if (res.ReasonPhrase != null) throw new UniException(res.ReasonPhrase, "-1", response.RequestId);
        }

        return response;
    }
}

[Serializable]
public class UniSmsModel : SmsModel
{
    [JsonPropertyName("to")] public string To { get; set; } = "";

    [JsonPropertyName("signature")] public string Signature { get; set; } = "";

    [JsonPropertyName("templateId")] public string TemplateId { get; set; } = "";

    [JsonPropertyName("templateData")] public Dictionary<string, object> TemplateData { get; set; } = new();
}