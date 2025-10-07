using HakwadagAssassinGame.Domain.Aggregates.ApplicationUserAggregate;

namespace HakwadagAssassinGame.Application.Commands.ApplicationUsers;

public sealed class SetDisplayNameCommand : ICommand<Result>
{
    public required string UserId { get; init; }
    public required string DisplayName { get; init; }
}

public sealed class SetDisplayNameCommandValidator : AbstractValidator<SetDisplayNameCommand>
{
    public SetDisplayNameCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.DisplayName)
            .NotEmpty()
            .MinimumLength(5)
            .MaximumLength(15);
    }
}

public sealed class SetDisplayNameCommandHandler : ICommandHandler<SetDisplayNameCommand, Result>
{
    private readonly IApplicationUserRepository _applicationUserRepository;
    private readonly IFusionCache _cache;
    private readonly ILogger<CreateApplicationUserCommandHandler> _logger;

    public SetDisplayNameCommandHandler(IApplicationUserRepository applicationUserRepository, IFusionCache cache,
        ILogger<CreateApplicationUserCommandHandler> logger)
    {
        _applicationUserRepository = applicationUserRepository;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Result> Handle(SetDisplayNameCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var applicationUser = await _applicationUserRepository.GetAsync(command.UserId, cancellationToken);
            if (applicationUser is null) return Result.NotFound("User not found");

            applicationUser.SetDisplayName(command.DisplayName);
            await _applicationUserRepository.SaveChangesAsync(cancellationToken);

            var displayNameCacheKey = $"user:display_name:{command.UserId}";
            await _cache.SetAsync(displayNameCacheKey, applicationUser.DisplayName,
                options => options.SetDuration(TimeSpan.FromHours(12)),
                cancellationToken);

            var userCacheKey = $"user:{command.UserId}";
            await _cache.SetAsync(userCacheKey, applicationUser,
                options => options.SetDuration(TimeSpan.FromHours(12)),
                cancellationToken);

            return Result.NoContent();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "{Message}", e.Message);
            return Result.Error();
        }
    }
}