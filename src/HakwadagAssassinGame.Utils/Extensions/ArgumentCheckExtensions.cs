using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace HakwadagAssassinGame.Utils.Extensions;

public static class ArgumentCheckExtensions
{
    public static T ThrowIfNull<T>(this T argument, string? exceptionMessage = null,
        [CallerArgumentExpression("argument")] string? paramName = null)
    {
        ThrowIfNullInternal(argument, paramName, exceptionMessage);

        return argument;
    }

    public static string ThrowIfNullOrEmpty(this string? argument, string? exceptionMessage = null,
        [CallerArgumentExpression("argument")] string? paramName = null)
    {
        ThrowIfNullInternal(argument, paramName, exceptionMessage);

        if (argument == string.Empty)
            throw new ArgumentException(exceptionMessage ?? "Argument cannot be empty.", paramName);
        return argument;
    }

    public static int ThrowIfNegative(this int? argument, string? exceptionMessage = null,
        [CallerArgumentExpression("argument")] string? paramName = null)
    {
        ThrowIfNullInternal(argument, paramName, exceptionMessage);

        IntThrowIfNegativeInternal(argument.Value, paramName, exceptionMessage);

        return argument.Value;
    }

    public static int ThrowIfNegative(this int argument, string? exceptionMessage = null,
        [CallerArgumentExpression("argument")] string? paramName = null)
    {
        IntThrowIfNegativeInternal(argument, paramName, exceptionMessage);

        return argument;
    }

    public static IEnumerable<T> ThrowIfNullOrEmpty<T>(this IEnumerable<T>? argument, string? exceptionMessage = null,
        [CallerArgumentExpression("argument")] string? paramName = null)
    {
        ThrowIfNullInternal(argument, paramName, exceptionMessage);

        if (!argument.Any()) throw new ArgumentException(exceptionMessage ?? "Argument cannot be empty.", paramName);

        return argument;
    }

    public static T ThrowIf<T>(this T argument, Func<T, bool> predicate, string? exceptionMessage = null,
        [CallerArgumentExpression("argument")] string? paramName = null)
    {
        if (predicate(argument)) throw new ArgumentException(exceptionMessage ?? "Argument is not valid.", paramName);

        return argument;
    }

    private static void ThrowIfNullInternal<T>([NotNull] T argument, string? paramName, string? exceptionMessage = null)
    {
        if (argument is null)
            throw new ArgumentNullException(paramName, exceptionMessage ?? "Argument cannot be null.");
    }

    private static void IntThrowIfNegativeInternal(int argument, string? paramName, string? exceptionMessage = null)
    {
        if (argument < 0)
            throw new ArgumentOutOfRangeException(paramName, exceptionMessage ?? "Argument cannot be lower than zero.");
    }
}