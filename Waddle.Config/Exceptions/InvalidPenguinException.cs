namespace Waddle.Config.Exceptions;

public class InvalidPenguinException : Exception
{
    public InvalidPenguinException() { }

    public InvalidPenguinException(string? message)
        : base(message) { }

    public InvalidPenguinException(string? message, Exception? innerException)
        : base(message, innerException) { }
}
