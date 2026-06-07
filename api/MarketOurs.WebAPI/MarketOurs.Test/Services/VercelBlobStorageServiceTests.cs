using System.Net;
using System.Text;
using MarketOurs.WebAPI.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MarketOurs.Test.Services;

[NonParallelizable]
public class VercelBlobStorageServiceTests
{
    private string? _originalToken;
    private string? _originalStoreId;
    private string? _originalAccess;
    private string? _originalBasePath;

    [SetUp]
    public void SetUp()
    {
        _originalToken = Environment.GetEnvironmentVariable("BLOB_READ_WRITE_TOKEN");
        _originalStoreId = Environment.GetEnvironmentVariable("BLOB_STORE_ID");
        _originalAccess = Environment.GetEnvironmentVariable("BLOB_ACCESS");
        _originalBasePath = Environment.GetEnvironmentVariable("BLOB_BASE_PATH");

        Environment.SetEnvironmentVariable("BLOB_READ_WRITE_TOKEN", "vercel_blob_rw_teststore_secret");
        Environment.SetEnvironmentVariable("BLOB_STORE_ID", "store_teststore");
        Environment.SetEnvironmentVariable("BLOB_ACCESS", "public");
        Environment.SetEnvironmentVariable("BLOB_BASE_PATH", "uploads");
    }

    [TearDown]
    public void TearDown()
    {
        Environment.SetEnvironmentVariable("BLOB_READ_WRITE_TOKEN", _originalToken);
        Environment.SetEnvironmentVariable("BLOB_STORE_ID", _originalStoreId);
        Environment.SetEnvironmentVariable("BLOB_ACCESS", _originalAccess);
        Environment.SetEnvironmentVariable("BLOB_BASE_PATH", _originalBasePath);
    }

    [Test]
    public async Task SaveFileAsync_UsesCurrentVercelBlobUploadApi()
    {
        HttpMethod? capturedMethod = null;
        Uri? capturedUri = null;
        var capturedHeaders = new Dictionary<string, string[]>();
        var handler = new CapturingHandler(request =>
        {
            capturedMethod = request.Method;
            capturedUri = request.RequestUri;
            foreach (var header in request.Headers)
            {
                capturedHeaders[header.Key] = header.Value.ToArray();
            }

            var body = """
                       {"url":"https://teststore.public.blob.vercel-storage.com/uploads/images/file.png"}
                       """;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
        });
        var service = CreateService(new HttpClient(handler));
        var file = CreateFormFile("avatar.png", "image/png");

        var url = await service.SaveFileAsync(file, "images");

        Assert.That(url, Is.EqualTo("https://teststore.public.blob.vercel-storage.com/uploads/images/file.png"));
        Assert.That(capturedUri, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(capturedMethod, Is.EqualTo(HttpMethod.Put));
            Assert.That(capturedUri!.GetLeftPart(UriPartial.Path), Is.EqualTo("https://vercel.com/api/blob/"));
            Assert.That(capturedUri.Query, Does.Contain("pathname=uploads%2Fimages%2F"));
            Assert.That(capturedHeaders["x-vercel-blob-store-id"], Is.EqualTo(new[] { "teststore" }));
            Assert.That(capturedHeaders["x-vercel-blob-access"], Is.EqualTo(new[] { "public" }));
            Assert.That(capturedHeaders.ContainsKey("access"), Is.False);
        });
    }

    private static VercelBlobStorageService CreateService(HttpClient httpClient)
    {
        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(item => item.WebRootPath).Returns(Path.GetTempPath());
        var localStorage = new LocalStorageService(environment.Object, NullLogger<LocalStorageService>.Instance);

        return new VercelBlobStorageService(
            httpClient,
            localStorage,
            NullLogger<VercelBlobStorageService>.Instance);
    }

    private static FormFile CreateFormFile(string fileName, string contentType)
    {
        var bytes = Encoding.UTF8.GetBytes("test image content");
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    private sealed class CapturingHandler(Func<HttpRequestMessage, HttpResponseMessage> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(send(request));
        }
    }
}
