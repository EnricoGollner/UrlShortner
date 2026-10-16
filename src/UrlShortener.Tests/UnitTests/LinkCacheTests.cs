using Microsoft.Extensions.Caching.Memory;
using UrlShortener.Api.Data;
using UrlShortener.Api.Models;
using UrlShortener.Api.Services;

namespace UrlShortener.Tests.UnitTests;

public class LinkCacheTests
{
    private class TrackingLinkRepository : ILinkRepository
    {
        public int GetByCodeCallCount { get; private set; }
        public int IncrementCallCount { get; private set; }

        private readonly Dictionary<string, Link> _links = new(StringComparer.OrdinalIgnoreCase);

        public void AddSeed(Link link)
        {
            _links[link.Code] = link;
        }

        public Task<Link?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
        {
            GetByCodeCallCount++;
            _links.TryGetValue(code, out var link);
            return Task.FromResult(link);
        }

        public Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_links.ContainsKey(code));
        }

        public Task AddAsync(Link link, CancellationToken cancellationToken = default)
        {
            _links[link.Code] = link;
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task IncrementAccessCountAsync(int linkId, CancellationToken cancellationToken = default)
        {
            IncrementCallCount++;
            var link = _links.Values.FirstOrDefault(l => l.Id == linkId);
            if (link != null)
            {
                link.AccessCount++;
            }
            return Task.CompletedTask;
        }
    }

    private class FakeUrlValidator : IUrlValidator
    {
        public Task<UrlValidationResult> ValidateAsync(string url, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(UrlValidationResult.Success());
        }
    }

    [Fact]
    public async Task GetLinkForRedirectAsync_RepeatedAccess_UsesCacheAndAvoidsDatabaseQuery()
    {
        // Arrange
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var repository = new TrackingLinkRepository();
        repository.AddSeed(new Link
        {
            Id = 1,
            Code = "CACHE01",
            TargetUrl = "https://example.com/cached-target",
            CreatedAtUtc = DateTime.UtcNow,
            AccessCount = 0
        });

        var service = new LinkService(repository, new CodeGenerator(), new FakeUrlValidator(), memoryCache);

        // Act 1: primeiro acesso (cache miss)
        var result1 = await service.GetLinkForRedirectAsync("CACHE01");

        // Act 2 & 3: acessos subsequentes (cache hits)
        var result2 = await service.GetLinkForRedirectAsync("CACHE01");
        var result3 = await service.GetLinkForRedirectAsync("CACHE01");

        // Assert
        Assert.True(result1.IsSuccess);
        Assert.True(result2.IsSuccess);
        Assert.True(result3.IsSuccess);
        Assert.Equal("https://example.com/cached-target", result2.TargetUrl);

        // Consulta ao banco ocorreu apenas 1 vez (no primeiro acesso)
        Assert.Equal(1, repository.GetByCodeCallCount);

        // Mas o contador atômico foi incrementado em todos os 3 acessos
        Assert.Equal(3, repository.IncrementCallCount);
    }

    [Fact]
    public async Task GetLinkForRedirectAsync_WhenCachedLinkExpires_ReturnsExpired()
    {
        // Arrange
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var repository = new TrackingLinkRepository();
        repository.AddSeed(new Link
        {
            Id = 2,
            Code = "CACHEXP",
            TargetUrl = "https://example.com/will-expire",
            CreatedAtUtc = DateTime.UtcNow.AddMinutes(-10),
            ExpiresAtUtc = DateTime.UtcNow.AddSeconds(1), // expira em 1 segundo
            AccessCount = 0
        });

        var service = new LinkService(repository, new CodeGenerator(), new FakeUrlValidator(), memoryCache);

        // Primeiro acesso (válido, colocado em cache)
        var result1 = await service.GetLinkForRedirectAsync("CACHEXP");
        Assert.True(result1.IsSuccess);

        // Aguarda expiração
        await Task.Delay(1100);

        // Segundo acesso através do cache: detecta expiração
        var result2 = await service.GetLinkForRedirectAsync("CACHEXP");

        // Assert
        Assert.False(result2.IsSuccess);
        Assert.Equal(GetLinkErrorType.Expired, result2.ErrorType);
        Assert.Equal("Este link expirou.", result2.ErrorMessage);
    }
}
