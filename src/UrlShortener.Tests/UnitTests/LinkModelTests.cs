using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using UrlShortener.Api.Data;
using UrlShortener.Api.Models;

namespace UrlShortener.Tests.UnitTests;

public class LinkModelTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;

    public LinkModelTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = new AppDbContext(_options);
        context.Database.EnsureCreated();
    }

    [Fact]
    public async Task CanInsertAndRetrieveLink()
    {
        using var context = new AppDbContext(_options);
        var link = new Link
        {
            Code = "ABC1234",
            TargetUrl = "https://example.com/long-url",
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            AccessCount = 0
        };

        context.Links.Add(link);
        await context.SaveChangesAsync();

        var savedLink = await context.Links.FirstOrDefaultAsync(l => l.Code == "ABC1234");
        Assert.NotNull(savedLink);
        Assert.Equal("https://example.com/long-url", savedLink.TargetUrl);
        Assert.Equal(0, savedLink.AccessCount);
        Assert.NotNull(savedLink.ExpiresAtUtc);
    }

    [Fact]
    public async Task DuplicateCode_ThrowsExceptionDueToUniqueConstraint()
    {
        using var context = new AppDbContext(_options);
        var link1 = new Link
        {
            Code = "UNIQUE1",
            TargetUrl = "https://example.com/first",
            CreatedAtUtc = DateTime.UtcNow
        };
        var link2 = new Link
        {
            Code = "UNIQUE1",
            TargetUrl = "https://example.com/second",
            CreatedAtUtc = DateTime.UtcNow
        };

        context.Links.Add(link1);
        await context.SaveChangesAsync();

        context.Links.Add(link2);
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}
