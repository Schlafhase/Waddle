namespace Waddle.Config.Exceptions;

public class NoMatchException : Exception
{
public NoMatchException() { }

public NoMatchException(string? message)
    : base(message) { }

public NoMatchException(string? message, Exception? innerException)
    : base(message, innerException) { }
}
