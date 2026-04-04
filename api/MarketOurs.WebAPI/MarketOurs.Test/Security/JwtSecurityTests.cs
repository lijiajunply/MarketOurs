using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using MarketOurs.Data.DTOs;
using MarketOurs.DataAPI.Configs;
using MarketOurs.DataAPI.Services;
using MarketOurs.WebAPI.Services;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Moq;

namespace MarketOurs.Test.Security;

[TestFixture]
[Category("Security")]
public class JwtSecurityTests
{
    private Mock<RsaKeyManager> _mockRsaKeyManager;
    private JwtConfig _jwtConfig;
    private Mock<ILogger<JwtService>> _mockLogger;
    private JwtService _jwtService;
    private RSA _rsaKey;

    [SetUp]
    public void Setup()
    {
        _rsaKey = RSA.Create(2048);
        _jwtConfig = new JwtConfig
        {
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            AccessTokenExpiryMinutes = 20,
            RsaPrivateKeyPath = "dummy_priv",
            RsaPublicKeyPath = "dummy_pub"
        };

        var rsaLogger = new Mock<ILogger<RsaKeyManager>>();
        _mockRsaKeyManager = new Mock<RsaKeyManager>(_jwtConfig, rsaLogger.Object);
        _mockRsaKeyManager.Setup(m => m.GetCurrentPrivateKey()).Returns(_rsaKey);
        _mockRsaKeyManager.Setup(m => m.GetCurrentPublicKey()).Returns(_rsaKey);

        _mockLogger = new Mock<ILogger<JwtService>>();
        _jwtService = new JwtService(_jwtConfig, _mockRsaKeyManager.Object, _mockLogger.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _rsaKey.Dispose();
    }

    [Test]
    public async Task GetAccessToken_ShouldContainRequiredClaims()
    {
        // Arrange
        var user = new UserDto { Id = "user123", Name = "John Doe", Role = "Admin", Email = "john@test.com" };

        // Act
        var token = await _jwtService.GetAccessToken(user, DeviceType.Web);
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        // Assert
        Assert.That(jwtToken.Issuer, Is.EqualTo(_jwtConfig.Issuer));
        Assert.That(jwtToken.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value, Is.EqualTo(user.Id));
        Assert.That(jwtToken.Claims.First(c => c.Type == ClaimTypes.Role).Value, Is.EqualTo(user.Role));
        Assert.That(jwtToken.Claims.First(c => c.Type == "usage").Value, Is.EqualTo("access"));
    }

    [Test]
    public async Task ValidateAccessToken_WithValidToken_ShouldReturnTrue()
    {
        // Arrange
        var user = new UserDto { Id = "user123", Name = "John", Role = "User", Email = "john@test.com" };
        var token = await _jwtService.GetAccessToken(user, DeviceType.Web);

        // Act
        var (isValid, claims) = _jwtService.ValidateAccessToken(token);

        // Assert
        Assert.That(isValid, Is.True);
        Assert.That(claims.Any(c => c.Type == ClaimTypes.NameIdentifier && c.Value == "user123"), Is.True);
    }

    [Test]
    public void ValidateAccessToken_WithExpiredToken_ShouldThrowException()
    {
        // Arrange
        // Manually create an expired token
        var now = DateTime.UtcNow.AddMinutes(-20);
        var rsaKey = new RsaSecurityKey(_rsaKey);
        var credentials = new SigningCredentials(rsaKey, SecurityAlgorithms.RsaSha256Signature);
        var header = new JwtHeader(credentials);
        var payload = new JwtPayload(
            _jwtConfig.Issuer,
            _jwtConfig.Audience,
            new[] 
            { 
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim("usage", "access")
            },
            now,
            now.AddMinutes(5)); // Expired 15 minutes ago
        
        var token = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(header, payload));

        // Act & Assert
        Assert.Throws<SecurityTokenExpiredException>(() => _jwtService.ValidateAccessToken(token));
    }

    [Test]
    public async Task ValidateAccessToken_WithTamperedToken_ShouldReturnFalse()
    {
        // Arrange
        var user = new UserDto { Id = "user1", Name = "Test", Role = "User" };
        var token = await _jwtService.GetAccessToken(user, DeviceType.Web);
        
        // Tamper with the token string (e.g., change a character in the payload part)
        var parts = token.Split('.');
        var payload = parts[1];
        var tamperedPayload = payload.Substring(0, payload.Length - 1) + (payload.EndsWith("A") ? "B" : "A");
        var tamperedToken = $"{parts[0]}.{tamperedPayload}.{parts[2]}";

        // Act
        var (isValid, _) = _jwtService.ValidateAccessToken(tamperedToken);

        // Assert
        Assert.That(isValid, Is.False, "Tampered token should not be valid.");
    }

    [Test]
    public async Task ValidateAccessToken_WithWrongSigningKey_ShouldReturnFalse()
    {
        // Arrange
        var user = new UserDto { Id = "user1", Name = "Test", Role = "User" };
        
        // Generate token with Key A
        var token = await _jwtService.GetAccessToken(user, DeviceType.Web);

        // Switch service to Key B for validation
        using var differentRsa = RSA.Create(2048);
        _mockRsaKeyManager.Setup(m => m.GetCurrentPublicKey()).Returns(differentRsa);

        // Act
        var (isValid, _) = _jwtService.ValidateAccessToken(token);

        // Assert
        Assert.That(isValid, Is.False, "Token signed with different key should be rejected.");
    }

    [Test]
    public async Task ValidateAccessToken_WithMissingUsageClaim_ShouldReturnFalse()
    {
        // Arrange
        // Manually create a token without the "usage" claim
        var now = DateTime.UtcNow;
        var rsaKey = new RsaSecurityKey(_rsaKey);
        var credentials = new SigningCredentials(rsaKey, SecurityAlgorithms.RsaSha256Signature);
        var header = new JwtHeader(credentials);
        var payload = new JwtPayload(
            _jwtConfig.Issuer,
            _jwtConfig.Audience,
            new[] { new Claim(ClaimTypes.NameIdentifier, "1") },
            now,
            now.AddMinutes(20));
        
        var token = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(header, payload));

        // Act
        var (isValid, _) = _jwtService.ValidateAccessToken(token);

        // Assert
        Assert.That(isValid, Is.False, "Token missing usage:access claim should be rejected.");
    }
}