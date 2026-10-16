using System.Net;
using System.Net.Sockets;

namespace UrlShortener.Api.Services;

public class UrlValidator : IUrlValidator
{
    public const int MaxUrlLength = 2048;
    private readonly HttpClient _httpClient;

    public UrlValidator(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<UrlValidationResult> ValidateAsync(string url, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return UrlValidationResult.Failure("A URL de destino é obrigatória.");
        }

        if (url.Length > MaxUrlLength)
        {
            return UrlValidationResult.Failure($"A URL de destino não pode exceder {MaxUrlLength} caracteres.");
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return UrlValidationResult.Failure("A URL de destino deve ser uma URL válida com protocolo HTTP ou HTTPS.");
        }

        if (IsPrivateOrLocal(uri))
        {
            return UrlValidationResult.Failure("A URL de destino não pode apontar para endereços locais ou privados.");
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Head, uri);
            var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (response.StatusCode == HttpStatusCode.MethodNotAllowed)
            {
                using var getRequest = new HttpRequestMessage(HttpMethod.Get, uri);
                response = await _httpClient.SendAsync(getRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            }

            var statusCode = (int)response.StatusCode;
            if (statusCode >= 400)
            {
                return UrlValidationResult.Failure("A URL de destino está inacessível ou não respondeu à verificação de rede.");
            }

            return UrlValidationResult.Success();
        }
        catch
        {
            return UrlValidationResult.Failure("A URL de destino está inacessível ou não respondeu à verificação de rede.");
        }
    }

    public static bool IsPrivateOrLocal(Uri uri)
    {
        if (uri.IsLoopback)
        {
            return true;
        }

        var host = uri.Host;
        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
            host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase) ||
            host.EndsWith(".local", StringComparison.OrdinalIgnoreCase) ||
            host.EndsWith(".internal", StringComparison.OrdinalIgnoreCase) ||
            host.EndsWith(".lan", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (IPAddress.TryParse(host, out var ipAddress))
        {
            return IsPrivateOrLocalIp(ipAddress);
        }

        try
        {
            var addresses = Dns.GetHostAddresses(host);
            if (addresses.Length > 0 && addresses.Any(IsPrivateOrLocalIp))
            {
                return true;
            }
        }
        catch
        {
            // Falha na resolução de DNS será tratada na etapa de requisição HTTP
        }

        return false;
    }

    public static bool IsPrivateOrLocalIp(IPAddress ip)
    {
        if (IPAddress.IsLoopback(ip))
        {
            return true;
        }

        if (ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal)
        {
            return true;
        }

        if (ip.AddressFamily == AddressFamily.InterNetworkV6)
        {
            var bytesV6 = ip.GetAddressBytes();
            // Unique local address fc00::/7 (first byte is 0xfc or 0xfd)
            if ((bytesV6[0] & 0xfe) == 0xfc)
            {
                return true;
            }
        }

        var ipv4 = ip.IsIPv4MappedToIPv6 ? ip.MapToIPv4() : ip;
        if (ipv4.AddressFamily == AddressFamily.InterNetwork)
        {
            var bytes = ipv4.GetAddressBytes();

            // 0.0.0.0/8
            if (bytes[0] == 0) return true;

            // 10.0.0.0/8
            if (bytes[0] == 10) return true;

            // 127.0.0.0/8 (Loopback)
            if (bytes[0] == 127) return true;

            // 169.254.0.0/16 (Link-local / APIPA)
            if (bytes[0] == 169 && bytes[1] == 254) return true;

            // 172.16.0.0/12 (172.16.0.0 - 172.31.255.255)
            if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) return true;

            // 192.168.0.0/16
            if (bytes[0] == 192 && bytes[1] == 168) return true;

            // 100.64.0.0/10 (Carrier grade NAT: 100.64.0.0 to 100.127.255.255)
            if (bytes[0] == 100 && bytes[1] >= 64 && bytes[1] <= 127) return true;
        }

        return false;
    }
}
