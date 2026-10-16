using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using UrlShortener.Api.Data;
using UrlShortener.Api.Models;
using UrlShortener.Api.Services;

namespace UrlShortener.Tests.UnitTests;

public class LinkServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;

    public LinkServiceTests()
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
        private readonly bool _isValid;
        private readonly string? _errorMessage;

        public FakeUrlValidator(bool isValid = true, string? errorMessage = null)
        {
            _isValid = isValid;
            _errorMessage = errorMessage;
        }

        public Task<UrlValidationResult> ValidateAsync(string url, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_isValid
                ? UrlValidationResult.Success()
                : UrlValidationResult.Failure(_errorMessage ?? "URL inválida."));
        }
    }

    private class SequenceCodeGenerator : ICodeGenerator
    {
        private readonly Queue<string> _codes;

        public SequenceCodeGenerator(params string[] codes)
        {
            _codes = new Queue<string>(codes);
        }

        public string GenerateCode()
        {
            return _codes.Count > 0 ? _codes.Dequeue() : "DEF1234";
        }
    }

    [Fact]
    public async Task CreateLinkAsync_WhenUrlIsInvalid_ReturnsValidationError()
    {
        using var context = new AppDbContext(_options);
        var repository = new LinkRepository(context);
        var service = new LinkService(
            repository,
            new CodeGenerator(),
            new FakeUrlValidator(isValid: false, errorMessage: "URL inválida."));

        var request = new CreateLinkRequest("ftp://invalid");
        var result = await service.CreateLinkAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Equal(CreateLinkErrorType.ValidationError, result.ErrorType);
        Assert.Equal("URL inválida.", result.ErrorMessage);
    }

    [Fact]
    public async Task CreateLinkAsync_WhenExpirationIsInThePast_ReturnsValidationError()
    {
        using var context = new AppDbContext(_options);
        var repository = new LinkRepository(context);
        var service = new LinkService(
            repository,
            new CodeGenerator(),
            new FakeUrlValidator(isValid: true));

        var pastDate = DateTime.UtcNow.AddMinutes(-10);
        var request = new CreateLinkRequest("https://example.com", ExpiresAtUtc: pastDate);
        var result = await service.CreateLinkAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Equal(CreateLinkErrorType.ValidationError, result.ErrorType);
        Assert.Equal("A data de expiração não pode estar no passado.", result.ErrorMessage);
    }

    [Fact]
    public async Task CreateLinkAsync_WhenExpirationIsDateOnly_SetsToEndOfDayUtc()
    {
        using var context = new AppDbContext(_options);
        var repository = new LinkRepository(context);
        var service = new LinkService(
            repository,
            new CodeGenerator(),
            new FakeUrlValidator(isValid: true));

        var futureDateOnly = DateTime.UtcNow.Date.AddDays(2);
        var request = new CreateLinkRequest("https://example.com", ExpiresAtUtc: futureDateOnly);
        var result = await service.CreateLinkAsync(request);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Link!.ExpiresAtUtc);
        Assert.Equal(new TimeSpan(23, 59, 59), result.Link.ExpiresAtUtc.Value.TimeOfDay);
    }

    [Fact]
    public async Task CreateLinkAsync_WithAutomaticCode_CreatesAndPersistsLink()
    {
        using var context = new AppDbContext(_options);
        var repository = new LinkRepository(context);
        var service = new LinkService(
            repository,
            new CodeGenerator(),
            new FakeUrlValidator(isValid: true));

        var request = new CreateLinkRequest("https://example.com/target");
        var result = await service.CreateLinkAsync(request);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Link);
        Assert.Equal(7, result.Link.Code.Length);
        Assert.Equal("https://example.com/target", result.Link.TargetUrl);
        Assert.Equal(0, result.Link.AccessCount);

        var saved = await repository.GetByCodeAsync(result.Link.Code);
        Assert.NotNull(saved);
        Assert.Equal(result.Link.Id, saved.Id);
    }

    [Fact]
    public async Task CreateLinkAsync_RepeatedSubmissionsOfSameTarget_GeneratesDistinctLinks()
    {
        using var context = new AppDbContext(_options);
        var repository = new LinkRepository(context);
        var service = new LinkService(
            repository,
            new CodeGenerator(),
            new FakeUrlValidator(isValid: true));

        var request = new CreateLinkRequest("https://example.com/same-target");
        var result1 = await service.CreateLinkAsync(request);
        var result2 = await service.CreateLinkAsync(request);

        Assert.True(result1.IsSuccess);
        Assert.True(result2.IsSuccess);
        Assert.NotEqual(result1.Link!.Code, result2.Link!.Code);
        Assert.NotEqual(result1.Link.Id, result2.Link.Id);
    }

    [Fact]
    public async Task CreateLinkAsync_WhenCollisionOccurs_RetriesAndSucceedsWithin5Attempts()
    {
        using var context = new AppDbContext(_options);
        var repository = new LinkRepository(context);

        // Pre-insert colliding link
        await repository.AddAsync(new Link
        {
            Code = "COLLID1",
            TargetUrl = "https://existing.com",
            CreatedAtUtc = DateTime.UtcNow
        });
        await repository.SaveChangesAsync();

        // Sequence generator produces COLLID1 first, then SUCCESS1
        var codeGenerator = new SequenceCodeGenerator("COLLID1", "SUCCESS1");
        var service = new LinkService(
            repository,
            codeGenerator,
            new FakeUrlValidator(isValid: true));

        var request = new CreateLinkRequest("https://example.com/retry-test");
        var result = await service.CreateLinkAsync(request);

        Assert.True(result.IsSuccess);
        Assert.Equal("SUCCESS1", result.Link!.Code);
    }

    [Fact]
    public async Task CreateLinkAsync_WhenAll5AttemptsCollide_ReturnsServiceUnavailable()
    {
        using var context = new AppDbContext(_options);
        var repository = new LinkRepository(context);

        // Insert colliding code
        await repository.AddAsync(new Link
        {
            Code = "COLLID1",
            TargetUrl = "https://existing.com",
            CreatedAtUtc = DateTime.UtcNow
        });
        await repository.SaveChangesAsync();

        // Always returns COLLID1
        var codeGenerator = new SequenceCodeGenerator("COLLID1", "COLLID1", "COLLID1", "COLLID1", "COLLID1", "COLLID1");
        var service = new LinkService(
            repository,
            codeGenerator,
            new FakeUrlValidator(isValid: true));

        var request = new CreateLinkRequest("https://example.com/collide-all");
        var result = await service.CreateLinkAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Equal(CreateLinkErrorType.ServiceUnavailable, result.ErrorType);
        Assert.Contains("Serviço temporariamente indisponível", result.ErrorMessage);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}
