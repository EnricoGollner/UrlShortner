namespace UrlShortener.Api.Models;

public class Link
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string TargetUrl { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public int AccessCount { get; set; }
}
