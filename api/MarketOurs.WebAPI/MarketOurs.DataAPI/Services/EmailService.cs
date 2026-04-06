using MailKit.Net.Smtp;
using MailKit.Security;
using MarketOurs.DataAPI.Configs;
using Microsoft.Extensions.Logging;
using MimeKit;
using Polly;
using Polly.Retry;

namespace MarketOurs.DataAPI.Services;

public interface IEmailService
{
    /// <summary>
    /// 发送邮件
    /// </summary>
    /// <param name="to">收件人邮箱地址</param>
    /// <param name="subject">邮件主题</param>
    /// <param name="body">邮件正文</param>
    /// <param name="isHtml">是否为HTML格式</param>
    /// <returns>发送结果</returns>
    Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = false);

    /// <summary>
    /// 使用模板发送邮件
    /// </summary>
    /// <param name="to">收件人</param>
    /// <param name="subject">主题</param>
    /// <param name="templateContent">模板内容</param>
    /// <param name="model">数据模型</param>
    /// <param name="isHtml">是否为HTML</param>
    /// <returns>发送结果</returns>
    Task<bool> SendEmailWithTemplateAsync(string to, string subject, string templateContent, object model, bool isHtml = true);
}

public class EmailService(
    EmailConfig emailConfig, 
    ILogger<EmailService> logger, 
    ITemplateService templateService,
    ISmtpClient? smtpClient = null) : IEmailService
{
    private readonly ISmtpClient _smtpClient = smtpClient ?? new SmtpClient();
    private readonly AsyncRetryPolicy _retryPolicy = Policy
        .Handle<Exception>()
        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            (exception, timeSpan, retryCount, context) =>
            {
                logger.LogWarning(exception, "邮件发送失败，正在进行第 {RetryCount} 次重试，等待时间: {TimeSpan}s", retryCount, timeSpan.TotalSeconds);
            });

    public async Task<bool> SendEmailWithTemplateAsync(string to, string subject, string templateContent, object model, bool isHtml = true)
    {
        try
        {
            var body = await templateService.RenderAsync(templateContent, model);
            return await SendEmailAsync(to, subject, body, isHtml);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "邮件模板渲染失败，收件人: {To}, 主题: {Subject}", to, subject);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = false)
    {
        try
        {
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                logger.LogInformation("开始发送邮件，收件人: {To}, 主题: {Subject}", to, subject);

                var smtpServer = emailConfig.Host ?? "smtp.gmail.com";
                var port = emailConfig.Port;

                var username = emailConfig.Username ?? "iOS Club";
                var password = emailConfig.Password ?? "iOS Club";
                var fromAddress = emailConfig.Email ?? "iOS Club";

                // 检查必要配置是否存在
                if (string.IsNullOrEmpty(smtpServer) ||
                    string.IsNullOrEmpty(username) ||
                    string.IsNullOrEmpty(password) ||
                    string.IsNullOrEmpty(fromAddress))
                {
                    logger.LogError("邮件发送失败: 必要的SMTP配置缺失");
                    return false;
                }

                try
                {
                    var message = new MimeMessage();
                    message.From.Add(new MailboxAddress("", fromAddress));
                    message.To.Add(new MailboxAddress("", to));
                    message.Subject = subject;

                    var bodyBuilder = new BodyBuilder();
                    if (isHtml)
                    {
                        bodyBuilder.HtmlBody = body;
                    }
                    else
                    {
                        bodyBuilder.TextBody = body;
                    }

                    message.Body = bodyBuilder.ToMessageBody();

                    // 根据端口选择安全选项
                    var secureSocketOptions = port switch
                    {
                        587 => SecureSocketOptions.StartTls,
                        465 => SecureSocketOptions.SslOnConnect,
                        _ => SecureSocketOptions.Auto
                    };

                    logger.LogDebug("连接到SMTP服务器: {SmtpServer}, 端口: {Port}, 安全选项: {SecureSocketOptions}", smtpServer, port,
                        secureSocketOptions);
                    
                    // 确保在重试时重新连接，如果已经连接则跳过
                    if (!_smtpClient.IsConnected)
                    {
                        await _smtpClient.ConnectAsync(smtpServer, port, secureSocketOptions);
                    }

                    if (!_smtpClient.IsAuthenticated)
                    {
                        logger.LogDebug("SMTP服务器连接成功，开始认证");
                        await _smtpClient.AuthenticateAsync(fromAddress, password);
                    }

                    logger.LogDebug("SMTP认证成功，开始发送邮件");
                    await _smtpClient.SendAsync(message);
                    
                    // 发送完成后主动断开，或者保持连接由外部管理
                    // 这里为了简单，每次发送完断开。如果高频发送建议保持连接。
                    await _smtpClient.DisconnectAsync(true);

                    logger.LogInformation("邮件发送成功，收件人: {To}, 主题: {Subject}", to, subject);
                    return true;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "邮件发送尝试失败，收件人: {To}, 主题: {Subject}", to, subject);
                    throw; // 抛出异常以触发 Polly 重试
                }
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "邮件发送在所有重试后仍然失败，收件人: {To}, 主题: {Subject}", to, subject);
            return false;
        }
    }
}