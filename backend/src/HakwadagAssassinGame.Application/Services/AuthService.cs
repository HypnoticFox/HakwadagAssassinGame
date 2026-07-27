using HakwadagAssassinGame.Application.Dtos;
using HakwadagAssassinGame.Application.Exceptions;
using HakwadagAssassinGame.Application.Interfaces;
using HakwadagAssassinGame.Core.Entities;
using HakwadagAssassinGame.Core.Interfaces;

namespace HakwadagAssassinGame.Application.Services;

/// <summary>Provides OTP-based authentication.</summary>
public interface IAuthService
{
    /// <summary>Sends an OTP to an email address.</summary>
    Task SendOtpAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>Verifies an OTP and creates a session token.</summary>
    Task<AuthResponse> VerifyOtpAsync(string email, string code, CancellationToken cancellationToken = default);

    /// <summary>Resolves a session token to a player.</summary>
    Task<PlayerDto?> GetMeAsync(string token, CancellationToken cancellationToken = default);
}

/// <summary>Default authentication orchestration service.</summary>
public sealed class AuthService : IAuthService
{
    private readonly IOtpService otpService;
    private readonly IPlayerRepository playerRepository;
    private readonly ITokenStore tokenStore;

    /// <summary>Initializes the authentication service.</summary>
    public AuthService(IOtpService otpService, IPlayerRepository playerRepository, ITokenStore tokenStore)
    {
        this.otpService = otpService;
        this.playerRepository = playerRepository;
        this.tokenStore = tokenStore;
    }

    /// <inheritdoc />
    public Task SendOtpAsync(string email, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        return otpService.SendOtpAsync(email.Trim(), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AuthResponse> VerifyOtpAsync(string email, string code, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        var normalizedEmail = email.Trim();
        if (!await otpService.VerifyOtpAsync(normalizedEmail, code.Trim(), cancellationToken))
        {
            throw new UnauthorizedException("The OTP is invalid or expired.");
        }

        var player = await playerRepository.GetByEmailAsync(normalizedEmail, cancellationToken);
        if (player is null)
        {
            player = Player.Create(normalizedEmail, normalizedEmail.Split('@')[0]);
            await playerRepository.AddAsync(player, cancellationToken);
        }

        var token = Guid.NewGuid().ToString("N");
        await tokenStore.StoreAsync(token, player.Id, cancellationToken);
        return new AuthResponse(token, PlayerDto.FromEntity(player));
    }

    /// <inheritdoc />
    public async Task<PlayerDto?> GetMeAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var playerId = await tokenStore.GetPlayerIdAsync(token, cancellationToken);
        if (!playerId.HasValue)
        {
            return null;
        }

        var player = await playerRepository.GetByIdAsync(playerId.Value, cancellationToken);
        return player is null ? null : PlayerDto.FromEntity(player);
    }
}
