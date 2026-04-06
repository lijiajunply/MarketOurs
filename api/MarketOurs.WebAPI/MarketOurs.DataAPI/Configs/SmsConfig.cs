namespace MarketOurs.DataAPI.Configs;

public class SmsConfig
{
    public string AccessKeyId { get; set; } = "";
    public string AccessKeySecret  { get; set; } = "";
    public string Endpoint { get; set; } = "uni.apistd.com";
    public string SigningAlgorithm { get; set; } = "hmac-sha256";
}