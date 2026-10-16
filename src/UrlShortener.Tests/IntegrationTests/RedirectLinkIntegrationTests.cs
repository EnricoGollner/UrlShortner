using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using UrlShortener.Api.Models;
using UrlShortener.Api.Services;

namespace UrlShortener.Tests.IntegrationTests;

public class RedirectLinkIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public RedirectLinkIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddScoped<IUrlValidator, IntegrationTestUrlValidator>();
            });
        });

        // Configura client sem auto-redirect para inspecionar o status 302 e header Location
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    private class IntegrationTestUrlValidator : IUrlValidator
    {
        public Task<UrlValidationResult> ValidateAsync(string url, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(UrlValidationResult.Success());
        }
    }

    [Fact]
    public async Task GetRedirect_WithExistingCode_Returns302RedirectAndTargetLocation()
    {
        // Arrange: cria um link primeiro
        const string targetUrl = "https://example.com/redirect-destination";
        var createResponse = await _client.PostAsJsonAsync("/api/links", new CreateLinkRequest(targetUrl));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateLinkResponse>();
        Assert.NotNull(created);

        // Act: acessa GET /{code}
        var redirectResponse = await _client.GetAsync($"/{created.Code}");

        // Assert: status 302 Found com Location correto
        Assert.Equal(HttpStatusCode.Redirect, redirectResponse.StatusCode);
        Assert.Equal(targetUrl, redirectResponse.Headers.Location?.ToString());
    }

    [Fact]
    public async Task GetRedirect_WithDifferentCasing_Returns302Redirect()
    {
        // Arrange: cria link com código em maiúsculas
        var customCode = $"UP{Guid.NewGuid():N}".Replace("0", "9").ToUpperInvariant()[..8];
        const string targetUrl = "https://example.com/case-destination";
        var createResponse = await _client.PostAsJsonAsync("/api/links", new CreateLinkRequest(targetUrl, Code: customCode));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        // Act: acessa em minúsculas
        var redirectResponse = await _client.GetAsync($"/{customCode.ToLower()}");

        // Assert: redireciona com sucesso (case-insensitive)
        Assert.Equal(HttpStatusCode.Redirect, redirectResponse.StatusCode);
        Assert.Equal(targetUrl, redirectResponse.Headers.Location?.ToString());
    }

    [Fact]
    public async Task GetRedirect_WithNonExistentCode_Returns404NotFoundProblemDetails()
    {
        // Act
        var response = await _client.GetAsync("/NONEXISTENT");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(404, problem.Status);
        Assert.Equal("Link não encontrado", problem.Title);
    }

    [Fact]
    public async Task GetRedirect_WithExpiredCode_Returns410GoneProblemDetails()
    {
        // Arrange: insere link expirado via DbContext
        var expiredCode = $"EXP{Guid.NewGuid():N}".Replace("0", "9").ToUpperInvariant()[..8];
        const string targetUrl = "https://example.com/expired-target";

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<UrlShortener.Api.Data.AppDbContext>();
            db.Links.Add(new Link
            {
                Code = expiredCode,
                TargetUrl = targetUrl,
                CreatedAtUtc = DateTime.UtcNow.AddDays(-2),
                ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-5)
            });
            await db.SaveChangesAsync();
        }

        // Act
        var response = await _client.GetAsync($"/{expiredCode}");

        // Assert
        Assert.Equal(HttpStatusCode.Gone, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(410, problem.Status);
        Assert.Equal("Link expirado", problem.Title);
        Assert.Equal("Este link expirou.", problem.Detail);
    }
}
