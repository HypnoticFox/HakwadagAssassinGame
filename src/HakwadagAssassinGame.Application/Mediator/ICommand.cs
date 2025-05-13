using MediatR;

namespace HakwadagAssassinGame.Application.Mediator;

public interface ICommand<out TResponse> : IRequest<TResponse>
{
}