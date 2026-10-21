using Microsoft.AspNetCore.Mvc;
using UrlShortener.Api.Models;
using UrlShortener.Api.Services;

namespace UrlShortener.Api.Endpoints;

public static class LinkEndpoints
{
    public static void MapLinkEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/links")
            .WithTags("Links");

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
        })
        .WithSummary("Cria um link encurtado")
        .WithDescription("Cria um novo link encurtado com código aleatório ou código próprio customizado.")
        .Produces<CreateLinkResponse>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        group.MapGet("/{code}/stats", async (
            string code,
            ILinkService linkService,
            CancellationToken cancellationToken) =>
        {
            var result = await linkService.GetStatsAsync(code, cancellationToken);

            if (!result.IsSuccess)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Link não encontrado",
                    detail: result.ErrorMessage);
            }

            return Results.Ok(result.Stats);
        })
        .WithSummary("Obtém estatísticas de acesso do link")
        .WithDescription("Retorna total de acessos e data de criação do link. Consulta pública (FR-014).")
        .Produces<LinkStatsResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound);

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
        })
        .WithTags("Links")
        .WithSummary("Redireciona para a URL de destino")
        .WithDescription("Redireciona via 302 Found para o endereço original associado ao código encurtado.")
        .Produces(StatusCodes.Status302Found)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status410Gone);
    }
}
