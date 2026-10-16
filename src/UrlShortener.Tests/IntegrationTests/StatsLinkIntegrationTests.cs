using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using UrlShortener.Api.Data;
using UrlShortener.Api.Models;
using UrlShortener.Api.Services;

namespace UrlShortener.Tests.IntegrationTests;

public class StatsLinkIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private const string ValidApiKey = "secret-stats-key";

    public StatsLinkIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddScoped<IUrlValidator, IntegrationTestUrlValidator>();
            });
        });

        _client = _factory.CreateClient();
    }

    private class IntegrationTestUrlValidator : IUrlValidator
    {
        public Task<UrlValidationResult> ValidateAsync(string url, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(UrlValidationResult.Success());
        }
    }

    [Fact]
    public async Task GetStats_WithInvalidKey_Returns401Unauthorized()
    {
        // Act: requisição com chave errada (critério de aceite de T-09)
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/links/ANYCODE/stats");
        request.Headers.Add("X-Api-Key", "invalid-key");

        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(401, problem.Status);
        Assert.Equal("Não autorizado", problem.Title);
    }

    [Fact]
    public async Task GetStats_WithoutKey_Returns401Unauthorized()
    {
        // Act: requisição sem fornecer chave
        var response = await _client.GetAsync("/api/links/ANYCODE/stats");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetStats_WithValidKey_Returns200WithStatsResponse()
    {
        // Arrange: cria um link e incrementa seu contador via acesso
        var customCode = $"ST{Guid.NewGuid():N}".Replace("0", "9").ToUpperInvariant()[..8];
        const string targetUrl = "https://example.com/analytics";

        var createResponse = await _client.PostAsJsonAsync("/api/links", new CreateLinkRequest(targetUrl, Code: customCode));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        // Realiza 2 acessos para contar
        await _client.GetAsync($"/{customCode}");
        await _client.GetAsync($"/{customCode}");

        // Act: consulta stats com a chave correta
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/links/{customCode}/stats");
        request.Headers.Add("X-Api-Key", ValidApiKey);

        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var stats = await response.Content.ReadFromJsonAsync<LinkStatsResponse>();
        Assert.NotNull(stats);
        Assert.Equal(customCode, stats.Code);
        Assert.Equal(targetUrl, stats.TargetUrl);
        Assert.Equal(2, stats.AccessCount);
        Assert.False(stats.IsExpired);
    }

    [Fact]
    public async Task GetStats_WithValidKeyForExpiredLink_Returns200WithStats_RF07()
    {
        // Arrange: insere link expirado diretamente no banco
        var expiredCode = $"SE{Guid.NewGuid():N}".Replace("0", "9").ToUpperInvariant()[..8];
        const string targetUrl = "https://example.com/expired-stats";

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Links.Add(new Link
            {
                Code = expiredCode,
                TargetUrl = targetUrl,
                CreatedAtUtc = DateTime.UtcNow.AddDays(-10),
                ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-5),
                AccessCount = 15
            });
            await db.SaveChangesAsync();
        }

        // Act: RF-07 exige que consulta de stats devolva acessos mesmo após expiração
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/links/{expiredCode}/stats");
        request.Headers.Add("X-Api-Key", ValidApiKey);

        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var stats = await response.Content.ReadFromJsonAsync<LinkStatsResponse>();
        Assert.NotNull(stats);
        Assert.Equal(expiredCode, stats.Code);
        Assert.Equal(15, stats.AccessCount);
        Assert.True(stats.IsExpired);
    }

    [Fact]
    public async Task GetStats_WithValidKeyForNonExistentCode_Returns404NotFound()
    {
        // Act
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/links/NONEXIST/stats");
        request.Headers.Add("X-Api-Key", ValidApiKey);

        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(404, problem.Status);
        Assert.Equal("Link não encontrado", problem.Title);
    }
}
