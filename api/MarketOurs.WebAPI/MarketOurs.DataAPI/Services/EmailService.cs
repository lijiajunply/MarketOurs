using MailKit.Net.Smtp;
using MailKit.Security;
using MarketOurs.DataAPI.Configs;
using Microsoft.Extensions.Logging;
using MimeKit;

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
}

public class EmailService(EmailConfig emailConfig, ILogger<EmailService> logger) : IEmailService
{
    public async Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = false)
    {
        logger.LogInformation("开始发送邮件，收件人: {To}, 主题: {Subject}", to, subject);

        var smtpServer = emailConfig.Host ?? "smtp.gmail.com";;
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

            using var client = new SmtpClient();

            // 根据端口选择安全选项
            var secureSocketOptions = port switch
            {
                587 => SecureSocketOptions.StartTls,
                465 => SecureSocketOptions.SslOnConnect,
                _ => SecureSocketOptions.Auto
            };

            logger.LogDebug("连接到SMTP服务器: {SmtpServer}, 端口: {Port}, 安全选项: {SecureSocketOptions}", smtpServer, port,
                secureSocketOptions);
            await client.ConnectAsync(smtpServer, port, secureSocketOptions);

            logger.LogDebug("SMTP服务器连接成功，开始认证");
            await client.AuthenticateAsync(fromAddress, password);

            logger.LogDebug("SMTP认证成功，开始发送邮件");
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            logger.LogInformation("邮件发送成功，收件人: {To}, 主题: {Subject}", to, subject);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "邮件发送失败，收件人: {To}, 主题: {Subject}", to, subject);
            return false;
        }
    }
}