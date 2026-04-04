using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using MarketOurs.DataAPI.Configs;
using MarketOurs.DataAPI.Services;
using Microsoft.Extensions.Logging;
using MimeKit;
using Moq;
using NUnit.Framework;

namespace MarketOurs.Test.Services;

[TestFixture]
public class EmailServiceTests
{
    private Mock<ILogger<EmailService>> _mockLogger;
    private Mock<ISmtpClient> _mockSmtpClient;
    private Mock<ITemplateService> _mockTemplateService;
    private EmailService _emailService;
    private EmailConfig _config;

    [SetUp]
    public void SetUp()
    {
        _mockLogger = new Mock<ILogger<EmailService>>();
        _mockSmtpClient = new Mock<ISmtpClient>();
        _mockTemplateService = new Mock<ITemplateService>();
        
        _config = new EmailConfig
        {
            Host = "smtp.test.com",
            Port = 587,
            Username = "testuser",
            Password = "testpassword",
            Email = "from@test.com"
        };

        _emailService = new EmailService(_config, _mockLogger.Object, _mockTemplateService.Object, _mockSmtpClient.Object);
    }

    [Test]
    public async Task SendEmailAsync_ValidInputs_ReturnsTrue()
    {
        // Arrange
        var to = "to@test.com";
        var subject = "Test Subject";
        var body = "Test Body";

        // Act
        var result = await _emailService.SendEmailAsync(to, subject, body);

        // Assert
        Assert.That(result, Is.True);
        _mockSmtpClient.Verify(c => c.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<SecureSocketOptions>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockSmtpClient.Verify(c => c.AuthenticateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockSmtpClient.Verify(c => c.SendAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>()), Times.Once);
    }

    [Test]
    public async Task SendEmailAsync_RetriesOnFailure_EventuallySucceeds()
    {
        // Arrange
        var callCount = 0;
        _mockSmtpClient.Setup(c => c.SendAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>()))
            .Callback(() =>
            {
                callCount++;
                if (callCount < 2) throw new Exception("Temporary failure");
            })
            .Returns(Task.FromResult("OK"));

        // Act
        var result = await _emailService.SendEmailAsync("to@test.com", "Subject", "Body");

        // Assert
        Assert.That(result, Is.True);
        Assert.That(callCount, Is.EqualTo(2));
        _mockSmtpClient.Verify(c => c.SendAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>()), Times.Exactly(2));
    }

    [Test]
    public async Task SendEmailAsync_FailsAfterAllRetries_ReturnsFalse()
    {
        // Arrange
        _mockSmtpClient.Setup(c => c.SendAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>()))
            .ThrowsAsync(new Exception("Persistent failure"));

        // Act & Assert
        // Note: Polly handles the exception and SendEmailAsync returns false due to catch in execute block or final failure
        // Actually, our EmailService catch block for the entire policy execution returns false
        var result = await _emailService.SendEmailAsync("to@test.com", "Subject", "Body");

        Assert.That(result, Is.False);
        _mockSmtpClient.Verify(c => c.SendAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>()), Times.Exactly(4)); // 1 original + 3 retries
    }

    [Test]
    public async Task SendEmailWithTemplateAsync_RendersAndSends()
    {
        // Arrange
        var template = "Hello {{ name }}";
        var model = new { name = "World" };
        var renderedBody = "Hello World";
        _mockTemplateService.Setup(s => s.RenderAsync(template, model)).ReturnsAsync(renderedBody);

        // Act
        var result = await _emailService.SendEmailWithTemplateAsync("to@test.com", "Subject", template, model);

        // Assert
        Assert.That(result, Is.True);
        _mockTemplateService.Verify(s => s.RenderAsync(template, model), Times.Once);
        _mockSmtpClient.Verify(c => c.SendAsync(It.Is<MimeMessage>(m => m.HtmlBody == renderedBody), It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>()), Times.Once);
    }
}