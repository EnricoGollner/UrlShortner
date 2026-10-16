using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace UrlShortener.Tests.IntegrationTests;

public class SwaggerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public SwaggerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetSwaggerJson_ReturnsSuccessAndValidDocument()
    {
        // Act
        var response = await _client.GetAsync("/swagger/v1/swagger.json");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("UrlShortener API", content);
        Assert.Contains("/api/links", content);
        Assert.Contains("ApiKey", content);
        Assert.Contains("X-Api-Key", content);
    }

    [Fact]
    public async Task GetSwaggerUI_ReturnsSuccessAndHtml()
    {
        // Act
        var response = await _client.GetAsync("/swagger/index.html");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("swagger-ui", content, StringComparison.OrdinalIgnoreCase);
    }
}
