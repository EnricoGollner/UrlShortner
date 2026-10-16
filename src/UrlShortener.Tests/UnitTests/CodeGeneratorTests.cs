using UrlShortener.Api.Services;

namespace UrlShortener.Tests.UnitTests;

public class CodeGeneratorTests
{
    private readonly CodeGenerator _generator = new();

    [Fact]
    public void GenerateCode_ShouldReturnStringWithExactLengthOfSeven()
    {
        // Act
        var code = _generator.GenerateCode();

        // Assert
        Assert.NotNull(code);
        Assert.Equal(7, code.Length);
    }

    [Fact]
    public void GenerateCode_ShouldOnlyContainAllowedAlphabetCharacters()
    {
        // Act
        var code = _generator.GenerateCode();

        // Assert
        Assert.All(code, c => Assert.Contains(c, CodeGenerator.AllowedAlphabet));
    }

    [Fact]
    public void GenerateCode_ShouldNeverContainDigitZero()
    {
        // Act & Assert across 1000 samples
        for (var i = 0; i < 1000; i++)
        {
            var code = _generator.GenerateCode();
            Assert.DoesNotContain('0', code);
        }
    }

    [Fact]
    public void GenerateCode_ShouldNeverContainLowercaseLetters()
    {
        // Act & Assert across 1000 samples
        for (var i = 0; i < 1000; i++)
        {
            var code = _generator.GenerateCode();
            Assert.DoesNotContain(code, char.IsLower);
        }
    }

    [Fact]
    public void GenerateCode_ShouldNeverContainWhitespaceOrSpecialCharacters()
    {
        // Act & Assert across 1000 samples
        for (var i = 0; i < 1000; i++)
        {
            var code = _generator.GenerateCode();
            Assert.DoesNotContain(code, char.IsWhiteSpace);
            Assert.DoesNotContain(code, char.IsPunctuation);
            Assert.DoesNotContain(code, char.IsSymbol);
        }
    }

    [Fact]
    public void GenerateCode_ShouldProduceUniqueCodesAcrossMultipleInvocations()
    {
        // Act
        const int count = 500;
        var generatedCodes = new HashSet<string>();

        for (var i = 0; i < count; i++)
        {
            generatedCodes.Add(_generator.GenerateCode());
        }

        // Assert: 7 chars from 35-char alphabet has ~64 billion possibilities, collision in 500 is negligible
        Assert.Equal(count, generatedCodes.Count);
    }
}
