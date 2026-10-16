using System.Net;
using UrlShortener.Api.Services;

namespace UrlShortener.Tests.UnitTests;

public class UrlValidatorTests
{
    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_handler(request));
        }
    }

    private static UrlValidator CreateValidator(HttpStatusCode responseStatus = HttpStatusCode.OK)
    {
        var handler = new MockHttpMessageHandler(_ => new HttpResponseMessage(responseStatus));
        var client = new HttpClient(handler);
        return new UrlValidator(client);
    }

    [Theory]
    [InlineData("http://example.com")]
    [InlineData("https://example.com/some/path?param=123")]
    [InlineData("https://google.com")]
    public async Task ValidateAsync_ValidHttpAndHttpsUrls_ReturnsSuccess(string url)
    {
        var validator = CreateValidator(HttpStatusCode.OK);

        var result = await validator.ValidateAsync(url);

        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateAsync_UrlExceeds2048Chars_ReturnsFailure()
    {
        var validator = CreateValidator();
        var longUrl = "https://example.com/" + new string('a', 2040); // > 2048 characters

        var result = await validator.ValidateAsync(longUrl);

        Assert.False(result.IsValid);
        Assert.Contains("2048", result.ErrorMessage);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ValidateAsync_EmptyOrWhitespaceUrl_ReturnsFailure(string? url)
    {
        var validator = CreateValidator();

        var result = await validator.ValidateAsync(url!);

        Assert.False(result.IsValid);
        Assert.Equal("A URL de destino é obrigatória.", result.ErrorMessage);
    }

    [Theory]
    [InlineData("ftp://example.com/file.txt")]
    [InlineData("mailto:test@example.com")]
    [InlineData("file:///C:/path/to/file")]
    [InlineData("not-a-valid-url")]
    public async Task ValidateAsync_InvalidSchemeOrMalformed_ReturnsFailure(string url)
    {
        var validator = CreateValidator();

        var result = await validator.ValidateAsync(url);

        Assert.False(result.IsValid);
        Assert.Contains("HTTP ou HTTPS", result.ErrorMessage);
    }

    [Theory]
    [InlineData("http://localhost")]
    [InlineData("http://localhost:8080/api")]
    [InlineData("http://127.0.0.1")]
    [InlineData("http://127.0.0.1:5000/test")]
    [InlineData("http://10.0.0.1")]
    [InlineData("http://192.168.1.1/admin")]
    [InlineData("http://172.16.0.1")]
    [InlineData("http://172.31.255.255")]
    [InlineData("http://169.254.169.254/latest/meta-data")]
    [InlineData("http://service.internal/path")]
    [InlineData("http://server.local/api")]
    public async Task ValidateAsync_LocalOrPrivateAddresses_ReturnsFailure(string url)
    {
        var validator = CreateValidator();

        var result = await validator.ValidateAsync(url);

        Assert.False(result.IsValid);
        Assert.Equal("A URL de destino não pode apontar para endereços locais ou privados.", result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateAsync_InaccessibleServerHttpError_ReturnsFailure()
    {
        var validator = CreateValidator(HttpStatusCode.InternalServerError);

        var result = await validator.ValidateAsync("https://example.com/error");

        Assert.False(result.IsValid);
        Assert.Equal("A URL de destino está inacessível ou não respondeu à verificação de rede.", result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateAsync_NetworkExceptionThrown_ReturnsFailure()
    {
        var handler = new MockHttpMessageHandler(_ => throw new HttpRequestException("Connection refused"));
        var client = new HttpClient(handler);
        var validator = new UrlValidator(client);

        var result = await validator.ValidateAsync("https://example.com/offline");

        Assert.False(result.IsValid);
        Assert.Equal("A URL de destino está inacessível ou não respondeu à verificação de rede.", result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateAsync_HeadReturnsMethodNotAllowed_RetriesGetAndSucceeds()
    {
        var callCount = 0;
        var handler = new MockHttpMessageHandler(request =>
        {
            callCount++;
            if (request.Method == HttpMethod.Head)
            {
                return new HttpResponseMessage(HttpStatusCode.MethodNotAllowed);
            }
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var client = new HttpClient(handler);
        var validator = new UrlValidator(client);

        var result = await validator.ValidateAsync("https://example.com/no-head");

        Assert.True(result.IsValid);
        Assert.Equal(2, callCount);
    }
}
