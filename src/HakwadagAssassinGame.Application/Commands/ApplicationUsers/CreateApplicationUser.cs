using HakwadagAssassinGame.Domain.Aggregates.ApplicationUserAggregate;

namespace HakwadagAssassinGame.Application.Commands.ApplicationUsers;

public sealed class CreateApplicationUserCommand : ICommand<Result>
{
    public required string Id { get; init; }

    public required string DisplayName { get; init; }
}

public sealed class CreateApplicationUserCommandValidator : AbstractValidator<CreateApplicationUserCommand>
{
    public CreateApplicationUserCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.DisplayName)
            .NotEmpty()
            .MinimumLength(5)
            .MaximumLength(15);
    }
}

public sealed class CreateApplicationUserCommandHandler : ICommandHandler<CreateApplicationUserCommand, Result>
{
    private readonly IApplicationUserRepository _applicationUserRepository;
    private readonly IFusionCache _cache;
    private readonly ILogger<CreateApplicationUserCommand> _logger;

    public CreateApplicationUserCommandHandler(IApplicationUserRepository applicationUserRepository,
        IFusionCache cache,
        ILogger<CreateApplicationUserCommand> logger)
    {
        _applicationUserRepository = applicationUserRepository;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Result> Handle(CreateApplicationUserCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var cacheKey = $"user:{request.Id}";

            await _cache.GetOrSetAsync<ApplicationUser>(cacheKey,
                async (_, ct) =>
                    await CreateApplicationUserInternalAsync(request.Id, request.DisplayName, ct),
                options => options.SetDuration(TimeSpan.FromHours(12)),
                cancellationToken
            );

            return Result.Success();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "{Message}", e.Message);
            return Result.Error();
        }
    }

    private async Task<ApplicationUser> CreateApplicationUserInternalAsync(string id, string displayName,
        CancellationToken ct)
    {
        var existingUser = await _applicationUserRepository.GetAsync(id, ct);

        if (existingUser is not null) return existingUser;

        var newApplicationUser = _applicationUserRepository.Add(new ApplicationUser(id, displayName));
        await _applicationUserRepository.SaveChangesAsync(ct);

        return newApplicationUser;
    }
}