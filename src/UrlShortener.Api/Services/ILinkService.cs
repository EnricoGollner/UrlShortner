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

public enum GetLinkErrorType
{
    None,
    NotFound,
    Expired
}

public record GetLinkResult(
    bool IsSuccess,
    string? TargetUrl = null,
    string? ErrorMessage = null,
    GetLinkErrorType ErrorType = GetLinkErrorType.None
);

public record LinkStatsResult(
    bool IsSuccess,
    LinkStatsResponse? Stats = null,
    string? ErrorMessage = null,
    GetLinkErrorType ErrorType = GetLinkErrorType.None
);

public interface ILinkService
{
    Task<CreateLinkResult> CreateLinkAsync(CreateLinkRequest request, CancellationToken cancellationToken = default);
    Task<GetLinkResult> GetLinkForRedirectAsync(string code, CancellationToken cancellationToken = default);
    Task<LinkStatsResult> GetStatsAsync(string code, CancellationToken cancellationToken = default);
}
