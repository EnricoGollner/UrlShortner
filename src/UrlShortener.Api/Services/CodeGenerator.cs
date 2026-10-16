using System.Security.Cryptography;

namespace UrlShortener.Api.Services;

public class CodeGenerator : ICodeGenerator
{
    public const int DefaultLength = 7;
    public const string AllowedAlphabet = "123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    public string GenerateCode()
    {
        return RandomNumberGenerator.GetString(AllowedAlphabet, DefaultLength);
    }
}
