namespace HakwadagAssassinGame.Core.Interfaces;

/// <summary>Generates invite codes for games.</summary>
public interface IInviteCodeGenerator
{
    /// <summary>Generates a new invite code.</summary>
    string GenerateCode();
}
