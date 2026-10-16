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

    [Fact]
    public async Task PostCreateLink_WithValidCustomCode_Returns201CreatedAndCustomCode()
    {
        // Arrange
        var customCode = $"CST{Guid.NewGuid():N}".Replace("0", "9").ToUpperInvariant()[..8];
        var request = new CreateLinkRequest("https://example.com/custom-test", Code: customCode);

        // Act
        var response = await _client.PostAsJsonAsync("/api/links", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<CreateLinkResponse>();
        Assert.NotNull(body);
        Assert.Equal(customCode, body.Code);
        Assert.Equal($"/{customCode}", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task PostCreateLink_WithDuplicateCustomCode_Returns409Conflict()
    {
        // Arrange
        var customCode = $"DUP{Guid.NewGuid():N}".Replace("0", "9").ToUpperInvariant()[..8];
        var request1 = new CreateLinkRequest("https://example.com/first-link", Code: customCode);
        var request2 = new CreateLinkRequest("https://example.com/second-link", Code: customCode);

        // Act
        var response1 = await _client.PostAsJsonAsync("/api/links", request1);
        var response2 = await _client.PostAsJsonAsync("/api/links", request2);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response1.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, response2.StatusCode);

        var problem = await response2.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(409, problem.Status);
        Assert.Equal("Conflito de código", problem.Title);
        Assert.Equal("O código informado já está em uso.", problem.Detail);
    }

    [Fact]
    public async Task PostCreateLink_WithInvalidCustomCodeFormat_Returns400BadRequest()
    {
        // Arrange: lowercase letters or 0 are invalid
        var request = new CreateLinkRequest("https://example.com/invalid-code", Code: "invalid0");

        // Act
        var response = await _client.PostAsJsonAsync("/api/links", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(400, problem.Status);
        Assert.Equal("Erro de validação", problem.Title);
        Assert.Contains("apenas letras maiúsculas de A a Z e números de 1 a 9", problem.Detail);
    }
}
