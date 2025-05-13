namespace HakwadagAssassinGame.Domain.Exceptions;

public class ApplicationUserDomainException : DomainException
{
    public ApplicationUserDomainException()
    {
    }

    public ApplicationUserDomainException(string? message) : base(message)
    {
    }

    public ApplicationUserDomainException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}