using MarketOurs.DataAPI.Services;

namespace MarketOurs.Test.Services;

[TestFixture]
public class TemplateServiceTests
{
    private FluidTemplateService _templateService;

    [SetUp]
    public void SetUp()
    {
        _templateService = new FluidTemplateService();
    }

    [Test]
    public async Task RenderAsync_ValidTemplate_RendersCorrectly()
    {
        // Arrange
        var template = "Hello {{ name }}!";
        var model = new { name = "World" };

        // Act
        var result = await _templateService.RenderAsync(template, model);

        // Assert
        Assert.That(result, Is.EqualTo("Hello World!"));
    }

    [Test]
    public async Task RenderAsync_InvalidTemplate_ThrowsException()
    {
        // Arrange
        var template = "{% if %}"; // Missing expression in Fluid/Liquid

        // Act & Assert
        Assert.ThrowsAsync<Exception>(async () => await _templateService.RenderAsync(template, new { name = "test" }));
    }
}