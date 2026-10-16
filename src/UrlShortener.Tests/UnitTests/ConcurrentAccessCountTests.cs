using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using UrlShortener.Api.Data;
using UrlShortener.Api.Models;
using UrlShortener.Api.Services;

namespace UrlShortener.Tests.UnitTests;

public class ConcurrentAccessCountTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;

    public ConcurrentAccessCountTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = new AppDbContext(_options);
        context.Database.EnsureCreated();
    }

    private class FakeUrlValidator : IUrlValidator
    {
        public Task<UrlValidationResult> ValidateAsync(string url, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(UrlValidationResult.Success());
        }
    }

    [Fact]
    public async Task GetLinkForRedirectAsync_WhenAccessed_IncrementsAccessCount()
    {
        // Arrange
        using (var setupContext = new AppDbContext(_options))
        {
            setupContext.Links.Add(new Link
            {
                Code = "COUNT01",
                TargetUrl = "https://example.com",
                CreatedAtUtc = DateTime.UtcNow,
                AccessCount = 0
            });
            await setupContext.SaveChangesAsync();
        }

        using var testContext = new AppDbContext(_options);
        var repository = new LinkRepository(testContext);
        var service = new LinkService(repository, new CodeGenerator(), new FakeUrlValidator());

        // Act
        var result = await service.GetLinkForRedirectAsync("COUNT01");

        // Assert
        Assert.True(result.IsSuccess);

        using var verifyContext = new AppDbContext(_options);
        var link = await verifyContext.Links.FirstAsync(l => l.Code == "COUNT01");
        Assert.Equal(1, link.AccessCount);
    }

    [Fact]
    public async Task GetLinkForRedirectAsync_WhenLinkExpired_DoesNotIncrementAccessCount()
    {
        // Arrange
        using (var setupContext = new AppDbContext(_options))
        {
            setupContext.Links.Add(new Link
            {
                Code = "EXPIRE1",
                TargetUrl = "https://example.com",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-2),
                ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-5),
                AccessCount = 0
            });
            await setupContext.SaveChangesAsync();
        }

        using var testContext = new AppDbContext(_options);
        var repository = new LinkRepository(testContext);
        var service = new LinkService(repository, new CodeGenerator(), new FakeUrlValidator());

        // Act
        var result = await service.GetLinkForRedirectAsync("EXPIRE1");

        // Assert: não redireciona e contador permanece zero (RN-03, RN-04)
        Assert.False(result.IsSuccess);
        Assert.Equal(GetLinkErrorType.Expired, result.ErrorType);

        using var verifyContext = new AppDbContext(_options);
        var link = await verifyContext.Links.FirstAsync(l => l.Code == "EXPIRE1");
        Assert.Equal(0, link.AccessCount);
    }

    [Fact]
    public async Task ConcurrentAccess_DoesNotLoseCount_D02AtomicUpdate()
    {
        // Arrange
        const int concurrentRequests = 50;
        const string code = "ATOMIC1";

        using (var setupContext = new AppDbContext(_options))
        {
            setupContext.Links.Add(new Link
            {
                Code = code,
                TargetUrl = "https://example.com/target",
                CreatedAtUtc = DateTime.UtcNow,
                AccessCount = 0
            });
            await setupContext.SaveChangesAsync();
        }

        // Act: executa múltiplas chamadas concorrentes
        var tasks = Enumerable.Range(0, concurrentRequests).Select(async _ =>
        {
            using var context = new AppDbContext(_options);
            var repository = new LinkRepository(context);
            var service = new LinkService(repository, new CodeGenerator(), new FakeUrlValidator());
            var res = await service.GetLinkForRedirectAsync(code);
            Assert.True(res.IsSuccess);
        });

        await Task.WhenAll(tasks);

        // Assert: todas as 50 contagens devem ser preservadas sem perda por corrida (RN-04, D-02)
        using var verifyContext = new AppDbContext(_options);
        var link = await verifyContext.Links.FirstAsync(l => l.Code == code);
        Assert.Equal(concurrentRequests, link.AccessCount);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}
