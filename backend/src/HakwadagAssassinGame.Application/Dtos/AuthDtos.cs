using HakwadagAssassinGame.Core.Entities;

namespace HakwadagAssassinGame.Application.Dtos;

/// <summary>Requests that an OTP be sent to an email address.</summary>
public record SendOtpRequest(string Email);

/// <summary>Requests verification of an OTP.</summary>
public record VerifyOtpRequest(string Email, string Code);

/// <summary>Contains an authentication token and the authenticated player.</summary>
public record AuthResponse(string Token, PlayerDto Player);

/// <summary>Public player data.</summary>
public record PlayerDto(Guid Id, string Email, string DisplayName, string? AvatarUrl)
{
    /// <summary>Creates a DTO from a player entity.</summary>
    public static PlayerDto FromEntity(Player player) =>
        new(player.Id, player.Email, player.DisplayName, player.AvatarUrl);
}
