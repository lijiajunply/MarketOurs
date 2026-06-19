using MarketOurs.DataAPI.Configs;
using MarketOurs.DataAPI.Exceptions;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using StackExchange.Redis;

namespace MarketOurs.DataAPI.Services;

public interface ICaptchaService
{
    Task<CaptchaChallengeDto> GenerateChallengeAsync();
    Task<string?> VerifyChallengeAsync(string token, int x);
    Task<bool> ValidateCaptchaTokenAsync(string captchaToken);
}

public class CaptchaChallengeDto
{
    public string Token { get; set; } = string.Empty;
    public string BackgroundImage { get; set; } = string.Empty;
    public string PuzzleImage { get; set; } = string.Empty;
    public int PuzzleWidth { get; set; }
    public int PuzzleHeight { get; set; }
}

public class CaptchaService(IEnumerable<IConnectionMultiplexer> redisEnumerable, ILogger<CaptchaService> logger) : ICaptchaService
{
    private readonly IConnectionMultiplexer? _redis = redisEnumerable.FirstOrDefault();

    private const int BgWidth = 300;
    private const int BgHeight = 160;
    private const int PuzzleWidth = 50;
    private const int PuzzleHeight = 50;
    private const int MinX = 20;
    private const int MaxX = 230;
    private const int Tolerance = 5;
    private const int ChallengeTtlMinutes = 5;
    private const int CaptchaTokenTtlMinutes = 5;

    private static readonly PngEncoder PngEncoder = new();

    public async Task<CaptchaChallengeDto> GenerateChallengeAsync()
    {
        if (_redis == null)
            throw new BusinessException(ErrorCode.CacheOperationFailed, "Redis service unavailable");

        var random = new Random();
        var puzzleX = random.Next(MinX, MaxX);
        var puzzleY = random.Next(15, BgHeight - PuzzleHeight - 15);

        var bgBase64 = GenerateBackgroundImage(puzzleX, puzzleY);
        var puzzleBase64 = GeneratePuzzlePiece(puzzleX, puzzleY);

        var token = Guid.NewGuid().ToString("N");
        var db = _redis.GetDatabase();
        await db.StringSetAsync(
            CacheKeys.CaptchaChallenge(token),
            $"{puzzleX}:{puzzleY}",
            TimeSpan.FromMinutes(ChallengeTtlMinutes));

        return new CaptchaChallengeDto
        {
            Token = token,
            BackgroundImage = bgBase64,
            PuzzleImage = puzzleBase64,
            PuzzleWidth = PuzzleWidth,
            PuzzleHeight = PuzzleHeight
        };
    }

    public async Task<string?> VerifyChallengeAsync(string token, int x)
    {
        if (_redis == null)
            throw new BusinessException(ErrorCode.CacheOperationFailed, "Redis service unavailable");

        var db = _redis.GetDatabase();
        var key = CacheKeys.CaptchaChallenge(token);
        var value = await db.StringGetAsync(key);

        if (!value.HasValue)
            return null;

        var parts = value.ToString().Split(':');
        if (parts.Length != 2 || !int.TryParse(parts[0], out var expectedX))
            return null;

        if (Math.Abs(expectedX - x) > Tolerance)
            return null;

        await db.KeyDeleteAsync(key);

        var captchaToken = Guid.NewGuid().ToString("N");
        await db.StringSetAsync(
            CacheKeys.CaptchaToken(captchaToken),
            "1",
            TimeSpan.FromMinutes(CaptchaTokenTtlMinutes));

        return captchaToken;
    }

    public async Task<bool> ValidateCaptchaTokenAsync(string captchaToken)
    {
        if (_redis == null) return true;

        var db = _redis.GetDatabase();
        var key = CacheKeys.CaptchaToken(captchaToken);
        var exists = await db.KeyExistsAsync(key);
        if (!exists) return false;

        await db.KeyDeleteAsync(key);
        return true;
    }

    private static string GenerateBackgroundImage(int cutoutX, int cutoutY)
    {
        using var image = new Image<Rgba32>(BgWidth, BgHeight);

        var rng = new Random();
        var seed = rng.Next();

        var bgR = (byte)(180 + (seed % 60));
        var bgG = (byte)(180 + ((seed >> 8) % 60));
        var bgB = (byte)(180 + ((seed >> 16) % 60));

        for (var y = 0; y < BgHeight; y++)
        {
            for (var x = 0; x < BgWidth; x++)
            {
                if (x >= cutoutX && x < cutoutX + PuzzleWidth &&
                    y >= cutoutY && y < cutoutY + PuzzleHeight)
                {
                    image[x, y] = new Rgba32(0, 0, 0, 100);
                }
                else
                {
                    var noise = (byte)((x * y + seed) % 30);
                    image[x, y] = new Rgba32(
                        (byte)(bgR + noise),
                        (byte)(bgG + noise),
                        (byte)(bgB + noise));
                }
            }
        }

        using var ms = new MemoryStream();
        image.Save(ms, PngEncoder);
        return Convert.ToBase64String(ms.ToArray());
    }

    private static string GeneratePuzzlePiece(int cutoutX, int cutoutY)
    {
        using var pieceImage = new Image<Rgba32>(PuzzleWidth, PuzzleHeight);
        var rng = new Random((cutoutX << 16) | cutoutY);

        for (var y = 0; y < PuzzleHeight; y++)
        {
            for (var x = 0; x < PuzzleWidth; x++)
            {
                var noise = (byte)((x * y + (cutoutX << 8) + cutoutY) % 40 + 10);
                pieceImage[x, y] = new Rgba32(
                    (byte)(180 + noise),
                    (byte)(190 + noise),
                    (byte)(200 + noise));
            }
        }

        using var ms = new MemoryStream();
        pieceImage.Save(ms, PngEncoder);
        return Convert.ToBase64String(ms.ToArray());
    }
}
