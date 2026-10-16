namespace UrlShortener.Api.Models;

public record CreateLinkRequest(
    string TargetUrl,
    string? Code = null,
    DateTime? ExpiresAtUtc = null
);

public record CreateLinkResponse(
    string ShortUrl,
    string Code
);
