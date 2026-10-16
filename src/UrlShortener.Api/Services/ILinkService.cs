using UrlShortener.Api.Models;

namespace UrlShortener.Api.Services;

public enum CreateLinkErrorType
{
    None,
    ValidationError,
    Conflict,
    ServiceUnavailable
}

public record CreateLinkResult(
    bool IsSuccess,
    Link? Link = null,
    string? ErrorMessage = null,
    CreateLinkErrorType ErrorType = CreateLinkErrorType.None
);

public interface ILinkService
{
    Task<CreateLinkResult> CreateLinkAsync(CreateLinkRequest request, CancellationToken cancellationToken = default);
}
