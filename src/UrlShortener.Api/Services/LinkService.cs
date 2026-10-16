using UrlShortener.Api.Data;
using UrlShortener.Api.Models;

namespace UrlShortener.Api.Services;

public class LinkService : ILinkService
{
    public const int MaxCollisionAttempts = 5;

    private readonly ILinkRepository _linkRepository;
    private readonly ICodeGenerator _codeGenerator;
    private readonly IUrlValidator _urlValidator;

    public LinkService(
        ILinkRepository linkRepository,
        ICodeGenerator codeGenerator,
        IUrlValidator urlValidator)
    {
        _linkRepository = linkRepository;
        _codeGenerator = codeGenerator;
        _urlValidator = urlValidator;
    }

    public async Task<CreateLinkResult> CreateLinkAsync(CreateLinkRequest request, CancellationToken cancellationToken = default)
    {
        // Validação da URL de destino (RN-02)
        var urlValidation = await _urlValidator.ValidateAsync(request.TargetUrl, cancellationToken);
        if (!urlValidation.IsValid)
        {
            return new CreateLinkResult(
                false,
                null,
                urlValidation.ErrorMessage,
                CreateLinkErrorType.ValidationError);
        }

        // Validação e normalização da data de expiração (RF-06)
        DateTime? normalizedExpiresAtUtc = null;
        if (request.ExpiresAtUtc.HasValue)
        {
            var expiresAt = request.ExpiresAtUtc.Value;
            var now = DateTime.UtcNow;

            if (expiresAt < now)
            {
                return new CreateLinkResult(
                    false,
                    null,
                    "A data de expiração não pode estar no passado.",
                    CreateLinkErrorType.ValidationError);
            }

            // RF-06: se for informada apenas a data, permanece válido até o final daquele dia (23:59:59 UTC)
            if (expiresAt.TimeOfDay == TimeSpan.Zero)
            {
                normalizedExpiresAtUtc = new DateTime(expiresAt.Year, expiresAt.Month, expiresAt.Day, 23, 59, 59, DateTimeKind.Utc);
            }
            else
            {
                normalizedExpiresAtUtc = DateTime.SpecifyKind(expiresAt, DateTimeKind.Utc);
            }
        }

        string finalCode;

        if (string.IsNullOrWhiteSpace(request.Code))
        {
            // Geração automática com até 5 tentativas consecutivas em caso de colisão (RN-06, D-06)
            string? codeCandidate = null;
            for (var attempt = 1; attempt <= MaxCollisionAttempts; attempt++)
            {
                var candidate = _codeGenerator.GenerateCode();
                if (!await _linkRepository.ExistsByCodeAsync(candidate, cancellationToken))
                {
                    codeCandidate = candidate;
                    break;
                }
            }

            if (codeCandidate == null)
            {
                return new CreateLinkResult(
                    false,
                    null,
                    "Serviço temporariamente indisponível devido a colisões consecutivas na geração de código.",
                    CreateLinkErrorType.ServiceUnavailable);
            }

            finalCode = codeCandidate;
        }
        else
        {
            var trimmedCode = request.Code.Trim();

            if (await _linkRepository.ExistsByCodeAsync(trimmedCode, cancellationToken))
            {
                return new CreateLinkResult(
                    false,
                    null,
                    "O código informado já está em uso.",
                    CreateLinkErrorType.Conflict);
            }

            finalCode = trimmedCode;
        }

        var link = new Link
        {
            Code = finalCode,
            TargetUrl = request.TargetUrl,
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = normalizedExpiresAtUtc,
            AccessCount = 0
        };

        await _linkRepository.AddAsync(link, cancellationToken);
        await _linkRepository.SaveChangesAsync(cancellationToken);

        return new CreateLinkResult(true, link);
    }
}
