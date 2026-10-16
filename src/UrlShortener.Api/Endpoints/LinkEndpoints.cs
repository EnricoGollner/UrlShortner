using Microsoft.AspNetCore.Mvc;
using UrlShortener.Api.Models;
using UrlShortener.Api.Services;

namespace UrlShortener.Api.Endpoints;

public static class LinkEndpoints
{
    public static void MapLinkEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/links");

        group.MapPost("/", async (
            [FromBody] CreateLinkRequest request,
            ILinkService linkService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await linkService.CreateLinkAsync(request, cancellationToken);

            if (!result.IsSuccess)
            {
                return result.ErrorType switch
                {
                    CreateLinkErrorType.ValidationError => Results.Problem(
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "Erro de validação",
                        detail: result.ErrorMessage),

                    CreateLinkErrorType.Conflict => Results.Problem(
                        statusCode: StatusCodes.Status409Conflict,
                        title: "Conflito de código",
                        detail: result.ErrorMessage),

                    CreateLinkErrorType.ServiceUnavailable => Results.Problem(
                        statusCode: StatusCodes.Status503ServiceUnavailable,
                        title: "Serviço indisponível",
                        detail: result.ErrorMessage),

                    _ => Results.Problem(
                        statusCode: StatusCodes.Status500InternalServerError,
                        title: "Erro interno",
                        detail: result.ErrorMessage)
                };
            }

            var link = result.Link!;
            var baseUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";
            var shortUrl = $"{baseUrl}/{link.Code}";

            var response = new CreateLinkResponse(shortUrl, link.Code);
            return Results.Created($"/{link.Code}", response);
        });

        group.MapGet("/{code}/stats", async (
            string code,
            HttpContext httpContext,
            IConfiguration configuration,
            ILinkService linkService,
            CancellationToken cancellationToken) =>
        {
            var expectedKey = configuration["StatsApiKey"] ?? "secret-stats-key";

            string? providedKey = null;
            if (httpContext.Request.Headers.TryGetValue("X-Api-Key", out var headerKey))
            {
                providedKey = headerKey.ToString();
            }
            else if (httpContext.Request.Headers.TryGetValue("X-Stats-Key", out var statsHeaderKey))
            {
                providedKey = statsHeaderKey.ToString();
            }
            else if (httpContext.Request.Query.TryGetValue("key", out var queryKey))
            {
                providedKey = queryKey.ToString();
            }

            if (string.IsNullOrWhiteSpace(providedKey) || !string.Equals(providedKey, expectedKey, StringComparison.Ordinal))
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status401Unauthorized,
                    title: "Não autorizado",
                    detail: "Chave de acesso inválida ou não fornecida.");
            }

            var result = await linkService.GetStatsAsync(code, cancellationToken);

            if (!result.IsSuccess)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Link não encontrado",
                    detail: result.ErrorMessage);
            }

            return Results.Ok(result.Stats);
        });

        app.MapGet("/{code}", async (
            string code,
            ILinkService linkService,
            CancellationToken cancellationToken) =>
        {
            var result = await linkService.GetLinkForRedirectAsync(code, cancellationToken);

            if (!result.IsSuccess)
            {
                return result.ErrorType switch
                {
                    GetLinkErrorType.NotFound => Results.Problem(
                        statusCode: StatusCodes.Status404NotFound,
                        title: "Link não encontrado",
                        detail: result.ErrorMessage),

                    GetLinkErrorType.Expired => Results.Problem(
                        statusCode: StatusCodes.Status410Gone,
                        title: "Link expirado",
                        detail: result.ErrorMessage),

                    _ => Results.Problem(
                        statusCode: StatusCodes.Status500InternalServerError,
                        title: "Erro interno",
                        detail: result.ErrorMessage)
                };
            }

            return Results.Redirect(result.TargetUrl!, permanent: false);
        });
    }
}
