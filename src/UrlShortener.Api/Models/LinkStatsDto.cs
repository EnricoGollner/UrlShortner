namespace UrlShortener.Api.Models;

public record LinkStatsResponse(
    string Code,
    string TargetUrl,
    int AccessCount,
    DateTime CreatedAtUtc,
    DateTime? ExpiresAtUtc,
    bool IsExpired
);
