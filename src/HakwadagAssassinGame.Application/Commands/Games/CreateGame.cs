using HakwadagAssassinGame.Domain.Aggregates.AssassinGameAggregate;

namespace HakwadagAssassinGame.Application.Commands.Games;

public sealed class CreateGameCommand : ICommand<Result<int>>
{
    public required string HostApplicationUserId { get; init; }
}

public sealed class CreateGameCommandValidator : AbstractValidator<CreateGameCommand>
{
    public CreateGameCommandValidator()
    {
        RuleFor(x => x.HostApplicationUserId).NotEmpty();
    }
}

public sealed class CreateGameCommandHandler : ICommandHandler<CreateGameCommand, Result<int>>
{
    private readonly IAssassinGameRepository _applicationUserRepository;
    private readonly IFusionCache _cache;
    private readonly ILogger<CreateGameCommandHandler> _logger;
    
    public CreateGameCommandHandler(IAssassinGameRepository applicationUserRepository,
        IFusionCache cache,
        ILogger<CreateGameCommandHandler> logger)
    {
        _applicationUserRepository = applicationUserRepository;
        _cache = cache;
        _logger = logger;
    }
    
    public async Task<Result<int>> Handle(CreateGameCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var game = _applicationUserRepository.Add(new Game(request.HostApplicationUserId));
            
            await _applicationUserRepository.SaveChangesAsync(cancellationToken);
            
            return Result.Success(game.Id);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "{Message}", e.Message);
            return Result.Error();
        }
    }
}

