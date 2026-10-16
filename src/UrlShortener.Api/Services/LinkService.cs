using Microsoft.EntityFrameworkCore;
using UrlShortener.Api.Data;
using UrlShortener.Api.Models;

namespace UrlShortener.Api.Services;

public class LinkService : ILinkService
{
    public const int MaxCollisionAttempts = 5;
    public const int MinCustomCodeLength = 4;
    public const int MaxCustomCodeLength = 20;

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

        if (request.Code is null)
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
            var code = request.Code;

            // Validação de formato para código próprio (RF-03, RN-05)
            if (code.Length < MinCustomCodeLength || code.Length > MaxCustomCodeLength)
            {
                return new CreateLinkResult(
                    false,
                    null,
                    $"O código próprio deve ter entre {MinCustomCodeLength} e {MaxCustomCodeLength} caracteres.",
                    CreateLinkErrorType.ValidationError);
            }

            if (!code.All(c => (c >= 'A' && c <= 'Z') || (c >= '1' && c <= '9')))
            {
                return new CreateLinkResult(
                    false,
                    null,
                    "O código próprio deve conter apenas letras maiúsculas de A a Z e números de 1 a 9.",
                    CreateLinkErrorType.ValidationError);
            }

            // Checagem de unicidade prévia (RN-01, D-04)
            if (await _linkRepository.ExistsByCodeAsync(code, cancellationToken))
            {
                return new CreateLinkResult(
                    false,
                    null,
                    "O código informado já está em uso.",
                    CreateLinkErrorType.Conflict);
            }

            finalCode = code;
        }

        var link = new Link
        {
            Code = finalCode,
            TargetUrl = request.TargetUrl,
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = normalizedExpiresAtUtc,
            AccessCount = 0
        };

        try
        {
            await _linkRepository.AddAsync(link, cancellationToken);
            await _linkRepository.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // D-04: caso ocorra concorrência na inserção violando o índice único
            return new CreateLinkResult(
                false,
                null,
                "O código informado já está em uso.",
                CreateLinkErrorType.Conflict);
        }

        return new CreateLinkResult(true, link);
    }

    public async Task<GetLinkResult> GetLinkForRedirectAsync(string code, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return new GetLinkResult(false, null, "Link não encontrado.", GetLinkErrorType.NotFound);
        }

        var link = await _linkRepository.GetByCodeAsync(code, cancellationToken);
        if (link == null)
        {
            return new GetLinkResult(false, null, "Link não encontrado.", GetLinkErrorType.NotFound);
        }

        // RN-03: link expirado não redireciona e não conta acesso
        if (link.ExpiresAtUtc.HasValue && link.ExpiresAtUtc.Value <= DateTime.UtcNow)
        {
            return new GetLinkResult(false, null, "Este link expirou.", GetLinkErrorType.Expired);
        }

        // RF-05, RN-04, D-02: incrementa contador de forma atômica no banco antes de redirecionar
        await _linkRepository.IncrementAccessCountAsync(link.Id, cancellationToken);

        return new GetLinkResult(true, link.TargetUrl);
    }
}
