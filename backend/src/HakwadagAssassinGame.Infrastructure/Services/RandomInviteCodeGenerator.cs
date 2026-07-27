using System.Security.Cryptography;
using HakwadagAssassinGame.Core.Interfaces;

namespace HakwadagAssassinGame.Infrastructure.Services;

/// <summary>Generates secure, human-readable invite codes.</summary>
public sealed class RandomInviteCodeGenerator : IInviteCodeGenerator
{
    private const string Alphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";

    /// <inheritdoc />
    public string GenerateCode()
    {
        Span<char> code = stackalloc char[6];
        for (var index = 0; index < code.Length; index++)
        {
            code[index] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        }

        return new string(code);
    }
}
