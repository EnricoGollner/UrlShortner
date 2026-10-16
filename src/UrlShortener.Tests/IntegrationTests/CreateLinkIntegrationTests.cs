using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using UrlShortener.Api.Models;
using UrlShortener.Api.Services;

namespace UrlShortener.Tests.IntegrationTests;

public class CreateLinkIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public CreateLinkIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddScoped<IUrlValidator, IntegrationTestUrlValidator>();
            });
        }).CreateClient();
    }

    private class IntegrationTestUrlValidator : IUrlValidator
    {
        public Task<UrlValidationResult> ValidateAsync(string url, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(url) || (!url.StartsWith("http://") && !url.StartsWith("https://")))
            {
                return Task.FromResult(UrlValidationResult.Failure("A URL de destino deve ser uma URL válida com protocolo HTTP ou HTTPS."));
            }

            if (url.Contains("localhost") || url.Contains("127.0.0.1") || url.Contains("10.0.0.1"))
            {
                return Task.FromResult(UrlValidationResult.Failure("A URL de destino não pode apontar para endereços locais ou privados."));
            }

            return Task.FromResult(UrlValidationResult.Success());
        }
    }

    [Fact]
    public async Task PostCreateLink_WithValidUrl_Returns201CreatedAndLocationHeader()
    {
        // Arrange
        var request = new CreateLinkRequest("https://example.com/some/destination");

        // Act
        var response = await _client.PostAsJsonAsync("/api/links", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var body = await response.Content.ReadFromJsonAsync<CreateLinkResponse>();
        Assert.NotNull(body);
        Assert.Equal(7, body.Code.Length);
        Assert.Contains(body.Code, body.ShortUrl);
        Assert.Equal($"/{body.Code}", response.Headers.Location.ToString());
    }

    [Fact]
    public async Task PostCreateLink_RepeatedCallsWithSameUrl_ReturnsDistinctCodes()
    {
        // Arrange
        var request = new CreateLinkRequest("https://example.com/repeated");

        // Act
        var response1 = await _client.PostAsJsonAsync("/api/links", request);
        var response2 = await _client.PostAsJsonAsync("/api/links", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response1.StatusCode);
        Assert.Equal(HttpStatusCode.Created, response2.StatusCode);

        var body1 = await response1.Content.ReadFromJsonAsync<CreateLinkResponse>();
        var body2 = await response2.Content.ReadFromJsonAsync<CreateLinkResponse>();

        Assert.NotNull(body1);
        Assert.NotNull(body2);
        Assert.NotEqual(body1.Code, body2.Code);
    }

    [Fact]
    public async Task PostCreateLink_WithInvalidUrl_Returns400BadRequestWithProblemDetails()
    {
        // Arrange
        var request = new CreateLinkRequest("ftp://invalid-protocol.com");

        // Act
        var response = await _client.PostAsJsonAsync("/api/links", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(400, problem.Status);
        Assert.Equal("Erro de validação", problem.Title);
        Assert.Contains("HTTP ou HTTPS", problem.Detail);
    }
}
