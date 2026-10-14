using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace UrlShortener.Tests.IntegrationTests;

public class RootEndpointIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public RootEndpointIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetRoot_ReturnsSuccessStatusCode()
    {
        // Act
        var response = await _client.GetAsync("/");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal("UrlShortener API", content);
    }
}
