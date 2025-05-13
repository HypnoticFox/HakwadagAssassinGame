namespace HakwadagAssassinGame.Domain.Exceptions;

public class AssassinGameDomainException : DomainException
{
    public AssassinGameDomainException()
    {
    }

    public AssassinGameDomainException(string? message) : base(message)
    {
    }

    public AssassinGameDomainException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}