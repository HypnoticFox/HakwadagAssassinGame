﻿using MediatR;

namespace HakwadagAssassinGame.Application.Mediator;

public interface ICommandHandler<in TRequest, TResponse> : IRequestHandler<TRequest, TResponse>
    where TRequest : ICommand<TResponse>
{
}