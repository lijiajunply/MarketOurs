using System.Security.Cryptography;
using MarketOurs.Data.DTOs;
using MarketOurs.DataAPI.Configs;
using MarketOurs.DataAPI.Services;
using MarketOurs.WebAPI.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace MarketOurs.Test.Stress;

[TestFixture]
[Category("HighLoad")]
[Category("Security")]
public class KeyRotationStressTests
{
    private Mock<RsaKeyManager> _mockRsaKeyManager;
    private JwtConfig _jwtConfig;
    private Mock<ILogger<JwtService>> _mockLogger;
    private JwtService _jwtService;
    private RSA _currentKey;

    [SetUp]
    public void Setup()
    {
        _currentKey = RSA.Create(2048);
        _jwtConfig = new JwtConfig
        {
            Issuer = "StressIssuer",
            Audience = "StressAudience",
            AccessTokenExpiryMinutes = 20,
            RsaPrivateKeyPath = "stress_priv",
            RsaPublicKeyPath = "stress_pub"
        };

        var rsaLogger = new Mock<ILogger<RsaKeyManager>>();
        _mockRsaKeyManager = new Mock<RsaKeyManager>(_jwtConfig, rsaLogger.Object);
        _mockRsaKeyManager.Setup(m => m.GetCurrentPrivateKey()).Returns(() => _currentKey);
        _mockRsaKeyManager.Setup(m => m.GetCurrentPublicKey()).Returns(() => _currentKey);

        _mockLogger = new Mock<ILogger<JwtService>>();
        _jwtService = new JwtService(_jwtConfig, _mockRsaKeyManager.Object, _mockLogger.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _currentKey.Dispose();
    }

    [Test]
    public async Task KeyRotation_UnderHeavyTokenIssuance_ShouldNotCrash()
    {
        // Arrange
        const int totalTokens = 5000;
        var user = new UserDto { Id = "u1", Name = "User 1", Role = "User", Email = "u1@test.com" };
        
        bool rotationTriggered = false;
        int tokensIssued = 0;

        // Act
        var issuanceTask = Task.Run(async () => 
        {
            for (int i = 0; i < totalTokens; i++)
            {
                await _jwtService.GetAccessToken(user, DeviceType.Web);
                Interlocked.Increment(ref tokensIssued);
                
                // Trigger rotation halfway
                if (i == totalTokens / 2)
                {
                    rotationTriggered = true;
                    var oldKey = _currentKey;
                    _currentKey = RSA.Create(2048); // New Key!
                    oldKey.Dispose();
                }
            }
        });

        var concurrentIssuanceTask = Parallel.ForEachAsync(Enumerable.Range(0, totalTokens), async (_, _) => 
        {
            await _jwtService.GetAccessToken(user, DeviceType.Web);
        });

        await Task.WhenAll(issuanceTask, concurrentIssuanceTask);

        // Assert
        await TestContext.Out.WriteLineAsync($"Successfully issued {tokensIssued + totalTokens} tokens during key rotation simulation.");
        Assert.That(rotationTriggered, Is.True);
        Assert.That(tokensIssued, Is.EqualTo(totalTokens));
    }

    [Test]
    public async Task TokenValidation_DuringKeyRotation_ShouldHandleTransition()
    {
        // Arrange
        var user = new UserDto { Id = "u1", Name = "User 1", Role = "User", Email = "u1@test.com" };
        
        // 1. Issue token with Key A
        var tokenA = await _jwtService.GetAccessToken(user, DeviceType.Web);
        
        // 2. Rotate to Key B
        var keyA = _currentKey;
        _currentKey = RSA.Create(2048);
        
        // 3. Issue token with Key B
        var tokenB = await _jwtService.GetAccessToken(user, DeviceType.Web);

        // 4. Validate Token B (Success expected)
        var (isValidB, _) = _jwtService.ValidateAccessToken(tokenB);
        Assert.That(isValidB, Is.True, "New token should be valid with new key.");

        // 5. Validate Token A (Failure expected if system only keeps 1 key)
        var (isValidA, _) = _jwtService.ValidateAccessToken(tokenA);
        
        await TestContext.Out.WriteLineAsync($"Old token valid after rotation: {isValidA}");
        // Current implementation uses ONE key at a time, so old tokens become invalid immediately.
        // This test documents that behavior.
        Assert.That(isValidA, Is.False, "Old token should be invalid immediately after rotation in current implementation.");
        
        keyA.Dispose();
    }
}