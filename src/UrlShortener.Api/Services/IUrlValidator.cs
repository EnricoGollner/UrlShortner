namespace UrlShortener.Api.Services;

public record UrlValidationResult(bool IsValid, string? ErrorMessage = null)
{
    public static UrlValidationResult Success() => new(true);
    public static UrlValidationResult Failure(string errorMessage) => new(false, errorMessage);
}

public interface IUrlValidator
{
    Task<UrlValidationResult> ValidateAsync(string url, CancellationToken cancellationToken = default);
}
